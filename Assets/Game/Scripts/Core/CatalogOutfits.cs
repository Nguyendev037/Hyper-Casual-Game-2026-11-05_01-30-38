using System.Collections.Generic;

namespace RHMG.Core
{
    public static partial class Catalog
    {
        public static readonly IReadOnlyList<OutfitDefinition> Outfits = BuildOutfits();

        /// <summary>Upgrades eligible for the choice pool (excludes the heal fallback), in Catalog order.</summary>
        public static readonly IReadOnlyList<UpgradeDefinition> UpgradePool = BuildUpgradePool();

        private static readonly UpgradeDefinition HealFallback = new UpgradeDefinition(
            HealFallbackId, null, 0f, 0f, 0, "Hồi máu", "Hồi 20 máu");

        public static OutfitDefinition GetOutfit(string id)
        {
            for (int i = 0; i < Outfits.Count; i++)
            {
                if (Outfits[i].Id == id)
                {
                    return Outfits[i];
                }
            }

            return null;
        }

        public static OutfitDefinition GetDefaultOutfit(OutfitSlot slot)
        {
            for (int i = 0; i < Outfits.Count; i++)
            {
                if (Outfits[i].Slot == slot && Outfits[i].DefaultOwned)
                {
                    return Outfits[i];
                }
            }

            return null;
        }

        public static UpgradeDefinition GetUpgrade(string id)
        {
            if (id == HealFallbackId)
            {
                return HealFallback;
            }

            for (int i = 0; i < UpgradePool.Count; i++)
            {
                if (UpgradePool[i].Id == id)
                {
                    return UpgradePool[i];
                }
            }

            return null;
        }

        private static StatModifier[] Mods(params StatModifier[] mods)
        {
            return mods;
        }

        private static IReadOnlyList<OutfitDefinition> BuildOutfits()
        {
            return new OutfitDefinition[]
            {
                new OutfitDefinition(
                    "outfit.head.ash_hat", OutfitSlot.Head, "Mũ phù thủy Tro Đỏ", 80, true,
                    Mods(new StatModifier(StatKind.BurnPower, 0.08f)),
                    "Lửa cháy mạnh hơn 8%", "Không có"),
                new OutfitDefinition(
                    "outfit.head.dawn_clip", OutfitSlot.Head, "Kẹp tóc Bình Minh", 80, false,
                    Mods(new StatModifier(StatKind.ShieldPower, 0.08f)),
                    "Lá chắn mạnh hơn 8%", "Không có"),
                new OutfitDefinition(
                    "outfit.body.journey_cloak", OutfitSlot.Body, "Áo choàng Hành Trình", 120, true,
                    Mods(new StatModifier(StatKind.MoveSpeed, 0.05f)),
                    "Di chuyển nhanh hơn 5%", "Không có"),
                new OutfitDefinition(
                    "outfit.body.rose_dress", OutfitSlot.Body, "Váy Hồng Sa", 120, false,
                    Mods(new StatModifier(StatKind.MaxHp, 0.10f), new StatModifier(StatKind.MoveSpeed, -0.03f)),
                    "Máu tối đa +10%", "Di chuyển chậm hơn 3%"),
                new OutfitDefinition(
                    "outfit.staff.sprout", OutfitSlot.Staff, "Gậy Hoa Mầm", 100, true,
                    Mods(new StatModifier(StatKind.WoodHealPower, 0.10f), new StatModifier(StatKind.StaffDamage, -0.05f)),
                    "Hồi phục mộc +10%", "Sát thương gậy giảm 5%"),
                new OutfitDefinition(
                    "outfit.staff.sunray", OutfitSlot.Staff, "Gậy Tia Nắng", 100, false,
                    Mods(new StatModifier(StatKind.StaffDamage, 0.08f), new StatModifier(StatKind.AttackInterval, 0.05f)),
                    "Sát thương gậy +8%", "Bắn chậm hơn 5%")
            };
        }

        private static UpgradeDefinition Up(string id, StatKind? stat, float percent, float flatHp, int max, string title, string desc)
        {
            return new UpgradeDefinition(id, stat, percent, flatHp, max, title, desc);
        }

        // SPEC-P01 §5. Order = Catalog order used by the roll.
        private static IReadOnlyList<UpgradeDefinition> BuildUpgradePool()
        {
            return new UpgradeDefinition[]
            {
                Up("upgrade.staff_dmg", StatKind.StaffDamage, 0.12f, 0f, 5, "Gậy mạnh hơn", "Sát thương gậy +12%"),
                Up("upgrade.atk_speed", StatKind.AttackInterval, -0.08f, 0f, 5, "Bắn nhanh hơn", "Tốc độ bắn nhanh hơn 8%"),
                Up("upgrade.max_hp", null, 0f, 15f, 5, "Thêm máu", "Máu tối đa +15 và hồi tương ứng"),
                Up("upgrade.move_speed", StatKind.MoveSpeed, 0.06f, 0f, 5, "Chân nhanh", "Di chuyển nhanh hơn 6%"),
                Up("upgrade.magnet", StatKind.MagnetRadius, 0.30f, 0f, 5, "Nam châm linh thạch", "Tầm hút linh thạch +30%"),
                Up("upgrade.range", StatKind.Range, 0.08f, 0f, 5, "Tầm xa", "Tầm bắn +8%"),
                Up("upgrade.multishot", StatKind.ProjectileCount, 1f, 0f, 3, "Đa phát", "Bắn thêm 1 đạn"),
                Up("upgrade.pierce", StatKind.Pierce, 1f, 0f, 3, "Xuyên thấu", "Đạn xuyên thêm 1 mục tiêu"),
                Up("upgrade.armor", StatKind.Armor, 0.06f, 0f, 5, "Giáp cát", "Sát thương nhận vào giảm 6%"),
                Up("upgrade.regen", StatKind.Regen, 0.4f, 0f, 5, "Hồi phục", "Hồi 0,4 máu mỗi giây"),
                Up("upgrade.xp_gain", StatKind.XpGain, 0.08f, 0f, 3, "Học nhanh", "Linh thạch nhận được +8%"),
                Up("upgrade.dog_dmg", StatKind.DogDamage, 0.20f, 0f, 5, "Cún dữ hơn", "Sát thương của cún +20%"),
                Up("upgrade.spirit_power", StatKind.SpiritPower, 0.15f, 0f, 5, "Tinh linh mạnh hơn", "Sát thương tinh linh +15%"),
                Up("upgrade.cmd_cd", StatKind.CommandCooldown, -0.08f, 0f, 5, "Hồi chiêu nhanh", "Hồi chiêu tinh linh giảm 8%")
            };
        }
    }
}
