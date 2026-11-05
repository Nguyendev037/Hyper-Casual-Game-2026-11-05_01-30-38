using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    /// <summary>Spirits carried inside ONE run (SPEC-P02 §2-§3). Not the permanent unlock list.</summary>
    public sealed class RunSpirits
    {
        private readonly List<RunSpiritEntry> _slots = new List<RunSpiritEntry>();
        private readonly List<string> _unlocked = new List<string>();

        public RunSpirits()
        {
        }

        /// <summary>unlockedBaseIds = permanently unlocked spirits (card.spirit.new only offers these).</summary>
        public RunSpirits(IEnumerable<string> unlockedBaseIds)
        {
            if (unlockedBaseIds != null)
            {
                foreach (string id in unlockedBaseIds)
                {
                    Unlock(id);
                }
            }
        }

        public IReadOnlyList<RunSpiritEntry> Slots
        {
            get { return _slots; }
        }

        public IReadOnlyList<string> Unlocked
        {
            get { return _unlocked; }
        }

        public bool Unlock(string spiritId)
        {
            if (!Catalog.IsBaseSpirit(spiritId) || _unlocked.Contains(spiritId))
            {
                return false;
            }

            _unlocked.Add(spiritId);
            return true;
        }

        public bool IsUnlocked(string spiritId)
        {
            return _unlocked.Contains(spiritId);
        }

        public bool Has(string spiritId)
        {
            return Find(spiritId) != null;
        }

        public int LevelOf(string spiritId)
        {
            RunSpiritEntry e = Find(spiritId);
            return e == null ? 0 : e.level;
        }

        /// <summary>Replaces the slots with a validated checkpoint list. False (slots untouched) when invalid.</summary>
        public bool Restore(IEnumerable<RunSpiritEntry> entries)
        {
            List<RunSpiritEntry> copy = new List<RunSpiritEntry>();
            if (entries != null)
            {
                foreach (RunSpiritEntry e in entries)
                {
                    if (e == null || Catalog.GetSpirit(e.spiritId) == null || e.level < 1 || e.level > SurvivorTuning.SpiritMaxLevel)
                    {
                        return false;
                    }

                    for (int i = 0; i < copy.Count; i++)
                    {
                        if (copy[i].spiritId == e.spiritId)
                        {
                            return false;
                        }
                    }

                    RunSpiritEntry c = new RunSpiritEntry();
                    c.spiritId = e.spiritId;
                    c.level = e.level;
                    copy.Add(c);
                }
            }

            if (copy.Count > SurvivorTuning.SpiritSlots)
            {
                return false;
            }

            _slots.Clear();
            _slots.AddRange(copy);
            return true;
        }

        /// <summary>False: slots full / already owned / not a base spirit.</summary>
        public bool Add(string spiritId)
        {
            if (!Catalog.IsBaseSpirit(spiritId) || Has(spiritId) || _slots.Count >= SurvivorTuning.SpiritSlots)
            {
                return false;
            }

            RunSpiritEntry e = new RunSpiritEntry();
            e.spiritId = spiritId;
            e.level = 1;
            _slots.Add(e);
            return true;
        }

        /// <summary>False: not owned / already at max level.</summary>
        public bool LevelUp(string spiritId)
        {
            RunSpiritEntry e = Find(spiritId);
            if (e == null || e.level >= SurvivorTuning.SpiritMaxLevel)
            {
                return false;
            }

            e.level++;
            return true;
        }

        /// <summary>First recipe (Catalog order) whose both ingredients are in slots at Lv &gt;= 3; null if none.</summary>
        public FusionRecipe FusablePair(out string a, out string b)
        {
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                FusionRecipe r = Catalog.Recipes[i];
                if (CanFuse(r))
                {
                    a = r.IngredientA;
                    b = r.IngredientB;
                    return r;
                }
            }

            a = null;
            b = null;
            return null;
        }

        /// <summary>All currently fusable recipes, in Catalog order (the station lets the player choose).</summary>
        public List<FusionRecipe> FusableRecipes()
        {
            List<FusionRecipe> list = new List<FusionRecipe>();
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                if (CanFuse(Catalog.Recipes[i]))
                {
                    list.Add(Catalog.Recipes[i]);
                }
            }

            return list;
        }

        /// <summary>Replaces the two ingredient slots by one result slot (Lv = min). False and no change when invalid.</summary>
        public bool Fuse(string recipeId)
        {
            FusionRecipe r = Catalog.GetRecipe(recipeId);
            if (r == null || !CanFuse(r))
            {
                return false;
            }

            RunSpiritEntry a = Find(r.IngredientA);
            RunSpiritEntry b = Find(r.IngredientB);
            int level = Math.Min(a.level, b.level);
            a.spiritId = r.OutputId;
            a.level = level;
            _slots.Remove(b);
            return true;
        }

        public bool AnyLevelable()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].level < SurvivorTuning.SpiritMaxLevel)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanFuse(FusionRecipe r)
        {
            RunSpiritEntry a = Find(r.IngredientA);
            RunSpiritEntry b = Find(r.IngredientB);
            return a != null && b != null && !Has(r.OutputId)
                && a.level >= SurvivorTuning.FusionMinLevel && b.level >= SurvivorTuning.FusionMinLevel;
        }

        private RunSpiritEntry Find(string spiritId)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].spiritId == spiritId)
                {
                    return _slots[i];
                }
            }

            return null;
        }
    }
}
