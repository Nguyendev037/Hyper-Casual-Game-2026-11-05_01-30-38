using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T3: LevelUpService (SPEC-P01 §5).
    public class LevelUpServiceTests
    {
        private static RunSpirits Spirits(params string[] unlocked)
        {
            return new RunSpirits(unlocked);
        }

        [Test]
        public void Roll_ThreeDistinctCards_ForManySeeds()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                LevelUpService svc = new LevelUpService(new SeededRng(seed));
                List<LevelUpCard> cards = svc.Roll(new RunUpgrades(new SeededRng(seed)), Spirits("spirit.dew", "spirit.ember", "spirit.bud"));
                Assert.AreEqual(3, cards.Count, "seed " + seed);
                HashSet<string> ids = new HashSet<string>();
                foreach (LevelUpCard c in cards)
                {
                    Assert.IsTrue(ids.Add(c.Id), "duplicate " + c.Id + " seed " + seed);
                }
            }
        }

        [Test]
        public void Roll_SameSeedSameCards_DifferentSeedsDiffer()
        {
            List<LevelUpCard> a = new LevelUpService(new SeededRng(7)).Roll(new RunUpgrades(new SeededRng(1)), Spirits());
            List<LevelUpCard> b = new LevelUpService(new SeededRng(7)).Roll(new RunUpgrades(new SeededRng(1)), Spirits());
            CollectionAssert.AreEqual(Ids(a), Ids(b));
            bool differs = false;
            for (int seed = 8; seed < 40 && !differs; seed++)
            {
                differs = !Ids(new LevelUpService(new SeededRng(seed)).Roll(new RunUpgrades(new SeededRng(1)), Spirits())).SequenceEqual(Ids(a));
            }

            Assert.IsTrue(differs);
        }

        [Test]
        public void Roll_SkipsMaxedStatCards()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            for (int i = 0; i < 5; i++)
            {
                upgrades.Take("upgrade.staff_dmg");
            }

            for (int seed = 1; seed <= 100; seed++)
            {
                foreach (LevelUpCard c in new LevelUpService(new SeededRng(seed)).Roll(upgrades, Spirits()))
                {
                    Assert.AreNotEqual("upgrade.staff_dmg", c.Id);
                }
            }
        }

        [Test]
        public void Roll_AllMaxedNoSpirits_FallsBackToSingleHeal()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            foreach (UpgradeDefinition def in Catalog.UpgradePool)
            {
                for (int i = 0; i < def.MaxStacks; i++)
                {
                    upgrades.Take(def.Id);
                }
            }

            List<LevelUpCard> cards = new LevelUpService(new SeededRng(3)).Roll(upgrades, Spirits());
            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(CardKind.Heal, cards[0].Kind);
            Assert.AreEqual(Catalog.HealFallbackId, cards[0].Id);
        }

        [Test]
        public void Roll_FewCandidates_AreTopUpWithHeal()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            foreach (UpgradeDefinition def in Catalog.UpgradePool)
            {
                if (def.Id == "upgrade.regen" || def.Id == "upgrade.pierce")
                {
                    continue;
                }

                for (int i = 0; i < def.MaxStacks; i++)
                {
                    upgrades.Take(def.Id);
                }
            }

            List<LevelUpCard> cards = new LevelUpService(new SeededRng(3)).Roll(upgrades, Spirits());
            Assert.AreEqual(3, cards.Count);
            Assert.AreEqual(CardKind.Heal, cards[2].Kind);
        }

        [Test]
        public void SpiritNewCards_OnlyUnlockedBaseSpiritsNotInSlots_AndOnlyWhenSlotFree()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            RunSpirits spirits = Spirits("spirit.dew", "spirit.bud");
            spirits.Add("spirit.dew");
            HashSet<string> seen = new HashSet<string>();
            for (int seed = 1; seed <= 300; seed++)
            {
                foreach (LevelUpCard c in new LevelUpService(new SeededRng(seed)).Roll(upgrades, spirits))
                {
                    if (c.Kind == CardKind.SpiritNew)
                    {
                        seen.Add(c.Id);
                    }
                }
            }

            CollectionAssert.AreEquivalent(new[] { "card.spirit.new.bud" }, seen); // ember is locked, dew is already in a slot
        }

        [Test]
        public void SpiritUpCards_OnlyForOwnedSpiritsBelowMax()
        {
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            RunSpirits spirits = Spirits("spirit.dew", "spirit.ember");
            spirits.Add("spirit.dew");
            spirits.Add("spirit.ember");
            while (spirits.LevelUp("spirit.ember"))
            {
            }

            HashSet<string> seen = new HashSet<string>();
            for (int seed = 1; seed <= 300; seed++)
            {
                foreach (LevelUpCard c in new LevelUpService(new SeededRng(seed)).Roll(upgrades, spirits))
                {
                    if (c.Kind == CardKind.SpiritUp)
                    {
                        seen.Add(c.Id);
                    }
                }
            }

            CollectionAssert.AreEquivalent(new[] { "card.spirit.up.dew" }, seen);
        }

        [Test]
        public void Apply_ChangesState_AndInvalidCardsThrowWithoutChange()
        {
            LevelUpService svc = new LevelUpService(new SeededRng(1));
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(1));
            RunSpirits spirits = Spirits("spirit.dew", "spirit.ember");

            svc.Apply(new LevelUpCard { Id = "upgrade.range", Kind = CardKind.Stat, TargetId = "upgrade.range" }, upgrades, spirits);
            Assert.AreEqual(1, upgrades.StackCount("upgrade.range"));

            svc.Apply(new LevelUpCard { Id = "card.spirit.new.dew", Kind = CardKind.SpiritNew, TargetId = "spirit.dew" }, upgrades, spirits);
            Assert.AreEqual(1, spirits.LevelOf("spirit.dew"));
            svc.Apply(new LevelUpCard { Id = "card.spirit.up.dew", Kind = CardKind.SpiritUp, TargetId = "spirit.dew" }, upgrades, spirits);
            Assert.AreEqual(2, spirits.LevelOf("spirit.dew"));

            svc.Apply(new LevelUpCard { Id = Catalog.HealFallbackId, Kind = CardKind.Heal }, upgrades, spirits);
            Assert.AreEqual(1, upgrades.Taken.Count);

            Assert.Throws<System.ArgumentException>(() => svc.Apply(
                new LevelUpCard { Id = "card.spirit.up.ember", Kind = CardKind.SpiritUp, TargetId = "spirit.ember" }, upgrades, spirits));
            Assert.Throws<System.ArgumentException>(() => svc.Apply(
                new LevelUpCard { Id = "x", Kind = CardKind.Stat, TargetId = "upgrade.nope" }, upgrades, spirits));
            Assert.Throws<System.ArgumentNullException>(() => svc.Apply(null, upgrades, spirits));
        }

        [Test]
        public void PoolCapacity_IsLargeEnoughForFortyPicks()
        {
            Assert.GreaterOrEqual(LevelUpService.PoolCapacity, 45);
            Assert.GreaterOrEqual(LevelUpService.PoolCapacity, 70);
        }

        [Test]
        public void FortyPicks_NeverRunOutOfRealCards()
        {
            LevelUpService svc = new LevelUpService(new SeededRng(11));
            RunUpgrades upgrades = new RunUpgrades(new SeededRng(11));
            RunSpirits spirits = Spirits("spirit.dew", "spirit.ember", "spirit.bud");
            spirits.Add("spirit.dew");
            for (int pick = 0; pick < 40; pick++)
            {
                List<LevelUpCard> cards = svc.Roll(upgrades, spirits);
                Assert.AreEqual(3, cards.Count, "pick " + pick);
                foreach (LevelUpCard c in cards)
                {
                    Assert.AreNotEqual(CardKind.Heal, c.Kind, "pick " + pick);
                }

                svc.Apply(cards[0], upgrades, spirits);
            }
        }

        private static List<string> Ids(List<LevelUpCard> cards)
        {
            List<string> ids = new List<string>();
            foreach (LevelUpCard c in cards)
            {
                ids.Add(c.Id);
            }

            return ids;
        }
    }

    internal static class EnumerableExt
    {
        public static bool SequenceEqual(this List<string> a, List<string> b)
        {
            return System.Linq.Enumerable.SequenceEqual(a, b);
        }
    }
}
