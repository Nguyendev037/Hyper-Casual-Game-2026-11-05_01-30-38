using UnityEngine;
using RHMG.Runtime.Combat;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Look of one pooled projectile: glowing sprite core (slow spin, +-8% pulse) and an element-tinted trail.</summary>
    public sealed class VfxProjectileView : MonoBehaviour
    {
        private const float PulseAmplitude = 0.08f;
        private const float PulseHz = 3.2f;
        private const float SpinDegreesPerSecond = 40f;
        private const float DripInterval = 0.07f;

        private VfxSprite core;
        private TrailRenderer trail;
        private float baseSize = 0.5f;
        private float phase;
        private float spin;
        private float dripTimer;
        private bool merged;
        private Color dripColor;
        private Camera cam;

        public static VfxProjectileView Create(GameObject root, float radius)
        {
            VfxProjectileView view = root.AddComponent<VfxProjectileView>();
            view.core = VfxSprite.Create(root.transform, "Core");
            VfxPool.NoteCreated();
            view.core.SetMaterial(false);
            view.core.SetSortingOrder(10);
            view.core.gameObject.SetActive(true);
            view.baseSize = Mathf.Max(0.2f, radius * 3.6f);
            view.trail = root.GetComponent<TrailRenderer>();
            view.ConfigureTrail();
            view.SetStyle(VfxStyle.Arcane, 1f);
            return view;
        }

        private void ConfigureTrail()
        {
            if (trail == null)
            {
                return;
            }

            trail.sharedMaterial = VfxAssets.Trail();
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.minVertexDistance = 0.06f;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Texture texture = VfxAssets.Texture("vfx_trail");
            block.SetTexture("_BaseMap", texture);
            block.SetTexture("_MainTex", texture);
            trail.SetPropertyBlock(block);
        }

        /// <summary>Apply the look. damageScale grows the core with damage (clamped by the caller).</summary>
        public void SetStyle(VfxStyle style, float damageScale)
        {
            Color tint = VfxStyles.Tint(style);
            merged = VfxStyles.IsMerged(style);
            dripColor = tint;
            float size = baseSize * damageScale * (merged ? 1.5f : 1f);
            if (style == VfxStyle.Boss)
            {
                size *= 1.25f;
            }

            core.SetTexture(VfxAssets.Texture(VfxStyles.CoreId(style)));
            core.SetColor(tint);
            core.SetSize(size);
            coreSize = size;
            ApplyTrail(style, size);
        }

        private float coreSize = 0.5f;

        private void ApplyTrail(VfxStyle style, float size)
        {
            if (trail == null)
            {
                return;
            }

            Color tail = VfxStyles.TrailTint(style);
            trail.time = merged ? 0.2f : (style == VfxStyle.Enemy || style == VfxStyle.Boss ? 0.16f : 0.14f);
            trail.widthMultiplier = Mathf.Max(0.12f, size * 0.7f);
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(tail, 0f), new GradientColorKey(tail, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
        }

        /// <summary>Called by Projectile.Fire after the object is active and positioned.</summary>
        public void Begin()
        {
            phase = Random.value * 6.28f;
            dripTimer = 0f;
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        public void End()
        {
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        private void LateUpdate()
        {
            if (core == null)
            {
                return;
            }

            if (cam == null || !cam.isActiveAndEnabled)
            {
                cam = Camera.main;
            }

            float t = Time.time;
            float pulse = 1f + PulseAmplitude * Mathf.Sin(t * PulseHz * 6.28f + phase);
            core.SetSize(coreSize * pulse);
            spin += SpinDegreesPerSecond * Time.deltaTime;
            if (cam != null)
            {
                core.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, spin);
            }

            if (merged)
            {
                dripTimer -= Time.deltaTime;
                if (dripTimer <= 0f)
                {
                    dripTimer = DripInterval;
                    VfxHub.Drip(transform.position, dripColor);
                }
            }
        }
    }
}
