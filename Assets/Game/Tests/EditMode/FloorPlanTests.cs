using System;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T2: FloorPlan.Kind / StationAfter / Budget (SPEC-P01 §3, 01-CONTRACTS §1).
    public class FloorPlanTests
    {
        [Test]
        public void Kind_EveryFifthIsGuardian_FiftyIsMid_HundredIsFinal()
        {
            int guardians = 0;
            for (int f = 1; f <= 100; f++)
            {
                FloorKind k = FloorPlan.Kind(f);
                if (f == 100) { Assert.AreEqual(FloorKind.FinalBoss, k); }
                else if (f == 50) { Assert.AreEqual(FloorKind.MidBoss, k); }
                else if (f % 5 == 0) { Assert.AreEqual(FloorKind.Guardian, k, "floor " + f); guardians++; }
                else { Assert.AreEqual(FloorKind.Normal, k, "floor " + f); }
            }

            Assert.AreEqual(18, guardians);
        }

        [Test]
        public void BossKind_MapsToEnemies()
        {
            Assert.AreEqual(EnemyKind.CageWarden, FloorPlan.BossKind(5));
            Assert.AreEqual(EnemyKind.SandQueen, FloorPlan.BossKind(50));
            Assert.AreEqual(EnemyKind.BlueHairGuy, FloorPlan.BossKind(100));
            Assert.IsNull(FloorPlan.BossKind(7));
        }

        [Test]
        public void StationAfter_FiveToNinetyFive()
        {
            int stations = 0;
            for (int f = 1; f <= 100; f++)
            {
                bool expected = f % 5 == 0 && f < 100;
                Assert.AreEqual(expected, FloorPlan.StationAfter(f), "floor " + f);
                stations += expected ? 1 : 0;
            }

            Assert.AreEqual(19, stations);
            Assert.IsFalse(FloorPlan.StationAfter(0));
            Assert.IsFalse(FloorPlan.StationAfter(105));
        }

        [Test]
        public void Budget_FloorOne()
        {
            SpawnBudget b = FloorPlan.Budget(1);
            Assert.AreEqual(9, b.Total);
            Assert.AreEqual(25f, b.Duration, 1e-6f);
            Assert.AreEqual(1f, b.BeetleW, 1e-6f);
            Assert.AreEqual(0f, b.EyeW, 1e-6f);
            Assert.AreEqual(0f, b.SentinelW, 1e-6f);
            Assert.AreEqual(0f, b.EliteChance, 1e-6f);
            Assert.AreEqual(1f, b.HpMult, 1e-6f);
            Assert.AreEqual(1f, b.DmgMult, 1e-6f);
        }

        [Test]
        public void Budget_WeightsAndCaps()
        {
            Assert.AreEqual(0.06f, FloorPlan.Budget(3).EyeW, 1e-6f);
            Assert.AreEqual(0.4f, FloorPlan.Budget(20).EyeW, 1e-6f);
            Assert.AreEqual(0.4f, FloorPlan.Budget(80).EyeW, 1e-6f);
            Assert.AreEqual(0f, FloorPlan.Budget(5).SentinelW, 1e-6f);
            Assert.AreEqual(0.09f, FloorPlan.Budget(6).SentinelW, 1e-6f);
            Assert.AreEqual(0.35f, FloorPlan.Budget(99).SentinelW, 1e-6f);
            Assert.AreEqual(0f, FloorPlan.Budget(14).EliteChance, 1e-6f);
            Assert.AreEqual(0.03f, FloorPlan.Budget(15).EliteChance, 1e-6f);
            Assert.AreEqual(0.08f, FloorPlan.Budget(99).EliteChance, 1e-6f);
        }

        [Test]
        public void Budget_TotalDurationAndMultipliers()
        {
            Assert.AreEqual(10, FloorPlan.Budget(20).Total); // guardian floor: half of 20
            Assert.AreEqual(7, FloorPlan.Budget(10).Total);
            Assert.AreEqual(0f, FloorPlan.Budget(10).Duration, 1e-6f);
            Assert.AreEqual(0f, FloorPlan.Budget(100).Duration, 1e-6f);
            Assert.AreEqual(25f, FloorPlan.Budget(19).Duration, 1e-6f);
            Assert.AreEqual(30f, FloorPlan.Budget(21).Duration, 1e-6f);
            Assert.AreEqual(30f, FloorPlan.Budget(59).Duration, 1e-6f);
            Assert.AreEqual(35f, FloorPlan.Budget(61).Duration, 1e-6f);
            Assert.AreEqual(1f + 0.07f * 99, FloorPlan.Budget(100).HpMult, 1e-4f);
            Assert.AreEqual(2.98f, FloorPlan.Budget(100).DmgMult, 1e-4f);
            Assert.AreEqual(1f + 0.07f * 49, FloorPlan.Budget(50).HpMult, 1e-4f);
        }

        [Test]
        public void Budget_OutsideDomainThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorPlan.Budget(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorPlan.Budget(-3));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorPlan.Budget(101));
        }
    }
}
