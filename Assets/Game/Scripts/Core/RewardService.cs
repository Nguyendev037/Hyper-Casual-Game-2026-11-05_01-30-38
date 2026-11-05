namespace RHMG.Core
{
    public sealed class RewardService
    {
        private readonly SaveData _data;

        public RewardService(SaveData data)
        {
            _data = data;
        }

        /// <summary>Idempotent per (runId, floor). Returns gold granted; 0 when already granted.</summary>
        public int GrantFloorClear(string runId, int floorIndex, bool isGuardianFloor)
        {
            if (string.IsNullOrEmpty(runId) || floorIndex < 1)
            {
                return 0;
            }

            string txn = runId + ":floor:" + floorIndex;
            if (_data.rewardTransactionIds.Contains(txn))
            {
                return 0;
            }

            _data.rewardTransactionIds.Add(txn);
            int gold = Catalog.FloorClearGold;
            if (!_data.claimedFirstClearFloors.Contains(floorIndex))
            {
                _data.claimedFirstClearFloors.Add(floorIndex);
                gold += Catalog.FirstClearBonus;
            }

            if (isGuardianFloor)
            {
                gold += Catalog.GuardianBonus;
            }

            _data.gold += gold;
            if (floorIndex > _data.highestClearedFloor)
            {
                _data.highestClearedFloor = floorIndex;
            }

            return gold;
        }

        /// <summary>Idempotent per spirit. Returns true when the rescue was new.</summary>
        public bool GrantSpiritRescue(string spiritId, Element element)
        {
            if (string.IsNullOrEmpty(spiritId))
            {
                return false;
            }

            string txn = "rescue:" + spiritId;
            if (_data.rewardTransactionIds.Contains(txn))
            {
                return false;
            }

            _data.rewardTransactionIds.Add(txn);
            if (!_data.unlockedSpiritIds.Contains(spiritId))
            {
                _data.unlockedSpiritIds.Add(spiritId);
            }

            if (!HasBond(spiritId))
            {
                BondEntry bond = new BondEntry();
                bond.spiritId = spiritId;
                bond.level = 1;
                _data.bondLevels.Add(bond);
            }

            _data.dogSealMask = DogProgress.AddSeal(_data.dogSealMask, element);
            return true;
        }

        private bool HasBond(string spiritId)
        {
            for (int i = 0; i < _data.bondLevels.Count; i++)
            {
                if (_data.bondLevels[i].spiritId == spiritId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
