using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class CatalogTests
    {
        [Test]
        public void Lookups_ReturnNullForMissing()
        {
            Assert.IsNull(Catalog.GetSpirit("nope"));
            Assert.IsNull(Catalog.GetOutfit("nope"));
            Assert.IsNull(Catalog.GetUpgrade("nope"));
            Assert.IsNull(Catalog.GetFloor(0));
            Assert.IsNull(Catalog.GetFloor(101));
            Assert.IsNull(Catalog.FindRecipe("spirit.dew", "spirit.steam"));
        }

        [Test]
        public void Spirits_MatchLockedValues()
        {
            SpiritDefinition steam = Catalog.GetSpirit("spirit.steam");
            Assert.IsTrue(steam.IsFusion);
            Assert.AreEqual(12.65f, steam.AutoDamage, 1e-4f);
            Assert.AreEqual(0f, steam.CommandShield, 1e-6f);
            Assert.AreEqual(Element.Fire, Catalog.GetSpirit("spirit.ember").Element);
            Assert.AreEqual("spirit.steam", Catalog.FindRecipe("spirit.ember", "spirit.dew").OutputId);
        }

        [Test]
        public void Outfits_SixWithThreeDefaultsAndPerkIds()
        {
            Assert.AreEqual(6, Catalog.Outfits.Count);
            int defaults = 0;
            foreach (OutfitDefinition outfit in Catalog.Outfits)
            {
                defaults += outfit.DefaultOwned ? 1 : 0;
                Assert.AreEqual(outfit.Id, outfit.AppearanceId);
            }

            Assert.AreEqual(3, defaults);
            Assert.AreEqual("perk.head.dawn_clip", Catalog.GetOutfit("outfit.head.dawn_clip").PerkId);
        }

        [Test]
        public void Floors_HundredWithBossesEveryFifth()
        {
            Assert.AreEqual(100, Catalog.Floors.Count);
            Assert.AreEqual(100, Catalog.FloorCount);
            Assert.IsTrue(Catalog.GetFloor(5).IsGuardianFloor);
            Assert.IsFalse(Catalog.GetFloor(4).IsGuardianFloor);
            Assert.AreEqual(1, Catalog.GetFloor(3).Waves.Count);
            Assert.AreEqual(EnemyKind.CageWarden, Catalog.GetFloor(5).Waves[0].Spawns[0].kind);
            Assert.AreEqual(EnemyKind.SandQueen, Catalog.GetFloor(50).Waves[0].Spawns[0].kind);
            Assert.AreEqual(EnemyKind.BlueHairGuy, Catalog.GetFloor(100).Waves[0].Spawns[0].kind);
            Assert.AreEqual(700f, Catalog.GetEnemy(EnemyKind.CageWarden).MaxHp, 1e-6f);
        }

        [Test]
        public void Spirits_SixWithThreeRecipesAndNewIds()
        {
            Assert.AreEqual(6, Catalog.Spirits.Count);
            Assert.AreEqual(3, Catalog.Recipes.Count);
            Assert.AreEqual("spirit.growth", Catalog.FindRecipe("spirit.bud", "spirit.dew").OutputId);
            Assert.AreEqual("spirit.wildfire", Catalog.FindRecipe("spirit.bud", "spirit.ember").OutputId);
            Assert.AreEqual(Element.Wood, Catalog.GetSpirit("spirit.bud").Element);
            Assert.IsTrue(Catalog.GetSpirit("spirit.wildfire").IsFusion);
            Assert.IsNull(Catalog.GetSpirit("spirit.leaf"));
            CollectionAssert.AreEqual(new[] { "spirit.dew", "spirit.ember", "spirit.bud" }, Catalog.BaseSpiritIds);
        }
    }
}
