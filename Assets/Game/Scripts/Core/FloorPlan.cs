using System;

namespace RHMG.Core
{
    public enum FloorKind { Normal, Guardian, MidBoss, FinalBoss }

    public struct SpawnBudget
    {
        public int Total;
        public float BeetleW, EyeW, SentinelW, EliteChance, HpMult, DmgMult;
        /// <summary>Seconds; 0 = no limit (boss floors end when the boss dies).</summary>
        public float Duration;
    }

    public static class FloorPlan
    {
        /// <summary>100 Final, 50 MidBoss, f%5==0 Guardian, else Normal. Outside 1..100 gives Normal.</summary>
        public static FloorKind Kind(int floor)
        {
            if (floor < 1 || floor > SurvivorTuning.FloorCount)
            {
                return FloorKind.Normal;
            }

            if (floor == 100)
            {
                return FloorKind.FinalBoss;
            }

            if (floor == 50)
            {
                return FloorKind.MidBoss;
            }

            return floor % 5 == 0 ? FloorKind.Guardian : FloorKind.Normal;
        }

        public static bool StationAfter(int floor)
        {
            return floor >= 1 && floor < SurvivorTuning.FloorCount && floor % SurvivorTuning.StationEvery == 0;
        }

        /// <summary>Boss enemy of the floor, or null on normal floors.</summary>
        public static EnemyKind? BossKind(int floor)
        {
            switch (Kind(floor))
            {
                case FloorKind.Guardian: return EnemyKind.CageWarden;
                case FloorKind.MidBoss: return EnemyKind.SandQueen;
                case FloorKind.FinalBoss: return EnemyKind.BlueHairGuy;
                default: return null;
            }
        }

        public static SpawnBudget Budget(int floor)
        {
            if (floor < 1 || floor > SurvivorTuning.FloorCount)
            {
                throw new ArgumentOutOfRangeException("floor");
            }

            bool boss = Kind(floor) != FloorKind.Normal;
            double total = 8.0 + 0.6 * floor;
            if (boss)
            {
                total *= SurvivorTuning.BossFloorSpawnFactor;
            }

            SpawnBudget b = new SpawnBudget();
            b.Total = (int)Math.Round(total, MidpointRounding.AwayFromZero);
            b.BeetleW = 1f;
            b.EyeW = floor >= 3 ? Math.Min(0.4f, 0.02f * floor) : 0f;
            b.SentinelW = floor >= 6 ? Math.Min(0.35f, 0.015f * floor) : 0f;
            b.EliteChance = floor >= 15 ? Math.Min(0.08f, 0.002f * floor) : 0f;
            b.HpMult = 1f + 0.07f * (floor - 1);
            b.DmgMult = 1f + 0.02f * (floor - 1);
            b.Duration = boss ? 0f : (floor <= 20 ? 25f : (floor <= 60 ? 30f : 35f));
            return b;
        }
    }
}
