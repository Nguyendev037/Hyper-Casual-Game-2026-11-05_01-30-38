using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed partial class SaveData
    {
        private List<BondEntry> CloneBonds()
        {
            if (bondLevels == null)
            {
                return null;
            }

            List<BondEntry> list = new List<BondEntry>();
            for (int i = 0; i < bondLevels.Count; i++)
            {
                BondEntry source = bondLevels[i];
                if (source == null)
                {
                    list.Add(null);
                    continue;
                }

                BondEntry entry = new BondEntry();
                entry.spiritId = source.spiritId;
                entry.level = source.level;
                list.Add(entry);
            }

            return list;
        }

        private List<SlotEquip> CloneEquips()
        {
            if (equippedOutfits == null)
            {
                return null;
            }

            List<SlotEquip> list = new List<SlotEquip>();
            for (int i = 0; i < equippedOutfits.Count; i++)
            {
                SlotEquip source = equippedOutfits[i];
                if (source == null)
                {
                    list.Add(null);
                    continue;
                }

                SlotEquip entry = new SlotEquip();
                entry.slot = source.slot;
                entry.outfitId = source.outfitId;
                list.Add(entry);
            }

            return list;
        }

        private List<FusionLink> CloneFusions()
        {
            if (activeFusions == null)
            {
                return null;
            }

            List<FusionLink> list = new List<FusionLink>();
            for (int i = 0; i < activeFusions.Count; i++)
            {
                FusionLink source = activeFusions[i];
                if (source == null)
                {
                    list.Add(null);
                    continue;
                }

                FusionLink link = new FusionLink();
                link.recipeId = source.recipeId;
                link.ingredientA = source.ingredientA;
                link.ingredientB = source.ingredientB;
                list.Add(link);
            }

            return list;
        }

        private RunCheckpoint CloneCheckpoint()
        {
            if (runCheckpoint == null)
            {
                return null;
            }

            RunCheckpoint c = new RunCheckpoint();
            c.valid = runCheckpoint.valid;
            c.runId = runCheckpoint.runId;
            c.seed = runCheckpoint.seed;
            c.floor = runCheckpoint.floor;
            c.level = runCheckpoint.level;
            c.xp = runCheckpoint.xp;
            c.hp = runCheckpoint.hp;
            c.takenCards = runCheckpoint.takenCards == null ? null : new List<string>(runCheckpoint.takenCards);
            if (runCheckpoint.spirits != null)
            {
                c.spirits = new List<RunSpiritEntry>();
                for (int i = 0; i < runCheckpoint.spirits.Count; i++)
                {
                    RunSpiritEntry e = runCheckpoint.spirits[i];
                    if (e == null)
                    {
                        c.spirits.Add(null);
                        continue;
                    }

                    RunSpiritEntry copy = new RunSpiritEntry();
                    copy.spiritId = e.spiritId;
                    copy.level = e.level;
                    c.spirits.Add(copy);
                }
            }

            return c;
        }
    }
}
