using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // Plan 05 WE: chapters, 12 chapter enemies, 4 chapter guardians (SPEC-P02 section 4, 01-CONTRACTS sections 1-3).
    public class ChapterEnemyTests
    {
        private static readonly EnemyKind[] NewRegular =
        {
            EnemyKind.EmberImp, EnemyKind.SparkTurret, EnemyKind.AnvilGolem, EnemyKind.BookBat, EnemyKind.MirrorGhost,
            EnemyKind.InkSlime, EnemyKind.PlagueKnight, EnemyKind.BoneArcher, EnemyKind.BombMite, EnemyKind.ShadowWolf,
            EnemyKind.StarWisp, EnemyKind.VoidEye
        };

        private static readonly EnemyKind[] NewGuardians =
        {
            EnemyKind.ForgeGolem, EnemyKind.MirrorWitch, EnemyKind.CorruptKnight, EnemyKind.TwilightDrake
        };

        [Test]
        public void Chapter_IsFloorMinusOneOverTwenty()
        {
            Assert.AreEqual(0, FloorPlan.Chapter(1));
            Assert.AreEqual(0, FloorPlan.Chapter(20));
            Assert.AreEqual(1, FloorPlan.Chapter(21));
            Assert.AreEqual(1, FloorPlan.Chapter(40));
            Assert.AreEqual(2, FloorPlan.Chapter(41));
            Assert.AreEqual(3, FloorPlan.Chapter(61));
            Assert.AreEqual(3, FloorPlan.Chapter(80));
            Assert.AreEqual(4, FloorPlan.Chapter(81));
            Assert.AreEqual(4, FloorPlan.Chapter(100));
            for (int f = 1; f <= 100; f++)
            {
                Assert.AreEqual((f - 1) / 20, FloorPlan.Chapter(f), "floor " + f);
            }
        }

        [Test]
        public void EveryEnemyKind_HasADefinitionWithArchetypeAndChapter()
        {
            foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
            {
                EnemyDefinition d = Catalog.GetEnemy(kind);
                Assert.IsNotNull(d, kind.ToString());
                Assert.That(d.Chapter, Is.InRange(0, 4), kind.ToString());
                Assert.IsTrue(d.MaxHp > 0f && d.Radius > 0f && d.MoveSpeed > 0f, kind.ToString());
            }

            Assert.AreEqual(EnemyArchetype.Chaser, Catalog.GetEnemy(EnemyKind.SandBeetle).Archetype);
            Assert.AreEqual(EnemyArchetype.Shooter, Catalog.GetEnemy(EnemyKind.SandEye).Archetype);
            Assert.AreEqual(EnemyArchetype.Charger, Catalog.GetEnemy(EnemyKind.Sentinel).Archetype);
        }

        [Test]
        public void ArchetypeTable_MatchesContract()
        {
            EnemyArchetype[] expected =
            {
                EnemyArchetype.Chaser, EnemyArchetype.Shooter, EnemyArchetype.Tank, EnemyArchetype.Flyer,
                EnemyArchetype.Teleporter, EnemyArchetype.Splitter, EnemyArchetype.Charger, EnemyArchetype.Shooter,
                EnemyArchetype.Exploder, EnemyArchetype.Chaser, EnemyArchetype.Orbiter, EnemyArchetype.Beam
            };
            int[] chapters = { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4 };
            for (int i = 0; i < NewRegular.Length; i++)
            {
                EnemyDefinition d = Catalog.GetEnemy(NewRegular[i]);
                Assert.AreEqual(expected[i], d.Archetype, NewRegular[i].ToString());
                Assert.AreEqual(chapters[i], d.Chapter, NewRegular[i].ToString());
            }

            for (int i = 0; i < NewGuardians.Length; i++)
            {
                EnemyDefinition d = Catalog.GetEnemy(NewGuardians[i]);
                Assert.AreEqual(EnemyArchetype.Boss, d.Archetype, NewGuardians[i].ToString());
                Assert.AreEqual(i + 1, d.Chapter, NewGuardians[i].ToString());
            }
        }

        [Test]
        public void EachChapter_HasThreeDistinctOwnRegularKinds()
        {
            HashSet<EnemyKind> seen = new HashSet<EnemyKind>();
            for (int c = 0; c < 5; c++)
            {
                EnemyKind chaser = FloorPlan.RegularKind(c, SpawnRole.Chaser);
                EnemyKind shooter = FloorPlan.RegularKind(c, SpawnRole.Shooter);
                EnemyKind special = FloorPlan.RegularKind(c, SpawnRole.Special);
                Assert.AreEqual(3, new HashSet<EnemyKind> { chaser, shooter, special }.Count, "chapter " + c);
                foreach (EnemyKind k in new[] { chaser, shooter, special })
                {
                    Assert.AreEqual(c, Catalog.GetEnemy(k).Chapter, "chapter " + c + " " + k);
                    Assert.IsTrue(seen.Add(k), "kind reused across chapters: " + k);
                }
            }

            Assert.AreEqual(15, seen.Count);
        }

        [Test]
        public void Budget_UsesTheChapterRoster_ForEveryFloor()
        {
            for (int f = 1; f <= 100; f++)
            {
                SpawnBudget b = FloorPlan.Budget(f);
                int c = (f - 1) / 20;
                Assert.AreEqual(c, b.Chapter, "floor " + f);
                Assert.AreEqual(FloorPlan.RegularKind(c, SpawnRole.Chaser), b.ChaserKind, "floor " + f);
                Assert.AreEqual(FloorPlan.RegularKind(c, SpawnRole.Shooter), b.ShooterKind, "floor " + f);
                Assert.AreEqual(FloorPlan.RegularKind(c, SpawnRole.Special), b.SpecialKind, "floor " + f);
            }
        }

        [Test]
        public void Budget_NumbersAreTheSameAsPlan04_RegardlessOfChapter()
        {
            for (int f = 1; f <= 100; f++)
            {
                SpawnBudget b = FloorPlan.Budget(f);
                bool boss = FloorPlan.Kind(f) != FloorKind.Normal;
                double total = (8.0 + 0.6 * f) * (boss ? SurvivorTuning.BossFloorSpawnFactor : 1.0);
                Assert.AreEqual((int)Math.Round(total, MidpointRounding.AwayFromZero), b.Total, "floor " + f);
                Assert.AreEqual(1f, b.BeetleW, 1e-6f);
                Assert.AreEqual(f >= 3 ? Math.Min(0.4f, 0.02f * f) : 0f, b.EyeW, 1e-6f, "floor " + f);
                Assert.AreEqual(f >= 6 ? Math.Min(0.35f, 0.015f * f) : 0f, b.SentinelW, 1e-6f, "floor " + f);
                Assert.AreEqual(1f + 0.07f * (f - 1), b.HpMult, 1e-4f);
            }
        }

        [Test]
        public void FloorWaves_ContainOnlyTheChapterRosterAndBoss_AndSumToTotal()
        {
            for (int f = 1; f <= 100; f++)
            {
                SpawnBudget b = FloorPlan.Budget(f);
                FloorDefinition floor = Catalog.GetFloor(f);
                int regular = 0;
                foreach ((EnemyKind kind, int count) spawn in floor.Waves[0].Spawns)
                {
                    if (EnemyKinds.IsBoss(spawn.kind))
                    {
                        Assert.AreEqual(FloorPlan.BossKind(f), spawn.kind, "floor " + f);
                        Assert.AreEqual(1, spawn.count);
                        continue;
                    }

                    Assert.That(spawn.kind == b.ChaserKind || spawn.kind == b.ShooterKind || spawn.kind == b.SpecialKind,
                        "floor " + f + " has foreign kind " + spawn.kind);
                    regular += spawn.count;
                }

                Assert.AreEqual(b.Total, regular, "floor " + f);
            }
        }

        [Test]
        public void ChapterOne_GuardianAndFloor21_SpawnEmberImpFamily()
        {
            Assert.AreEqual(EnemyKind.EmberImp, FloorPlan.Budget(21).ChaserKind);
            Assert.AreEqual(EnemyKind.SparkTurret, FloorPlan.Budget(21).ShooterKind);
            Assert.AreEqual(EnemyKind.AnvilGolem, FloorPlan.Budget(21).SpecialKind);
            Assert.AreEqual(EnemyKind.Sentinel, FloorPlan.Budget(20).SpecialKind);
        }

        [Test]
        public void Guardians_FollowTheChapterTable()
        {
            for (int f = 5; f <= 95; f += 5)
            {
                if (FloorPlan.Kind(f) != FloorKind.Guardian)
                {
                    continue;
                }

                EnemyKind expected;
                if (f <= 20) { expected = EnemyKind.CageWarden; }
                else if (f <= 40) { expected = EnemyKind.ForgeGolem; }
                else if (f <= 60) { expected = EnemyKind.MirrorWitch; }
                else if (f <= 80) { expected = EnemyKind.CorruptKnight; }
                else { expected = EnemyKind.TwilightDrake; }
                Assert.AreEqual(expected, FloorPlan.BossKind(f), "floor " + f);
            }

            Assert.AreEqual(EnemyKind.CageWarden, FloorPlan.BossKind(20));
            Assert.AreEqual(EnemyKind.ForgeGolem, FloorPlan.BossKind(25));
            Assert.AreEqual(EnemyKind.ForgeGolem, FloorPlan.BossKind(40));
            Assert.AreEqual(EnemyKind.MirrorWitch, FloorPlan.BossKind(45));
            Assert.AreEqual(EnemyKind.SandQueen, FloorPlan.BossKind(50));
            Assert.AreEqual(EnemyKind.MirrorWitch, FloorPlan.BossKind(55));
            Assert.AreEqual(EnemyKind.MirrorWitch, FloorPlan.BossKind(60));
            Assert.AreEqual(EnemyKind.CorruptKnight, FloorPlan.BossKind(65));
            Assert.AreEqual(EnemyKind.CorruptKnight, FloorPlan.BossKind(80));
            Assert.AreEqual(EnemyKind.TwilightDrake, FloorPlan.BossKind(85));
            Assert.AreEqual(EnemyKind.TwilightDrake, FloorPlan.BossKind(95));
            Assert.AreEqual(EnemyKind.BlueHairGuy, FloorPlan.BossKind(100));
            Assert.IsNull(FloorPlan.BossKind(24));
        }

        [Test]
        public void NewGuardians_TtkAtExpectedLevel_Is25To60Seconds()
        {
            foreach (double collect in new[] { 1.0, 0.85 })
            {
                for (int floor = 5; floor <= 95; floor += 5)
                {
                    EnemyKind? boss = FloorPlan.BossKind(floor);
                    if (FloorPlan.Kind(floor) != FloorKind.Guardian || !boss.HasValue)
                    {
                        continue;
                    }

                    double ttk = BalanceProbe.BossHp(boss.Value, floor) /
                        BalanceProbe.ReferenceDps(BalanceProbe.CardsOnArrival(floor, collect));
                    Assert.That(ttk, Is.InRange(25.0, 60.0), boss.Value + " floor " + floor + " collect " + collect + " TTK " + ttk);
                    Assert.AreEqual(BalanceProbe.GuardianTtk(floor, collect), ttk, 1e-6, "floor " + floor);
                }
            }
        }

        [Test]
        public void NewGuardians_NoSingleHit_ExceedsFortyPercentOfReferenceMaxHp()
        {
            double limit = SurvivorTuning.BossPerHitMaxFraction * BalanceProbe.ReferenceMaxHp(40);
            for (int floor = 25; floor <= 95; floor += 5)
            {
                EnemyKind? boss = FloorPlan.BossKind(floor);
                if (FloorPlan.Kind(floor) != FloorKind.Guardian || !boss.HasValue)
                {
                    continue;
                }

                Assert.LessOrEqual(BalanceProbe.MaxSingleHit(boss.Value, floor), limit, boss.Value + " floor " + floor);
                BossAttackDef extra = Catalog.GuardianExtra(boss.Value);
                if (extra != null)
                {
                    Assert.LessOrEqual(extra.Damage, Catalog.GetEnemy(boss.Value).AttackDamage, boss.Value.ToString());
                }
            }
        }

        [Test]
        public void GuardianExtras_AreTelegraphed_AtLeastSixTenths_AndBigAreasNineTenths()
        {
            foreach (EnemyKind kind in NewGuardians)
            {
                BossAttackDef extra = Catalog.GuardianExtra(kind);
                Assert.IsNotNull(extra, kind.ToString());
                Assert.GreaterOrEqual(extra.Telegraph, Catalog.BossMinTelegraph, kind.ToString());
            }

            Assert.AreEqual("rain", Catalog.GuardianExtra(EnemyKind.ForgeGolem).Name);
            Assert.AreEqual(5, Catalog.GuardianExtra(EnemyKind.ForgeGolem).Count);
            Assert.AreEqual(1.0f, Catalog.GuardianExtra(EnemyKind.ForgeGolem).Telegraph, 1e-6f);
            Assert.AreEqual("mirrors", Catalog.GuardianExtra(EnemyKind.MirrorWitch).Name);
            Assert.AreEqual("triple_dash", Catalog.GuardianExtra(EnemyKind.CorruptKnight).Name);
            Assert.AreEqual(3, Catalog.GuardianExtra(EnemyKind.CorruptKnight).Count);
            Assert.AreEqual(0.7f, Catalog.GuardianExtra(EnemyKind.CorruptKnight).Telegraph, 1e-6f);
            Assert.AreEqual("breath", Catalog.GuardianExtra(EnemyKind.TwilightDrake).Name);
            Assert.AreEqual(0.9f, Catalog.GuardianExtra(EnemyKind.TwilightDrake).Telegraph, 1e-6f);
            Assert.GreaterOrEqual(Catalog.GuardianExtra(EnemyKind.MirrorWitch).Telegraph, 0.9f);
            Assert.IsNull(Catalog.GuardianExtra(EnemyKind.CageWarden));
            Assert.IsNull(Catalog.GuardianExtra(EnemyKind.SandQueen));
        }

        [Test]
        public void TelegraphedArchetypes_WarnAtLeastSixTenths()
        {
            EnemyArchetype[] needWarning =
            {
                EnemyArchetype.Shooter, EnemyArchetype.Charger, EnemyArchetype.Tank, EnemyArchetype.Teleporter,
                EnemyArchetype.Exploder, EnemyArchetype.Orbiter, EnemyArchetype.Beam
            };
            foreach (EnemyKind kind in NewRegular)
            {
                EnemyDefinition d = Catalog.GetEnemy(kind);
                if (Array.IndexOf(needWarning, d.Archetype) >= 0)
                {
                    Assert.GreaterOrEqual(d.TelegraphTime, 0.6f, kind.ToString());
                }
            }

            Assert.AreEqual(0.7f, Catalog.GetEnemy(EnemyKind.PlagueKnight).TelegraphTime, 1e-6f);
            Assert.AreEqual(0.8f, Catalog.GetEnemy(EnemyKind.AnvilGolem).TelegraphTime, 1e-6f);
            Assert.AreEqual(0.7f, Catalog.GetEnemy(EnemyKind.BombMite).TelegraphTime, 1e-6f);
            Assert.AreEqual(0.9f, Catalog.GetEnemy(EnemyKind.VoidEye).TelegraphTime, 1e-6f);
            Assert.AreEqual(0.6f, Catalog.GetEnemy(EnemyKind.MirrorGhost).TelegraphTime, 1e-6f);
        }

        [Test]
        public void ChapterStats_AreChapterZeroRoleBaseTimesTheContractFactor()
        {
            EnemyDefinition beetle = Catalog.GetEnemy(EnemyKind.SandBeetle);
            EnemyDefinition sentinel = Catalog.GetEnemy(EnemyKind.Sentinel);
            Assert.AreEqual(beetle.MoveSpeed * 1.3f, Catalog.GetEnemy(EnemyKind.EmberImp).MoveSpeed, 1e-5f);
            Assert.AreEqual(beetle.MoveSpeed * 1.4f, Catalog.GetEnemy(EnemyKind.ShadowWolf).MoveSpeed, 1e-5f);
            Assert.AreEqual(sentinel.MaxHp * 2.2f, Catalog.GetEnemy(EnemyKind.AnvilGolem).MaxHp, 1e-4f);
            Assert.AreEqual(beetle.MoveSpeed * 0.6f, Catalog.GetEnemy(EnemyKind.AnvilGolem).MoveSpeed, 1e-5f);
            Assert.AreEqual(1.8f, Catalog.GolemSlamRadius, 1e-6f);
            Assert.AreEqual(3.5f, Catalog.GolemSlamInterval, 1e-6f);
            Assert.AreEqual(0.35f, Catalog.SlimeChildHpFraction, 1e-6f);
            Assert.AreEqual(0.6f, Catalog.SlimeChildScale, 1e-6f);
            Assert.AreEqual(4f, Catalog.TeleportInterval, 1e-6f);
            Assert.AreEqual(4f, Catalog.TeleportDistance, 1e-6f);
            Assert.AreEqual(0.6f, Catalog.KnightShieldReduction, 1e-6f);
            Assert.AreEqual(8f, Catalog.ArcherRange, 1e-6f);
            Assert.AreEqual(3, Catalog.TurretFanCount);
            Assert.AreEqual(1.6f, Catalog.MiteTriggerRadius, 1e-6f);
            Assert.AreEqual(1.5f, Catalog.MiteBlastRadius, 1e-6f);
            Assert.AreEqual(0.5f, Catalog.MiteBlastEnemyFraction, 1e-6f);
            Assert.AreEqual(5f, Catalog.WispOrbitRadius, 1e-6f);
            Assert.AreEqual(6, Catalog.WispRingCount);
            Assert.AreEqual(3f, Catalog.WispRingInterval, 1e-6f);
            Assert.AreEqual(9f, Catalog.VoidBeamLength, 1e-6f);
            Assert.AreEqual(0.4f, Catalog.VoidBeamDuration, 1e-6f);
            Assert.AreEqual(1.5f, Catalog.FlyerZigzagAmplitude, 1e-6f);
        }

        [Test]
        public void ChapterEnemies_DropTheXpOfTheirRole_SoTheXpBudgetIsPlan04()
        {
            foreach (EnemyKind kind in NewRegular)
            {
                EnemyDefinition d = Catalog.GetEnemy(kind);
                Assert.That(d.XpKind, Is.EqualTo(EnemyKind.SandBeetle).Or.EqualTo(EnemyKind.SandEye)
                    .Or.EqualTo(EnemyKind.Sentinel), kind.ToString());
            }

            for (int f = 1; f <= 100; f++)
            {
                SpawnBudget b = FloorPlan.Budget(f);
                double w = b.BeetleW + b.EyeW + b.SentinelW;
                double perEnemy = (b.BeetleW * XpCurve.XpValue(Catalog.GetEnemy(b.ChaserKind).XpKind, false)
                    + b.EyeW * XpCurve.XpValue(Catalog.GetEnemy(b.ShooterKind).XpKind, false)
                    + b.SentinelW * XpCurve.XpValue(Catalog.GetEnemy(b.SpecialKind).XpKind, false)) / w;
                double legacy = (b.BeetleW * XpCurve.XpValue(EnemyKind.SandBeetle, false)
                    + b.EyeW * XpCurve.XpValue(EnemyKind.SandEye, false)
                    + b.SentinelW * XpCurve.XpValue(EnemyKind.Sentinel, false)) / w;
                Assert.AreEqual(legacy, perEnemy, 1e-9, "floor " + f);
            }

            foreach (EnemyKind kind in NewGuardians)
            {
                Assert.AreEqual(EnemyKind.CageWarden, Catalog.GetEnemy(kind).XpKind, kind.ToString());
                Assert.AreEqual(SurvivorTuning.XpWarden, XpCurve.XpValue(Catalog.GetEnemy(kind).XpKind, false));
            }
        }

        [Test]
        public void PoolKind_MapsEveryKindToALegacyPool()
        {
            EnemyKind[] legacy = { EnemyKind.SandBeetle, EnemyKind.SandEye, EnemyKind.Sentinel, EnemyKind.CageWarden,
                EnemyKind.SandQueen, EnemyKind.BlueHairGuy };
            foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
            {
                EnemyKind pool = Catalog.GetEnemy(kind).PoolKind;
                Assert.GreaterOrEqual(Array.IndexOf(legacy, pool), 0, kind.ToString());
                Assert.LessOrEqual((int)pool, 5, kind.ToString());
            }

            foreach (EnemyKind kind in NewGuardians)
            {
                Assert.AreEqual(EnemyKind.CageWarden, Catalog.GetEnemy(kind).PoolKind, kind.ToString());
            }
        }

        [Test]
        public void EnemyKinds_IsBossAndIsGuardian()
        {
            foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
            {
                bool boss = kind == EnemyKind.CageWarden || kind == EnemyKind.SandQueen || kind == EnemyKind.BlueHairGuy
                    || Array.IndexOf(NewGuardians, kind) >= 0;
                Assert.AreEqual(boss, EnemyKinds.IsBoss(kind), kind.ToString());
                bool guardian = kind == EnemyKind.CageWarden || Array.IndexOf(NewGuardians, kind) >= 0;
                Assert.AreEqual(guardian, EnemyKinds.IsGuardian(kind), kind.ToString());
            }
        }
    }
}
