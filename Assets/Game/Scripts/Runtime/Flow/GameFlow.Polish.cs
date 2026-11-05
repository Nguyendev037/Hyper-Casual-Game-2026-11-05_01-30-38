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
            panel.SetAsFirstSibling();
        }

        void DrawWardrobeCard(OutfitDefinition outfit, int index, bool owned, bool equipped,
            bool showcase)
        {
            float top = 0.80f - index * 0.105f;
            RectTransform card = UiKit.Panel(root, "OutfitCard", new Vector2(0.09f, top - 0.09f),
                new Vector2(0.91f, top), new Color(0.17f, 0.14f, 0.18f, 0.98f));
            Text title = UiKit.Text(card, GroupName(outfit.Slot) + " · " + outfit.DisplayName,
                44, Color.white);
            SetCardRect(title.rectTransform, new Vector2(0.03f, 0.56f), new Vector2(0.68f, 0.98f));
            Text detail = UiKit.Text(card, outfit.BenefitText + " · " + outfit.TradeoffText,
                40, Color.white);
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetCardRect(detail.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.68f, 0.58f));
            string label = equipped ? "Đang mặc" : owned ? "Mặc" : showcase
                ? "Thử" : "Mua " + outfit.Price;
            UnityEngine.UI.Button action = UiKit.Button(card, label, new Vector2(210, 100),
                new Color(0.26f, 0.21f, 0.27f, 1f));
            SetCardRect((RectTransform)action.transform, new Vector2(0.70f, 0.12f),
                new Vector2(0.98f, 0.88f));
            action.interactable = !equipped;
            action.onClick.AddListener(() => SelectOutfit(outfit));
            action.onClick.AddListener(() => audioHub.Play("ui"));
        }

        static string GroupName(OutfitSlot slot)
        {
            switch (slot)
            {
                case OutfitSlot.Head: return "Đầu";
                case OutfitSlot.Body: return "Thân";
                default: return "Gậy";
            }
        }

        static string DogTierName(DogTier tier)
        {
            switch (tier)
            {
                case DogTier.SandPup: return "Cún cát phép";
                case DogTier.SpiritHound: return "Linh khuyển";
                case DogTier.SunMoonLion: return "Sư Tử Nhật Nguyệt";
                default: return "Cún đồng hành";
            }
        }

        static string SpiritName(string id)
        {
            switch (id)
            {
                case "spirit.dew": return "Sương";
                case "spirit.ember": return "Ember";
                case "spirit.steam": return "Hơi Nước";
                default: return Catalog.GetSpirit(id).DisplayName;
            }
        }

        string CommandElementName()
        {
            switch (Catalog.GetSpirit(spiritId).Element)
            {
                case Element.Water: return "Thủy";
                case Element.Fire: return "Hỏa";
                case Element.Wood: return "Mộc";
                case Element.Metal: return "Kim";
                case Element.Earth: return "Thổ";
                case Element.Light: return "Ánh Sáng";
                case Element.Dark: return "Bóng Tối";
                default: return "Lệnh";
            }
        }

        Color CommandElementColor()
        {
            return RHMG.Runtime.Visuals.Palette.ForElement(Catalog.GetSpirit(spiritId).Element);
        }

        static void SetCardRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
