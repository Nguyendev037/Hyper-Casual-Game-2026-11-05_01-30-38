using System.Collections;
using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Companion dog rig with 4 growth tiers, Day/Night lion forms, and bite action.</summary>
    public sealed class DogRig : MonoBehaviour
    {
        public Transform Seat;
        public Transform Mouth;

        [Header("Rig Parts")]
        public Transform Visual;
        public Transform BlobShadow;
        public Transform CollarRenderer;
        public Transform SandRing;
        public Transform ManeRoot;
        public Transform SunSymbol;
        public Transform MoonSymbol;
        public Transform Tail;
        public Transform LegFL, LegFR, LegBL, LegBR;

        private DogTier m_CurrentTier = DogTier.Companion;
        private DayPhase m_CurrentPhase = DayPhase.Day;
        private float m_Speed01;
        private float m_BiteTimer;
        private Coroutine m_PopRoutine;

        // 2D sprite mode: primitives stay built but hidden; null billboard = primitive fallback (S1).
        // SpriteRoot carries the lion pop scale so the billboard (child of the yawed root, S4) pops too.
        private Transform m_SpriteRoot;
        private SpriteBillboard m_Billboard;

        private const float BaseVisualScale = 1.4f;
        private const float LionScaleFactor = 1.9f;
        private const float BaseShadowRadius = 0.55f;

        private void Awake()
        {
            if (Mouth == null)
            {
                var mouth = transform.Find("Visual/Mouth");
                if (mouth != null) Mouth = mouth;
            }
            if (Seat == null)
            {
                var seat = transform.Find("Seat");
                if (seat != null) Seat = seat;
            }
            m_Facing.Face(Vector3.back); // face the camera until movement sets a direction
        }

        public void SetTier(DogTier tier)
        {
            m_CurrentTier = tier;
            UpdateTierVisuals();
            RefreshSprite();
        }

        public void SetLionForm(DayPhase p)
        {
            m_CurrentPhase = p;
            UpdateLionFormVisuals();
            RefreshSprite();

            if (m_CurrentTier == DogTier.SunMoonLion && gameObject.activeInHierarchy)
            {
                if (m_PopRoutine != null) StopCoroutine(m_PopRoutine);
                m_PopRoutine = StartCoroutine(LionTransitionPop());
            }
        }

        public void SetMoving(float speed01)
        {
            m_Speed01 = Mathf.Clamp01(speed01);
            if (m_Billboard != null) m_Billboard.SetMotion(m_Speed01);
        }

        public void PlayBite()
        {
            m_BiteTimer = 0.20f;
            if (m_Billboard != null) m_Billboard.Lunge(0.35f, 0.20f);
        }

        /// <summary>World-space direction the dog turns toward (smoothly, whole rig incl. lunge).</summary>
        public void SetFacing(Vector3 direction)
        {
            m_Facing.Face(direction);
        }

        private Facing m_Facing;

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateAnimation(dt);

            if (SandRing != null && SandRing.gameObject.activeSelf)
            {
                SandRing.Rotate(Vector3.up, 120f * dt, Space.Self);
            }
        }

        private void UpdateAnimation(float dt)
        {
            if (Visual == null) return;

            // Trot bobbing & tail wag
            float bobFreq = m_Speed01 > 0.05f ? 14f : 3f;
            float bobAmp = m_Speed01 > 0.05f ? 0.06f : 0.015f;
            float bobY = Mathf.Sin(Time.time * bobFreq) * bobAmp;

            // Tail wagging
            if (Tail != null)
            {
                float wagFreq = m_Speed01 > 0.05f ? 16f : 4f;
                Tail.localRotation = Quaternion.Euler(20f, Mathf.Sin(Time.time * wagFreq) * 25f, 0f);
            }

            // Leg swing
            if (m_Speed01 > 0.05f)
            {
                float legAngle = Mathf.Sin(Time.time * 14f) * 28f * m_Speed01;
                if (LegFL != null) LegFL.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                if (LegFR != null) LegFR.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
                if (LegBL != null) LegBL.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
                if (LegBR != null) LegBR.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
            }

            // Bite lunge
            float lungeZ = 0f;
            if (m_BiteTimer > 0f)
            {
                m_BiteTimer -= dt;
                float t01 = Mathf.Clamp01(m_BiteTimer / 0.20f);
                lungeZ = Mathf.Sin(t01 * Mathf.PI) * 0.25f;
            }

            Visual.localPosition = new Vector3(0f, bobY, lungeZ);
            m_Facing.Step(dt, 540f);
            transform.rotation = m_Facing.Rotation;
        }

        /// <summary>Shows the 2D sprite for the current tier/phase, or falls back to the primitive dog when it is missing.</summary>
        private void RefreshSprite()
        {
            Sprite sprite = CharacterSprites.Dog(m_CurrentTier, m_CurrentPhase);
            if (sprite != null)
            {
                float height = CharacterSprites.DogHeight(m_CurrentTier);
                if (m_Billboard == null)
                {
                    m_SpriteRoot = new GameObject("SpriteRoot").transform;
                    m_SpriteRoot.SetParent(transform, false);
                    m_Billboard = SpriteBillboard.Create(m_SpriteRoot, sprite, height);
                    m_Billboard.SetMotion(m_Speed01);
                }
                else
                {
                    m_Billboard.SetSprite(sprite, height);
                }
                m_SpriteRoot.localScale = Vector3.one;
                CharacterSprites.SetPrimitivesVisible(transform, BlobShadow, false);
            }
            else if (m_Billboard != null)
            {
                if (Application.isPlaying) Destroy(m_SpriteRoot.gameObject);
                else DestroyImmediate(m_SpriteRoot.gameObject);
                m_SpriteRoot = null;
                m_Billboard = null;
                CharacterSprites.SetPrimitivesVisible(transform, BlobShadow, true);
            }
        }

        private void UpdateTierVisuals()
        {
            // Visual-only scale: root stays at 1 so the collision root is untouched.
            bool isLion = m_CurrentTier == DogTier.SunMoonLion;
            ApplyVisualScale(isLion ? BaseVisualScale * LionScaleFactor : BaseVisualScale);

            // Collar styling
            if (CollarRenderer != null)
            {
                var r = CollarRenderer.GetComponent<Renderer>();
                if (r != null)
                {
                    if (m_CurrentTier == DogTier.Companion)
                        r.sharedMaterial = MaterialLibrary.Lit(Palette.Wood);
                    else
                        r.sharedMaterial = MaterialLibrary.Unlit(Palette.FireOrange);
                }
            }

            // Sand ring at feet for SpiritHound & above
            if (SandRing != null)
            {
                SandRing.gameObject.SetActive(m_CurrentTier == DogTier.SpiritHound);
            }

            // Mane for SunMoonLion
            if (ManeRoot != null)
            {
                ManeRoot.gameObject.SetActive(m_CurrentTier == DogTier.SunMoonLion);
            }

            UpdateLionFormVisuals();
        }

        private void UpdateLionFormVisuals()
        {
            if (ManeRoot == null) return;

            bool isDay = m_CurrentPhase == DayPhase.Day;
            Color maneColor = isDay ? Palette.LightGold : Palette.Silver;

            var maneRenderers = ManeRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var mr in maneRenderers)
            {
                if (mr.transform == SunSymbol || mr.transform == MoonSymbol) continue;
                mr.sharedMaterial = MaterialLibrary.Lit(maneColor);
            }

            bool showPhaseSymbols = m_CurrentTier == DogTier.SunMoonLion;
            if (SunSymbol != null) SunSymbol.gameObject.SetActive(showPhaseSymbols && isDay);
            if (MoonSymbol != null) MoonSymbol.gameObject.SetActive(showPhaseSymbols && !isDay);
        }

        private IEnumerator LionTransitionPop()
        {
            float elapsed = 0f;
            const float duration = 0.40f;
            float baseScale = BaseVisualScale * LionScaleFactor;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                ApplyVisualScale(baseScale * pop);
                yield return null;
            }
            ApplyVisualScale(baseScale);
        }

        /// <summary>Scales only the visual hierarchy and keeps the mount seat on the visual's back.</summary>
        private void ApplyVisualScale(float scale)
        {
            if (Visual != null) Visual.localScale = Vector3.one * scale;
            if (m_SpriteRoot != null)
            {
                bool lion = m_CurrentTier == DogTier.SunMoonLion;
                m_SpriteRoot.localScale = Vector3.one * (scale / (lion ? BaseVisualScale * LionScaleFactor : BaseVisualScale));
            }
            if (Seat != null) Seat.localPosition = new Vector3(0f, 0.60f * scale, -0.05f * scale);
            if (BlobShadow != null)
            {
                float radius = BaseShadowRadius * (scale / BaseVisualScale);
                BlobShadow.localScale = new Vector3(radius, 1f, radius);
            }
        }
    }
}
