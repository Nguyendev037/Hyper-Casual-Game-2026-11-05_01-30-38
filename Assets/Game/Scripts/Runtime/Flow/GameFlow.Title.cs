using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    // SPEC-P01 section 1: the title screen. Built from the "title" screen of ui_layout.json when it exists;
    // otherwise from the old "menu" screen plus the biggest available key art (fallback C3, no new design).
    public sealed partial class GameFlow
    {
        const string KeyartSprite = "title_keyart", LogoSprite = "logo_title";
        TitleFx titleFx;

        bool DrawLayoutTitle()
        {
            titleFx = null;
            string screen = UiLayout.Has("title") ? "title" : "menu";
            if (!Layout(screen)) return false;
            IDictionary<string, string> spriteOf = UiLayout.SpriteIds(screen);
            RectTransform keyart = null, logo = null;
            foreach (KeyValuePair<string, string> pair in spriteOf)
            {
                if (pair.Value == KeyartSprite) keyart = layout[pair.Key];
                if (pair.Value == LogoSprite) logo = layout[pair.Key];
            }

            if (screen == "menu") { keyart = AddFallbackKeyart(); logo = AddFallbackLogo(); }
            else if (keyart != null) EnsureKeyartImage(keyart);
            if (logo != null) EnsureLogoImage(logo);

            TryText("game_logo_text", "Red Hair Magic Girl");
            TryText("game_subtitle_text", "Cùng cún leo tháp, cứu tinh linh cát");
            TryText("menu_version", "v" + Application.version);
            LayoutAction("btn_menu_start", "Chơi", StartRun);
            LayoutAction("btn_menu_continue", Loc.F("Tiếp tục (Tầng {0})",
                HasCheckpoint ? save.runCheckpoint.floor : 1), ResumeRun);
            layout["btn_menu_continue"].gameObject.SetActive(HasCheckpoint);
            LayoutAction("btn_menu_wardrobe", "Tủ đồ", () => { wardrobeFromMenu = true; DrawWardrobe(); });
            LayoutAction("btn_menu_spirits", "Sổ tinh linh", DrawSpiritBook);
            LayoutAction("btn_menu_settings", "Cài đặt", ShowSettings);
            foreach (string id in new[] { "btn_menu_start", "btn_menu_continue", "btn_menu_wardrobe",
                         "btn_menu_spirits", "btn_menu_settings" })
                TitleFx.AddPress(layout[id].gameObject);
            // Credits has no cell of its own: the logo (image or text) opens it, as before.
            RectTransform creditsHost = logo != null ? logo : layout.ContainsKey("game_logo_text") ? layout["game_logo_text"] : null;
            if (creditsHost != null)
            {
                LayoutAction(creditsHost.name, null, ShowCredits);
                creditsHost.gameObject.name += "_Credits";
            }

            IDictionary<string, object> fx = UiLayout.Fx(screen);
            var sprites = new List<Sprite>();
            foreach (string id in TitleFx.SpriteIds(fx))
            {
                Sprite sprite = UiLayout.Sprite(id);
                if (sprite != null) sprites.Add(sprite);
            }
            titleFx = TitleFx.Attach(root, keyart, logo, fx, sprites.ToArray(), 5);
            return true;
        }

        void TryText(string id, string value)
        {
            if (layout.ContainsKey(id) && layout[id].GetComponentInChildren<Text>() != null) LayoutText(id, value);
        }

        // Girl image to use when title_keyart is not installed: the equipped outfit billboard, else the portrait.
        Sprite FallbackGirlSprite()
        {
            Sprite keyartArt = UiSkin.Ui(KeyartSprite);
            if (keyartArt != null) return keyartArt;
            WardrobeService service = wardrobe;
            Sprite sprite = service == null ? null : CharacterSprites.Girl(service.GetEquipped(OutfitSlot.Head),
                service.GetEquipped(OutfitSlot.Body), service.GetEquipped(OutfitSlot.Staff));
            return sprite != null ? sprite : UiSkin.Portrait("portrait_girl");
        }

        void EnsureKeyartImage(RectTransform keyart)
        {
            Image image = keyart.GetComponent<Image>();
            if (image == null || image.sprite != null) return;
            image.sprite = FallbackGirlSprite();
            image.color = image.sprite == null ? Color.clear : Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
        }

        // logo_title missing: the node would draw a flat colour box, so swap it for the plain title text.
        void EnsureLogoImage(RectTransform logo)
        {
            Image image = logo.GetComponent<Image>();
            if (image == null || image.sprite != null) { if (image != null) image.preserveAspect = true; return; }
            image.enabled = false;
            if (logo.GetComponentInChildren<Text>() != null) return;
            Text text = UiKit.Text(logo, "Red Hair Magic Girl", 96, UiLayout.Palette("accent_red"), TextAnchor.MiddleCenter);
            text.font = UiSkin.Title;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 24;
            text.resizeTextMaxSize = 96;
        }

        // Fallback logo: the image replaces the old title text (the text is never shown when the image exists).
        RectTransform AddFallbackLogo()
        {
            Sprite art = UiSkin.Ui(LogoSprite);
            if (art == null) return null;
            var go = new GameObject(LogoSprite, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 280f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            Image image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            foreach (string id in new[] { "game_logo_text", "game_subtitle_text" })
                if (layout.ContainsKey(id)) layout[id].gameObject.SetActive(false);
            layout[LogoSprite] = rect;
            return rect;
        }

        // No "title" screen: put the largest key art in the free band between the subtitle and the first button
        // (old menu elements untouched), behind the text so no control is covered.
        RectTransform AddFallbackKeyart()
        {
            var go = new GameObject("title_keyart_fallback", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            // 45% of the screen height; the lowest ~4% slips behind the first button, never over the face.
            rect.anchorMin = new Vector2(0.1f, 0.50f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = FallbackGirlSprite();
            image.color = image.sprite == null ? Color.clear : Color.white;
            int index = layout.ContainsKey("menu_bg") ? layout["menu_bg"].GetSiblingIndex() + 1 : 0;
            rect.SetSiblingIndex(index);
            return rect;
        }
    }
}
