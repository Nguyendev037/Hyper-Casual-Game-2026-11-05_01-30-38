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
                new EnemyDefinition(EnemyKind.CageWarden, "Hộ Vệ Lồng", 700f, 0f, 20f, 3f, 1.5f, 1.2f, 0.9f)
            };
        }

        private static WaveDefinition Wave(float delay, params (EnemyKind kind, int count)[] spawns)
        {
            return new WaveDefinition(spawns, delay);
        }

        private static IReadOnlyList<FloorDefinition> BuildFloors()
        {
            const EnemyKind beetle = EnemyKind.SandBeetle;
            const EnemyKind eye = EnemyKind.SandEye;
            const EnemyKind sentinel = EnemyKind.Sentinel;
            const EnemyKind warden = EnemyKind.CageWarden;

            return new FloorDefinition[]
            {
                new FloorDefinition(1, false,
                    new WaveDefinition[] { Wave(0f, (beetle, 3)), Wave(10f, (beetle, 3)) },
                    "Kéo joystick để di chuyển, phép tự bắn"),
                new FloorDefinition(2, false,
                    new WaveDefinition[] { Wave(0f, (beetle, 4), (eye, 1)), Wave(10f, (beetle, 2), (eye, 1)) },
                    "Mắt Cát bắn từ xa, hãy né"),
                new FloorDefinition(3, false,
                    new WaveDefinition[] { Wave(0f, (beetle, 2), (sentinel, 2)), Wave(10f, (beetle, 2), (sentinel, 1)) },
                    "Lính Gác sẽ lao tới sau cảnh báo"),
                new FloorDefinition(4, false,
                    new WaveDefinition[]
                    {
                        Wave(0f, (beetle, 2), (eye, 2), (sentinel, 1)),
                        Wave(10f, (beetle, 1), (eye, 1), (sentinel, 1))
                    },
                    "Dùng nút lệnh khi bị vây"),
                new FloorDefinition(5, true,
                    new WaveDefinition[] { Wave(0f, (warden, 1), (beetle, 2)), Wave(12f, (beetle, 2)) },
                    "Hộ vệ tầng 5: tránh vòng đập và quạt đạn")
            };
        }
    }
}
