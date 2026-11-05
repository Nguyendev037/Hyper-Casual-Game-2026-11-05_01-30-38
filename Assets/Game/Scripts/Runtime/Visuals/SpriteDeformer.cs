using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Draws a billboard sprite as a CPU-deformed grid mesh so characters really move (SPEC-P06):
    /// legs step, chest breathes, hair and hem trail on springs, optional "_step" frame swaps while running.
    /// Lives on the same GameObject as <see cref="SpriteBillboard"/>, which keeps owning rotation/flip/scale.
    /// Any failure (packed sprite, missing material) returns null and the plain SpriteRenderer stays in use.</summary>
    public sealed class SpriteDeformer : MonoBehaviour
    {
        private const string SpritePath = "RHMG/Sprites/";
        private const int MaxCrowdMeshes = 20;
        private const float RankInterval = 0.25f;
        private const float SpringK = 40f, SpringDamp = 4.4f;
        private const float CastSeconds = 0.18f, HitSeconds = 0.12f, DeathSeconds = 0.5f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly Dictionary<string, Sprite> s_StepCache = new Dictionary<string, Sprite>();
        private static readonly List<SpriteDeformer> s_Crowd = new List<SpriteDeformer>();
        private static readonly Stopwatch s_Watch = new Stopwatch();
        private static float s_NextRank;
        private static Vector3 s_Focus;
        private static bool s_HasFocus;
        private static int s_PerfFrames, s_PerfFrameStamp = -1, s_ActiveMeshes;
        private static double s_PerfMs;

        private SpriteRenderer m_Source;
        private MeshFilter m_Filter;
        private MeshRenderer m_MeshRenderer;
        private GameObject m_MeshView;
        private Mesh m_Mesh;
        private MaterialPropertyBlock m_Block;
        private AnimProfile m_Profile;
        private Sprite m_Idle, m_Step;
        private Texture m_ShownTexture;
        private Vector2[] m_Norm;
        private Vector3[] m_Verts;
        private Color32[] m_Colors;
        private Rect m_Bounds;              // sprite local rect (units)
        private bool m_MeshAllowed = true;   // crowd LOD switch
        private float m_Phase, m_StepPhase;
        private float m_HairPos, m_HairVel, m_HemPos, m_HemVel;
        private float m_Cast, m_Hit, m_Death = -1f, m_Windup, m_WindupLength;

        /// <summary>Average CPU ms per frame spent in all deformers over the frames sampled since the last read.</summary>
        public static double ReadAverageMs(out int frames, out int activeMeshes)
        {
            frames = s_PerfFrames;
            activeMeshes = s_ActiveMeshes;
            double avg = frames > 0 ? s_PerfMs / frames : 0d;
            s_PerfMs = 0d;
            s_PerfFrames = 0;
            return avg;
        }

        public bool MeshActive => m_MeshRenderer != null && m_MeshRenderer.enabled;

        public static SpriteDeformer Attach(GameObject host, SpriteRenderer source, Sprite sprite, AnimProfile profile)
        {
            if (host == null || source == null || sprite == null || profile == null) return null;
            Material material = CharacterSprites.SpriteMaterial();
            if (material == null) return null;
            SpriteDeformer d = host.AddComponent<SpriteDeformer>();
            d.m_Source = source;
            d.m_Profile = profile;
            // Unity forbids MeshRenderer and SpriteRenderer on one GameObject.
            d.m_MeshView = new GameObject("DeformedMesh");
            d.m_MeshView.transform.SetParent(host.transform, false);
            d.m_Filter = d.m_MeshView.AddComponent<MeshFilter>();
            d.m_MeshRenderer = d.m_MeshView.AddComponent<MeshRenderer>();
            d.m_MeshRenderer.sharedMaterial = material;
            d.m_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            d.m_MeshRenderer.receiveShadows = false;
            d.m_Block = new MaterialPropertyBlock();
            d.m_Phase = UnityEngine.Random.value * 10f; // desync crowds
            if (!d.SetSprite(sprite))
            {
                Destroy(d.m_MeshView);
                Destroy(d);
                return null;
            }
            if (profile.Crowd)
            {
                s_Crowd.Add(d);
                s_NextRank = 0f;
            }
            return d;
        }

        private void OnEnable()
        {
            if (m_Profile != null && m_Profile.Crowd) s_NextRank = 0f;
        }

        private void OnDisable()
        {
            if (m_Profile != null && m_Profile.Crowd) s_NextRank = 0f;
        }

        /// <summary>Rebuilds the grid for a new image; false when the sprite cannot be drawn as a mesh.</summary>
        public bool SetSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                m_Idle = null;
                m_Step = null;
                if (m_MeshRenderer != null) m_MeshRenderer.enabled = false;
                return true;
            }
            AnimProfile profile = AnimProfiles.ForSpriteName(sprite.name);
            if (profile != null) m_Profile = profile;
            if (!BuildMesh(sprite))
            {
                m_Idle = null;
                if (m_MeshRenderer != null) m_MeshRenderer.enabled = false;
                return false;
            }
            m_Idle = sprite;
            m_Step = LoadStep(sprite.name);
            ShowTexture(sprite.texture);
            return true;
        }

        public void PlayCast() { m_Cast = CastSeconds; }
        public void PlayHit() { m_Hit = HitSeconds; }
        public void PlayDeath() { m_Death = 0f; }
        public void Revive()
        {
            m_Death = -1f;
            m_Cast = m_Hit = m_Windup = 0f;
        }
        public void PlayWindup(float seconds) { m_WindupLength = Mathf.Max(0.05f, seconds); m_Windup = m_WindupLength; }

        /// <summary>Called by SpriteBillboard.LateUpdate before it writes the transform.</summary>
        public void Tick(float dt, float speed01)
        {
            if (m_Idle == null || m_Verts == null) return;
            CountFrame();
            if (m_Profile.Kind == AnimKind.Biped && !m_Profile.Crowd)
            {
                s_Focus = transform.position;
                s_HasFocus = true;
            }
            if (m_Profile.Crowd) RankCrowd();

            bool meshOn = m_MeshAllowed;
            if (m_MeshRenderer.enabled != meshOn) m_MeshRenderer.enabled = meshOn;
            if (m_Source.enabled == meshOn) m_Source.enabled = !meshOn;
            if (!meshOn) return;

            s_Watch.Restart();
            Animate(dt, Mathf.Clamp01(speed01));
            s_Watch.Stop();
            s_PerfMs += s_Watch.Elapsed.TotalMilliseconds;
        }

        private void OnDestroy()
        {
            s_Crowd.Remove(this);
            if (m_Mesh != null) Destroy(m_Mesh);
            if (m_MeshView != null) Destroy(m_MeshView);
        }

        private static void CountFrame()
        {
            if (s_PerfFrameStamp == Time.frameCount) return;
            s_PerfFrameStamp = Time.frameCount;
            s_PerfFrames++;
        }

        private static void RankCrowd()
        {
            if (Time.unscaledTime < s_NextRank) return;
            s_NextRank = Time.unscaledTime + RankInterval;
            s_Crowd.RemoveAll(d => d == null);
            Vector3 focus = s_HasFocus ? s_Focus : Vector3.zero;
            s_Crowd.Sort((a, b) =>
                (a.transform.position - focus).sqrMagnitude.CompareTo((b.transform.position - focus).sqrMagnitude));
            int active = 0;
            for (int i = 0; i < s_Crowd.Count; i++)
            {
                SpriteDeformer d = s_Crowd[i];
                bool alive = d.isActiveAndEnabled && d.m_Idle != null;
                d.m_MeshAllowed = alive && active < MaxCrowdMeshes;
                if (d.m_MeshAllowed) active++;
            }
            s_ActiveMeshes = active;
        }

        private static Sprite LoadStep(string name)
        {
            if (string.IsNullOrEmpty(name) || name.EndsWith("_step")) return null;
            if (s_StepCache.TryGetValue(name, out Sprite cached)) return cached;
            Sprite step = Resources.Load<Sprite>(SpritePath + name + "_step");
            s_StepCache[name] = step;
            return step;
        }

        private bool BuildMesh(Sprite sprite)
        {
            Rect texRect;
            try
            {
                texRect = sprite.textureRect;
            }
            catch (Exception)
            {
                return false; // tightly packed sprite: keep SpriteRenderer
            }
            Texture2D tex = sprite.texture;
            if (tex == null || tex.width <= 0 || tex.height <= 0) return false;

            int cols = Mathf.Max(1, m_Profile.Cols), rows = Mathf.Max(1, m_Profile.Rows);
            int count = (cols + 1) * (rows + 1);
            Bounds b = sprite.bounds;
            m_Bounds = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            m_Norm = new Vector2[count];
            m_Verts = new Vector3[count];
            m_Colors = new Color32[count];
            Vector2[] uv = new Vector2[count];
            int[] tris = new int[cols * rows * 6];
            for (int r = 0, i = 0; r <= rows; r++)
            {
                for (int c = 0; c <= cols; c++, i++)
                {
                    float u = (float)c / cols, v = (float)r / rows;
                    m_Norm[i] = new Vector2(u, v);
                    m_Verts[i] = new Vector3(m_Bounds.x + u * m_Bounds.width, m_Bounds.y + v * m_Bounds.height, 0f);
                    uv[i] = new Vector2((texRect.x + u * texRect.width) / tex.width, (texRect.y + v * texRect.height) / tex.height);
                    m_Colors[i] = new Color32(255, 255, 255, 255);
                }
            }
            for (int r = 0, t = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int a = r * (cols + 1) + c, bIdx = a + 1, d = a + cols + 1, e = d + 1;
                    tris[t++] = a; tris[t++] = d; tris[t++] = bIdx;
                    tris[t++] = bIdx; tris[t++] = d; tris[t++] = e;
                }
            }
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh { name = "SpriteDeformer" };
                m_Mesh.MarkDynamic();
            }
            m_Mesh.Clear();
            m_Mesh.vertices = m_Verts;
            m_Mesh.uv = uv;
            m_Mesh.colors32 = m_Colors;
            m_Mesh.triangles = tris;
            // Generous fixed bounds: deformation never needs a recalculation for culling.
            m_Mesh.bounds = new Bounds(new Vector3(m_Bounds.center.x, m_Bounds.center.y, 0f),
                new Vector3(m_Bounds.width * 1.6f, m_Bounds.height * 1.6f, 0.1f));
            m_Filter.sharedMesh = m_Mesh;
            return true;
        }

        private void ShowTexture(Texture texture)
        {
            if (texture == null || texture == m_ShownTexture) return;
            m_ShownTexture = texture;
            m_MeshRenderer.GetPropertyBlock(m_Block);
            m_Block.SetTexture(MainTexId, texture);
            m_MeshRenderer.SetPropertyBlock(m_Block);
        }

        private void Animate(float dt, float s)
        {
            AnimProfile p = m_Profile;
            m_Phase += dt;
            float twoPi = Mathf.PI * 2f;
            if (s > 0.05f) m_StepPhase += dt * p.StepHz * twoPi * (0.35f + 0.65f * s);
            else m_StepPhase = Mathf.MoveTowards(m_StepPhase, Mathf.Round(m_StepPhase / Mathf.PI) * Mathf.PI, dt * 6f);
            float legSin = Mathf.Sin(m_StepPhase);

            // Secondary motion: hair/hem trail behind (local -x is "behind" when drawn facing right).
            float idleSway = 0.15f * Mathf.Sin(m_Phase * twoPi * 0.4f);
            float hairTarget = -s + idleSway + 0.2f * Mathf.Abs(legSin) * s;
            float hemTarget = -0.8f * s + 0.5f * idleSway;
            Spring(ref m_HairPos, ref m_HairVel, hairTarget, dt);
            Spring(ref m_HemPos, ref m_HemVel, hemTarget, dt);

            // Running frame: passing pose (_step) while the legs cross, contact pose (idle) at full stride.
            if (m_Step != null) ShowTexture(s > 0.3f && Mathf.Abs(legSin) < 0.5f ? m_Step.texture : m_Idle.texture);

            float cast = 0f;
            if (m_Cast > 0f)
            {
                m_Cast -= dt;
                float t = 1f - Mathf.Clamp01(m_Cast / CastSeconds);
                cast = t < 0.45f ? -Mathf.Sin(t / 0.45f * Mathf.PI * 0.5f) : Mathf.Sin((t - 0.45f) / 0.55f * Mathf.PI);
            }
            float hit = 0f;
            if (m_Hit > 0f)
            {
                m_Hit -= dt;
                hit = Mathf.Sin(Mathf.Clamp01(m_Hit / HitSeconds) * Mathf.PI);
            }
            float windup = 0f;
            if (m_Windup > 0f)
            {
                m_Windup -= dt;
                windup = Mathf.Sin(Mathf.Clamp01(1f - m_Windup / m_WindupLength) * Mathf.PI * 0.5f);
                if (m_Windup <= 0f) m_Cast = CastSeconds; // release = strike
            }
            float death = 0f;
            if (m_Death >= 0f)
            {
                m_Death = Mathf.Min(DeathSeconds, m_Death + dt);
                death = m_Death / DeathSeconds;
            }

            float breath = Mathf.Sin(m_Phase * twoPi / 2.4f);
            float W = m_Bounds.width, H = m_Bounds.height;
            Color sc = m_Source.color;
            byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(sc.a * (1f - death) * 255f), 0, 255);
            Color32 tint = new Color32((byte)(sc.r * 255f), (byte)(sc.g * 255f), (byte)(sc.b * 255f), alpha);

            for (int i = 0; i < m_Verts.Length; i++)
            {
                float u = m_Norm[i].x, v = m_Norm[i].y;
                float dx = 0f, dy = 0f;
                switch (p.Kind)
                {
                    case AnimKind.Biped:
                    case AnimKind.Heavy:
                        Biped(p, u, v, s, legSin, breath, W, H, ref dx, ref dy);
                        if (v > p.HipY) dx += (cast * 0.08f) * H * (v - p.HipY);
                        if (windup > 0f)
                        {
                            dx += (u - 0.5f) * W * 0.08f * windup - 0.06f * H * (v - p.HipY) * windup * (v > p.HipY ? 1f : 0f);
                            dy += v * H * 0.08f * windup;
                        }
                        break;
                    case AnimKind.Quadruped:
                        Quadruped(p, u, v, s, breath, W, H, ref dx, ref dy);
                        break;
                    case AnimKind.Floating:
                        Floating(p, u, v, s, W, H, cast, ref dx, ref dy);
                        break;
                    case AnimKind.Crawler:
                        if (v < p.LegTop)
                            dx += Mathf.Sin(m_Phase * twoPi * p.StepHz + u * 12f) * p.Stride * H * (p.LegTop - v) / p.LegTop * (0.3f + 0.7f * s);
                        else dy += 0.01f * H * Mathf.Sin(m_Phase * twoPi * p.StepHz * 0.5f);
                        break;
                    case AnimKind.Eye:
                    {
                        float ex = u - 0.5f, ey = v - 0.6f;
                        float r = Mathf.Sqrt(ex * ex + ey * ey);
                        if (r < 0.3f)
                        {
                            float k = (p.PulseAmp * Mathf.Sin(m_Phase * twoPi * 0.8f) + 0.15f * windup) * (1f - r / 0.3f);
                            dx += ex * W * k;
                            dy += ey * H * k;
                        }
                        break;
                    }
                }
                if (hit > 0f)
                {
                    dx += (u - 0.5f) * W * 0.10f * hit;
                    dy -= v * H * 0.10f * hit;
                }
                if (death > 0f)
                {
                    dx -= v * H * 0.6f * death;
                    dy -= v * H * 0.35f * death;
                }
                m_Verts[i].x = m_Bounds.x + u * W + dx;
                m_Verts[i].y = m_Bounds.y + v * H + dy;
                m_Colors[i] = tint;
            }
            m_Mesh.vertices = m_Verts;
            m_Mesh.colors32 = m_Colors;
            if (m_MeshRenderer.sortingOrder != m_Source.sortingOrder) m_MeshRenderer.sortingOrder = m_Source.sortingOrder;
        }

        private void Biped(AnimProfile p, float u, float v, float s, float legSin, float breath, float W, float H,
            ref float dx, ref float dy)
        {
            if (v < p.LegTop)
            {
                float w = (p.LegTop - v) / p.LegTop;
                float side = (1f - SmoothStep(0.38f, 0.62f, u)) * 2f - 1f; // +1 left leg, -1 right leg
                dx += p.Stride * H * legSin * side * w * s;
                dy += p.LegLift * H * Mathf.Max(0f, legSin * side) * w * s;
            }
            float span = Mathf.Max(0.01f, p.HeadY - p.HipY);
            float bell = Mathf.Clamp01(1f - Mathf.Abs(v - p.ChestY) / span);
            float rest = 1f - s;
            dx += (u - 0.5f) * W * p.BreathAmp * breath * bell * rest;
            dy += p.BreathAmp * H * 0.5f * breath * SmoothStep(p.HipY, p.HeadY, v) * rest;

            float hair = Mathf.Clamp01((p.HairMaxX - u) / p.HairMaxX) * Bump(v, p.HairFromY, p.HairToY);
            dx += m_HairPos * p.HairSway * H * hair;

            if (p.HemSway > 0f && v > p.HemFrom - 0.06f && v < p.HemTo)
            {
                float hw = v >= p.HemFrom ? 1f - (v - p.HemFrom) / (p.HemTo - p.HemFrom) : 1f - (p.HemFrom - v) / 0.06f;
                dx += m_HemPos * p.HemSway * H * hw + (u - 0.5f) * W * 0.04f * Mathf.Abs(legSin) * s * hw;
            }
        }

        private void Quadruped(AnimProfile p, float u, float v, float s, float breath, float W, float H,
            ref float dx, ref float dy)
        {
            if (v < p.LegTop)
            {
                float w = (p.LegTop - v) / p.LegTop;
                float front = SmoothStep(0.45f, 0.65f, u);
                float ls = Mathf.Lerp(Mathf.Sin(m_StepPhase + Mathf.PI), Mathf.Sin(m_StepPhase), front);
                dx += p.Stride * H * ls * w * s;
                dy += p.LegLift * H * Mathf.Max(0f, ls) * w * s;
            }
            dy += 0.015f * H * Mathf.Sin(2f * m_StepPhase) * s * SmoothStep(p.LegTop, 0.5f, v);
            float tail = Mathf.Clamp01((p.TailMaxX - u) / p.TailMaxX) * Bump(v, p.TailFromY, p.TailToY);
            float wag = Mathf.Sin(m_Phase * Mathf.PI * 2f * (3f + 5f * s));
            dy += p.TailWag * H * wag * tail;
            dx += p.TailWag * 0.5f * H * wag * tail;
            if (u > 0.55f) dx += m_HairPos * p.HairSway * H * SmoothStep(p.HeadY, 1f, v);
            float span = Mathf.Max(0.01f, p.HeadY - p.HipY);
            dx += (u - 0.5f) * W * p.BreathAmp * breath * Mathf.Clamp01(1f - Mathf.Abs(v - p.ChestY) / span) * (1f - s);
        }

        private void Floating(AnimProfile p, float u, float v, float s, float W, float H, float cast,
            ref float dx, ref float dy)
        {
            float t = m_Phase * Mathf.PI * 2f * 1.2f;
            dy += p.FloatAmp * H * 0.5f * Mathf.Sin(t);
            float k = p.PulseAmp * Mathf.Sin(t + 1f) + 0.25f * Mathf.Max(0f, cast);
            dx += (u - 0.5f) * W * k;
            dy += (v - 0.5f) * H * (cast > 0f ? k : -k * 0.6f);
            dx += (v - 0.5f) * H * 0.15f * s;
            if (v < 0.4f) dx += m_HairPos * p.HairSway * H * (0.4f - v) / 0.4f;
        }

        private static void Spring(ref float pos, ref float vel, float target, float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            vel += (SpringK * (target - pos) - SpringDamp * vel) * dt;
            pos += vel * dt;
        }

        private static float SmoothStep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Bump(float x, float from, float to)
        {
            if (x <= from || x >= to) return 0f;
            return Mathf.Sin((x - from) / (to - from) * Mathf.PI);
        }
    }
}
