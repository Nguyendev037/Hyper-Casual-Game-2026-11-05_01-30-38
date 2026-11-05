using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.Visuals;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        // Spirit book (SPEC-P02 §5): read-only list of the 6 spirits, their recipes and whether they are unlocked.
        // Permanent fusion is gone: fusing happens at the stations during a run.
        void DrawSpiritBook()
        {
            bool fromMenu = mode == Mode.Menu;
            SetMode(Mode.Rest);
            SetPaused(true);
            Clear();
            AddListPanel();
            Label("Sổ tinh linh", 0.84f, 72);
            IReadOnlyList<SpiritDefinition> spirits = Catalog.Spirits;
            for (int i = 0; i < spirits.Count; i++)
            {
                SpiritDefinition def = spirits[i];
                bool unlocked = def.IsFusion
                    ? SpiritBookFusionUnlocked(def.Id)
                    : save.unlockedSpiritIds.Contains(def.Id);
                string line = SpiritName(def.Id) + " · " + ElementName(def.Element) + " · " +
                    (unlocked ? Loc.T("Đã mở khóa") : Loc.T("Chưa mở"));
                if (def.IsFusion)
                {
                    FusionRecipe recipe = RecipeFor(def.Id);
                    if (recipe != null)
                    {
                        line += "\n" + SpiritName(recipe.IngredientA) + " + " + SpiritName(recipe.IngredientB);
                    }
                }

                Text entry = UiKit.Text(root, line, 36, unlocked ? Color.white : new Color(1f, 1f, 1f, 0.55f),
                    TextAnchor.MiddleCenter);
                SetCardRect(entry.rectTransform, new Vector2(0.1f, 0.74f - i * 0.1f), new Vector2(0.9f, 0.82f - i * 0.1f));
                entry.raycastTarget = false;
            }

            Button("Quay lại", 0.02f, () =>
            {
                if (fromMenu) { ShowMenu(); return; }
                SetPaused(false);
                DrawRest();
            });
        }

        static FusionRecipe RecipeFor(string outputId)
        {
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                if (Catalog.Recipes[i].OutputId == outputId)
                {
                    return Catalog.Recipes[i];
                }
            }

            return null;
        }

        // A fusion spirit shows as unlocked once both of its ingredients are.
        bool SpiritBookFusionUnlocked(string outputId)
        {
            FusionRecipe recipe = RecipeFor(outputId);
            return recipe != null && save.unlockedSpiritIds.Contains(recipe.IngredientA) &&
                save.unlockedSpiritIds.Contains(recipe.IngredientB);
        }

        void DrawWardrobe()
        {
            SetPaused(true);
            Clear();
            if (DrawLayoutWardrobe()) return;
            AddListPanel();
            Label(Loc.F("Tủ đồ · {0}", mode == Mode.Showcase ? Loc.T("Thử miễn phí") : Loc.F("{0} vàng", save.gold)),
                0.86f, 80);
            IReadOnlyList<OutfitDefinition> outfits = Catalog.Outfits;
            for (int index = 0; index < outfits.Count; index++)
            {
                OutfitDefinition outfit = outfits[index];
                WardrobeService service = mode == Mode.Showcase ? showcaseWardrobe : wardrobe;
                bool owned = service.IsOwned(outfit.Id);
                bool equipped = service.GetEquipped(outfit.Slot) == outfit.Id;
                DrawWardrobeCard(outfit, index, owned, equipped, mode == Mode.Showcase);
            }
            Button("Quay lại", 0.005f, () =>
            {
                if (mode == Mode.Showcase)
                {
                    OpenShowcase();
                }
                else
                {
                    SetPaused(false);
                    DrawRest();
                }
            });
        }

        void SelectOutfit(OutfitDefinition outfit)
        {
            bool inShowcase = mode == Mode.Showcase;
            WardrobeService service = inShowcase ? showcaseWardrobe : wardrobe;
            if (!service.IsOwned(outfit.Id))
            {
                PurchaseResult result = service.Purchase(outfit.Id);
                if (result != PurchaseResult.Success)
                {
                    Toast(result == PurchaseResult.InsufficientGold ? "Không đủ vàng" : "Không thể mua món này");
                    return;
                }
                if (!inShowcase)
                {
                    Log("outfit_purchase", outfit.Id);
                }
            }
            if (!service.Equip(outfit.Id))
            {
                Toast("Không thể mặc món này");
                return;
            }
            if (!inShowcase)
            {
                SaveNow();
            }
            string[] equipped = new List<string>(service.EquippedIds()).ToArray();
            combat?.Girl.ApplyOutfit(outfit.Slot, outfit.AppearanceId);
            PlayerStats stats = StatsCalculator.Compute(
                equipped,
                run == null ? new string[0] : run.Upgrades.Taken,
                inShowcase ? showcaseSave.dogSealMask : save.dogSealMask,
                inShowcase);
            playerMaxHp = Mathf.CeilToInt(stats.MaxHp);
            combat?.ApplyStats(stats);
            DrawWardrobe();
            Toast(Loc.T(outfit.BenefitText));
            toastText.color = UiLayout.Palette("equip_fx");
            toastUntil = Time.unscaledTime + 1.5f;
        }

    }
}
