using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Camera-facing sprite for a character. Facing is shown ONLY by flipping X (S3); the sign comes from the
    /// parent's forward (S4, the parent already yaws via Facing). All motion is code: breathe, bob, squash, lean, punch.</summary>
    public sealed class SpriteBillboard : MonoBehaviour
    {
        private const float FlipSeconds = 0.12f;
        private const float FlipDeadZone = 0.15f;
        private const float BreathAmp = 0.02f, BreathPeriod = 2.4f;
        private const float BobMeters = 0.06f, BobHz = 9f, SquashAmp = 0.05f, LeanDegrees = 6f;
        private const float PunchSeconds = 0.15f;

        private static Camera s_Camera;
        private static int s_CameraFrame = -1;
        private static readonly Camera[] s_CameraBuffer = new Camera[16];

        private SpriteRenderer m_Renderer;
        private Transform m_Parent;
        private float m_BaseScale;      // local uniform scale that gives the requested world height at creation
        private float m_ParentScaleY;   // parent lossy Y at creation: later parent scale changes still show (lion pop)
        private float m_Speed, m_SpeedTarget;
        private float m_FlipX = 1f;     // animated sign: +1 facing right, -1 left, 0 mid turn
        private float m_TargetSign = 1f;
        private float m_Punch;
        private float m_Pop;
        private const float PopSeconds = 0.2f;
        private float m_FlashTimer;
        private float m_LungeTimer, m_LungeDuration, m_LungeDistance;
        private float m_Phase;
        private Sprite m_Sprite;

        public bool FacingRight => m_TargetSign > 0f;

        public static SpriteBillboard Create(Transform parent, Sprite sprite, float worldHeight, string name = "Billboard")
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            SpriteBillboard b = go.AddComponent<SpriteBillboard>();
            b.m_Parent = parent;
            b.m_ParentScaleY = parent != null ? Mathf.Max(0.0001f, Mathf.Abs(parent.lossyScale.y)) : 1f;
            b.m_Renderer = go.AddComponent<SpriteRenderer>();
            b.m_Renderer.sortingOrder = 0;
            b.m_Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.m_Renderer.receiveShadows = false;
            Material material = CharacterSprites.SpriteMaterial();
            if (material != null) b.m_Renderer.sharedMaterial = material;
            b.SetSprite(sprite, worldHeight);
            b.m_TargetSign = b.ReadParentSign(1f);
            b.m_FlipX = b.m_TargetSign;
            return b;
        }

        /// <summary>Swaps the image; the flip state is kept.</summary>
        public void SetSprite(Sprite sprite, float worldHeight)
        {
            m_Sprite = sprite;
            m_Renderer.sprite = sprite;
            m_Renderer.enabled = sprite != null;
            float h = sprite != null ? sprite.bounds.size.y : 1f;
            m_BaseScale = h > 0.0001f ? worldHeight / h / m_ParentScaleY : 1f;
        }

        public void SetSortingOrder(int order)
        {
            m_Renderer.sortingOrder = order;
        }

        /// <summary>0 = standing (breathe), 1 = running (bob, squash, lean).</summary>
        public void SetMotion(float speed01)
        {
            m_SpeedTarget = Mathf.Clamp01(speed01);
        }

        public void PunchCast()
        {
            m_Punch = PunchSeconds;
        }

        /// <summary>Uniform 1 -> 1.15 -> 1 in 0.2 s (outfit change), same feel as the primitive slot pop.</summary>
        public void Pop()
        {
            m_Pop = PopSeconds;
        }

        /// <summary>Dash along the parent's flat forward and back.</summary>
        public void Lunge(float distance, float seconds)
        {
            m_LungeDistance = distance;
            m_LungeDuration = Mathf.Max(0.01f, seconds);
            m_LungeTimer = m_LungeDuration;
        }

        public void Flash(Color color, float seconds)
        {
            m_Renderer.color = color;
            m_FlashTimer = Mathf.Max(0.01f, seconds);
        }

        /// <summary>Point on the image (0..1, left-bottom origin, as drawn facing right) in world space; mirrors when flipped.</summary>
        public Vector3 AnchorWorld(Vector2 normalized)
        {
            if (m_Sprite == null) return transform.position;
            Bounds b = m_Sprite.bounds;
            float s = Mathf.Abs(transform.lossyScale.y);
            float x = (b.min.x + normalized.x * b.size.x) * (FacingRight ? 1f : -1f) * s;
            float y = (b.min.y + normalized.y * b.size.y) * s;
            return transform.position + transform.rotation * new Vector3(x, y, 0f);
        }

        private float ReadParentSign(float current)
        {
            if (m_Parent == null) return current;
            float fx = m_Parent.forward.x;
            if (Mathf.Abs(fx) < FlipDeadZone) return current;
            return fx > 0f ? 1f : -1f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            m_TargetSign = ReadParentSign(m_TargetSign);
            m_FlipX = Mathf.MoveTowards(m_FlipX, m_TargetSign, 2f / FlipSeconds * dt);
            m_Speed = Mathf.MoveTowards(m_Speed, m_SpeedTarget, 6f * dt);
            m_Phase += dt;

            float breath = 1f + Mathf.Sin(m_Phase * Mathf.PI * 2f / BreathPeriod) * BreathAmp * (1f - m_Speed);
            float runPhase = m_Phase * Mathf.PI * BobHz;
            float bob = Mathf.Abs(Mathf.Sin(runPhase)) * BobMeters * m_Speed;
            float squash = Mathf.Cos(runPhase * 2f) * SquashAmp * m_Speed;
            float scaleY = breath * (1f + squash);
            float scaleX = 1f - squash;

            if (m_Pop > 0f)
            {
                m_Pop -= dt;
                float pop = 1f + 0.15f * Mathf.Sin(Mathf.Clamp01(m_Pop / PopSeconds) * Mathf.PI);
                scaleX *= pop;
                scaleY *= pop;
            }
            if (m_Punch > 0f)
            {
                m_Punch -= dt;
                float k = Mathf.Sin(Mathf.Clamp01(m_Punch / PunchSeconds) * Mathf.PI);
                scaleY *= 1f + 0.12f * k;
                scaleX *= 1f - 0.08f * k;
            }

            float lunge = 0f;
            if (m_LungeTimer > 0f)
            {
                m_LungeTimer -= dt;
                lunge = Mathf.Sin(Mathf.Clamp01(m_LungeTimer / m_LungeDuration) * Mathf.PI) * m_LungeDistance;
            }

            if (m_FlashTimer > 0f)
            {
                m_FlashTimer -= dt;
                if (m_FlashTimer <= 0f) m_Renderer.color = Color.white;
            }

            // Lean toward the run direction: negative Z roll tilts the head to screen-right.
            float roll = -m_TargetSign * LeanDegrees * m_Speed;
            Camera cam = ActiveCamera();
            if (cam != null) transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, roll);

            Vector3 basePos = m_Parent != null ? m_Parent.position : transform.position;
            Vector3 flat = m_Parent != null ? m_Parent.forward : Vector3.zero;
            flat.y = 0f;
            flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.zero;
            transform.position = basePos + Vector3.up * bob + flat * lunge;
            transform.localScale = new Vector3(m_BaseScale * scaleX * m_FlipX, m_BaseScale * scaleY, m_BaseScale);
        }

        // Main camera if tagged, otherwise the enabled camera with the highest depth; cached once per frame.
        private static Camera ActiveCamera()
        {
            if (s_CameraFrame == Time.frameCount && s_Camera != null) return s_Camera;
            s_CameraFrame = Time.frameCount;
            Camera main = Camera.main;
            if (main != null)
            {
                s_Camera = main;
                return s_Camera;
            }
            int count = Camera.GetAllCameras(s_CameraBuffer);
            Camera best = null;
            for (int i = 0; i < count; i++)
            {
                Camera c = s_CameraBuffer[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (best == null || c.depth > best.depth) best = c;
            }
            s_Camera = best;
            return s_Camera;
        }
    }
}
