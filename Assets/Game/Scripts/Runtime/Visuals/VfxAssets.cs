using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Loads VFX art by id from Resources/RHMG/VFX; missing art falls back to code-made textures (plan05 C3).</summary>
    public static class VfxAssets
    {
        public const string Folder = "RHMG/VFX/";
        public const string AdditiveName = "RHMG/VfxAdditive";
        public const string TrailName = "RHMG/VfxTrail";

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static Material additive;
        private static Material trail;
        private static Mesh quad;

        /// <summary>Texture for an id. Never null: the fallback is generated on first use.</summary>
        public static Texture2D Texture(string id)
        {
            if (Cache.TryGetValue(id, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = null;
            Sprite sprite = Resources.Load<Sprite>(Folder + id);
            if (sprite != null)
            {
                texture = sprite.texture;
            }

            if (texture == null)
            {
                texture = VfxFallbackTextures.Make(id);
            }

            Cache[id] = texture;
            return texture;
        }

        /// <summary>True when real art (not the generated fallback) is installed for the id.</summary>
        public static bool HasArt(string id)
        {
            return Resources.Load<Sprite>(Folder + id) != null;
        }

        public static string ImpactId(int frame)
        {
            return "vfx_impact_" + Mathf.Clamp(frame, 0, 5);
        }

        public static Material Additive()
        {
            if (additive == null)
            {
                additive = Resources.Load<Material>(AdditiveName);
                if (additive == null)
                {
                    additive = MaterialLibrary.UnlitTransparent(Color.white);
                }
            }

            return additive;
        }

        public static Material Trail()
        {
            if (trail == null)
            {
                trail = Resources.Load<Material>(TrailName);
                if (trail == null)
                {
                    trail = MaterialLibrary.UnlitTransparent(Color.white);
                }
            }

            return trail;
        }

        public static Mesh Quad()
        {
            if (quad != null)
            {
                return quad;
            }

            quad = new Mesh { name = "VfxQuad" };
            quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateNormals();
            quad.RecalculateBounds();
            return quad;
        }
    }
}
