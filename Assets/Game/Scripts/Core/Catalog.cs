using System.Collections.Generic;

namespace RHMG.Core
{
    /// <summary>Static design tables (readonly). Lookups return null when missing; never throw.</summary>
    public static partial class Catalog
    {
        public const int FloorClearGold = 10;
        public const int FirstClearBonus = 5;
        public const int GuardianBonus = 25;
        public const float OutfitBonusCap = 0.20f;
        public const int FloorCount = 5;

        public const float BaseMaxHp = 100f;
        public const float BaseMoveSpeed = 4f;
        public const float BaseStaffDamage = 10f;
        public const float BaseAttackInterval = 0.65f;
        public const float BaseRange = 6f;
        public const float BaseDogDamage = 6f;
        public const float BaseDogInterval = 1.2f;
        public const float BaseCommandCooldown = 12f;
        public const float BasePower = 1f;

        public const string HealFallbackId = "upgrade.heal20";
        public const float HealFallbackAmount = 20f;

        public static readonly IReadOnlyList<string> DemoTrialSpiritIds = new string[] { "spirit.dew" };

        public static readonly IReadOnlyList<SpiritDefinition> Spirits = BuildSpirits();
        public static readonly IReadOnlyList<FusionRecipe> Recipes = BuildRecipes();

        public static SpiritDefinition GetSpirit(string id)
        {
            for (int i = 0; i < Spirits.Count; i++)
            {
                if (Spirits[i].Id == id)
                {
                    return Spirits[i];
                }
            }

            return null;
        }

        public static FusionRecipe GetRecipe(string recipeId)
        {
            for (int i = 0; i < Recipes.Count; i++)
            {
                if (Recipes[i].Id == recipeId)
                {
                    return Recipes[i];
                }
            }

            return null;
        }

        public static FusionRecipe FindRecipe(string a, string b)
        {
            for (int i = 0; i < Recipes.Count; i++)
            {
                if (Recipes[i].Matches(a, b))
                {
                    return Recipes[i];
                }
            }

            return null;
        }

        private static IReadOnlyList<FusionRecipe> BuildRecipes()
        {
            return new FusionRecipe[]
            {
                new FusionRecipe("recipe.steam", "spirit.dew", "spirit.ember", "spirit.steam")
            };
        }

        private static IReadOnlyList<SpiritDefinition> BuildSpirits()
        {
            return new SpiritDefinition[]
            {
                new SpiritDefinition(
                    "spirit.dew", "Dew", Element.Water, false,
                    6f, 1.0f, 0f, 0.30f, 0f,
                    15f, 3.0f, 10f, 0.4f,
                    "Giọt nước làm chậm", "Sóng đẩy + lá chắn nhỏ"),
                new SpiritDefinition(
                    "spirit.ember", "Ember", Element.Fire, false,
                    5f, 0.9f, 0f, 0f, 2f,
                    25f, 2.5f, 0f, 0f,
                    "Tia lửa gây cháy", "Vòng lửa quanh mục tiêu"),
                new SpiritDefinition(
                    "spirit.steam", "Hơi Nước", Element.Water, true,
                    12.65f, 1.3f, 1.5f, 0.35f, 0f,
                    46f, 3.5f, 0f, 0.5f,
                    "Nổ vùng nhỏ rồi làm chậm", "Bùng hơi nước diện rộng")
            };
        }
    }
}
