using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    // SPEC-P01 section 2: optional HUD v2 nodes. Every id below is OPTIONAL; when the json lacks one the HUD
    // simply stays as the v1 layout (the required v1 ids are validated in UiLayout).
    public sealed partial class GameFlow
    {
        static readonly string[] HudPortraitIds = { "hud_portrait", "girl_portrait", "hp_portrait", "portrait_girl" };
        static readonly string[] BorderSuffixes = { "_border", "_frame", "_ring" };

        void HudV2Extras()
        {
            foreach (string id in HudPortraitIds)
            {
                if (!layout.ContainsKey(id)) continue;
                Image image = layout[id].GetComponent<Image>();
                if (image == null || image.sprite != null) continue;
                Sprite portrait = UiSkin.Portrait("portrait_girl");
                if (portrait != null) LayoutIcon(id, portrait);
                else image.enabled = false;
            }
        }

        /// <summary>Colour token of the level border: 1-2 common, 3-4 rare, 5 gold.</summary>
        public static string LevelBorderToken(int level)
        {
            return level >= 5 ? "accent_gold_bright" : level >= 3 ? "card_rare_border" : "card_common_border";
        }

        void TintLevelBorder(string dockId, int level)
        {
            foreach (string suffix in BorderSuffixes)
            {
                if (!layout.TryGetValue(dockId + suffix, out RectTransform border)) continue;
                Image image = border.GetComponent<Image>();
                if (image == null) continue;
                image.color = UiLayout.Palette(LevelBorderToken(level));
                return;
            }
        }
    }
}
