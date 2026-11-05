using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Combat;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Look of a projectile core (SPEC-P03 §1). Auto = chosen from the owner.</summary>
    public enum VfxStyle
    {
        Auto,
        Arcane,
        Water,
        Fire,
        Wood,
        Steam,
        Growth,
        Wildfire,
        Enemy,
        Boss
    }

    /// <summary>Maps a style to its core sprite id, tint and trail colour.</summary>
    public static class VfxStyles
    {
        public static VfxStyle Resolve(VfxStyle style, ProjectileOwner owner)
        {
            if (style != VfxStyle.Auto)
            {
                return style;
            }

            return owner == ProjectileOwner.Player ? VfxStyle.Arcane : VfxStyle.Enemy;
        }

        public static VfxStyle ForSpirit(SpiritDefinition definition)
        {
            if (definition == null)
            {
                return VfxStyle.Arcane;
            }

            string id = definition.Id ?? string.Empty;
            if (id.EndsWith("steam"))
            {
                return VfxStyle.Steam;
            }

            if (id.EndsWith("growth"))
            {
                return VfxStyle.Growth;
            }

            if (id.EndsWith("wildfire"))
            {
                return VfxStyle.Wildfire;
            }

            switch (definition.Element)
            {
                case Element.Water: return VfxStyle.Water;
                case Element.Fire: return VfxStyle.Fire;
                case Element.Wood: return VfxStyle.Wood;
                default: return VfxStyle.Arcane;
            }
        }

        public static string CoreId(VfxStyle style)
        {
            switch (style)
            {
                case VfxStyle.Water: return "vfx_core_water";
                case VfxStyle.Fire: return "vfx_core_fire";
                case VfxStyle.Wood: return "vfx_core_wood";
                case VfxStyle.Steam: return "vfx_core_steam";
                case VfxStyle.Growth: return "vfx_core_growth";
                case VfxStyle.Wildfire: return "vfx_core_wildfire";
                case VfxStyle.Enemy: return "vfx_core_enemy";
                case VfxStyle.Boss: return "vfx_core_boss";
                default: return "vfx_core_arcane";
            }
        }

        public static bool IsMerged(VfxStyle style)
        {
            return style == VfxStyle.Steam || style == VfxStyle.Growth || style == VfxStyle.Wildfire;
        }

        public static Color Tint(VfxStyle style)
        {
            switch (style)
            {
                case VfxStyle.Water: return Palette.WaterBlue;
                case VfxStyle.Fire: return Palette.FireOrange;
                case VfxStyle.Wood: return Palette.LeafGreen;
                case VfxStyle.Steam: return Color.Lerp(Palette.WaterBlue, Palette.FireOrange, 0.5f);
                case VfxStyle.Growth: return Color.Lerp(Palette.WaterBlue, Palette.LeafGreen, 0.5f);
                case VfxStyle.Wildfire: return Color.Lerp(Palette.FireOrange, Palette.LeafGreen, 0.45f);
                case VfxStyle.Enemy: return new Color(0.72f, 0.28f, 0.82f, 1f);
                case VfxStyle.Boss: return new Color(0.48f, 0.12f, 0.62f, 1f);
                default: return Color.Lerp(Palette.HairRed, Palette.LightGold, 0.35f);
            }
        }

        public static Color TrailTint(VfxStyle style)
        {
            if (style == VfxStyle.Enemy || style == VfxStyle.Boss)
            {
                return new Color(0.95f, 0.18f, 0.22f, 1f);
            }

            return Tint(style);
        }
    }
}
