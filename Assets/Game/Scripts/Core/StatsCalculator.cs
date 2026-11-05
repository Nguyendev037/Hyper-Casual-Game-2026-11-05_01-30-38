using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class PlayerStats
    {
        public float MaxHp;
        public float MoveSpeed;
        public float StaffDamage;
        public float AttackInterval;
        public float Range;
        public float DogDamage;
        public float DogInterval;
        public float CommandCooldown;
        public float ShieldPower;
        public float BurnPower;
        public float WoodHealPower;
    }

    public static class StatsCalculator
    {
        public const float MountedMoveMultiplier = 1.15f;
        public const float MinAttackInterval = 0.25f;
        public const float MinCommandCooldown = 4f;

        public static float ClampOutfitBonus(float percent)
        {
            return Math.Max(-Catalog.OutfitBonusCap, Math.Min(Catalog.OutfitBonusCap, percent));
        }

        public static PlayerStats Compute(
            IEnumerable<string> equippedOutfitIds,
            IEnumerable<string> runUpgradeIds,
            int dogSealMask,
            bool mounted)
        {
            int statCount = Enum.GetValues(typeof(StatKind)).Length;
            float[] outfit = new float[statCount];
            float[] upgrade = new float[statCount];
            float flatHp = 0f;

            AccumulateOutfits(equippedOutfitIds, outfit);
            for (int i = 0; i < statCount; i++)
            {
                outfit[i] = ClampOutfitBonus(outfit[i]);
            }

            flatHp = AccumulateUpgrades(runUpgradeIds, upgrade);

            PlayerStats s = new PlayerStats();
            s.MaxHp = Catalog.BaseMaxHp * Scale(outfit, upgrade, StatKind.MaxHp) + flatHp;
            s.MoveSpeed = Catalog.BaseMoveSpeed * Scale(outfit, upgrade, StatKind.MoveSpeed);
            s.StaffDamage = Catalog.BaseStaffDamage * Scale(outfit, upgrade, StatKind.StaffDamage);
            s.AttackInterval = Catalog.BaseAttackInterval * Scale(outfit, upgrade, StatKind.AttackInterval);
            s.Range = Catalog.BaseRange;
            s.DogDamage = Catalog.BaseDogDamage * Scale(outfit, upgrade, StatKind.DogDamage)
                * DogProgress.DogDamageMultiplier(DogProgress.GetTier(dogSealMask));
            s.DogInterval = Catalog.BaseDogInterval;
            s.CommandCooldown = Catalog.BaseCommandCooldown * Scale(outfit, upgrade, StatKind.CommandCooldown);
            s.ShieldPower = Catalog.BasePower * Scale(outfit, upgrade, StatKind.ShieldPower);
            s.BurnPower = Catalog.BasePower * Scale(outfit, upgrade, StatKind.BurnPower);
            s.WoodHealPower = Catalog.BasePower * Scale(outfit, upgrade, StatKind.WoodHealPower);

            if (mounted)
            {
                s.MoveSpeed *= MountedMoveMultiplier;
            }

            s.AttackInterval = Math.Max(s.AttackInterval, MinAttackInterval);
            s.CommandCooldown = Math.Max(s.CommandCooldown, MinCommandCooldown);
            return s;
        }

        private static float Scale(float[] outfit, float[] upgrade, StatKind kind)
        {
            return 1f + outfit[(int)kind] + upgrade[(int)kind];
        }

        private static void AccumulateOutfits(IEnumerable<string> ids, float[] outfit)
        {
            if (ids == null)
            {
                return;
            }

            foreach (string id in ids)
            {
                OutfitDefinition def = Catalog.GetOutfit(id);
                if (def == null)
                {
                    continue;
                }

                for (int i = 0; i < def.Modifiers.Count; i++)
                {
                    outfit[(int)def.Modifiers[i].Stat] += def.Modifiers[i].Percent;
                }
            }
        }

        private static float AccumulateUpgrades(IEnumerable<string> ids, float[] upgrade)
        {
            float flatHp = 0f;
            if (ids == null)
            {
                return flatHp;
            }

            Dictionary<string, int> stacks = new Dictionary<string, int>();
            foreach (string id in ids)
            {
                UpgradeDefinition def = Catalog.GetUpgrade(id);
                if (def == null || def.MaxStacks <= 0)
                {
                    continue;
                }

                int count;
                stacks.TryGetValue(id, out count);
                if (count >= def.MaxStacks)
                {
                    continue;
                }

                stacks[id] = count + 1;
                flatHp += def.FlatHp;
                if (def.Stat.HasValue)
                {
                    upgrade[(int)def.Stat.Value] += def.Percent;
                }
            }

            return flatHp;
        }
    }
}
