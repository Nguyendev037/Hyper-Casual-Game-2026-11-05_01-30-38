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

        private float m_Speed01;
        private float m_CastTimer;
        private float m_FlashTimer;
        private Color m_FlashColor;
        private MaterialPropertyBlock m_Mpb;
        private readonly List<Renderer> m_CachedRenderers = new List<Renderer>();

        private readonly Dictionary<OutfitSlot, Coroutine> m_PopRoutines = new Dictionary<OutfitSlot, Coroutine>();

        private void Awake()
        {
            m_Mpb = new MaterialPropertyBlock();
            CacheRenderers();
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
        }

        public void PlayCast()
        {
            m_CastTimer = 0.25f;
        }

        public void Flash(Color c, float t)
        {
            m_FlashColor = c;
            m_FlashTimer = Mathf.Max(0.01f, t);
            ApplyFlashTint(true);
        }

        public void SetMounted(Transform seat)
        {
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

            Visual.localPosition = new Vector3(0f, bobY, 0f);
            Visual.localRotation = Quaternion.Euler(castPitch, 0f, swayZ);

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
