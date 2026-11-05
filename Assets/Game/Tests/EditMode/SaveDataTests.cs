using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class SaveDataTests
    {
        [Test]
        public void CreateDefault_HasDefaultOutfitsOwnedAndEquipped()
        {
            SaveData data = SaveData.CreateDefault();
            Assert.AreEqual(0, data.gold);
            Assert.AreEqual(3, data.ownedOutfitIds.Count);
            Assert.Contains("outfit.head.ash_hat", data.ownedOutfitIds);
            Assert.Contains("outfit.body.journey_cloak", data.ownedOutfitIds);
            Assert.Contains("outfit.staff.sprout", data.ownedOutfitIds);
            Assert.AreEqual(3, data.equippedOutfits.Count);
            Assert.AreEqual(0, data.unlockedSpiritIds.Count);
            Assert.AreEqual(0, data.activeFusions.Count);
        }

        [Test]
        public void Normalize_NullListsAndNegativeGold_AreRepaired()
        {
            SaveData data = new SaveData();
            data.gold = -50;
            data.Normalize();
            Assert.AreEqual(0, data.gold);
            Assert.IsNotNull(data.unlockedSpiritIds);
            Assert.IsNotNull(data.bondLevels);
            Assert.IsNotNull(data.claimedFirstClearFloors);
            Assert.IsNotNull(data.rewardTransactionIds);
            Assert.IsNotNull(data.activeFusions);
        }

        [Test]
        public void Normalize_DropsUnknownOutfitsAndDedupes()
        {
            SaveData data = new SaveData();
            data.ownedOutfitIds = new List<string> { "outfit.fake", "outfit.head.dawn_clip", "outfit.head.dawn_clip" };
            data.claimedFirstClearFloors = new List<int> { 2, 2, 3 };
            data.Normalize();
            Assert.IsFalse(data.ownedOutfitIds.Contains("outfit.fake"));
            Assert.AreEqual(1, data.ownedOutfitIds.FindAll(x => x == "outfit.head.dawn_clip").Count);
            Assert.AreEqual(4, data.ownedOutfitIds.Count);
            Assert.AreEqual(2, data.claimedFirstClearFloors.Count);
        }

        [Test]
        public void Normalize_EquipNotOwnedOrWrongSlot_FallsBackToDefault()
        {
            SaveData data = SaveData.CreateDefault();
            data.equippedOutfits[0].outfitId = "outfit.body.rose_dress";
            data.equippedOutfits[1].outfitId = "outfit.body.rose_dress";
            data.Normalize();
            Assert.AreEqual("outfit.head.ash_hat", data.equippedOutfits[0].outfitId);
            Assert.AreEqual("outfit.body.journey_cloak", data.equippedOutfits[1].outfitId);
        }

        [Test]
        public void Normalize_MissingSlotsRestored()
        {
            SaveData data = SaveData.CreateDefault();
            data.equippedOutfits.Clear();
            data.Normalize();
            Assert.AreEqual(3, data.equippedOutfits.Count);
        }

        [Test]
        public void DeepClone_IsIndependentCopy()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 12;
            data.unlockedSpiritIds.Add("spirit.ember");
            SaveData copy = data.DeepClone();

            copy.gold = 99;
            copy.unlockedSpiritIds.Add("spirit.dew");
            copy.equippedOutfits[0].outfitId = "x";

            Assert.AreEqual(12, data.gold);
            Assert.AreEqual(1, data.unlockedSpiritIds.Count);
            Assert.AreEqual("outfit.head.ash_hat", data.equippedOutfits[0].outfitId);
        }
    }
}
