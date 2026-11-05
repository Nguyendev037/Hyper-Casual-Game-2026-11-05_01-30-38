using System;
using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        bool DrawLayoutWardrobe()
        {
            if (!Layout("wardrobe")) return false;
            WardrobeService service = mode == Mode.Showcase ? showcaseWardrobe : wardrobe;
            var items = new List<OutfitDefinition>();
            foreach (OutfitDefinition outfit in Catalog.Outfits)
                if (outfit.Slot == wardrobeTab) items.Add(outfit);
            if (wardrobeSelection == null || wardrobeSelection.Slot != wardrobeTab) wardrobeSelection = items[0];
            LayoutText("wardrobe_title", Loc.F("Tủ đồ · {0}", Loc.F("{0} vàng", save.gold)));
            LayoutAction("btn_wardrobe_back", "‹", () =>
            {
                if (wardrobeFromMenu) { wardrobeFromMenu = false; ShowMenu(); }
                else if (mode == Mode.Showcase) OpenShowcase();
                else { SetPaused(false); DrawRest(); }
            });
            LayoutAction("tab_hair", "Thân", () => { wardrobeTab = OutfitSlot.Body; DrawWardrobe(); });
            LayoutAction("tab_outfit", "Đầu", () => { wardrobeTab = OutfitSlot.Head; DrawWardrobe(); });
            LayoutAction("tab_staff", "Gậy", () => { wardrobeTab = OutfitSlot.Staff; DrawWardrobe(); });
            layout["tab_hair"].GetComponent<Image>().sprite = UiSkin.Ui(wardrobeTab == OutfitSlot.Body ? "tab_on" : "tab_off");
            layout["tab_outfit"].GetComponent<Image>().sprite = UiSkin.Ui(wardrobeTab == OutfitSlot.Head ? "tab_on" : "tab_off");
            layout["tab_staff"].GetComponent<Image>().sprite = UiSkin.Ui(wardrobeTab == OutfitSlot.Staff ? "tab_on" : "tab_off");
            layout["badge_equipped_tag"].gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
            {
                string prefix = "item_" + (i + 1);
                bool exists = i < items.Count;
                layout[prefix + "_card"].gameObject.SetActive(exists);
                if (!exists) continue;
                OutfitDefinition outfit = items[i];
                bool equipped = service.GetEquipped(outfit.Slot) == outfit.Id;
                string status = equipped ? Loc.T("Đang mặc") : service.IsOwned(outfit.Id) ? Loc.T("Mặc") : Loc.F("Mua {0}", outfit.Price);
                LayoutText(prefix + "_name", Loc.T(outfit.DisplayName));
                // Gemini supplied the icon extent in R03; use the approved name rectangle as its alignment.
                RectTransform title = layout[prefix + "_name"];
                Image icon = UiKit.Icon(layout[prefix + "_card"], UiSkin.IconIdFor(outfit.Id), new Vector2(220, 220));
                icon.rectTransform.anchorMin = title.anchorMin;
                icon.rectTransform.anchorMax = title.anchorMax;
                icon.rectTransform.pivot = new Vector2(0.5f, 0f);
                icon.rectTransform.anchoredPosition = title.anchoredPosition + new Vector2(0, title.sizeDelta.y * 0.5f);
                icon.raycastTarget = false;
                LayoutAction(prefix + "_card", null, () => { wardrobeSelection = outfit; DrawWardrobe(); });
                if (equipped)
                {
                    Image badge = UiKit.Icon(layout[prefix + "_card"], "icon_heart", new Vector2(80, 80));
                    badge.sprite = UiSkin.Ui("badge_equipped");
                    RectTransform source = layout["badge_equipped_tag"];
                    badge.rectTransform.anchorMin = source.anchorMin;
                    badge.rectTransform.anchorMax = source.anchorMax;
                    badge.rectTransform.pivot = source.pivot;
                    badge.rectTransform.anchoredPosition = source.anchoredPosition;
                }
            }
            string actionText = service.GetEquipped(wardrobeSelection.Slot) == wardrobeSelection.Id ? Loc.T("Đang mặc") :
                service.IsOwned(wardrobeSelection.Id) ? Loc.T("Mặc") : Loc.F("Mua {0}", wardrobeSelection.Price);
            actionText += "\n" + Loc.T(wardrobeSelection.BenefitText);
            if (wardrobeSelection.TradeoffText != "Không có") actionText += "\n" + Loc.T(wardrobeSelection.TradeoffText);
            LayoutAction("btn_equip_action", actionText, () => SelectOutfit(wardrobeSelection));
            var preview = layout["girl_preview_container"].gameObject.AddComponent<WardrobePreview>();
            preview.Initialize(service.GetEquipped(OutfitSlot.Head), service.GetEquipped(OutfitSlot.Body),
                service.GetEquipped(OutfitSlot.Staff));
            return true;
        }

        string RunSpiritsSummary()
        {
            if (run == null) return string.Empty;
            var names = new List<string>();
            foreach (RunSpiritEntry spirit in run.Spirits.Slots)
                names.Add(SpiritName(spirit.spiritId) + " " + spirit.level + "/5");
            return string.Join(" · ", names);
        }

        string RunDuration()
        {
            int seconds = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - runStarted));
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        bool DrawLayoutPause()
        {
            if (!Layout("pause")) return false;
            LayoutText("pause_title", "Tạm dừng");
            LayoutText("pause_quote", "Cùng cún leo tháp, cứu tinh linh cát");
            LayoutText("recap_floor_txt", run == null ? "" : Loc.F("Tầng {0}/{1}", run.CurrentFloor, Catalog.FloorCount));
            LayoutText("recap_spirits_txt", RunSpiritsSummary());
            LayoutText("recap_cards_txt", run == null ? "" : Loc.F("Số thẻ đã chọn: {0}", LevelUpsThisRun));
            LayoutText("recap_gold_txt", Loc.F("{0} vàng", save.gold));
            LayoutText("recap_time_txt", RunDuration());
            LayoutAction("btn_pause_resume", "Tiếp tục", ContinuePause);
            LayoutAction("btn_pause_restart", "Quay lại tầng", () => { if (run != null) RestartAfterDefeat(); else ShowMenu(); });
            LayoutAction("btn_pause_exit", "Về menu", ConfirmLeaveRun);
            return true;
        }

        const float ResultTextMargin = 24f;

        // Long (VI) spirit names touched the card edge: move the label into an inset child, keep best-fit min 24.
        static void InsetText(Text source, float margin)
        {
            if (source == null) return;
            var host = (RectTransform)source.transform;
            var child = new GameObject("Inset", typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)child.transform;
            rect.SetParent(host, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, 0f);
            rect.offsetMax = new Vector2(-margin, 0f);
            Text text = child.GetComponent<Text>();
            text.font = source.font;
            text.fontSize = source.resizeTextMaxSize;
            text.color = source.color;
            text.alignment = source.alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 24;
            text.resizeTextMaxSize = source.resizeTextMaxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = source.text;
            source.text = string.Empty;
        }

        bool DrawLayoutResult(bool defeated)
        {
            if (!Layout("result")) return false;
            LayoutText("result_title", defeated ? "Thất bại" : "Chiến thắng · kết quả chuyến đi");
            LayoutText("result_subtitle", defeated ? "Quay lại đầu cụm để tiếp tục" : "Anh trai được giải thoát!");
            LayoutText("stat_floor_lbl", "Tầng đạt");
            LayoutText("stat_floor_val", (run == null ? save.highestClearedFloor : run.CurrentFloor).ToString());
            LayoutText("stat_level_lbl", "Cấp độ");
            LayoutText("stat_level_val", (run == null ? 1 : run.Progress.Level).ToString());
            LayoutText("stat_gold_lbl", "Vàng nhận");
            LayoutText("stat_gold_val", (run == null ? 0 : run.GoldEarnedThisRun).ToString());
            LayoutText("stat_kills_lbl", "Linh thạch");
            LayoutText("stat_kills_val", GemPickupsThisRun.ToString());
            LayoutText("stat_spirits_lbl", "Tinh linh");
            InsetText(LayoutText("stat_spirits_val", RunSpiritsSummary()), ResultTextMargin);
            LayoutText("stat_time_lbl", "Thời gian");
            LayoutText("stat_time_val", RunDuration());
            LayoutAction("btn_result_again", "Chơi lại", defeated ? (Action)RestartAfterDefeat : StartRun);
            LayoutAction("btn_result_menu", "Về menu", ShowMenu);
            return true;
        }
    }
}
