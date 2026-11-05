using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    public static partial class UiKit
    {
        static Sprite circleSprite;
        static Sprite roundedSprite;
        static Font Font => UiSkin.Body;

        public static RectTransform Panel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            // Skin only visible, non-fullscreen panels: roots, tints and touch areas stay as-is.
            bool fullScreen = anchorMin == Vector2.zero && anchorMax == Vector2.one;
            Sprite skin = color.a < 0.05f || fullScreen ? null : UiSkin.Ui("panel");
            image.color = skin == null ? color : Color.white;
            image.sprite = skin ?? RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return rect;
        }

        public static Button Button(Transform parent, string label, Vector2 size, Color color)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = UiSkin.Ui(color == RHMG.Runtime.Visuals.Palette.HairRed
                ? "button_primary" : "button_secondary") ?? RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = image.sprite == RoundedSprite() ? color : Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            AddPressFeedback(button);
            var text = Text(go.transform, label, 48, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static Text Text(
            Transform parent,
            string value,
            int size,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Font;
            // Vietnamese source sentence → current language (SPEC-P04). Already-formatted text passes through.
            text.text = RHMG.Core.Loc.T(value);
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            // Noto Sans KR has a taller line than Be Vietnam Pro: Truncate would hide whole lines in Korean.
            text.verticalOverflow = RHMG.Core.Loc.Current == RHMG.Core.Language.Ko
                ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.06f, 0.1f, 0.95f);
            outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        public static Image Circle(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = CircleSprite();
            image.color = color;
            return image;
        }

        public static Button CircleButton(Transform parent, string label, Color color)
        {
            var go = new GameObject("CircleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = UiSkin.Ui("circle_button") ?? CircleSprite();
            image.color = image.sprite == CircleSprite() ? color : Color.white;
            image.type = Image.Type.Simple;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            AddPressFeedback(button);
            Text text = Text(go.transform, label, 48, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static RectTransform Countdown(Transform parent, string name, out Image fill)
        {
            Image background = Circle(parent, name, new Color(0.08f, 0.07f, 0.1f, 0.82f));
            Image image = Circle(background.transform, "Fill",
                new Color(0.94f, 0.77f, 0.32f, 0.95f));
            RectTransform fillRect = (RectTransform)image.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            fill = image;
            return (RectTransform)background.transform;
        }

        public static RectTransform ProgressBar(
            Transform parent,
            string name,
            Color fillColor,
            out Image fill)
        {
            var background = Panel(parent, name, Vector2.zero, Vector2.one, new Color(0.08f, 0.07f, 0.1f, 0.9f));
            var foreground = Panel(background, "Fill", Vector2.zero, Vector2.one, fillColor);
            fill = foreground.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            background.GetComponent<Image>().raycastTarget = false;
            fill.raycastTarget = false;
            Sprite frame = UiSkin.Ui("bar_frame");
            Sprite barFill = UiSkin.Ui("bar_fill");
            if (frame != null)
            {
                // bar_frame (256x64) has a heart baked in on the left and a narrow channel:
                // draw it unsliced and fit the fill into the channel. Callers size the bar 4:1.
                Image frameImage = background.GetComponent<Image>();
                frameImage.sprite = frame;
                frameImage.type = Image.Type.Simple;
                frameImage.color = Color.white;
                foreground.anchorMin = BarChannelMin;
                foreground.anchorMax = BarChannelMax;
                foreground.offsetMin = Vector2.zero;
                foreground.offsetMax = Vector2.zero;
            }
            if (barFill != null) fill.sprite = barFill;
            return background;
        }

        /// <summary>Channel of the skinned bar frame as anchors (measured on bar_frame.png).</summary>
        public static readonly Vector2 BarChannelMin = new Vector2(0.33f, 0.31f);
        public static readonly Vector2 BarChannelMax = new Vector2(0.83f, 0.59f);

        static Sprite CircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "UiKitCircle";
            texture.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= 1f ? Color.white : Color.clear);
                }
            }
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSprite;
        }

        static Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 32;
            const int radius = 7;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "UiKitRounded";
            texture.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = x < radius ? radius : x >= size - radius ? size - radius - 1 : x;
                    int cy = y < radius ? radius : y >= size - radius ? size - radius - 1 : y;
                    float dx = x - cx;
                    float dy = y - cy;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= radius * radius
                        ? Color.white : Color.clear);
                }
            }
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            return roundedSprite;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 2);
            rect.offsetMax = new Vector2(-8, -2);
        }
    }

    public sealed class SafeAreaFitter : MonoBehaviour
    {
        Rect lastArea;

        void Awake()
        {
            Apply();
        }

        void Update()
        {
            if (lastArea != Screen.safeArea)
            {
                Apply();
            }
        }

        void Apply()
        {
            lastArea = Screen.safeArea;
            var rect = transform as RectTransform;
            if (rect == null || Screen.width == 0 || Screen.height == 0)
            {
                return;
            }
            Vector2 min = lastArea.position;
            Vector2 max = lastArea.position + lastArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
