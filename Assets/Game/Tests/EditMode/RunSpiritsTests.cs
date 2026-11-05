using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T4: RunSpirits (SPEC-P02 §2-§3).
    public class RunSpiritsTests
    {
        private static void LevelTo(RunSpirits s, string id, int level)
        {
            while (s.LevelOf(id) < level)
            {
                Assert.IsTrue(s.LevelUp(id));
            }
        }

        [Test]
        public void Add_OnlyBaseSpirits_NoDuplicates_NoMoreThanThreeSlots()
        {
            RunSpirits s = new RunSpirits();
            Assert.IsTrue(s.Add("spirit.dew"));
            Assert.IsFalse(s.Add("spirit.dew"));
            Assert.IsFalse(s.Add("spirit.steam"));
            Assert.IsFalse(s.Add("spirit.leaf"));
            Assert.IsFalse(s.Add(null));
            Assert.IsTrue(s.Add("spirit.ember"));
            Assert.IsTrue(s.Add("spirit.bud"));
            Assert.AreEqual(3, s.Slots.Count);
            Assert.AreEqual(1, s.Slots[0].level);
        }

        [Test]
        public void LevelUp_StopsAtMaxAndFailsForMissing()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(s.LevelUp("spirit.dew"));
            }

            Assert.AreEqual(5, s.LevelOf("spirit.dew"));
            Assert.IsFalse(s.LevelUp("spirit.dew"));
            Assert.IsFalse(s.LevelUp("spirit.ember"));
        }

        [Test]
        public void FusablePair_NeedsBothAtLevelThree()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            s.Add("spirit.ember");
            string a;
            string b;
            Assert.IsNull(s.FusablePair(out a, out b));
            LevelTo(s, "spirit.dew", 3);
            LevelTo(s, "spirit.ember", 2);
            Assert.IsNull(s.FusablePair(out a, out b));
            LevelTo(s, "spirit.ember", 3);
            FusionRecipe r = s.FusablePair(out a, out b);
            Assert.IsNotNull(r);
            Assert.AreEqual("recipe.steam", r.Id);
            Assert.AreEqual("spirit.dew", a);
            Assert.AreEqual("spirit.ember", b);
        }

        [Test]
        public void Fuse_ResultLevelIsMin_TakesSlotOfA_FreesSlotB()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            s.Add("spirit.ember");
            s.Add("spirit.bud");
            LevelTo(s, "spirit.dew", 4);
            LevelTo(s, "spirit.ember", 3);
            Assert.IsTrue(s.Fuse("recipe.steam"));
            Assert.AreEqual(2, s.Slots.Count);
            Assert.AreEqual("spirit.steam", s.Slots[0].spiritId);
            Assert.AreEqual(3, s.Slots[0].level);
            Assert.AreEqual("spirit.bud", s.Slots[1].spiritId);
            Assert.IsFalse(s.Has("spirit.dew"));
            Assert.IsFalse(s.Has("spirit.ember"));
        }

        [Test]
        public void Fuse_InvalidRecipeOrConditions_ReturnsFalseAndChangesNothing()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            s.Add("spirit.ember");
            LevelTo(s, "spirit.dew", 3);
            LevelTo(s, "spirit.ember", 2);
            Assert.IsFalse(s.Fuse("recipe.steam"));
            Assert.IsFalse(s.Fuse("recipe.nope"));
            Assert.IsFalse(s.Fuse(null));
            Assert.IsFalse(s.Fuse("recipe.growth")); // bud missing
            Assert.AreEqual(2, s.Slots.Count);
            Assert.AreEqual("spirit.dew", s.Slots[0].spiritId);
            Assert.AreEqual(3, s.Slots[0].level);
        }

        [Test]
        public void FusablePair_PrefersCatalogOrder_SteamThenGrowthThenWildfire()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            s.Add("spirit.ember");
            s.Add("spirit.bud");
            LevelTo(s, "spirit.dew", 3);
            LevelTo(s, "spirit.ember", 3);
            LevelTo(s, "spirit.bud", 3);
            string a;
            string b;
            Assert.AreEqual("recipe.steam", s.FusablePair(out a, out b).Id);
            List<FusionRecipe> all = s.FusableRecipes();
            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("recipe.growth", all[1].Id);
            Assert.AreEqual("recipe.wildfire", all[2].Id);
            Assert.IsTrue(s.Fuse("recipe.wildfire"));
            Assert.AreEqual("spirit.wildfire", s.Slots[1].spiritId);
            Assert.IsNull(s.FusablePair(out a, out b));
        }

        [Test]
        public void Unlock_OnlyBaseSpirits_AndRestoreValidates()
        {
            RunSpirits s = new RunSpirits(new[] { "spirit.dew", "spirit.steam", "spirit.dew" });
            CollectionAssert.AreEqual(new[] { "spirit.dew" }, s.Unlocked);
            Assert.IsFalse(s.Unlock("spirit.bogus"));

            List<RunSpiritEntry> good = new List<RunSpiritEntry>
            {
                new RunSpiritEntry { spiritId = "spirit.steam", level = 4 },
                new RunSpiritEntry { spiritId = "spirit.bud", level = 2 }
            };
            Assert.IsTrue(s.Restore(good));
            Assert.AreEqual(2, s.Slots.Count);

            Assert.IsFalse(s.Restore(new List<RunSpiritEntry> { new RunSpiritEntry { spiritId = "spirit.nope", level = 1 } }));
            Assert.IsFalse(s.Restore(new List<RunSpiritEntry> { new RunSpiritEntry { spiritId = "spirit.dew", level = 6 } }));
            Assert.IsFalse(s.Restore(new List<RunSpiritEntry>
            {
                new RunSpiritEntry { spiritId = "spirit.dew", level = 1 },
                new RunSpiritEntry { spiritId = "spirit.dew", level = 1 }
            }));
            Assert.AreEqual(2, s.Slots.Count); // failed restores leave the slots untouched
        }
    }
}
