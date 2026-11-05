using RHMG.Core;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        void AddListPanel()
        {
            RectTransform panel = UiKit.Panel(root, "ListPanel", new Vector2(0.07f, 0.12f),
                new Vector2(0.93f, 0.88f), new Color(0.08f, 0.06f, 0.1f, 0.88f));
            // Dark night card instead of the light parchment: white text needs a dark backing (SPEC-P02 §1).
            Sprite dark = UiSkin.Ui("card");
            if (dark != null)
            {
                Image image = panel.GetComponent<Image>();
                image.sprite = dark;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            // Right above the backdrop/tint that Clear() placed first, below every label and button.
            int index = 0;
            while (index < root.childCount &&
                   (root.GetChild(index).name == "Background" || root.GetChild(index).name == "TowerTint"))
            {
                index++;
            }
            panel.SetSiblingIndex(index);
        }

        void DrawWardrobeCard(OutfitDefinition outfit, int index, bool owned, bool equipped,
            bool showcase)
        {
            int column = index % 3;
            int row = index / 3;
            // Three columns inside the list panel (0.07–0.93): width 0.25W, gap 0.035W (≥ 32 px at 966 wide).
            float left = 0.09f + column * 0.285f;
            float centerY = row == 0 ? 0.66f : 0.35f;
            UnityEngine.UI.Button action = UiKit.Button(root, string.Empty, new Vector2(270f, CardHeight),
                new Color(0.17f, 0.14f, 0.18f, 0.98f));
            action.gameObject.name = "OutfitCard";
            RectTransform card = (RectTransform)action.transform;
            card.anchorMin = new Vector2(left, centerY);
            card.anchorMax = new Vector2(left + 0.25f, centerY);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(0f, CardHeight);
            card.anchoredPosition = Vector2.zero;
            Image cardBackground = card.GetComponent<Image>();
            cardBackground.sprite = UiSkin.Ui("card") ?? cardBackground.sprite;
            if (UiSkin.Ui("card") != null) cardBackground.color = Color.white;
            Text buttonLabel = action.GetComponentInChildren<Text>();
            buttonLabel.text = string.Empty;
            Image icon = UiKit.Icon(card, UiSkin.IconIdFor(outfit.Id), new Vector2(112, 112));
            SetCardRect(icon.rectTransform, new Vector2(0.25f, 0.71f), new Vector2(0.75f, 0.95f));
            // Name only: the slot is obvious from the icon and the prefix pushed long EN/KO names off the card.
            Text title = UiKit.Text(card, Loc.T(outfit.DisplayName), 36, Color.white, TextAnchor.MiddleCenter);
            FitCardText(title);
            SetCardRect(title.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.71f));
            // Benefit + drawback: the drawback must be visible before buying (SPEC-P02 §4).
            string effects = Loc.T(outfit.BenefitText);
            if (!string.IsNullOrEmpty(outfit.TradeoffText) && outfit.TradeoffText != "Không có")
            {
                effects += "\n<color=#FF9C8A>" + Loc.T(outfit.TradeoffText) + "</color>";
            }
            Text detail = UiKit.Text(card, effects, 36, Color.white, TextAnchor.MiddleCenter);
            FitCardText(detail);
            SetCardRect(detail.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.52f));
            string label = equipped ? "Đang mặc" : owned ? "Mặc" : showcase
                ? "Thử" : Loc.F("Mua {0}", outfit.Price);
            Text status = UiKit.Text(card, label, 36, RHMG.Runtime.Visuals.Palette.LightGold, TextAnchor.MiddleCenter);
            FitCardText(status);
            SetCardRect(status.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.19f));
            action.interactable = !equipped;
            action.onClick.AddListener(() => SelectOutfit(outfit));
            action.onClick.AddListener(() => audioHub.Play("ui"));
            sizeAudit.Capture(root, mode.ToString());
        }

        const float CardHeight = 420f;

        // Long names (EN/KO) shrink to fit instead of being cut; 28 px is the agreed floor for card captions.
        static void FitCardText(Text text)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 28;
            text.resizeTextMaxSize = 36;
        }

        static string GroupName(OutfitSlot slot)
        {
            switch (slot)
            {
                case OutfitSlot.Head: return Loc.T("Đầu");
                case OutfitSlot.Body: return Loc.T("Thân");
                default: return Loc.T("Gậy");
            }
        }

        static string DogTierName(DogTier tier)
        {
            switch (tier)
            {
                case DogTier.SandPup: return Loc.T("Cún cát phép");
                case DogTier.SpiritHound: return Loc.T("Linh khuyển");
                case DogTier.SunMoonLion: return Loc.T("Sư Tử Nhật Nguyệt");
                default: return Loc.T("Cún đồng hành");
            }
        }

        static string SpiritName(string id)
        {
            switch (id)
            {
                case "spirit.dew": return Loc.T("Sương");
                case "spirit.ember": return "Ember";
                case "spirit.steam": return Loc.T("Hơi Nước");
                default: return Loc.T(Catalog.GetSpirit(id).DisplayName);
            }
        }

        static string ElementName(Element element)
        {
            switch (element)
            {
                case Element.Water: return Loc.T("Thủy");
                case Element.Fire: return Loc.T("Hỏa");
                case Element.Wood: return Loc.T("Mộc");
                case Element.Metal: return Loc.T("Kim");
                case Element.Earth: return Loc.T("Thổ");
                case Element.Light: return Loc.T("Ánh Sáng");
                case Element.Dark: return Loc.T("Bóng Tối");
                default: return Loc.T("Lệnh");
            }
        }

        static void SetCardRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
