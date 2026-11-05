namespace RHMG.Runtime.Combat
{
    /// <summary>Presentation/geometry constants of the combat runtime. Balance numbers live in RHMG.Core (B1).</summary>
    public static class CombatTuning
    {
        // SPEC-P01 §2: combat arena 36 x 56 m, camera follows the girl; station arena 14 x 14 m (SPEC-P02 §4).
        public const float ArenaWidth = 36f;
        public const float ArenaDepth = 56f;
        public const float ArenaLimitX = 17.6f;
        public const float ArenaLimitZ = 27.6f;
        public const float StationSize = 14f;
        public const float StationLimit = 6.6f;
        public const float CameraViewWidth = 15f;
        public const float CameraHeight = 12f;
        public const float CameraMinSize = 7.5f;
        public const float CameraFollowRate = 8f;
        public const float SpawnMinDistance = 11f;
        public const float SpawnMaxDistance = 14f;
        public const int SpawnTries = 8;
        public const float FloorEndCollectSeconds = 0.6f;
        public const float GemPickupRadius = 0.5f;
        public const float EliteScale = 1.4f;
        public const float EliteHpMult = 6f;
        public const float SpiritOrbitRadius = 0.9f;
        public const float SpiritOrbitHeight = 1.4f;
        public const float SpiritCastGap = 1.5f;
        public const float PlayerRadius = 0.45f;
        public const float PlayerInvulnerability = 0.6f;
        public const float BeetleContactInterval = 0.8f;
        public const float TargetRefreshInterval = 0.2f;
        public const float DogFollowOffset = 1.2f;
        public const float DogFollowSpeed = 5f;
        public const float DogReturnDistance = 7f;
        public const float DogTargetRadius = 4f;
        public const float DogBiteRadius = 1f;
        public const float DogReturnBurstRadius = 0.7f;
        public const float LionSignatureInterval = 10f;
        public const float LionSunRadius = 2.5f;
        public const float LionSunDamage = 30f;
        public const float LionMoonDashDistance = 2f;
        public const float LionMoonShield = 15f;
        public const float EyeMinDistance = 4f;
        public const float EyeMaxDistance = 6f;
        public const float EyeProjectileSpeed = 5f;
        public const float EyeTelegraphTime = 0.6f;
        public const float PlayerProjectileSpeed = 10f;
        public const float SpiritProjectileSpeed = 8f;
        public const float ProjectileRadius = 0.15f;
        public const float ProjectileVisualRadius = 0.14f;
        public const float MultishotSpreadDegrees = 12f;
        public const int MaxProjectiles = 160;
        public const float AutoEffectDuration = 1.5f;
        public const float CommandEffectDuration = 1.5f;
        public const float WardenFanRadius = 6f;
        public const float WardenFanAngle = 60f;
        public const float WardenProjectileSpeed = 6f;
        public const float WardenRecovery = 1.6f;
        public const float SentinelChargeDistance = 6f;
        public const float SentinelTelegraphWidth = 1.1f;
        public const float SpawnSafetyRadius = 3.5f;
        public const float SentinelTelegraphMinimum = 0.7f;

        // Boss presentation (damage numbers are in Core: Catalog.QueenAttacks / BlueHairAttacks).
        public const float BossFanAngle = 90f;
        public const float BossFanRadius = 6f;
        public const float BossBulletSpeed = 6.5f;
        public const float BossStopDistance = 3.5f;

        // Station (SPEC-P02 §4)
        public const float StationActivateSeconds = 1f;
        public const float StationTileRadius = 1.3f;
    }
}
