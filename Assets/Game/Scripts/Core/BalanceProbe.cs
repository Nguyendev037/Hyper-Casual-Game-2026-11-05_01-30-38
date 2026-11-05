using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    /// <summary>Pure-math balance measurements (SPEC-P01 §4, SPEC-P03 §3). No randomness, no UnityEngine.</summary>
    public static class BalanceProbe
    {
        public const double HitChance = 0.7;

        // ---------- XP / level expectation ----------

        /// <summary>Expected XP dropped by every enemy of the floor (weights, elites, boss).</summary>
        public static double ExpectedFloorXp(int floor)
        {
            SpawnBudget b = FloorPlan.Budget(floor);
            double w = b.BeetleW + b.EyeW + b.SentinelW;
            double perEnemy = (b.BeetleW * XpCurve.XpValue(EnemyKind.SandBeetle, false)
                + b.EyeW * XpCurve.XpValue(EnemyKind.SandEye, false)
                + b.SentinelW * XpCurve.XpValue(EnemyKind.Sentinel, false)) / w;
            perEnemy *= 1.0 + b.EliteChance * (SurvivorTuning.EliteXpMultiplier - 1);
            double xp = b.Total * perEnemy;
            EnemyKind? boss = FloorPlan.BossKind(floor);
            if (boss.HasValue)
            {
                xp += XpCurve.XpValue(boss.Value, false);
            }

            return xp;
        }

        /// <summary>Level after clearing floors 1..floor while collecting the given fraction of all XP.</summary>
        public static int LevelAfterFloor(int floor, double collect)
        {
            double xp = 0;
            for (int f = 1; f <= floor; f++)
            {
                xp += ExpectedFloorXp(f) * collect;
            }

            int level = 1;
            while (xp >= XpCurve.Required(level))
            {
                xp -= XpCurve.Required(level);
                level++;
            }

            return level;
        }

        /// <summary>Cards taken when arriving on the floor (level on arrival - 1).</summary>
        public static int CardsOnArrival(int floor, double collect)
        {
            return floor <= 1 ? 0 : LevelAfterFloor(floor - 1, collect) - 1;
        }

        // ---------- Reference build ----------

        private static List<string> ReferenceOrder()
        {
            // "Spread evenly in a fixed order": one card per category per round, in this category order.
            List<List<string>> cats = new List<List<string>>();
            cats.Add(Rep("upgrade.staff_dmg", 5));
            cats.Add(Rep("upgrade.atk_speed", 5));
            cats.Add(Rep("upgrade.multishot", 3));
            cats.Add(Rep("upgrade.range", 3));
            // Spirits: start with Dew (trial). Ember Lv3 + Dew Lv3 fuse into Steam, Bud joins, all Lv5.
            List<string> spirits = new List<string>();
            spirits.Add("spirit.new.spirit.ember");
            spirits.AddRange(Rep("spirit.up.spirit.dew", 2));
            spirits.AddRange(Rep("spirit.up.spirit.ember", 2));
            spirits.Add("spirit.new.spirit.bud");
            spirits.AddRange(Rep("spirit.up.spirit.steam", 2));
            spirits.AddRange(Rep("spirit.up.spirit.bud", 4));
            cats.Add(spirits);
            cats.Add(Rep("upgrade.dog_dmg", 3));
            cats.Add(Rep("upgrade.max_hp", 5));
            cats.Add(Rep("upgrade.armor", 3));
            cats.Add(Rep("upgrade.regen", 3));
            cats.Add(Rep("upgrade.magnet", 3));
            cats.Add(Rep("upgrade.move_speed", 5));

            List<string> o = new List<string>();
            for (int round = 0; ; round++)
            {
                bool any = false;
                for (int c = 0; c < cats.Count; c++)
                {
                    if (round < cats[c].Count)
                    {
                        o.Add(cats[c][round]);
                        any = true;
                    }
                }

                if (!any)
                {
                    break;
                }
            }

            return o;
        }

        private static List<string> Rep(string token, int times)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < times; i++)
            {
                list.Add(token);
            }

            return list;
        }

        private static void Add(List<string> list, string token, int times)
        {
            for (int i = 0; i < times; i++)
            {
                list.Add(token);
            }
        }

        public sealed class ReferenceBuild
        {
            public PlayerStats Stats;
            public RunSpirits Spirits;
            public int Cards;
        }

        /// <summary>The fixed reference build after its first <paramref name="cards"/> cards (SPEC-P03 §3).</summary>
        public static ReferenceBuild BuildReference(int cards)
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            RunSpirits spirits = new RunSpirits(Catalog.BaseSpiritIds);
            spirits.Add("spirit.dew");
            List<string> order = ReferenceOrder();
            for (int i = 0; i < cards && i < order.Count; i++)
            {
                string t = order[i];
                if (t.StartsWith("upgrade.", StringComparison.Ordinal))
                {
                    upgrades.Take(t);
                }
                else if (t.StartsWith("spirit.new.", StringComparison.Ordinal))
                {
                    spirits.Add(t.Substring("spirit.new.".Length));
                }
                else
                {
                    spirits.LevelUp(t.Substring("spirit.up.".Length));
                }

                string a;
                string b;
                FusionRecipe r = spirits.FusablePair(out a, out b);
                if (r != null)
                {
                    spirits.Fuse(r.Id);
                }
            }

            ReferenceBuild build = new ReferenceBuild();
            build.Cards = cards;
            build.Spirits = spirits;
            build.Stats = StatsCalculator.Compute(new string[0], upgrades.Taken, 0, false);
            return build;
        }

        public static double ReferenceMaxHp(int cards)
        {
            return BuildReference(cards).Stats.MaxHp;
        }

        /// <summary>Expected damage per second of the reference build after <paramref name="level"/> cards (hit chance 70%).</summary>
        public static double ReferenceDps(int level)
        {
            ReferenceBuild build = BuildReference(level);
            PlayerStats s = build.Stats;
            double dps = s.StaffDamage / s.AttackInterval * s.ProjectileCount;
            dps += s.DogDamage / s.DogInterval;
            double cdScale = s.CommandCooldown / Catalog.BaseCommandCooldown;
            for (int i = 0; i < build.Spirits.Slots.Count; i++)
            {
                RunSpiritEntry e = build.Spirits.Slots[i];
                SpiritDefinition d = Catalog.GetSpirit(e.spiritId);
                double dmg = SurvivorTuning.SpiritDamageMult(e.level) * s.SpiritPower;
                dps += d.AutoDamage * dmg / (d.AutoInterval * SurvivorTuning.SpiritIntervalMult(e.level));
                dps += d.CommandDamage * dmg / (d.CommandCooldown * SurvivorTuning.SpiritCooldownMult(e.level) * cdScale);
            }

            return dps * HitChance;
        }

        // ---------- Bosses ----------

        public static double BossHp(EnemyKind kind, int floor)
        {
            return Catalog.GetEnemy(kind).MaxHp * FloorPlan.Budget(floor).HpMult;
        }

        /// <summary>Seconds to kill the warden of the floor with the build expected on arrival.</summary>
        public static double GuardianTtk(int floor, double collect)
        {
            return BossHp(EnemyKind.CageWarden, floor) / ReferenceDps(CardsOnArrival(floor, collect));
        }

        /// <summary>Floor-50 queen with the build expected on arrival.</summary>
        public static double QueenTtk(double collect)
        {
            return BossHp(EnemyKind.SandQueen, 50) / ReferenceDps(CardsOnArrival(50, collect));
        }

        /// <summary>Floor-100 Blue Hair Guy with ReferenceDps(40); the three anchors add 3 x 6% HP.</summary>
        public static double FinalBossTtk()
        {
            double hp = BossHp(EnemyKind.BlueHairGuy, 100);
            hp *= 1.0 + SurvivorTuning.BlueHairAnchors * SurvivorTuning.BlueHairAnchorHpFraction;
            return hp / ReferenceDps(40);
        }

        public static float DamageScale(EnemyKind kind)
        {
            if (kind == EnemyKind.BlueHairGuy) { return SurvivorTuning.BlueHairDamageScale; }
            return kind == EnemyKind.SandQueen ? SurvivorTuning.QueenDamageScale : 1f;
        }

        /// <summary>Largest single hit the boss can land on that floor.</summary>
        public static double MaxSingleHit(EnemyKind kind, int floor)
        {
            double mult = FloorPlan.Budget(floor).DmgMult * DamageScale(kind);
            IReadOnlyList<BossAttackDef> attacks = Catalog.GetBossAttacks(kind);
            double max = 0;
            if (attacks != null)
            {
                for (int i = 0; i < attacks.Count; i++)
                {
                    max = Math.Max(max, attacks[i].Damage);
                }
            }
            else
            {
                EnemyDefinition d = Catalog.GetEnemy(kind);
                max = Math.Max(d.AttackDamage, Math.Max(d.ContactDamage, Catalog.WardenFanDamage));
            }

            return max * mult;
        }

        /// <summary>Expected damage per second if the player stands still, for the given phase (1 or 2).</summary>
        public static double IdleDps(EnemyKind kind, int floor, int phase)
        {
            double mult = FloorPlan.Budget(floor).DmgMult * DamageScale(kind);
            IReadOnlyList<BossAttackDef> attacks = Catalog.GetBossAttacks(kind);
            double dps = 0;
            if (attacks != null)
            {
                for (int i = 0; i < attacks.Count; i++)
                {
                    if (attacks[i].Phase == phase)
                    {
                        dps += attacks[i].Damage * attacks[i].Count / attacks[i].Interval;
                    }
                }
            }
            else
            {
                EnemyDefinition d = Catalog.GetEnemy(kind);
                dps = d.AttackDamage / d.AttackInterval;
            }

            return dps * mult;
        }
    }
}
