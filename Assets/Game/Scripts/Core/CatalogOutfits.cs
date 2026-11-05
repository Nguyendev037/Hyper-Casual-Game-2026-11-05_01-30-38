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

        private static IReadOnlyList<UpgradeDefinition> BuildUpgradePool()
        {
            return new UpgradeDefinition[]
            {
                new UpgradeDefinition("upgrade.staff_dmg", StatKind.StaffDamage, 0.15f, 0f, 2,
                    "Gậy mạnh hơn", "Sát thương gậy +15%"),
                new UpgradeDefinition("upgrade.atk_speed", StatKind.AttackInterval, -0.10f, 0f, 2,
                    "Bắn nhanh hơn", "Tốc độ bắn nhanh hơn 10%"),
                new UpgradeDefinition("upgrade.max_hp", null, 0f, 15f, 2,
                    "Thêm máu", "Máu tối đa +15 và hồi tương ứng"),
                new UpgradeDefinition("upgrade.move_speed", StatKind.MoveSpeed, 0.08f, 0f, 2,
                    "Chân nhanh", "Di chuyển nhanh hơn 8%"),
                new UpgradeDefinition("upgrade.dog_dmg", StatKind.DogDamage, 0.20f, 0f, 2,
                    "Cún dữ hơn", "Sát thương của cún +20%"),
                new UpgradeDefinition("upgrade.cmd_cd", StatKind.CommandCooldown, -0.15f, 0f, 2,
                    "Lệnh nhanh hơn", "Hồi chiêu lệnh giảm 15%")
            };
        }
    }
}
