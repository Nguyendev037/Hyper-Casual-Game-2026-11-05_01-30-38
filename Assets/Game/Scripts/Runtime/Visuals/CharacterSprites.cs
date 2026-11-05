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

        /// <summary>Image id of a chapter enemy or guardian (01-CONTRACTS sections 2-3); null for the original kinds.</summary>
        public static string ChapterSpriteId(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.EmberImp: return "enemy_ember_imp";
                case EnemyKind.SparkTurret: return "enemy_spark_turret";
                case EnemyKind.AnvilGolem: return "enemy_anvil_golem";
                case EnemyKind.BookBat: return "enemy_book_bat";
                case EnemyKind.MirrorGhost: return "enemy_mirror_ghost";
                case EnemyKind.InkSlime: return "enemy_ink_slime";
                case EnemyKind.PlagueKnight: return "enemy_plague_knight";
                case EnemyKind.BoneArcher: return "enemy_bone_archer";
                case EnemyKind.BombMite: return "enemy_bomb_mite";
                case EnemyKind.ShadowWolf: return "enemy_shadow_wolf";
                case EnemyKind.StarWisp: return "enemy_star_wisp";
                case EnemyKind.VoidEye: return "enemy_void_eye";
                case EnemyKind.ForgeGolem: return "boss_forge_golem";
                case EnemyKind.MirrorWitch: return "boss_mirror_witch";
                case EnemyKind.CorruptKnight: return "boss_corrupt_knight";
                case EnemyKind.TwilightDrake: return "boss_twilight_drake";
                default: return null;
            }
        }

        /// <summary>True when the chapter image is missing and the chapter-0 sprite of the same role is used instead (C3).</summary>
        public static bool UsesFallback(EnemyKind kind)
        {
            string id = ChapterSpriteId(kind);
            return id != null && Load(id) == null;
        }

        /// <summary>Chapter tint of the fallback sprite: 1 ember orange, 2 ink violet, 3 sick green, 4 deep purple.</summary>
        public static Color FallbackTint(EnemyKind kind)
        {
            switch (Catalog.GetEnemy(kind).Chapter)
            {
                case 1: return new Color(1f, 0.62f, 0.35f, 1f);
                case 2: return new Color(0.72f, 0.55f, 0.95f, 1f);
                case 3: return new Color(0.6f, 0.95f, 0.55f, 1f);
                case 4: return new Color(0.55f, 0.38f, 0.8f, 1f);
                default: return Color.white;
            }
        }

        public static Sprite Enemy(EnemyKind kind)
        {
            string chapterId = ChapterSpriteId(kind);
            if (chapterId != null)
            {
                Sprite own = Load(chapterId);
                if (own != null) return own;
                kind = Catalog.GetEnemy(kind).PoolKind;
            }

            switch (kind)
            {
                case EnemyKind.SandEye: return Load("enemy_eye");
                case EnemyKind.Sentinel: return Load("enemy_sentinel");
                case EnemyKind.CageWarden: return Load("enemy_warden");
                case EnemyKind.SandQueen: return Load("boss_queen");
                case EnemyKind.BlueHairGuy: return Load("boss_blue_hair");
                default: return Load("enemy_beetle");
            }
        }

        public static Sprite Spirit(string spiritId)
        {
            if (string.IsNullOrEmpty(spiritId)) return null;
            string key = spiritId.Replace('.', '_');
            if (key != "spirit_dew" && key != "spirit_ember" && key != "spirit_steam" && key != "spirit_bud" &&
                key != "spirit_growth" && key != "spirit_wildfire") return null;
            return Load(key);
        }

        /// <summary>Corrupted anchor of the Blue Hair Guy fight (SPEC-P03 §2b). Image boss_anchor, else a dark crystal.</summary>
        public static Sprite Anchor()
        {
            return Load("boss_anchor");
        }

        public const float AnchorHeight = 2.2f;

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
            float h = EnemyBaseHeight(kind);
            return UsesFallback(kind) ? EnemyBaseHeight(Catalog.GetEnemy(kind).PoolKind) * 1.1f : h;
        }

        private static float EnemyBaseHeight(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.EmberImp: return 1.5f;
                case EnemyKind.SparkTurret: return 1.7f;
                case EnemyKind.AnvilGolem: return 3.0f;
                case EnemyKind.BookBat: return 1.5f;
                case EnemyKind.MirrorGhost: return 1.9f;
                case EnemyKind.InkSlime: return 1.5f;
                case EnemyKind.PlagueKnight: return 2.7f;
                case EnemyKind.BoneArcher: return 2.2f;
                case EnemyKind.BombMite: return 1.3f;
                case EnemyKind.ShadowWolf: return 1.7f;
                case EnemyKind.StarWisp: return 1.6f;
                case EnemyKind.VoidEye: return 1.9f;
                case EnemyKind.ForgeGolem: return 5.4f;
                case EnemyKind.MirrorWitch: return 4.8f;
                case EnemyKind.CorruptKnight: return 5.2f;
                case EnemyKind.TwilightDrake: return 5.6f;
                case EnemyKind.SandEye: return 1.7f;
                case EnemyKind.Sentinel: return 2.7f;
                case EnemyKind.CageWarden: return 4.8f;
                case EnemyKind.SandQueen: return 6.0f;
                case EnemyKind.BlueHairGuy: return 3.6f;
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

        /// <summary>Non-character props in Resources/RHMG/Sprites (gem_*, tile_*); null when missing (caller falls back).</summary>
        public static Sprite Prop(string id)
        {
            return Load(id);
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
