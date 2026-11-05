using System;

namespace RHMG.Core
{
    public static class XpCurve
    {
        /// <summary>Largest level whose rounded XP requirement fits a positive Int32.</summary>
        public static readonly int MaxLevel = FindMaxLevel();

        private static double RoundedRequirement(int level)
        {
            double v = SurvivorTuning.XpBase + SurvivorTuning.XpLinear * level + SurvivorTuning.XpQuad * level * level;
            return Math.Round(v, MidpointRounding.AwayFromZero);
        }

        private static int FindMaxLevel()
        {
            int low = 1, high = int.MaxValue;
            while (low < high)
            {
                int mid = (int)(low + ((long)high - low + 1) / 2);
                if (RoundedRequirement(mid) <= int.MaxValue) low = mid;
                else high = mid - 1;
            }
            return low;
        }

        /// <summary>XP needed for the next level; accepts 1..MaxLevel.</summary>
        public static int Required(int level)
        {
            if (level < 1 || level > MaxLevel)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            return (int)RoundedRequirement(level);
        }

        public static int XpValue(EnemyKind kind, bool elite)
        {
            int baseXp;
            switch (kind)
            {
                case EnemyKind.SandBeetle: baseXp = SurvivorTuning.XpBeetle; break;
                case EnemyKind.SandEye: baseXp = SurvivorTuning.XpEye; break;
                case EnemyKind.Sentinel: baseXp = SurvivorTuning.XpSentinel; break;
                case EnemyKind.CageWarden: return SurvivorTuning.XpWarden;
                case EnemyKind.SandQueen: return SurvivorTuning.XpQueen;
                case EnemyKind.BlueHairGuy: return SurvivorTuning.XpBlueHair;
                default: return 0;
            }

            return elite ? baseXp * SurvivorTuning.EliteXpMultiplier : baseXp;
        }

        /// <summary>XP granted by the station XP chest.</summary>
        public static int ChestXp(int level)
        {
            return (int)Math.Round(Required(level) * (double)SurvivorTuning.XpChestFraction, MidpointRounding.AwayFromZero);
        }
    }
}
