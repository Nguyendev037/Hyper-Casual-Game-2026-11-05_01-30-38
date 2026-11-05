using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class RewardAndWardrobeTests
    {
        private SaveData _data;

        [SetUp]
        public void SetUp()
        {
            _data = SaveData.CreateDefault();
        }

        [Test]
        public void GrantFloorClear_FirstTimeGivesBonus()
        {
            RewardService rewards = new RewardService(_data);
            Assert.AreEqual(15, rewards.GrantFloorClear("run1-1", 1, false));
            Assert.AreEqual(15, _data.gold);
            Assert.AreEqual(1, _data.highestClearedFloor);
        }

        [Test]
        public void GrantFloorClear_SameTransactionTwice_GrantsOnce()
        {
            RewardService rewards = new RewardService(_data);
            rewards.GrantFloorClear("run1-1", 2, false);
            int goldAfterFirst = _data.gold;
            Assert.AreEqual(0, rewards.GrantFloorClear("run1-1", 2, false));
            Assert.AreEqual(goldAfterFirst, _data.gold);
        }

        [Test]
        public void GrantFloorClear_SecondRunSameFloor_NoFirstClearBonus()
        {
            RewardService rewards = new RewardService(_data);
            rewards.GrantFloorClear("run1-1", 1, false);
            Assert.AreEqual(10, rewards.GrantFloorClear("run2-1", 1, false));
        }

        [Test]
        public void GrantFloorClear_GuardianFloorAddsBonus()
        {
            RewardService rewards = new RewardService(_data);
            Assert.AreEqual(40, rewards.GrantFloorClear("run1-1", 5, true));
            Assert.AreEqual(5, _data.highestClearedFloor);
        }

        [Test]
        public void GrantFloorClear_HighestFloorNeverDecreases()
        {
            RewardService rewards = new RewardService(_data);
            rewards.GrantFloorClear("run1-1", 4, false);
            rewards.GrantFloorClear("run2-1", 2, false);
            Assert.AreEqual(4, _data.highestClearedFloor);
        }

        [Test]
        public void GrantSpiritRescue_IsIdempotentAndAddsSeal()
        {
            RewardService rewards = new RewardService(_data);
            Assert.IsTrue(rewards.GrantSpiritRescue("spirit.ember", Element.Fire));
            Assert.IsFalse(rewards.GrantSpiritRescue("spirit.ember", Element.Fire));
            Assert.AreEqual(1, _data.unlockedSpiritIds.Count);
            Assert.AreEqual(1, DogProgress.SealCount(_data.dogSealMask));
        }

        [Test]
        public void Purchase_UnknownThenOwnedThenInsufficient()
        {
            WardrobeService w = new WardrobeService(_data);
            Assert.AreEqual(PurchaseResult.UnknownItem, w.Purchase("outfit.nope"));
            Assert.AreEqual(PurchaseResult.AlreadyOwned, w.Purchase("outfit.head.ash_hat"));
            Assert.AreEqual(PurchaseResult.InsufficientGold, w.Purchase("outfit.head.dawn_clip"));
            Assert.AreEqual(0, _data.gold);
        }

        [Test]
        public void Purchase_RapidRepeat_ChargesOnlyOnce()
        {
            _data.gold = 200;
            WardrobeService w = new WardrobeService(_data);
            Assert.AreEqual(PurchaseResult.Success, w.Purchase("outfit.head.dawn_clip"));
            Assert.AreEqual(PurchaseResult.AlreadyOwned, w.Purchase("outfit.head.dawn_clip"));
            Assert.AreEqual(PurchaseResult.AlreadyOwned, w.Purchase("outfit.head.dawn_clip"));
            Assert.AreEqual(120, _data.gold);
            Assert.IsTrue(w.IsOwned("outfit.head.dawn_clip"));
        }

        [Test]
        public void Equip_UnownedFails_OwnedSucceedsAndPersistsThroughClone()
        {
            _data.gold = 100;
            WardrobeService w = new WardrobeService(_data);
            Assert.IsFalse(w.Equip("outfit.staff.sunray"));
            w.Purchase("outfit.staff.sunray");
            Assert.IsTrue(w.Equip("outfit.staff.sunray"));
            Assert.AreEqual("outfit.staff.sunray", w.GetEquipped(OutfitSlot.Staff));

            WardrobeService reloaded = new WardrobeService(_data.DeepClone());
            Assert.AreEqual("outfit.staff.sunray", reloaded.GetEquipped(OutfitSlot.Staff));
            Assert.AreEqual(3, reloaded.EquippedIds().Count);
        }

        [Test]
        public void TryOnMode_EquipsUnownedWithoutChargingOrOwning()
        {
            _data.gold = 500;
            WardrobeService w = new WardrobeService(_data, true);
            Assert.IsTrue(w.Equip("outfit.body.rose_dress"));
            Assert.AreEqual(PurchaseResult.Success, w.Purchase("outfit.body.rose_dress"));
            Assert.AreEqual(500, _data.gold);
            Assert.IsFalse(w.IsOwned("outfit.body.rose_dress"));
        }
    }
}
