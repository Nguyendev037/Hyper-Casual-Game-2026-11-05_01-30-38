using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Rig controller for the chibi magic girl with code-based movement, cast recoil, and outfit slots.</summary>
    public sealed class GirlRig : MonoBehaviour
    {
        public Transform HeadSlot;
        public Transform BodySlot;
        public Transform StaffSlot;
        public Transform StaffTip;
        public Transform Visual;
        public Transform BlobShadow;

        private const float TurnSpeed = 720f;
        private float m_Speed01;
        private float m_Bank;
        private Facing m_Facing;
        private float m_CastTimer;
        private float m_FlashTimer;
        private Color m_FlashColor;
        private MaterialPropertyBlock m_Mpb;
        private readonly List<Renderer> m_CachedRenderers = new List<Renderer>();

        // 2D sprite mode: the primitive rig stays built (slots, StaffTip) but hidden; null billboard = primitive fallback (S1).
        private static readonly Vector2 StaffTipAnchor = new Vector2(0.80f, 0.78f);
        private const float MountedHeightFactor = 0.8f;
        private SpriteBillboard m_Billboard;
        private string m_HeadId, m_BodyId, m_StaffId;
        private bool m_Mounted;

        private readonly Dictionary<OutfitSlot, Coroutine> m_PopRoutines = new Dictionary<OutfitSlot, Coroutine>();

        private void Awake()
        {
            m_Mpb = new MaterialPropertyBlock();
            CacheRenderers();
            m_Facing.Face(Vector3.back); // face the camera until something sets a direction (showcase, gallery)
        }

        public void ApplyOutfit(OutfitSlot slot, string appearanceId)
        {
            Transform targetSlot = GetSlot(slot);
            if (targetSlot == null) return;

            // Destroy old children
            for (int i = targetSlot.childCount - 1; i >= 0; i--)
            {
                var child = targetSlot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            bool built = false;
            switch (slot)
            {
                case OutfitSlot.Head:
                    built = CostumeBuilder.BuildHead(targetSlot, appearanceId);
                    break;
                case OutfitSlot.Body:
                    built = CostumeBuilder.BuildBody(targetSlot, appearanceId);
                    break;
                case OutfitSlot.Staff:
                    built = CostumeBuilder.BuildStaff(targetSlot, appearanceId, out var newTip);
                    if (newTip != null) StaffTip = newTip;
                    break;
            }

            if (!built && !string.IsNullOrEmpty(appearanceId))
            {
                Debug.LogWarning($"[GirlRig] Unknown appearanceId for slot {slot}: {appearanceId}");
            }

            CacheRenderers();
            switch (slot)
            {
                case OutfitSlot.Head: m_HeadId = appearanceId; break;
                case OutfitSlot.Body: m_BodyId = appearanceId; break;
                case OutfitSlot.Staff: m_StaffId = appearanceId; break;
            }
            RefreshSprite();
            // The slot pop below only scales hidden primitives when a billboard is shown: pop the sprite too.
            if (m_Billboard != null) m_Billboard.Pop();

            // Pop scale 1 -> 1.15 -> 1 in 0.2 s
            if (m_PopRoutines.TryGetValue(slot, out var existing) && existing != null)
            {
                StopCoroutine(existing);
            }
            if (gameObject.activeInHierarchy)
            {
                m_PopRoutines[slot] = StartCoroutine(PopScaleRoutine(targetSlot));
            }
            else
            {
                targetSlot.localScale = Vector3.one;
            }
        }

        public void SetMoving(float speed01)
        {
            m_Speed01 = Mathf.Clamp01(speed01);
            if (m_Billboard != null) m_Billboard.SetMotion(m_Speed01);
        }

        /// <summary>World-space direction the girl should turn to (movement, or target when standing).</summary>
        public void SetFacing(Vector3 direction)
        {
            m_Facing.Face(direction);
        }

        /// <summary>Flat forward the girl is currently facing (smoothed).</summary>
        public Vector3 FacingForward => m_Facing.Forward;

        public void PlayCast()
        {
            m_CastTimer = 0.25f;
            if (m_Billboard != null) m_Billboard.PunchCast();
        }

        public void Flash(Color c, float t)
        {
            m_FlashColor = c;
            m_FlashTimer = Mathf.Max(0.01f, t);
            ApplyFlashTint(true);
            if (m_Billboard != null) m_Billboard.Flash(c, m_FlashTimer);
        }

        public void SetMounted(Transform seat)
        {
            m_Mounted = seat != null;
            if (seat != null)
            {
                transform.SetParent(seat, false);
                transform.localPosition = new Vector3(0f, 0.15f, 0f);
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                transform.SetParent(null, true);
                Vector3 pos = transform.position;
                pos.y = 0f;
                transform.position = pos;
            }

            if (BlobShadow != null)
            {
                BlobShadow.gameObject.SetActive(seat == null);
            }

            RefreshSprite();
        }

        private void LateUpdate()
        {
            // Projectiles spawn at StaffTip; keep it on the staff of the drawn image.
            if (m_Billboard != null && StaffTip != null)
            {
                StaffTip.position = m_Billboard.AnchorWorld(StaffTipAnchor);
            }
        }

        /// <summary>Shows the 2D sprite for the current outfit, or falls back to the primitive rig when it is missing.</summary>
        private void RefreshSprite()
        {
            if (Visual == null) return;
            Sprite sprite = CharacterSprites.Girl(m_HeadId, m_BodyId, m_StaffId);
            if (sprite != null)
            {
                float height = CharacterSprites.GirlHeight * (m_Mounted ? MountedHeightFactor : 1f);
                if (m_Billboard == null)
                {
                    m_Billboard = SpriteBillboard.Create(Visual, sprite, height);
                    m_Billboard.SetMotion(m_Speed01);
                }
                else
                {
                    m_Billboard.SetSprite(sprite, height);
                }
                m_Billboard.SetSortingOrder(m_Mounted ? 1 : 0); // rider is drawn in front of the lion
                CharacterSprites.SetPrimitivesVisible(Visual, null, false);
                if (StaffTip != null) StaffTip.position = m_Billboard.AnchorWorld(StaffTipAnchor);
            }
            else if (m_Billboard != null)
            {
                if (Application.isPlaying) Destroy(m_Billboard.gameObject);
                else DestroyImmediate(m_Billboard.gameObject);
                m_Billboard = null;
                CharacterSprites.SetPrimitivesVisible(Visual, null, true);
            }
            CacheRenderers();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateAnimation(dt);
            UpdateFlash(dt);
        }

        private void UpdateAnimation(float dt)
        {
            if (Visual == null) return;

            // Sin-based movement bobbing and breathing
            float bobFreq = m_Speed01 > 0.05f ? 12f : 2.5f;
            float bobAmp = m_Speed01 > 0.05f ? 0.05f : 0.015f;
            float bobY = Mathf.Sin(Time.time * bobFreq) * bobAmp;

            float swayZ = Mathf.Sin(Time.time * 6f) * 4f * m_Speed01;

            // Cast recoil
            float castPitch = 0f;
            if (m_CastTimer > 0f)
            {
                m_CastTimer -= dt;
                float t01 = Mathf.Clamp01(m_CastTimer / 0.25f);
                castPitch = Mathf.Sin(t01 * Mathf.PI) * 15f;
            }

            // Turn smoothly toward the facing; lean forward while running and bank into turns.
            float remainingTurn = m_Facing.Step(dt, TurnSpeed);
            float targetBank = -Mathf.Clamp(remainingTurn, -60f, 60f) * 0.2f * Mathf.Max(0.3f, m_Speed01);
            m_Bank = Mathf.Lerp(m_Bank, targetBank, Mathf.Clamp01(dt * 12f));
            float lean = 9f * m_Speed01;

            Visual.localPosition = new Vector3(0f, bobY, 0f);
            // World rotation so it also holds when the rig is parented to the dog's seat.
            Visual.rotation = m_Facing.Rotation * Quaternion.Euler(castPitch + lean, 0f, swayZ + m_Bank);

            if (StaffSlot != null)
            {
                StaffSlot.localRotation = Quaternion.Euler(castPitch * 1.5f, 0f, 0f);
            }
        }

        private void UpdateFlash(float dt)
        {
            if (m_FlashTimer > 0f)
            {
                m_FlashTimer -= dt;
                if (m_FlashTimer <= 0f)
                {
                    ApplyFlashTint(false);
                }
            }
        }

        private void ApplyFlashTint(bool active)
        {
            int propId = Shader.PropertyToID("_BaseColor");
            foreach (var r in m_CachedRenderers)
            {
                if (r == null) continue;
                if (active)
                {
                    m_Mpb.SetColor(propId, m_FlashColor);
                    r.SetPropertyBlock(m_Mpb);
                }
                else
                {
                    r.SetPropertyBlock(null);
                }
            }
        }

        private void CacheRenderers()
        {
            m_CachedRenderers.Clear();
            if (Visual != null)
            {
                Visual.GetComponentsInChildren(true, m_CachedRenderers);
                m_CachedRenderers.RemoveAll(r => r is SpriteRenderer); // sprite flash goes through the billboard
            }
        }

        private Transform GetSlot(OutfitSlot slot)
        {
            switch (slot)
            {
                case OutfitSlot.Head:  return HeadSlot;
                case OutfitSlot.Body:  return BodySlot;
                case OutfitSlot.Staff: return StaffSlot;
                default:               return null;
            }
        }

        private IEnumerator PopScaleRoutine(Transform tr)
        {
            float elapsed = 0f;
            const float duration = 0.20f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                if (tr != null) tr.localScale = Vector3.one * scale;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }
    }
}
