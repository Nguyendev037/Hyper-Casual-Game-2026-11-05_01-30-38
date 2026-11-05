using UnityEngine;
using UnityEngine.Rendering;
using RHMG.Runtime.Combat;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Factory for procedural visual effects: projectiles, telegraphs, spell rings, and bursts.</summary>
    public static class VfxFactory
    {
        public static GameObject Projectile(Color c, float radius)
        {
            radius = Mathf.Max(0.05f, radius);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            go.transform.localScale = Vector3.one * (radius * 2f);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = MaterialLibrary.Unlit(c);
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = radius * 1.8f;
            trail.endWidth = 0.01f;
            trail.sharedMaterial = MaterialLibrary.Unlit(c);
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = ShadowCastingMode.Off;

            return go;
        }

        public static GameObject Telegraph(Mesh shape)
        {
            GameObject go = new GameObject("Telegraph");
            TelegraphView view = go.AddComponent<TelegraphView>();

            // Inner fill with alpha 0.25
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(go.transform, false);
            MeshFilter mfFill = fill.AddComponent<MeshFilter>();
            mfFill.sharedMesh = shape;
            MeshRenderer mrFill = fill.AddComponent<MeshRenderer>();
            Color fillCol = new Color(Palette.WarnRed.r, Palette.WarnRed.g, Palette.WarnRed.b, 0.25f);
            mrFill.sharedMaterial = MaterialLibrary.UnlitTransparent(fillCol);
            mrFill.shadowCastingMode = ShadowCastingMode.Off;

            // Outer solid border
            GameObject border = new GameObject("Border");
            border.transform.SetParent(go.transform, false);
            MeshFilter mfBorder = border.AddComponent<MeshFilter>();
            if (shape != null && shape.name == "Fan")
            {
                mfBorder.sharedMesh = MeshBuilder.FanOutline(CombatTuning.WardenFanRadius,
                    CombatTuning.WardenFanAngle, 24);
            }
            else if (shape != null && shape.name == "BossFan")
            {
                mfBorder.sharedMesh = MeshBuilder.FanOutline(CombatTuning.BossFanRadius,
                    CombatTuning.BossFanAngle, 24);
            }
            else if (shape != null && shape.name == "Dash")
            {
                mfBorder.sharedMesh = MeshBuilder.RectangleOutline(CombatTuning.SentinelTelegraphWidth,
                    CombatTuning.SentinelChargeDistance);
            }
            else
            {
                float radius = shape != null ? Mathf.Max(0.5f, shape.bounds.extents.x) : 1f;
                mfBorder.sharedMesh = MeshBuilder.Ring(Mathf.Max(0.05f, radius * 0.94f), radius, 36);
            }
            border.transform.localPosition = Vector3.up * 0.01f;
            MeshRenderer mrBorder = border.AddComponent<MeshRenderer>();
            mrBorder.sharedMaterial = MaterialLibrary.Unlit(Palette.WarnRed);
            mrBorder.shadowCastingMode = ShadowCastingMode.Off;

            view.FillTransform = fill.transform;
            view.SetFill(0f);
            return go;
        }

        /// <summary>Gold ring under an elite enemy ("viền vàng", SPEC-P01 §3).</summary>
        public static GameObject EliteRing(float radius)
        {
            GameObject go = new GameObject("EliteRing");
            go.AddComponent<MeshFilter>().sharedMesh = MeshBuilder.Ring(radius * 0.86f, radius, 32);
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Unlit(Palette.LightGold);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localPosition = Vector3.up * 0.04f;
            return go;
        }

        /// <summary>Unit-radius flat ring (scale it to the wanted radius): storm edge, station tiles.</summary>
        public static GameObject UnitRing(Color c, float thickness01, bool transparent)
        {
            GameObject go = new GameObject("UnitRing");
            go.AddComponent<MeshFilter>().sharedMesh = MeshBuilder.Ring(1f - thickness01, 1f, 72);
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = transparent ? MaterialLibrary.UnlitTransparent(c) : MaterialLibrary.Unlit(c);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Unit-radius flat disc (scale it to the wanted radius).</summary>
        public static GameObject UnitDisc(Color c)
        {
            GameObject go = new GameObject("UnitDisc");
            go.AddComponent<MeshFilter>().sharedMesh = MeshBuilder.Disc(1f, 40);
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.UnlitTransparent(c);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public static GameObject SpellRing(float radius, Color c)
        {
            radius = Mathf.Max(0.1f, radius);
            GameObject go = new GameObject("SpellRing");

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = MeshBuilder.DashedRing(radius * 0.92f, radius, 16);

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Unlit(c);
            mr.shadowCastingMode = ShadowCastingMode.Off;

            return go;
        }

        public static GameObject Burst(Color c, float radius)
        {
            radius = Mathf.Max(0.1f, radius);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Burst";

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = MaterialLibrary.UnlitTransparent(Color.white);
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            BurstAnimator anim = go.AddComponent<BurstAnimator>();
            anim.TargetRadius = radius;
            anim.BaseColor = c;

            AutoDisable auto = go.AddComponent<AutoDisable>();
            auto.Lifetime = 0.3f;

            return go;
        }
    }

    /// <summary>Expands and fades burst sphere over 0.3s.</summary>
    internal sealed class BurstAnimator : MonoBehaviour
    {
        public float TargetRadius = 1f;
        public Color BaseColor = Color.white;
        private float m_Elapsed;
        private MeshRenderer m_Renderer;
        private MaterialPropertyBlock m_Properties;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            m_Renderer = GetComponent<MeshRenderer>();
            m_Properties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            m_Elapsed = 0f;
            transform.localScale = Vector3.one * (TargetRadius * 0.2f);
        }

        private void Update()
        {
            m_Elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(m_Elapsed / 0.3f);
            transform.localScale = Vector3.one * Mathf.Lerp(TargetRadius * 0.3f, TargetRadius * 2f, t);
            if (m_Renderer != null)
            {
                float alpha = Mathf.Lerp(BaseColor.a, 0f, t);
                m_Properties.SetColor(BaseColorId,
                    new Color(BaseColor.r, BaseColor.g, BaseColor.b, alpha));
                m_Renderer.SetPropertyBlock(m_Properties);
            }
        }
    }
}
