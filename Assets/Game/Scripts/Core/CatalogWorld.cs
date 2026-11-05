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
            return new EnemyDefinition[]
            {
                new EnemyDefinition(EnemyKind.SandBeetle, "Bọ Cát", 30f, 8f, 0f, 0f, 2.2f, 0.45f, 0f),
                new EnemyDefinition(EnemyKind.SandEye, "Mắt Cát", 20f, 0f, 10f, 2f, 1.2f, 0.4f, 0.6f),
                new EnemyDefinition(EnemyKind.Sentinel, "Lính Gác", 45f, 0f, 15f, 3f, SentinelDashSpeed, 0.5f, 0.7f),
                new EnemyDefinition(EnemyKind.CageWarden, "Hộ Vệ Lồng", 700f, 0f, 20f, 3f, 1.5f, 1.2f, 0.9f),
                new EnemyDefinition(EnemyKind.SandQueen, "Nữ Hoàng Cát Lồng", QueenBaseHp, 0f, 18f, 3.5f, 1.8f, 2.0f, 0.7f),
                new EnemyDefinition(EnemyKind.BlueHairGuy, "Blue Hair Guy", BlueHairBaseHp, 0f, 20f, 3f, 3.0f, 1.0f, 0.9f)
            };
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

                if (beetle > 0) { spawns.Add((EnemyKind.SandBeetle, beetle)); }
                if (eye > 0) { spawns.Add((EnemyKind.SandEye, eye)); }
                if (sentinel > 0) { spawns.Add((EnemyKind.Sentinel, sentinel)); }

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
