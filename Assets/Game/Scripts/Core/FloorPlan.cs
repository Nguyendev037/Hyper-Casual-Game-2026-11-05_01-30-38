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
        /// <summary>Chapter 0..4 of the floor (01-CONTRACTS section 1).</summary>
        public int Chapter;
        /// <summary>Kinds filling the three weight slots: BeetleW to Chaser, EyeW to Shooter, SentinelW to Special.</summary>
        public EnemyKind ChaserKind, ShooterKind, SpecialKind;
    }

    public static class FloorPlan
    {
        public const int ChapterCount = 5;
        public const int FloorsPerChapter = 20;

        private static readonly EnemyKind[][] Rosters =
        {
            new[] { EnemyKind.SandBeetle, EnemyKind.SandEye, EnemyKind.Sentinel },
            new[] { EnemyKind.EmberImp, EnemyKind.SparkTurret, EnemyKind.AnvilGolem },
            new[] { EnemyKind.BookBat, EnemyKind.MirrorGhost, EnemyKind.InkSlime },
            new[] { EnemyKind.BombMite, EnemyKind.BoneArcher, EnemyKind.PlagueKnight },
            new[] { EnemyKind.ShadowWolf, EnemyKind.StarWisp, EnemyKind.VoidEye }
        };

        private static readonly EnemyKind[] Guardians =
        {
            EnemyKind.CageWarden, EnemyKind.ForgeGolem, EnemyKind.MirrorWitch, EnemyKind.CorruptKnight,
            EnemyKind.TwilightDrake
        };

        /// <summary>(floor-1)/20 clamped to 0..4: floors 1-20 chapter 0 ... 81-100 chapter 4.</summary>
        public static int Chapter(int floor)
        {
            return Math.Max(0, Math.Min(ChapterCount - 1, (floor - 1) / FloorsPerChapter));
        }

        /// <summary>The regular enemy of the chapter that fills the given weight slot.</summary>
        public static EnemyKind RegularKind(int chapter, SpawnRole role)
        {
            int c = Math.Max(0, Math.Min(ChapterCount - 1, chapter));
            return Rosters[c][(int)role];
        }

        /// <summary>Guardian boss of the chapter (chapter 0 = Cage Warden).</summary>
        public static EnemyKind GuardianKind(int chapter)
        {
            return Guardians[Math.Max(0, Math.Min(ChapterCount - 1, chapter))];
        }

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
                case FloorKind.Guardian: return GuardianKind(Chapter(floor));
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
            b.Chapter = Chapter(floor);
            b.ChaserKind = RegularKind(b.Chapter, SpawnRole.Chaser);
            b.ShooterKind = RegularKind(b.Chapter, SpawnRole.Shooter);
            b.SpecialKind = RegularKind(b.Chapter, SpawnRole.Special);
            return b;
        }
    }
}
