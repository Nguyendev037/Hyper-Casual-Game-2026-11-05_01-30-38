using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.UI
{
    public static class UiSkin
    {
        static readonly Dictionary<string, Sprite> ui = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        static readonly HashSet<string> warned = new HashSet<string>();
        static Font body;
        static Font title;
        static Font korean;

        /// <summary>Hangul-capable font used for every text while the UI language is Korean (SPEC-P04).</summary>
        static Font Korean
        {
            get
            {
                if (korean == null)
                {
                    korean = Resources.Load<Font>("RHMG/Fonts/NotoSansKR-Bold");
                    if (korean == null) Warn("font:NotoSansKR-Bold");
                }
                return korean;
            }
        }

        static bool UseKorean => RHMG.Core.Loc.Current == RHMG.Core.Language.Ko && Korean != null;

        public static Font Body => UseKorean ? Korean : LatinBody;

        public static Font Title => UseKorean ? Korean : LatinTitle;

        static Font LatinBody
        {
            get
            {
                if (body == null)
                {
                    body = Resources.Load<Font>("RHMG/Fonts/BeVietnamPro-SemiBold");
                    if (body == null) Warn("font:BeVietnamPro-SemiBold");
                    body = body ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return body;
            }
        }

        static Font LatinTitle
        {
            get
            {
                if (title == null)
                {
                    title = Resources.Load<Font>("RHMG/Fonts/BeVietnamPro-ExtraBold");
                    if (title == null) Warn("font:BeVietnamPro-ExtraBold");
                    title = title ?? LatinBody;
                }
                return title;
            }
        }

        public static Sprite Ui(string id) => Load(ui, "RHMG/UI/", id);
        public static Sprite Icon(string id) => Load(icons, "RHMG/Icons/", id);

        /// <summary>
        /// Resources/RHMG/Portraits/[id]. The portrait PNGs are imported as plain textures (textureType Default),
        /// so Resources.Load&lt;Sprite&gt; returns null for them: fall back to the texture and wrap it once.
        /// </summary>
        public static Sprite Portrait(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (portraits.TryGetValue(id, out Sprite cached)) return cached;
            string path = "RHMG/Portraits/" + id;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = id;
                }
            }
            portraits[id] = sprite;
            if (sprite == null) Warn(path);
            return sprite;
        }

        static readonly Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();

        public static string IconIdFor(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;
            if (contentId.StartsWith("upgrade.")) return "icon_upgrade_" + contentId.Substring(8).Replace('.', '_');
            if (contentId.StartsWith("spirit.")) return "icon_spirit_" + contentId.Substring(7).Replace('.', '_');
            if (contentId.StartsWith("outfit."))
            {
                string[] parts = contentId.Split('.');
                if (parts.Length >= 3) return "icon_outfit_" + parts[parts.Length - 1];
            }
            return null;
        }

        static Sprite Load(Dictionary<string, Sprite> cache, string path, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache.TryGetValue(id, out Sprite cached)) return cached;
            Sprite sprite = Resources.Load<Sprite>(path + id);
            cache[id] = sprite;
            if (sprite == null) Warn(path + id);
            return sprite;
        }

        static void Warn(string id)
        {
            if (warned.Add(id)) Debug.LogWarning("[RHMG.UI] Missing skin asset: " + id);
        }
    }
}
