using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.Visuals;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        string fusionA = "spirit.dew";
        string fusionB = "spirit.ember";

        void DrawFusion()
        {
            SetMode(Mode.Rest);
            SetPaused(true);
            Clear();
            IReadOnlyList<string> available = fusion.AvailableSpirits();
            Label("Bàn dung hợp", 0.82f, 80);
            AddListPanel();
            Label(Loc.F("Nguyên liệu A: {0} · Dùng thử", SpiritName(fusionA)), 0.72f, 40);
            Label(Loc.F("Nguyên liệu B: {0}", SpiritName(fusionB)), 0.63f, 40);
            Button("Đổi A", 0.53f, () => CycleSpirit(available, true));
            Button("Đổi B", 0.43f, () => CycleSpirit(available, false));
            FusionCheck check = fusion.Preview(fusionA, fusionB, out FusionRecipe recipe);
            string preview = check == FusionCheck.Ok
                ? Loc.T(Catalog.GetSpirit(recipe.OutputId).DisplayName) + " — " + Loc.T("Nổ vùng nhỏ rồi làm chậm")
                : FusionMessage(check);
            Label(preview, 0.31f, 40);
            Log("fusion_preview", check == FusionCheck.Ok ? recipe.Id : check.ToString());
            Button("Dung hợp", 0.22f, () => ConfirmFusion(check, recipe));
            Button("Tách Hơi Nước", 0.12f, Unfuse);
            Button("Quay lại", 0.02f, () =>
            {
                SetPaused(false);
                DrawRest();
            });
        }

        void CycleSpirit(IReadOnlyList<string> available, bool first)
        {
            if (available.Count == 0)
            {
                return;
            }
            string current = first ? fusionA : fusionB;
            int index = 0;
            for (int i = 0; i < available.Count; i++)
            {
                if (available[i] == current)
                {
                    index = (i + 1) % available.Count;
                    break;
                }
            }
            if (first)
            {
                fusionA = available[index];
            }
            else
            {
                fusionB = available[index];
            }
            DrawFusion();
        }

        string FusionMessage(FusionCheck check)
        {
            switch (check)
            {
                case FusionCheck.NoRecipe:
                    return "Chưa có cộng hưởng";
                case FusionCheck.MissingIngredient:
                    return "Thiếu tinh linh nguyên liệu";
                case FusionCheck.IngredientLocked:
                    return "Nguyên liệu đang bị khóa";
                case FusionCheck.SameSpirit:
                    return "Hãy chọn hai tinh linh khác nhau";
                default:
                    return string.Empty;
            }
        }

        void ConfirmFusion(FusionCheck check, FusionRecipe recipe)
        {
            Log("fusion_preview", check == FusionCheck.Ok ? recipe.Id : check.ToString());
            if (check != FusionCheck.Ok)
            {
                Toast(FusionMessage(check));
                return;
            }
            FusionCheck result = fusion.Fuse(fusionA, fusionB);
            if (result != FusionCheck.Ok)
            {
                Toast(FusionMessage(result));
                return;
            }
            Log("fusion_confirm", recipe.Id);
            SaveNow();
            spiritId = recipe.OutputId;
            combat?.SetSpirit(spiritId);
            combat?.SpawnDummy(Vector3.zero);
            audioHub.Play("fusion");
            Toast("Đã dung hợp Hơi Nước");
        }

        void Unfuse()
        {
            if (fusion.Unfuse("recipe.steam"))
            {
                spiritId = "spirit.dew";
                combat?.SetSpirit(spiritId);
                SaveNow();
            }
        }

        void DrawWardrobe()
        {
            SetPaused(true);
            Clear();
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
        }

    }
}
