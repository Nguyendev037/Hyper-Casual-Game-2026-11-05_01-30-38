using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Pooled one-shot effects of SPEC-P03: muzzle flash, 6-frame impact, expanding ring, crit star.
    /// One instance per combat world; every effect is a pooled quad, nothing is instantiated per shot.</summary>
    public sealed partial class VfxHub : MonoBehaviour
    {
        public const int QuadBudget = 40;
        public const float MuzzleSeconds = 0.08f;
        public const float ImpactSeconds = 0.18f;
        public const float RingSeconds = 0.35f;
        public const float CritSeconds = 0.4f;

        private enum Kind
        {
            Muzzle,
            Impact,
            Ring,
            Crit
        }

        private struct Fx
        {
            public VfxSprite Sprite;
            public Kind Kind;
            public float Time;
            public float Duration;
            public float Size;
            public Color Color;
        }

        public static VfxHub Instance { get; private set; }

        private readonly List<Fx> active = new List<Fx>(QuadBudget);
        private VfxPool quads;
        private Camera cam;

        /// <summary>Number of effect quads currently playing.</summary>
        public int ActiveCount => active.Count;

        public static VfxHub Ensure(Transform parent)
        {
            if (Instance != null && Instance.transform.parent == parent)
            {
                return Instance;
            }

            GameObject go = new GameObject("VfxHub");
            go.transform.SetParent(parent, false);
            VfxHub hub = go.AddComponent<VfxHub>();
            hub.quads = new VfxPool(go.transform, "Fx", QuadBudget);
            hub.quads.Prewarm(QuadBudget);
            hub.BuildSparks();
            Instance = hub;
            return hub;
        }

        public static void Muzzle(Vector3 position, Color color)
        {
            if (Instance != null)
            {
                Instance.Begin(Kind.Muzzle, "vfx_muzzle", position, color, 0.75f, MuzzleSeconds, true);
            }
        }

        public static void Impact(Vector3 position, Color color, float size = 1f)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.Begin(Kind.Impact, VfxAssets.ImpactId(0), position, color, 0.9f * size, ImpactSeconds, true);
            Instance.EmitSparks(position, color, Random.Range(4, 7), 2.8f);
        }

        /// <summary>Ground ring that grows to the given world radius (area blasts, spirit skills, level up).</summary>
        public static void Ring(Vector3 position, Color color, float radius)
        {
            if (Instance != null)
            {
                Instance.Begin(Kind.Ring, "vfx_ring", position + Vector3.up * 0.06f, color,
                    Mathf.Max(0.5f, radius * 2f), RingSeconds, true);
            }
        }

        public static void Crit(Vector3 position)
        {
            if (Instance != null)
            {
                Instance.Begin(Kind.Crit, "vfx_crit", position, Palette.LightGold, 1.1f, CritSeconds, true);
            }
        }

        private void Begin(Kind kind, string textureId, Vector3 position, Color color, float size, float duration, bool glow)
        {
            VfxSprite sprite = quads.Get();
            if (sprite == null)
            {
                return;
            }

            sprite.SetMaterial(glow);
            sprite.SetTexture(VfxAssets.Texture(textureId));
            sprite.SetColor(color);
            sprite.SetSortingOrder(20);
            sprite.transform.position = position;
            sprite.SetSize(size);
            if (kind == Kind.Ring)
            {
                sprite.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                sprite.Face(CurrentCamera());
            }

            active.Add(new Fx
            {
                Sprite = sprite, Kind = kind, Time = 0f, Duration = duration, Size = size, Color = color
            });
        }

        private Camera CurrentCamera()
        {
            if (cam == null || !cam.isActiveAndEnabled)
            {
                cam = Camera.main;
            }

            return cam;
        }

        private void Update()
        {
            TickHitStop();
            Camera camera = CurrentCamera();
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Fx fx = active[i];
                fx.Time += dt;
                if (fx.Time >= fx.Duration)
                {
                    quads.Release(fx.Sprite);
                    active.RemoveAt(i);
                    continue;
                }

                Animate(ref fx, camera);
                active[i] = fx;
            }
        }

        private static void Animate(ref Fx fx, Camera camera)
        {
            float t = fx.Time / fx.Duration;
            Color color = fx.Color;
            switch (fx.Kind)
            {
                case Kind.Muzzle:
                    fx.Sprite.SetSize(fx.Size * Mathf.Lerp(1f, 0.5f, t));
                    color.a = 1f - t;
                    break;
                case Kind.Impact:
                    int frame = Mathf.Min(5, Mathf.FloorToInt(t * 6f));
                    fx.Sprite.SetTexture(VfxAssets.Texture(VfxAssets.ImpactId(frame)));
                    fx.Sprite.SetSize(fx.Size * Mathf.Lerp(0.8f, 1.25f, t));
                    color.a = 1f - t * t;
                    break;
                case Kind.Ring:
                    fx.Sprite.SetSize(fx.Size * Mathf.Lerp(0.3f, 1f, 1f - (1f - t) * (1f - t)));
                    color.a = 1f - t;
                    break;
                default:
                    float pop = t < 0.2f ? Mathf.Lerp(0.4f, 1.2f, t / 0.2f) : Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.8f);
                    fx.Sprite.SetSize(fx.Size * pop);
                    color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                    break;
            }

            if (fx.Kind != Kind.Ring)
            {
                fx.Sprite.Face(camera);
            }

            fx.Sprite.SetColor(color);
        }

        // Review G8 #4: a deactivated hub stops ticking, so restore the time scale here too (never stuck at 0.05).
        private void OnDisable()
        {
            RestoreHitStop();
        }

        private void OnDestroy()
        {
            RestoreHitStop();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
