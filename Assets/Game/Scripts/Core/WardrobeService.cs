using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class WardrobeService
    {
        private readonly SaveData _data;
        private readonly bool _tryOnMode;

        public WardrobeService(SaveData data, bool tryOnMode = false)
        {
            _data = data;
            _tryOnMode = tryOnMode;
        }

        public bool IsOwned(string id)
        {
            return id != null && _data.ownedOutfitIds.Contains(id);
        }

        /// <summary>Order: unknown, owned, gold &lt; price, then pay + own. In try-on mode: no-op Success.</summary>
        public PurchaseResult Purchase(string outfitId)
        {
            OutfitDefinition outfit = Catalog.GetOutfit(outfitId);
            if (outfit == null)
            {
                return PurchaseResult.UnknownItem;
            }

            if (_tryOnMode)
            {
                return PurchaseResult.Success;
            }

            if (IsOwned(outfitId))
            {
                return PurchaseResult.AlreadyOwned;
            }

            if (_data.gold < outfit.Price)
            {
                return PurchaseResult.InsufficientGold;
            }

            _data.gold -= outfit.Price;
            _data.ownedOutfitIds.Add(outfitId);
            return PurchaseResult.Success;
        }

        public bool Equip(string outfitId)
        {
            OutfitDefinition outfit = Catalog.GetOutfit(outfitId);
            if (outfit == null || (!_tryOnMode && !IsOwned(outfitId)))
            {
                return false;
            }

            for (int i = 0; i < _data.equippedOutfits.Count; i++)
            {
                if (_data.equippedOutfits[i].slot == (int)outfit.Slot)
                {
                    _data.equippedOutfits[i].outfitId = outfitId;
                    return true;
                }
            }

            SlotEquip entry = new SlotEquip();
            entry.slot = (int)outfit.Slot;
            entry.outfitId = outfitId;
            _data.equippedOutfits.Add(entry);
            return true;
        }

        public string GetEquipped(OutfitSlot slot)
        {
            for (int i = 0; i < _data.equippedOutfits.Count; i++)
            {
                if (_data.equippedOutfits[i].slot == (int)slot)
                {
                    return _data.equippedOutfits[i].outfitId;
                }
            }

            return Catalog.GetDefaultOutfit(slot).Id;
        }

        public IReadOnlyList<string> EquippedIds()
        {
            List<string> ids = new List<string>();
            foreach (OutfitSlot slot in Enum.GetValues(typeof(OutfitSlot)))
            {
                ids.Add(GetEquipped(slot));
            }

            return ids;
        }
    }
}
