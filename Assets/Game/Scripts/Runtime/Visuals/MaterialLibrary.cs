using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Runtime material cache based on URP master materials in Resources/RHMG.</summary>
    public static class MaterialLibrary
    {
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");

        private static readonly Dictionary<Color32, Material> s_LitCache = new Dictionary<Color32, Material>();
        private static readonly Dictionary<Color32, Material> s_UnlitCache = new Dictionary<Color32, Material>();
        private static readonly Dictionary<Color32, Material> s_UnlitTransCache = new Dictionary<Color32, Material>();

        private static Material s_BaseLit;
        private static Material s_BaseUnlit;
        private static Material s_BaseUnlitTrans;

        private static bool s_LoggedLitError;
        private static bool s_LoggedUnlitError;
        private static bool s_LoggedUnlitTransError;

        public static Material Lit(Color c)
        {
            Color32 key = c;
            if (s_LitCache.TryGetValue(key, out var mat) && mat != null)
                return mat;

            Material baseMat = GetBaseLit();
            Material instance;
            if (baseMat != null)
            {
                instance = new Material(baseMat);
            }
            else
            {
                instance = CreateFallback("Universal Render Pipeline/Simple Lit", false);
            }

            if (instance != null)
            {
                instance.hideFlags = HideFlags.DontSave;
                instance.SetColor(BaseColorPropId, c);
            }
            s_LitCache[key] = instance;
            return instance;
        }

        public static Material Unlit(Color c)
        {
            Color32 key = c;
            if (s_UnlitCache.TryGetValue(key, out var mat) && mat != null)
                return mat;

            Material baseMat = GetBaseUnlit();
            Material instance;
            if (baseMat != null)
            {
                instance = new Material(baseMat);
            }
            else
            {
                instance = CreateFallback("Universal Render Pipeline/Unlit", false);
            }

            if (instance != null)
            {
                instance.hideFlags = HideFlags.DontSave;
                instance.SetColor(BaseColorPropId, c);
            }
            s_UnlitCache[key] = instance;
            return instance;
        }

        public static Material UnlitTransparent(Color c)
        {
            Color32 key = c;
            if (s_UnlitTransCache.TryGetValue(key, out var mat) && mat != null)
                return mat;

            Material baseMat = GetBaseUnlitTransparent();
            Material instance;
            if (baseMat != null)
            {
                instance = new Material(baseMat);
            }
            else
            {
                instance = CreateFallback("Universal Render Pipeline/Unlit", true);
            }

            if (instance != null)
            {
                instance.hideFlags = HideFlags.DontSave;
                instance.SetColor(BaseColorPropId, c);
            }
            s_UnlitTransCache[key] = instance;
            return instance;
        }

        private static Material GetBaseLit()
        {
            if (s_BaseLit != null) return s_BaseLit;
            s_BaseLit = Resources.Load<Material>("RHMG/Lit");
            if (s_BaseLit == null && !s_LoggedLitError)
            {
                s_LoggedLitError = true;
                Debug.LogError("[MaterialLibrary] Missing material Resources.Load<Material>(\"RHMG/Lit\").");
            }
            return s_BaseLit;
        }

        private static Material GetBaseUnlit()
        {
            if (s_BaseUnlit != null) return s_BaseUnlit;
            s_BaseUnlit = Resources.Load<Material>("RHMG/Unlit");
            if (s_BaseUnlit == null && !s_LoggedUnlitError)
            {
                s_LoggedUnlitError = true;
                Debug.LogError("[MaterialLibrary] Missing material Resources.Load<Material>(\"RHMG/Unlit\").");
            }
            return s_BaseUnlit;
        }

        private static Material GetBaseUnlitTransparent()
        {
            if (s_BaseUnlitTrans != null) return s_BaseUnlitTrans;
            s_BaseUnlitTrans = Resources.Load<Material>("RHMG/UnlitTransparent");
            if (s_BaseUnlitTrans == null && !s_LoggedUnlitTransError)
            {
                s_LoggedUnlitTransError = true;
                Debug.LogError("[MaterialLibrary] Missing material Resources.Load<Material>(\"RHMG/UnlitTransparent\").");
            }
            return s_BaseUnlitTrans;
        }

        private static Material CreateFallback(string shaderName, bool transparent)
        {
#if UNITY_EDITOR
            Shader sh = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit");
            if (sh != null)
            {
                Material m = new Material(sh);
                if (transparent)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = 3000;
                }
                return m;
            }
#endif
            return null;
        }
    }
}
