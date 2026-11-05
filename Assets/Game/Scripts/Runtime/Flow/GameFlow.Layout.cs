using System;
using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        Dictionary<string, RectTransform> layout;
        bool approvedHud;
        OutfitSlot wardrobeTab = OutfitSlot.Body;
        OutfitDefinition wardrobeSelection;
        bool wardrobeFromMenu;

        bool Layout(string screen)
        {
            layout = new Dictionary<string, RectTransform>();
            var binders = new Dictionary<string, Action<RectTransform>>();
            foreach (string key in UiLayout.Ids(screen))
            {
                string id = key;
                binders[id] = rect => layout[id] = rect;
            }
            return UiLayout.Apply(screen, root, binders);
        }

        Text LayoutText(string id, string value)
        {
            Text text = layout[id].GetComponentInChildren<Text>();
            if (text != null) text.text = Loc.T(value);
            return text;
        }

        void LayoutAction(string id, string label, Action action)
        {
            RectTransform node = layout[id];
            if (label != null) LayoutText(id, label);
            Graphic graphic = node.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
        }

        void LayoutIcon(string id, Sprite sprite)
        {
            Image image = layout[id].GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.enabled = sprite != null;
        }

        Image LayoutFill(string id)
        {
            Image fill = layout[id].GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            return fill;
        }

        bool DrawLayoutMenu()
        {
            if (!Layout("menu")) return false;
            LayoutText("game_logo_text", "Red Hair Magic Girl");
            LayoutText("game_subtitle_text", "Cùng cún leo tháp, cứu tinh linh cát");
            LayoutText("menu_version", "v" + Application.version);
            LayoutAction("btn_menu_start", "Chơi", StartRun);
            LayoutAction("btn_menu_continue", Loc.F("Tiếp tục (Tầng {0})",
                HasCheckpoint ? save.runCheckpoint.floor : 1), ResumeRun);
            layout["btn_menu_continue"].gameObject.SetActive(HasCheckpoint);
            LayoutAction("btn_menu_wardrobe", "Tủ đồ", () => { wardrobeFromMenu = true; DrawWardrobe(); });
            LayoutAction("btn_menu_spirits", "Sổ tinh linh", DrawSpiritBook);
            LayoutAction("btn_menu_settings", "Cài đặt", ShowSettings);
            // Auxiliary actions have no approved menu cells. Credits uses the logo; showcase remains at Rest.
            LayoutAction("game_logo_text", null, ShowCredits);
            layout["game_logo_text"].gameObject.name = "game_logo_text_Credits";
            return true;
        }

        bool DrawLayoutHud(bool bossFloor)
        {
            approvedHud = false;
            if (!Layout("hud")) return false;
            approvedHud = true;
            hpFill = LayoutFill("hp_bar_fill");
            hpText = LayoutText("hp_text", string.Empty);
            levelText = LayoutText("level_text", string.Empty);
            LayoutText("xp_level_label", "Linh thạch");
            floorText = LayoutText("floor_badge", string.Empty);
            timeText = LayoutText("timer_badge", string.Empty);
            goldText = LayoutText("gold_badge", string.Empty);
            LayoutIcon("gold_icon", UiSkin.Icon("icon_gold"));
            xpFill = layout["xp_bar_fill"];
            bossFill = LayoutFill("boss_bar_fill");
            bossLabel = LayoutText("boss_title", bossFloor ? BossNameFor(run.CurrentFloor) : string.Empty);
            layout["boss_bar_frame"].gameObject.SetActive(bossFloor);
            LayoutAction("btn_pause", null, Pause);
            HudV2Extras();
            for (int i = 0; i < 3; i++)
            {
                string id = "spirit_dock_" + (i + 1);
                bool active = run != null && i < run.Spirits.Slots.Count;
                layout[id].gameObject.SetActive(active);
                if (!active) continue;
                Sprite icon = UiSkin.Icon(UiSkin.IconIdFor(run.Spirits.Slots[i].spiritId));
                // v2 docks keep their frame art and carry the icon in a child; v1 docks are the icon itself.
                if (layout.ContainsKey(id + "_icon")) LayoutIcon(id + "_icon", icon);
                else LayoutIcon(id, icon);
                TintLevelBorder(id, run.Spirits.Slots[i].level);
            }
            // Share the approved timer rectangle for day/night and floor time; no extra overlay.
            clockFill = null;
            clockIcon = null;
            clockText = null;
            displayedHp = displayedClock = displayedGold = displayedFloor = displayedLevel = displayedTime = int.MinValue;
            displayedPhase = (DayPhase)(-1);
            displayedXp = -1f;
            return true;
        }

        bool DrawLayoutCards()
        {
            if (!Layout("levelup")) return false;
            LayoutText("modal_title", "Lên cấp! Chọn một thẻ");
            LayoutText("modal_subtitle", Loc.F("Cấp {0}", run.Progress.Level));
            LevelUpCard selected = run.Cards.Count > 0 ? run.Cards[0] : null;
            for (int i = 0; i < 3; i++)
            {
                string prefix = "card_" + (i + 1);
                bool active = i < run.Cards.Count;
                layout[prefix].gameObject.SetActive(active);
                if (!active) continue;
                LevelUpCard card = run.Cards[i];
                DescribeCard(card, out string icon, out string title, out string body);
                LayoutText(prefix + "_name", title);
                LayoutText(prefix + "_desc", body);
                string rank = card.Kind == CardKind.Stat ? "Thường" : card.Kind == CardKind.Heal ? "Hồi máu" : "Tinh linh";
                string progress = string.Empty;
                if (card.Kind == CardKind.Stat)
                {
                    UpgradeDefinition definition = Catalog.GetUpgrade(card.TargetId);
                    progress = (run.Upgrades.StackCount(card.TargetId) + 1) + "/" + definition.MaxStacks;
                    if (definition.MaxStacks == 3) rank = "Hiếm";
                }
                else if (card.Kind == CardKind.SpiritNew || card.Kind == CardKind.SpiritUp)
                    progress = (run.Spirits.LevelOf(card.TargetId) + 1) + "/5";
                LayoutText(prefix + "_rank", Loc.T(rank) + " · " + progress);
                layout[prefix].GetComponent<Image>().sprite = UiSkin.Ui(card.Kind == CardKind.Stat
                    ? (rank == "Hiếm" ? "card_rare" : "card_common") : "card_spirit");
                LayoutIcon(prefix + "_icon", UiSkin.Icon(icon ?? "icon_heart"));
                LayoutAction(prefix, null, () =>
                {
                    selected = card;
                    foreach (string key in new[] { "card_1", "card_2", "card_3" })
                        layout[key].localScale = Vector3.one;
                    // Selection is indicated by the approved rarity art, without moving the card.
                    layout[prefix].GetComponent<Image>().sprite = UiSkin.Ui("card_rare");
                });
            }
            LayoutAction("btn_confirm_card", "Xác nhận", () => { if (selected != null) ChooseLevelUpCard(selected); });
            return true;
        }

        bool DrawLayoutStation(List<string> titles, List<string> descriptions, List<Sprite> icons, List<Action> actions,
            string heading)
        {
            if (!Layout("station")) return false;
            LayoutText("st_title", heading);
            LayoutText("st_subtitle", "Chọn một tinh linh");
            for (int i = 0; i < 3; i++)
            {
                string number = (i + 1).ToString();
                bool active = i < actions.Count;
                layout["st_slot_" + number].gameObject.SetActive(active);
                if (!active) continue;
                LayoutText("st_name_" + number, titles[i]);
                LayoutText("st_desc_" + number, descriptions[i]);
                LayoutIcon("st_icon_" + number, icons[i]);
                LayoutAction("st_slot_" + number, null, actions[i]);
            }
            LayoutAction("btn_leave_station", "Quay lại", ReturnToStation);
            return true;
        }
    }
}
