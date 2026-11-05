using System.Collections.Generic;

namespace RHMG.Core
{
    public static partial class Catalog
    {
        // CageWarden extra attack data (EnemyDefinition holds the ring attack).
        public const float WardenRingRadius = 3f;
        public const int WardenFanProjectiles = 7;
        public const float WardenFanDamage = 10f;
        public const float WardenFanTelegraph = 0.9f;
        public const float WardenEnrageHpFraction = 0.5f;
        public const float WardenEnrageRate = 1.35f;

        // Sentinel dash data (EnemyDefinition.MoveSpeed holds the dash speed).
        public const float SentinelDashSpeed = 9f;

        // Boss base HP BEFORE HpMult(floor); tuned by BalanceProbeTests (SPEC-P03 §3).
        public const float QueenBaseHp = 1700f;
        public const float BlueHairBaseHp = 1700f;

        // Plan 05 chapter enemy numbers (SPEC-P02 section 1, 01-CONTRACTS section 2). Runtime must not invent its own (B1).
        public const float EmberSpeedMult = 1.3f, WolfSpeedMult = 1.4f;
        public const float GolemHpMult = 2.2f, GolemSpeedMult = 0.6f, GolemSlamRadius = 1.8f;
        public const float GolemSlamInterval = 3.5f, GolemSlamTelegraph = 0.8f;
        public const float SlimeChildHpFraction = 0.35f, SlimeChildScale = 0.6f;
        public const float TeleportInterval = 4f, TeleportDistance = 4f, TeleportTelegraph = 0.6f;
        public const float KnightShieldReduction = 0.6f, KnightDashSpeed = 9f, KnightChargeDistance = 7f;
        public const float ArcherRange = 8f, ArcherProjectileSpeed = 8f;
        public const float TurretFanArc = 30f;
        public const int TurretFanCount = 3;
        public const float MiteTriggerRadius = 1.6f, MiteFuse = 0.7f, MiteBlastRadius = 1.5f, MiteBlastEnemyFraction = 0.5f;
        public const float MiteBlastDamage = 18f;
        public const float WispOrbitRadius = 5f, WispRingInterval = 3f, WispTelegraph = 0.6f, WispRingSpeed = 4f;
        public const int WispRingCount = 6;
        public const float VoidBeamLength = 9f, VoidBeamTelegraph = 0.9f, VoidBeamDuration = 0.4f;
        public const float FlyerZigzagAmplitude = 1.5f, FlyerZigzagPeriod = 1.6f;
        public const float WolfPairOffset = 1.2f;

        public static readonly IReadOnlyList<EnemyDefinition> Enemies = BuildEnemies();
        public static readonly IReadOnlyList<FloorDefinition> Floors = BuildFloors();

        public static EnemyDefinition GetEnemy(EnemyKind kind)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].Kind == kind)
                {
                    return Enemies[i];
                }
            }

            return null;
        }

        public static FloorDefinition GetFloor(int index)
        {
            for (int i = 0; i < Floors.Count; i++)
            {
                if (Floors[i].Index == index)
                {
                    return Floors[i];
                }
            }

            return null;
        }

        private static IReadOnlyList<EnemyDefinition> BuildEnemies()
        {
            const EnemyKind B = EnemyKind.SandBeetle, E = EnemyKind.SandEye, S = EnemyKind.Sentinel, W = EnemyKind.CageWarden;
            return new EnemyDefinition[]
            {
                new EnemyDefinition(EnemyKind.SandBeetle, "Bọ Cát", 30f, 8f, 0f, 0f, 2.2f, 0.45f, 0f, EnemyArchetype.Chaser),
                new EnemyDefinition(EnemyKind.SandEye, "Mắt Cát", 20f, 0f, 10f, 2f, 1.2f, 0.4f, 0.6f, EnemyArchetype.Shooter),
                new EnemyDefinition(EnemyKind.Sentinel, "Lính Gác", 45f, 0f, 15f, 3f, SentinelDashSpeed, 0.5f, 0.7f,
                    EnemyArchetype.Charger),
                new EnemyDefinition(EnemyKind.CageWarden, "Hộ Vệ Lồng", 700f, 0f, 20f, 3f, 1.5f, 1.2f, 0.9f, EnemyArchetype.Boss),
                new EnemyDefinition(EnemyKind.SandQueen, "Nữ Hoàng Cát Lồng", QueenBaseHp, 0f, 18f, 3.5f, 1.8f, 2.0f, 0.7f,
                    EnemyArchetype.Boss, 2),
                new EnemyDefinition(EnemyKind.BlueHairGuy, "Blue Hair Guy", BlueHairBaseHp, 0f, 20f, 3f, 3.0f, 1.0f, 0.9f,
                    EnemyArchetype.Boss, 4),
                // Chapter 1 (Beetle / Eye / Sentinel slots). Stats = chapter 0 role base x the factor of 01-CONTRACTS section 2.
                new EnemyDefinition(EnemyKind.EmberImp, "Quỷ Than", 30f, 8f, 0f, 0f, 2.2f * EmberSpeedMult, 0.45f, 0f,
                    EnemyArchetype.Chaser, 1, B, B),
                new EnemyDefinition(EnemyKind.SparkTurret, "Tháp Tia Lửa", 20f, 0f, 10f, 2f, 1.2f, 0.4f, 0.6f,
                    EnemyArchetype.Shooter, 1, E, E),
                new EnemyDefinition(EnemyKind.AnvilGolem, "Người Đe", 45f * GolemHpMult, 0f, 15f, GolemSlamInterval,
                    2.2f * GolemSpeedMult, 0.65f, GolemSlamTelegraph, EnemyArchetype.Tank, 1, S, S),
                // Chapter 2
                new EnemyDefinition(EnemyKind.BookBat, "Dơi Sách", 30f, 8f, 0f, 0f, 2.2f, 0.45f, 0f,
                    EnemyArchetype.Flyer, 2, B, B),
                new EnemyDefinition(EnemyKind.MirrorGhost, "Bóng Gương", 20f, 0f, 10f, TeleportInterval, 1.2f, 0.4f,
                    TeleportTelegraph, EnemyArchetype.Teleporter, 2, E, E),
                new EnemyDefinition(EnemyKind.InkSlime, "Slime Mực", 45f, 8f, 0f, 0f, 1.6f, 0.5f, 0f,
                    EnemyArchetype.Splitter, 2, S, S),
                // Chapter 3 (MoveSpeed of PlagueKnight is the walking speed; the dash uses KnightDashSpeed)
                new EnemyDefinition(EnemyKind.PlagueKnight, "Hiệp Sĩ Dịch", 45f, 0f, 15f, 3.5f, 1.4f, 0.55f, 0.7f,
                    EnemyArchetype.Charger, 3, S, S),
                new EnemyDefinition(EnemyKind.BoneArcher, "Xạ Thủ Xương", 20f, 0f, 10f, 2.2f, 1.2f, 0.4f, 0.6f,
                    EnemyArchetype.Shooter, 3, E, E),
                new EnemyDefinition(EnemyKind.BombMite, "Mạt Bom", 30f, 0f, MiteBlastDamage, 0f, 2.2f, 0.4f, MiteFuse,
                    EnemyArchetype.Exploder, 3, B, B),
                // Chapter 4
                new EnemyDefinition(EnemyKind.ShadowWolf, "Sói Bóng", 30f, 8f, 0f, 0f, 2.2f * WolfSpeedMult, 0.45f, 0f,
                    EnemyArchetype.Chaser, 4, B, B),
                new EnemyDefinition(EnemyKind.StarWisp, "Tinh Quang", 20f, 0f, 8f, WispRingInterval, 2.0f, 0.4f, WispTelegraph,
                    EnemyArchetype.Orbiter, 4, E, E),
                new EnemyDefinition(EnemyKind.VoidEye, "Mắt Hư Không", 45f, 0f, 15f, 4.5f, 1.0f, 0.5f, VoidBeamTelegraph,
                    EnemyArchetype.Beam, 4, S, S),
                // Chapter guardians: same 700 HP / 20 damage as the Cage Warden, so BalanceProbe TTK is unchanged.
                new EnemyDefinition(EnemyKind.ForgeGolem, "Golem Lò Rèn", 700f, 0f, 20f, 3f, 1.5f, 1.3f, 0.9f,
                    EnemyArchetype.Boss, 1, W, W),
                new EnemyDefinition(EnemyKind.MirrorWitch, "Phù Thủy Gương", 700f, 0f, 20f, 3f, 1.5f, 1.2f, 0.9f,
                    EnemyArchetype.Boss, 2, W, W),
                new EnemyDefinition(EnemyKind.CorruptKnight, "Hiệp Sĩ Sa Ngã", 700f, 0f, 20f, 3f, 1.5f, 1.2f, 0.9f,
                    EnemyArchetype.Boss, 3, W, W),
                new EnemyDefinition(EnemyKind.TwilightDrake, "Rồng Hoàng Hôn", 700f, 0f, 20f, 3f, 1.5f, 1.4f, 0.9f,
                    EnemyArchetype.Boss, 4, W, W)
            };
        }

        /// <summary>Extra attack of a chapter guardian on top of the Cage Warden kit (01-CONTRACTS section 3); null otherwise.
        /// Damage is BEFORE DmgMult(floor) and stays at or below the warden slam so MaxSingleHit is unchanged.</summary>
        public static BossAttackDef GuardianExtra(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.ForgeGolem: return new BossAttackDef("rain", 1, 18f, 5, 7f, 1.0f);
                case EnemyKind.MirrorWitch: return new BossAttackDef("mirrors", 1, 10f, 5, 7f, 0.9f);
                case EnemyKind.CorruptKnight: return new BossAttackDef("triple_dash", 1, 16f, 3, 8f, 0.7f);
                case EnemyKind.TwilightDrake: return new BossAttackDef("breath", 1, 9f, 16, 8f, 0.9f);
                default: return null;
            }
        }

        private static string HintFor(int floor)
        {
            switch (floor)
            {
                case 1: return "Kéo joystick để di chuyển, phép tự bắn";
                case 2: return "Mắt Cát bắn từ xa, hãy né";
                case 3: return "Lính Gác sẽ lao tới sau cảnh báo";
                case 4: return "Đi lại gần linh thạch để nhặt, đủ linh thạch sẽ lên cấp";
                case 5: return "Hộ vệ tầng 5: tránh vòng đập và quạt đạn";
                case 50: return "Nữ Hoàng Cát Lồng: tránh vạch đỏ báo trước";
                case 100: return "Blue Hair Guy: phá ba neo để làm anh choáng";
                default: return null;
            }
        }

        // One wave per floor holding the EXPECTED composition of SpawnBudget (the runtime spawns by weights).
        private static IReadOnlyList<FloorDefinition> BuildFloors()
        {
            FloorDefinition[] floors = new FloorDefinition[SurvivorTuning.FloorCount];
            for (int f = 1; f <= SurvivorTuning.FloorCount; f++)
            {
                SpawnBudget b = FloorPlan.Budget(f);
                float sum = b.BeetleW + b.EyeW + b.SentinelW;
                int eye = (int)System.Math.Round(b.Total * b.EyeW / sum, System.MidpointRounding.AwayFromZero);
                int sentinel = (int)System.Math.Round(b.Total * b.SentinelW / sum, System.MidpointRounding.AwayFromZero);
                int beetle = System.Math.Max(0, b.Total - eye - sentinel);

                List<(EnemyKind kind, int count)> spawns = new List<(EnemyKind kind, int count)>();
                EnemyKind? boss = FloorPlan.BossKind(f);
                if (boss.HasValue)
                {
                    spawns.Add((boss.Value, 1));
                }

                if (beetle > 0) { spawns.Add((b.ChaserKind, beetle)); }
                if (eye > 0) { spawns.Add((b.ShooterKind, eye)); }
                if (sentinel > 0) { spawns.Add((b.SpecialKind, sentinel)); }

                floors[f - 1] = new FloorDefinition(
                    f,
                    FloorPlan.Kind(f) != FloorKind.Normal,
                    new WaveDefinition[] { new WaveDefinition(spawns, 0f) },
                    HintFor(f));
            }

            return floors;
        }
    }
}
