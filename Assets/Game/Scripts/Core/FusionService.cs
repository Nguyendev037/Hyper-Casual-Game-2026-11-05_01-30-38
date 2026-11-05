using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class FusionService
    {
        private readonly SaveData _data;
        private readonly IReadOnlyCollection<string> _trialSpiritIds;

        public FusionService(SaveData data, IReadOnlyCollection<string> trialSpiritIds)
        {
            _data = data;
            _trialSpiritIds = trialSpiritIds ?? new string[0];
        }

        public FusionCheck Preview(string a, string b, out FusionRecipe recipe)
        {
            recipe = null;
            if (a == b)
            {
                return FusionCheck.SameSpirit;
            }

            recipe = Catalog.FindRecipe(a, b);
            if (recipe == null)
            {
                return FusionCheck.NoRecipe;
            }

            if (!IsKnown(a) || !IsKnown(b))
            {
                return FusionCheck.MissingIngredient;
            }

            if (IsLocked(a) || IsLocked(b))
            {
                return FusionCheck.IngredientLocked;
            }

            return FusionCheck.Ok;
        }

        public FusionCheck Fuse(string a, string b)
        {
            FusionRecipe recipe;
            FusionCheck check = Preview(a, b, out recipe);
            if (check != FusionCheck.Ok)
            {
                return check;
            }

            FusionLink link = new FusionLink();
            link.recipeId = recipe.Id;
            link.ingredientA = recipe.IngredientA;
            link.ingredientB = recipe.IngredientB;
            _data.activeFusions.Add(link);
            return FusionCheck.Ok;
        }

        public bool Unfuse(string recipeId)
        {
            for (int i = 0; i < _data.activeFusions.Count; i++)
            {
                if (_data.activeFusions[i].recipeId == recipeId)
                {
                    _data.activeFusions.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool IsLocked(string spiritId)
        {
            for (int i = 0; i < _data.activeFusions.Count; i++)
            {
                FusionLink link = _data.activeFusions[i];
                if (link.ingredientA == spiritId || link.ingredientB == spiritId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>(owned + trial) minus locked ingredients, plus outputs of active fusions.</summary>
        public IReadOnlyList<string> AvailableSpirits()
        {
            List<string> result = new List<string>();
            AddUnlocked(result, _data.unlockedSpiritIds);
            AddUnlocked(result, _trialSpiritIds);
            for (int i = 0; i < _data.activeFusions.Count; i++)
            {
                FusionRecipe recipe = Catalog.GetRecipe(_data.activeFusions[i].recipeId);
                if (recipe != null && !result.Contains(recipe.OutputId))
                {
                    result.Add(recipe.OutputId);
                }
            }

            return result;
        }

        private bool IsKnown(string spiritId)
        {
            return _data.unlockedSpiritIds.Contains(spiritId) || Contains(_trialSpiritIds, spiritId);
        }

        private void AddUnlocked(List<string> target, IEnumerable<string> source)
        {
            foreach (string id in source)
            {
                if (!IsLocked(id) && !target.Contains(id))
                {
                    target.Add(id);
                }
            }
        }

        private static bool Contains(IReadOnlyCollection<string> collection, string id)
        {
            foreach (string item in collection)
            {
                if (item == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
