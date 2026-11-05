using System.Collections.Generic;

namespace RHMG.Core
{
    /// <summary>One damaging boss attack (SPEC-P03 §2, §2b). Damage is BEFORE DmgMult(floor).</summary>
    public sealed class BossAttackDef
    {
        public readonly string Name;
        public readonly int Phase;
        public readonly float Damage;
        public readonly int Count;
        public readonly float Interval;
        public readonly float Telegraph;

        public BossAttackDef(string name, int phase, float damage, int count, float interval, float telegraph)
        {
            Name = name;
            Phase = phase;
            Damage = damage;
            Count = count;
            Interval = interval;
            Telegraph = telegraph;
        }
    }

    public static partial class Catalog
    {
        public const float BossMinTelegraph = 0.6f;
        public const float BossEnrageSeconds = 180f;
        public const float BossEnrageWarnSeconds = 150f;
        public const float BossEnrageDamageMult = 2f;

        public static readonly IReadOnlyList<BossAttackDef> QueenAttacks = new BossAttackDef[]
        {
            new BossAttackDef("fan", 1, 18f, 9, 3.5f, 0.7f),
            new BossAttackDef("dash", 1, 30f, 1, 8f, 0.8f),
            new BossAttackDef("spiral", 2, 14f, 36, 9f, 0.6f),
            new BossAttackDef("slam", 2, 35f, 1, 7f, 1.0f),
            new BossAttackDef("pillars", 2, 28f, 3, 6f, 1.0f)
        };

        public static readonly IReadOnlyList<BossAttackDef> BlueHairAttacks = new BossAttackDef[]
        {
            new BossAttackDef("fan", 1, 20f, 7, 3f, 0.9f),
            new BossAttackDef("dash", 1, 32f, 1, 7f, 0.9f),
            new BossAttackDef("ring", 1, 30f, 1, 9f, 1.0f),
            new BossAttackDef("spiral", 2, 14f, 36, 8f, 0.6f),
            new BossAttackDef("pillars", 2, 28f, 3, 6f, 1.0f)
        };

        /// <summary>Attack tables of the two scripted bosses; null for other kinds.</summary>
        public static IReadOnlyList<BossAttackDef> GetBossAttacks(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.SandQueen: return QueenAttacks;
                case EnemyKind.BlueHairGuy: return BlueHairAttacks;
                default: return null;
            }
        }
    }
}
