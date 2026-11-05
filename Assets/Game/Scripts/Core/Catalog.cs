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
        public const int FloorCount = SurvivorTuning.FloorCount;

        public const float BaseMaxHp = 100f;
        public const float BaseMoveSpeed = 4f;
        public const float BaseStaffDamage = 10f;
        public const float BaseAttackInterval = 0.65f;
        public const float BaseRange = SurvivorTuning.BaseRange;
        public const float BaseDogDamage = 6f;
        public const float BaseDogInterval = 1.2f;
        public const float BaseCommandCooldown = 12f;
        public const float BasePower = 1f;

        public const string HealFallbackId = "upgrade.heal20";
        public const float HealFallbackAmount = 20f;

        public static readonly IReadOnlyList<string> DemoTrialSpiritIds = new string[] { "spirit.dew" };

        /// <summary>Base (non-fusion) spirits in roll order.</summary>
        public static readonly IReadOnlyList<string> BaseSpiritIds = new string[] { "spirit.dew", "spirit.ember", "spirit.bud" };

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

        public static bool IsBaseSpirit(string id)
        {
            SpiritDefinition def = GetSpirit(id);
            return def != null && !def.IsFusion;
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
                new FusionRecipe("recipe.steam", "spirit.dew", "spirit.ember", "spirit.steam"),
                new FusionRecipe("recipe.growth", "spirit.dew", "spirit.bud", "spirit.growth"),
                new FusionRecipe("recipe.wildfire", "spirit.ember", "spirit.bud", "spirit.wildfire")
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
                    "Giọt nước làm chậm", "Sóng đẩy + lá chắn nhỏ", 12f),
                new SpiritDefinition(
                    "spirit.ember", "Ember", Element.Fire, false,
                    5f, 0.9f, 0f, 0f, 2f,
                    25f, 2.5f, 0f, 0f,
                    "Tia lửa gây cháy", "Vòng lửa quanh mục tiêu", 12f),
                new SpiritDefinition(
                    "spirit.bud", "Bud", Element.Wood, false,
                    5f, 1.0f, 0f, 0f, 0f,
                    20f, 3.0f, 0f, 0.5f,
                    "Hạt mầm hồi máu khi trúng", "Rễ gai giữ chân + hồi máu", 12f, 0.5f, 8f),
                new SpiritDefinition(
                    "spirit.steam", "Hơi Nước", Element.Water, true,
                    12.65f, 1.3f, 1.5f, 0.35f, 0f,
                    46f, 3.5f, 0f, 0.5f,
                    "Nổ vùng nhỏ rồi làm chậm", "Bùng hơi nước diện rộng", 10f),
                new SpiritDefinition(
                    "spirit.growth", "Sinh Trưởng", Element.Wood, true,
                    11f, 1.1f, 1.2f, 0.25f, 0f,
                    40f, 4.0f, 0f, 0f,
                    "Nổ nhỏ, làm chậm và hồi máu", "Vùng hồi phục diện rộng", 10f, 1f, 15f),
                new SpiritDefinition(
                    "spirit.wildfire", "Hoả Mộc", Element.Fire, true,
                    10f, 0.8f, 1.5f, 0f, 4f,
                    55f, 3.5f, 0f, 0f,
                    "Lửa cháy lan quanh mục tiêu", "Bão lửa càn quét", 10f)
            };
        }
    }
}
