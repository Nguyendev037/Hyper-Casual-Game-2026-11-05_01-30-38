using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Lookup and cache for the 2D character sprites in Resources/RHMG/Sprites. A missing sprite returns null
    /// (one warning per id) so callers keep the primitive rig (fallback S1).</summary>
    public static class CharacterSprites
    {
        private const string SpritePath = "RHMG/Sprites/";
        private const string MaterialPath = "RHMG/SpriteUnlit";

        // Heights ~1.4x the first pass (plan03 12:45): at the depth-fit camera 2.0 m read as ~45 px on a phone.
        public static float GirlHeight = 2.8f;
        public static float SpiritHeight = 1.1f;

        private static readonly Dictionary<string, Sprite> s_Cache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> s_Warned = new HashSet<string>();
        private static Material s_Material;
        private static bool s_MaterialWarned;

        private static readonly string[] Bodies = { "journey_cloak", "rose_dress" };
        private static readonly string[] Heads = { "ash_hat", "dawn_clip" };
        private static readonly string[] Staffs = { "sprout", "sunray" };

        /// <summary>Accepts full appearance ids (outfit.head.ash_hat) or short keys; null/unknown = slot default.</summary>
        public static Sprite Girl(string headId, string bodyId, string staffId)
        {
            string head = Key(headId, Heads);
            string body = Key(bodyId, Bodies);
            string staff = Key(staffId, Staffs);
            return Load("girl_" + body + "_" + head + "_" + staff);
        }

        public static Sprite Dog(DogTier tier, DayPhase phase)
        {
            switch (tier)
            {
                case DogTier.SandPup: return Load("dog_sandpup");
                case DogTier.SpiritHound: return Load("dog_spirithound");
                case DogTier.SunMoonLion: return Load(phase == DayPhase.Day ? "dog_lion_sun" : "dog_lion_moon");
                default: return Load("dog_companion");
            }
        }

        public static Sprite Enemy(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.SandEye: return Load("enemy_eye");
                case EnemyKind.Sentinel: return Load("enemy_sentinel");
                case EnemyKind.CageWarden: return Load("enemy_warden");
                default: return Load("enemy_beetle");
            }
        }

        public static Sprite Spirit(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId)) return null;
            string key = spiritId.Replace('.', '_');
            if (key != "spirit_dew" && key != "spirit_ember" && key != "spirit_steam") return null;
            return Load(key);
        }

        public static float DogHeight(DogTier tier)
        {
            switch (tier)
            {
                case DogTier.SandPup: return 1.7f;
                case DogTier.SpiritHound: return 2.0f;
                case DogTier.SunMoonLion: return 3.6f;
                default: return 1.5f;
            }
        }

        public static float EnemyHeight(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.SandEye: return 1.7f;
                case EnemyKind.Sentinel: return 2.7f;
                case EnemyKind.CageWarden: return 4.8f;
                default: return 1.5f;
            }
        }

        /// <summary>Shared sprite material, or null (SpriteRenderer then keeps its default material).</summary>
        internal static Material SpriteMaterial()
        {
            if (s_Material != null) return s_Material;
            s_Material = Resources.Load<Material>(MaterialPath);
            if (s_Material == null && !s_MaterialWarned)
            {
                s_MaterialWarned = true;
                Debug.LogWarning("[RHMG.Sprites] Missing material " + MaterialPath);
            }
            return s_Material;
        }

        /// <summary>Toggles every non-sprite renderer under root (the primitive rig); skip is a subtree left untouched.</summary>
        internal static void SetPrimitivesVisible(Transform root, Transform skip, bool visible)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r is SpriteRenderer) continue;
                if (skip != null && r.transform.IsChildOf(skip)) continue;
                r.enabled = visible;
            }
        }

        private static string Key(string id, string[] allowed)
        {
            if (!string.IsNullOrEmpty(id))
            {
                int dot = id.LastIndexOf('.');
                string tail = dot >= 0 ? id.Substring(dot + 1) : id;
                for (int i = 0; i < allowed.Length; i++)
                {
                    if (allowed[i] == tail) return tail;
                }
            }
            return allowed[0];
        }

        private static Sprite Load(string id)
        {
            if (s_Cache.TryGetValue(id, out Sprite cached) && cached != null) return cached;
            Sprite sprite = Resources.Load<Sprite>(SpritePath + id);
            if (sprite != null)
            {
                s_Cache[id] = sprite;
                return sprite;
            }
            if (s_Warned.Add(id)) Debug.LogWarning("[RHMG.Sprites] Missing " + id);
            return null;
        }
    }
}
