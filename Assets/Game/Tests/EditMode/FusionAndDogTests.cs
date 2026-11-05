using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class FusionAndDogTests
    {
        private SaveData _data;
        private FusionService _fusion;

        [SetUp]
        public void SetUp()
        {
            _data = SaveData.CreateDefault();
            _data.unlockedSpiritIds.Add("spirit.ember");
            _fusion = new FusionService(_data, Catalog.DemoTrialSpiritIds);
        }

        [Test]
        public void Preview_DewPlusEmber_IsOkInEitherOrder()
        {
            FusionRecipe recipe;
            Assert.AreEqual(FusionCheck.Ok, _fusion.Preview("spirit.dew", "spirit.ember", out recipe));
            Assert.AreEqual("spirit.steam", recipe.OutputId);
            Assert.AreEqual(FusionCheck.Ok, _fusion.Preview("spirit.ember", "spirit.dew", out recipe));
        }

        [Test]
        public void Preview_SameSpirit_NoRecipe_Missing()
        {
            FusionRecipe recipe;
            Assert.AreEqual(FusionCheck.SameSpirit, _fusion.Preview("spirit.dew", "spirit.dew", out recipe));
            Assert.AreEqual(FusionCheck.NoRecipe, _fusion.Preview("spirit.dew", "spirit.steam", out recipe));
            SaveData empty = SaveData.CreateDefault();
            FusionService noEmber = new FusionService(empty, Catalog.DemoTrialSpiritIds);
            Assert.AreEqual(FusionCheck.MissingIngredient, noEmber.Preview("spirit.dew", "spirit.ember", out recipe));
        }

        [Test]
        public void Fuse_LocksIngredientsAndOffersOutput()
        {
            Assert.AreEqual(FusionCheck.Ok, _fusion.Fuse("spirit.dew", "spirit.ember"));
            Assert.IsTrue(_fusion.IsLocked("spirit.dew"));
            Assert.IsTrue(_fusion.IsLocked("spirit.ember"));
            CollectionAssert.AreEquivalent(new[] { "spirit.steam" }, _fusion.AvailableSpirits());
            Assert.AreEqual(FusionCheck.IngredientLocked, _fusion.Fuse("spirit.dew", "spirit.ember"));
            Assert.AreEqual(1, _data.activeFusions.Count);
        }

        [Test]
        public void Unfuse_RestoresIngredients()
        {
            _fusion.Fuse("spirit.dew", "spirit.ember");
            Assert.IsTrue(_fusion.Unfuse("recipe.steam"));
            Assert.IsFalse(_fusion.Unfuse("recipe.steam"));
            Assert.IsFalse(_fusion.IsLocked("spirit.dew"));
            CollectionAssert.AreEquivalent(
                new[] { "spirit.ember", "spirit.dew" }, _fusion.AvailableSpirits());
        }

        [Test]
        public void TrialSpirit_NotWrittenToSave()
        {
            Assert.IsFalse(_data.unlockedSpiritIds.Contains("spirit.dew"));
            _fusion.Fuse("spirit.dew", "spirit.ember");
            _fusion.Unfuse("recipe.steam");
            Assert.IsFalse(_data.unlockedSpiritIds.Contains("spirit.dew"));
        }

        [Test]
        public void DogProgress_SixSealsIsHoundSevenIsLion()
        {
            int mask = 0;
            Element[] six = { Element.Metal, Element.Wood, Element.Water, Element.Fire, Element.Earth, Element.Dark };
            foreach (Element e in six)
            {
                mask = DogProgress.AddSeal(mask, e);
            }

            Assert.AreEqual(6, DogProgress.SealCount(mask));
            Assert.AreEqual(DogTier.SpiritHound, DogProgress.GetTier(mask));
            mask = DogProgress.AddSeal(mask, Element.Light);
            Assert.AreEqual(DogProgress.AllSevenMask, mask);
            Assert.AreEqual(DogTier.SunMoonLion, DogProgress.GetTier(mask));
        }

        [Test]
        public void DogProgress_RepeatedElementAndNonCommonIgnored()
        {
            int mask = DogProgress.AddSeal(0, Element.Fire);
            Assert.AreEqual(mask, DogProgress.AddSeal(mask, Element.Fire));
            Assert.AreEqual(mask, DogProgress.AddSeal(mask, Element.Space));
            Assert.AreEqual(mask, DogProgress.AddSeal(mask, Element.None));
            Assert.IsFalse(DogProgress.IsCommonElement(Element.Divine));
            Assert.AreEqual(1, DogProgress.SealCount(mask));
        }

        [Test]
        public void DogProgress_TiersAndMultipliers()
        {
            Assert.AreEqual(DogTier.Companion, DogProgress.GetTier(0));
            Assert.AreEqual(DogTier.SandPup, DogProgress.GetTier(0x03));
            Assert.AreEqual(DogTier.SpiritHound, DogProgress.GetTier(0x07));
            Assert.AreEqual(1.0f, DogProgress.DogDamageMultiplier(DogTier.Companion), 1e-6f);
            Assert.AreEqual(1.10f, DogProgress.DogDamageMultiplier(DogTier.SandPup), 1e-6f);
            Assert.AreEqual(1.25f, DogProgress.DogDamageMultiplier(DogTier.SpiritHound), 1e-6f);
            Assert.AreEqual(1.40f, DogProgress.DogDamageMultiplier(DogTier.SunMoonLion), 1e-6f);
        }
    }
}
