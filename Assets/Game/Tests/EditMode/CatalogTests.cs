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
            Assert.IsNull(Catalog.GetFloor(6));
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
        public void Floors_FiveWithGuardianOnLast()
        {
            Assert.AreEqual(5, Catalog.Floors.Count);
            Assert.IsTrue(Catalog.GetFloor(5).IsGuardianFloor);
            Assert.IsFalse(Catalog.GetFloor(4).IsGuardianFloor);
            Assert.AreEqual(2, Catalog.GetFloor(3).Waves.Count);
            Assert.AreEqual(10f, Catalog.GetFloor(1).Waves[1].DelayAfterPrevious, 1e-6f);
            Assert.AreEqual(EnemyKind.CageWarden, Catalog.GetFloor(5).Waves[0].Spawns[0].kind);
            Assert.AreEqual(700f, Catalog.GetEnemy(EnemyKind.CageWarden).MaxHp, 1e-6f);
        }
    }
}
