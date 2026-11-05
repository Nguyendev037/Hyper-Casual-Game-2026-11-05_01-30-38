using System;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T1 (TESTING-ACCEPTANCE §1): XP curve and expected level bands (SPEC-P01 §4).
    public class XpCurveTests
    {
        [Test]
        public void Required_IsPositiveAndStrictlyIncreasing_AndRejectsLevelBelowOne()
        {
            int previous = 0;
            for (int level = 1; level <= 150; level++)
            {
                int need = XpCurve.Required(level);
                Assert.Greater(need, previous, "level " + level);
                previous = need;
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => XpCurve.Required(0));
        }

        [Test]
        public void Required_MatchesFormulaWithLockedCoefficients()
        {
            for (int level = 1; level <= 60; level++)
            {
                double expected = Math.Round(5.0 + 2.0 * level + SurvivorTuning.XpQuad * level * level, MidpointRounding.AwayFromZero);
                Assert.AreEqual((int)expected, XpCurve.Required(level), "level " + level);
            }
        }

        [Test]
        public void XpValue_FollowsTable()
        {
            Assert.AreEqual(1, XpCurve.XpValue(EnemyKind.SandBeetle, false));
            Assert.AreEqual(2, XpCurve.XpValue(EnemyKind.SandEye, false));
            Assert.AreEqual(3, XpCurve.XpValue(EnemyKind.Sentinel, false));
            Assert.AreEqual(5, XpCurve.XpValue(EnemyKind.SandBeetle, true));
            Assert.AreEqual(15, XpCurve.XpValue(EnemyKind.Sentinel, true));
            Assert.AreEqual(40, XpCurve.XpValue(EnemyKind.CageWarden, false));
            Assert.AreEqual(120, XpCurve.XpValue(EnemyKind.SandQueen, false));
        }

        [Test]
        public void ExpectedXp_CountsEighteenWardensAndTheQueen()
        {
            int wardens = 0;
            for (int f = 1; f <= 100; f++)
            {
                if (FloorPlan.BossKind(f) == EnemyKind.CageWarden)
                {
                    wardens++;
                }
            }

            Assert.AreEqual(18, wardens);
            Assert.GreaterOrEqual(BalanceProbe.ExpectedFloorXp(50), 120.0);
            Assert.GreaterOrEqual(BalanceProbe.ExpectedFloorXp(10), 40.0);
            Assert.GreaterOrEqual(BalanceProbe.ExpectedFloorXp(95), 40.0);
        }

        [Test]
        public void LevelBands_Collect100Percent()
        {
            int l10 = BalanceProbe.LevelAfterFloor(10, 1.0);
            int l50 = BalanceProbe.LevelAfterFloor(50, 1.0);
            int l100 = BalanceProbe.LevelAfterFloor(100, 1.0);
            Assert.That(l10, Is.InRange(6, 11), "floor 10");
            Assert.That(l50, Is.InRange(22, 29), "floor 50");
            Assert.That(l100, Is.InRange(39, 43), "floor 100");
        }

        [Test]
        public void LevelBands_Collect85Percent()
        {
            Assert.That(BalanceProbe.LevelAfterFloor(100, 0.85), Is.InRange(36, 41), "floor 100 @85%");
        }

        [Test]
        public void RunProgress_LevelsUpAndKeepsRemainder()
        {
            RunProgress p = new RunProgress();
            Assert.AreEqual(1, p.Level);
            int need1 = XpCurve.Required(1);
            Assert.AreEqual(0, p.AddXp(need1 - 1, 1f));
            Assert.AreEqual(1, p.AddXp(1, 1f));
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(0, p.Xp);
            Assert.AreEqual(1, p.PendingLevelUps);
            Assert.AreEqual(need1, p.TotalXp);
        }

        [Test]
        public void RunProgress_MultipleLevelsAtOnce_ConsumeOneByOne()
        {
            RunProgress p = new RunProgress();
            int big = XpCurve.Required(1) + XpCurve.Required(2) + XpCurve.Required(3);
            Assert.AreEqual(3, p.AddXp(big, 1f));
            Assert.AreEqual(4, p.Level);
            Assert.AreEqual(3, p.PendingLevelUps);
            Assert.IsTrue(p.ConsumeLevelUp());
            Assert.IsTrue(p.ConsumeLevelUp());
            Assert.IsTrue(p.ConsumeLevelUp());
            Assert.IsFalse(p.ConsumeLevelUp());
        }

        [Test]
        public void RunProgress_IgnoresNonPositive_AndAppliesGainMultiplier()
        {
            RunProgress p = new RunProgress();
            Assert.AreEqual(0, p.AddXp(0, 1f));
            Assert.AreEqual(0, p.AddXp(-5, 1f));
            Assert.AreEqual(0, p.TotalXp);
            p.AddXp(10, 1.08f); // 10.8 -> 11
            Assert.AreEqual(11, p.TotalXp);
            RunProgress q = new RunProgress();
            q.AddXp(1, 0f); // never below 1 XP for a positive drop
            Assert.AreEqual(1, q.TotalXp);
        }

        [Test]
        public void Required_RejectsUnrepresentableLevels()
        {
            Assert.AreEqual(94588, XpCurve.MaxLevel);
            Assert.Greater(XpCurve.Required(94588), 0);
            Assert.LessOrEqual(Math.Round(5 + 2.0 * 94588 + 0.24 * 94588 * 94588), int.MaxValue);
            Assert.Greater(Math.Round(5 + 2.0 * 94589 + 0.24 * 94589 * 94589), int.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => XpCurve.Required(94589));
            Assert.Throws<ArgumentOutOfRangeException>(() => XpCurve.Required(int.MaxValue));
        }

        [Test]
        public void RunProgress_LargeAward_PreservesRemainderWithoutOverflow()
        {
            RunProgress p = new RunProgress(50000, 500000000);
            int expectedLevel = p.Level;
            long expectedXp = (long)p.Xp + int.MaxValue;
            while (expectedXp >= XpCurve.Required(expectedLevel))
            {
                expectedXp -= XpCurve.Required(expectedLevel++);
            }
            p.AddXp(int.MaxValue, 1f);
            Assert.AreEqual(500000000L + int.MaxValue, p.TotalXp);
            Assert.Greater(p.Level, 50000);
            Assert.AreEqual(expectedLevel, p.Level);
            Assert.AreEqual(expectedXp, p.Xp);
            Assert.AreEqual(expectedLevel - 50000, p.PendingLevelUps);
            Assert.That(p.Xp, Is.InRange(0, XpCurve.Required(p.Level) - 1));
        }

        [Test]
        public void RunProgress_RejectsInvalidRestoredState()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunProgress(int.MaxValue, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunProgress(1, XpCurve.Required(1)));
        }

        [Test]
        public void RunProgress_LargeMultiplier_SaturatesAwardWithoutOverflow()
        {
            RunProgress p = new RunProgress();
            Assert.DoesNotThrow(() => p.AddXp(int.MaxValue, float.MaxValue));
            Assert.AreEqual(int.MaxValue, p.TotalXp);
            Assert.That(p.Xp, Is.InRange(0, XpCurve.Required(p.Level) - 1));
        }

        [Test]
        public void RunProgress_NonfiniteMultiplier_RejectsBeforeMutation()
        {
            RunProgress p = new RunProgress();
            foreach (float multiplier in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => p.AddXp(1, multiplier));
            Assert.AreEqual(0, p.TotalXp);
            Assert.AreEqual(0, p.Xp);
            Assert.AreEqual(1, p.Level);
        }

        [Test]
        public void ChestXp_IsSixtyPercentOfRequired()
        {
            Assert.AreEqual((int)Math.Round(XpCurve.Required(10) * 0.6, MidpointRounding.AwayFromZero), XpCurve.ChestXp(10));
        }
    }
}
