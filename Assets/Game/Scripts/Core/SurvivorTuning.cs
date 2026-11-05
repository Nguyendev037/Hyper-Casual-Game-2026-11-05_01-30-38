using System;

namespace RHMG.Core
{
    /// <summary>Every balance number of plan 04 (SPEC-P01..P03). Runtime must not invent its own (B1).</summary>
    public static class SurvivorTuning
    {
        public const int FloorCount = 100, StationEvery = 5, SpiritSlots = 3, SpiritMaxLevel = 5, CardsPerRoll = 3;
        public const float BaseRange = 9f, BaseMagnetRadius = 1.6f, MagnetPullSpeed = 9f;
        public const int MaxAliveEnemies = 120, MaxGemsOnField = 300;

        // --- XP curve: Required(L) = round(XpBase + XpLinear * L + XpQuad * L^2) (SPEC-P01 §4) ---
        public const double XpBase = 5.0, XpLinear = 2.0, XpQuad = 0.24;
        public const float XpChestFraction = 0.6f;
        public const int XpBeetle = 1, XpEye = 2, XpSentinel = 3, XpWarden = 40, XpQueen = 120, XpBlueHair = 0;
        public const int EliteXpMultiplier = 5;

        // --- Floor budget (SPEC-P01 §3) ---
        public const float BossFloorSpawnFactor = 0.5f;
        public const float SpawnTimeFraction = 0.8f;

        // --- Spirits (SPEC-P02 §2) ---
        public const float SpiritDamagePerLevel = 0.25f, SpiritIntervalPerLevel = 0.05f, SpiritCooldownPerLevel = 0.06f;
        public const int FusionMinLevel = 3;

        // --- Station (SPEC-P02 §4) ---
        public const float StationHealFraction = 0.5f;
        public const int StationGoldNoRescue = 25;
        public const int FirstNoRescueStation = 20;

        // --- Bosses (SPEC-P03) ---
        public const float BossPerHitMaxFraction = 0.40f;
        public const float BossIdleDpsMinFractionPer10s = 0.25f;
        public const float BlueHairAnchorHpFraction = 0.06f;
        public const int BlueHairAnchors = 3;
        /// <summary>Final-boss damage scale (decision 2026-11-05: table x DmgMult(100) would exceed 40% of the reference HP).</summary>
        public const float BlueHairDamageScale = 0.6f;
        /// <summary>Queen damage scale (decision 2026-11-05: slam 35 x DmgMult(50) = 69.3 exceeded 40% of the 160 HP reference).</summary>
        public const float QueenDamageScale = 0.9f;

        public const float BossPhaseInvulnerable = 1.5f;
        public const float StormStartRadius = 14f;
        public const float StormEndRadius = 8f;
        public const float StormShrinkSeconds = 20f;
        public const float StormWarnSeconds = 2f;
        public const float StormDamageFractionPerSecond = 0.08f;
        public const float PurifyWindowSeconds = 8f;
        public const float PurifyDamageMult = 3f;
        public const float AnchorRespawnSeconds = 4f;
        public const float AnchorWarnSeconds = 1f;
        public const float KneelSeconds = 2f;
        public const float DayNightForceSeconds = 10f;
        public const float PhaseRateMult = 1.3f;


        public static float SpiritDamageMult(int level)
        {
            return 1f + SpiritDamagePerLevel * (level - 1);
        }

        public static float SpiritIntervalMult(int level)
        {
            return 1f - SpiritIntervalPerLevel * (level - 1);
        }

        public static float SpiritCooldownMult(int level)
        {
            return 1f - SpiritCooldownPerLevel * (level - 1);
        }

        /// <summary>Base spirit rescued at the station after this floor, or null (SPEC-P02 §2).</summary>
        public static string RescueSpiritAt(int floor)
        {
            switch (floor)
            {
                case 5: return "spirit.ember";
                case 10: return "spirit.dew";
                case 15: return "spirit.bud";
                default: return null;
            }
        }
    }
}
