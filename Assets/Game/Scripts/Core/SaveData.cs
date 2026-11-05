using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    [Serializable]
    public sealed class BondEntry
    {
        public string spiritId;
        public int level;
    }

    [Serializable]
    public sealed class SlotEquip
    {
        public int slot;
        public string outfitId;
    }

    [Serializable]
    public sealed class FusionLink
    {
        public string recipeId;
        public string ingredientA;
        public string ingredientB;
    }

    [Serializable]
    public sealed partial class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int highestClearedFloor;
        public int gold;
        public int runCounter;
        public List<string> unlockedSpiritIds;
        public List<BondEntry> bondLevels;
        public List<string> ownedOutfitIds;
        public List<SlotEquip> equippedOutfits;
        public int dogSealMask;
        public List<int> claimedFirstClearFloors;
        public List<string> rewardTransactionIds;
        public List<FusionLink> activeFusions;
        public bool tutorialSeen;
        public bool musicOn = true;
        public bool sfxOn = true;
        public bool vibrationOn;

        public static SaveData CreateDefault()
        {
            SaveData data = new SaveData();
            data.Normalize();
            return data;
        }

        /// <summary>Repairs the data in place. Never throws.</summary>
        public void Normalize()
        {
            if (gold < 0)
            {
                gold = 0;
            }

            if (highestClearedFloor < 0)
            {
                highestClearedFloor = 0;
            }

            if (runCounter < 0)
            {
                runCounter = 0;
            }

            dogSealMask &= DogProgress.AllSevenMask;

            unlockedSpiritIds = DedupeStrings(unlockedSpiritIds, false);
            rewardTransactionIds = DedupeStrings(rewardTransactionIds, false);
            ownedOutfitIds = DedupeStrings(ownedOutfitIds, true);
            EnsureDefaultOutfitsOwned();
            NormalizeEquipped();
            NormalizeBonds();
            NormalizeFusions();
            NormalizeClaimedFloors();
        }

        public SaveData DeepClone()
        {
            SaveData copy = new SaveData();
            copy.schemaVersion = schemaVersion;
            copy.highestClearedFloor = highestClearedFloor;
            copy.gold = gold;
            copy.runCounter = runCounter;
            copy.dogSealMask = dogSealMask;
            copy.tutorialSeen = tutorialSeen;
            copy.musicOn = musicOn;
            copy.sfxOn = sfxOn;
            copy.vibrationOn = vibrationOn;
            copy.unlockedSpiritIds = unlockedSpiritIds == null ? null : new List<string>(unlockedSpiritIds);
            copy.ownedOutfitIds = ownedOutfitIds == null ? null : new List<string>(ownedOutfitIds);
            copy.claimedFirstClearFloors = claimedFirstClearFloors == null ? null : new List<int>(claimedFirstClearFloors);
            copy.rewardTransactionIds = rewardTransactionIds == null ? null : new List<string>(rewardTransactionIds);
            copy.bondLevels = CloneBonds();
            copy.equippedOutfits = CloneEquips();
            copy.activeFusions = CloneFusions();
            return copy;
        }

        private static List<string> DedupeStrings(List<string> source, bool onlyKnownOutfits)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i];
                if (string.IsNullOrEmpty(id) || result.Contains(id))
                {
                    continue;
                }

                if (onlyKnownOutfits && Catalog.GetOutfit(id) == null)
                {
                    continue;
                }

                result.Add(id);
            }

            return result;
        }

        private void EnsureDefaultOutfitsOwned()
        {
            for (int i = 0; i < Catalog.Outfits.Count; i++)
            {
                OutfitDefinition outfit = Catalog.Outfits[i];
                if (outfit.DefaultOwned && !ownedOutfitIds.Contains(outfit.Id))
                {
                    ownedOutfitIds.Add(outfit.Id);
                }
            }
        }

        private void NormalizeEquipped()
        {
            List<SlotEquip> fixedList = new List<SlotEquip>();
            Array slots = Enum.GetValues(typeof(OutfitSlot));
            foreach (OutfitSlot slot in slots)
            {
                string chosen = null;
                if (equippedOutfits != null)
                {
                    for (int i = 0; i < equippedOutfits.Count && chosen == null; i++)
                    {
                        SlotEquip entry = equippedOutfits[i];
                        if (entry != null && entry.slot == (int)slot && IsValidEquip(entry.outfitId, slot))
                        {
                            chosen = entry.outfitId;
                        }
                    }
                }

                if (chosen == null)
                {
                    chosen = Catalog.GetDefaultOutfit(slot).Id;
                }

                SlotEquip result = new SlotEquip();
                result.slot = (int)slot;
                result.outfitId = chosen;
                fixedList.Add(result);
            }

            equippedOutfits = fixedList;
        }

        private bool IsValidEquip(string outfitId, OutfitSlot slot)
        {
            OutfitDefinition def = Catalog.GetOutfit(outfitId);
            return def != null && def.Slot == slot && ownedOutfitIds.Contains(outfitId);
        }

        private void NormalizeBonds()
        {
            List<BondEntry> result = new List<BondEntry>();
            if (bondLevels != null)
            {
                for (int i = 0; i < bondLevels.Count; i++)
                {
                    BondEntry entry = bondLevels[i];
                    if (entry == null || string.IsNullOrEmpty(entry.spiritId) || ContainsBond(result, entry.spiritId))
                    {
                        continue;
                    }

                    if (entry.level < 0)
                    {
                        entry.level = 0;
                    }

                    result.Add(entry);
                }
            }

            bondLevels = result;
        }

        private static bool ContainsBond(List<BondEntry> list, string spiritId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].spiritId == spiritId)
                {
                    return true;
                }
            }

            return false;
        }

        private void NormalizeFusions()
        {
            List<FusionLink> result = new List<FusionLink>();
            if (activeFusions != null)
            {
                for (int i = 0; i < activeFusions.Count; i++)
                {
                    FusionLink link = activeFusions[i];
                    if (link == null || string.IsNullOrEmpty(link.recipeId) || Catalog.GetRecipe(link.recipeId) == null)
                    {
                        continue;
                    }

                    bool duplicate = false;
                    for (int j = 0; j < result.Count; j++)
                    {
                        duplicate |= result[j].recipeId == link.recipeId;
                    }

                    if (!duplicate)
                    {
                        result.Add(link);
                    }
                }
            }

            activeFusions = result;
        }

        private void NormalizeClaimedFloors()
        {
            List<int> result = new List<int>();
            if (claimedFirstClearFloors != null)
            {
                for (int i = 0; i < claimedFirstClearFloors.Count; i++)
                {
                    int floor = claimedFirstClearFloors[i];
                    if (floor >= 1 && !result.Contains(floor))
                    {
                        result.Add(floor);
                    }
                }
            }

            claimedFirstClearFloors = result;
        }
    }
}
