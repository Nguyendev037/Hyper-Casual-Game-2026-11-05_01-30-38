using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class DayNightAndUpgradeTests
    {
        private static void Advance(DayNightClock clock, float seconds)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                float step = remaining > 0.1f ? 0.1f : remaining;
                clock.Tick(step);
                remaining -= step;
            }
        }

        [Test]
        public void Clock_FullCycleHasBothPhases()
        {
            DayNightClock clock = new DayNightClock();
            List<DayPhase> changes = new List<DayPhase>();
            clock.PhaseChanged += p => changes.Add(p);
            Assert.AreEqual(DayPhase.Day, clock.Phase);
            Advance(clock, 61f);
            Assert.AreEqual(DayPhase.Night, clock.Phase);
            Advance(clock, 60f);
            Assert.AreEqual(DayPhase.Day, clock.Phase);
            CollectionAssert.AreEqual(new[] { DayPhase.Night, DayPhase.Day }, changes);
        }

        [Test]
        public void Clock_PausedDoesNotAdvance()
        {
            DayNightClock clock = new DayNightClock();
            clock.Paused = true;
            clock.Tick(0.1f);
            Assert.AreEqual(0f, clock.Elapsed, 1e-6f);
            clock.Paused = false;
            clock.Tick(0.1f);
            Assert.AreEqual(0.1f, clock.Elapsed, 1e-5f);
        }

        [Test]
        public void Clock_DtClampedAndNonPositiveIgnored()
        {
            DayNightClock clock = new DayNightClock();
            clock.Tick(10f);
            Assert.AreEqual(0.25f, clock.Elapsed, 1e-6f);
            clock.Tick(0f);
            clock.Tick(-1f);
            Assert.AreEqual(0.25f, clock.Elapsed, 1e-6f);
        }

        [Test]
        public void Clock_NightBlendInterpolatesTwoSecondsAfterBoundary()
        {
            DayNightClock clock = new DayNightClock();
            Assert.AreEqual(0f, clock.NightBlend, 1e-6f);
            Advance(clock, 60f);
            Advance(clock, 1f);
            Assert.AreEqual(0.5f, clock.NightBlend, 0.02f);
            Advance(clock, 5f);
            Assert.AreEqual(1f, clock.NightBlend, 1e-6f);
            Advance(clock, 54f);
            Advance(clock, 1f);
            Assert.AreEqual(0.5f, clock.NightBlend, 0.02f);
        }

        [Test]
        public void Clock_ForceToggleJumpsAndFiresEventAndResetRestarts()
        {
            DayNightClock clock = new DayNightClock();
            DayPhase? last = null;
            clock.PhaseChanged += p => last = p;
            clock.Tick(0.2f);
            clock.ForceToggle();
            Assert.AreEqual(DayPhase.Night, clock.Phase);
            Assert.AreEqual(DayPhase.Night, last);
            Assert.AreEqual(60f, clock.PhaseRemaining, 1e-4f);
            clock.ForceToggle();
            Assert.AreEqual(DayPhase.Day, clock.Phase);
            clock.Reset();
            Assert.AreEqual(0f, clock.Elapsed, 1e-6f);
        }

        [Test]
        public void ElementMultiplier_MatchesDayNightTable()
        {
            Assert.AreEqual(1.1f, DayNightClock.ElementMultiplier(Element.Fire, DayPhase.Day), 1e-6f);
            Assert.AreEqual(1.1f, DayNightClock.ElementMultiplier(Element.Light, DayPhase.Day), 1e-6f);
            Assert.AreEqual(1.0f, DayNightClock.ElementMultiplier(Element.Water, DayPhase.Day), 1e-6f);
            Assert.AreEqual(1.1f, DayNightClock.ElementMultiplier(Element.Water, DayPhase.Night), 1e-6f);
            Assert.AreEqual(1.1f, DayNightClock.ElementMultiplier(Element.Dark, DayPhase.Night), 1e-6f);
            Assert.AreEqual(1.0f, DayNightClock.ElementMultiplier(Element.Fire, DayPhase.Night), 1e-6f);
        }

        [Test]
        public void SeededRng_SameSeedSameSequence_ZeroSeedWorks()
        {
            SeededRng a = new SeededRng(123);
            SeededRng b = new SeededRng(123);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(a.Next(1000), b.Next(1000));
            }

            SeededRng zero = new SeededRng(0);
            Assert.AreNotEqual(zero.Next(int.MaxValue), zero.Next(int.MaxValue));
            float f = new SeededRng(5).NextFloat();
            Assert.That(f >= 0f && f < 1f);
        }

        [Test]
        public void RollChoices_DistinctSameSeedSameResult()
        {
            List<string> first = new RunUpgrades(new SeededRng(7)).RollChoices();
            List<string> second = new RunUpgrades(new SeededRng(7)).RollChoices();
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(3, new HashSet<string>(first).Count);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void RollChoices_RespectsMaxStacksAndFallsBackToHeal()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            for (int n = 0; n < Catalog.GetUpgrade("upgrade.staff_dmg").MaxStacks; n++)
            {
                upgrades.Take("upgrade.staff_dmg");
            }

            for (int i = 0; i < 20; i++)
            {
                CollectionAssert.DoesNotContain(upgrades.RollChoices(), "upgrade.staff_dmg");
            }

            foreach (UpgradeDefinition def in Catalog.UpgradePool)
            {
                while (upgrades.StackCount(def.Id) < def.MaxStacks)
                {
                    upgrades.Take(def.Id);
                }
            }

            CollectionAssert.AreEqual(new[] { "upgrade.heal20" }, upgrades.RollChoices());
        }

        [Test]
        public void Take_InvalidOrMaxedThrows_HealIsNotStacked()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            Assert.Throws<System.ArgumentException>(() => upgrades.Take("upgrade.nope"));
            int max = Catalog.GetUpgrade("upgrade.dog_dmg").MaxStacks;
            for (int n = 0; n < max; n++)
            {
                upgrades.Take("upgrade.dog_dmg");
            }

            Assert.Throws<System.ArgumentException>(() => upgrades.Take("upgrade.dog_dmg"));
            upgrades.Take("upgrade.heal20");
            Assert.AreEqual(0, upgrades.StackCount("upgrade.heal20"));
            Assert.AreEqual(max, upgrades.Taken.Count);
        }
    }
}
