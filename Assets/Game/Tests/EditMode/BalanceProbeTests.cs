using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T8: "hard enough" measurements (SPEC-P03 §3-§4). Numbers come from BalanceProbe, never hard-coded here.
    public class BalanceProbeTests
    {
        private static readonly double[] Collects = { 1.0, 0.85 };

        [Test]
        public void ReferenceDps_GrowsWithCards_AndIsPositive()
        {
            double previous = 0;
            for (int cards = 0; cards <= 40; cards++)
            {
                double dps = BalanceProbe.ReferenceDps(cards);
                Assert.Greater(dps, 0.0);
                Assert.GreaterOrEqual(dps, previous - 1e-9, "cards " + cards);
                previous = dps;
            }
        }

        [Test]
        public void ReferenceBuild_AtFortyCards_HasSteamAndTheExpectedStats()
        {
            BalanceProbe.ReferenceBuild b = BalanceProbe.BuildReference(40);
            Assert.AreEqual(4, b.Stats.ProjectileCount);
            Assert.That(b.Stats.Range, Is.GreaterThan(9f));
            Assert.That(b.Stats.MaxHp, Is.GreaterThan(100f));
            Assert.IsTrue(b.Spirits.Has("spirit.dew") || b.Spirits.Has("spirit.steam"));
            Assert.IsTrue(BalanceProbe.BuildReference(50).Spirits.Has("spirit.steam"));
        }

        [Test]
        public void FinalBoss_TtkWithReferenceBuild_Is90To150Seconds()
        {
            double ttk = BalanceProbe.FinalBossTtk();
            Assert.That(ttk, Is.InRange(90.0, 150.0), "TTK " + ttk);
        }

        [Test]
        public void MidBossQueen_TtkAtExpectedLevel_Is60To110Seconds()
        {
            foreach (double collect in Collects)
            {
                double ttk = BalanceProbe.QueenTtk(collect);
                Assert.That(ttk, Is.InRange(60.0, 110.0), "collect " + collect + " TTK " + ttk);
            }
        }

        [Test]
        public void Guardians_TtkAtExpectedLevel_Is25To60Seconds()
        {
            foreach (double collect in Collects)
            {
                for (int floor = 5; floor <= 95; floor += 5)
                {
                    if (FloorPlan.Kind(floor) != FloorKind.Guardian)
                    {
                        continue;
                    }

                    double ttk = BalanceProbe.GuardianTtk(floor, collect);
                    Assert.That(ttk, Is.InRange(25.0, 60.0), "floor " + floor + " collect " + collect + " TTK " + ttk);
                }
            }
        }

        [Test]
        public void NoSingleHit_ExceedsFortyPercentOfReferenceMaxHp()
        {
            double limit = SurvivorTuning.BossPerHitMaxFraction * BalanceProbe.ReferenceMaxHp(40);
            Assert.LessOrEqual(BalanceProbe.MaxSingleHit(EnemyKind.SandQueen, 50), limit, "queen");
            Assert.LessOrEqual(BalanceProbe.MaxSingleHit(EnemyKind.BlueHairGuy, 100), limit, "blue hair guy");
            for (int floor = 5; floor <= 95; floor += 5)
            {
                if (FloorPlan.Kind(floor) == FloorKind.Guardian)
                {
                    Assert.LessOrEqual(BalanceProbe.MaxSingleHit(EnemyKind.CageWarden, floor), limit, "warden " + floor);
                }
            }
        }

        [Test]
        public void StandingStill_IsDeadly_AtLeastQuarterOfMaxHpPerTenSeconds()
        {
            double need = SurvivorTuning.BossIdleDpsMinFractionPer10s * BalanceProbe.ReferenceMaxHp(40) / 10.0;
            foreach (int phase in new[] { 1, 2 })
            {
                Assert.GreaterOrEqual(BalanceProbe.IdleDps(EnemyKind.SandQueen, 50, phase), need, "queen phase " + phase);
                Assert.GreaterOrEqual(BalanceProbe.IdleDps(EnemyKind.BlueHairGuy, 100, phase), need, "blue phase " + phase);
            }
        }

        [Test]
        public void EveryScriptedBossAttack_HasAtLeastSixTenthsOfASecondWarning()
        {
            foreach (EnemyKind kind in new[] { EnemyKind.SandQueen, EnemyKind.BlueHairGuy })
            {
                foreach (BossAttackDef attack in Catalog.GetBossAttacks(kind))
                {
                    Assert.GreaterOrEqual(attack.Telegraph, Catalog.BossMinTelegraph, kind + " " + attack.Name);
                }
            }

            Assert.IsNull(Catalog.GetBossAttacks(EnemyKind.CageWarden));
        }

        [Test]
        public void BossesDoNotChaseFasterThanNinetyPercentOfPlayerSpeed()
        {
            float cap = Catalog.BaseMoveSpeed * 0.9f;
            Assert.LessOrEqual(Catalog.GetEnemy(EnemyKind.SandQueen).MoveSpeed, cap);
            Assert.LessOrEqual(Catalog.GetEnemy(EnemyKind.BlueHairGuy).MoveSpeed, cap);
        }

        [Test]
        public void BossHp_UsesFloorMultiplier()
        {
            Assert.AreEqual(Catalog.QueenBaseHp * FloorPlan.Budget(50).HpMult, BalanceProbe.BossHp(EnemyKind.SandQueen, 50), 1e-3);
            Assert.AreEqual(700.0 * FloorPlan.Budget(10).HpMult, BalanceProbe.BossHp(EnemyKind.CageWarden, 10), 1e-3);
        }
    }
}
