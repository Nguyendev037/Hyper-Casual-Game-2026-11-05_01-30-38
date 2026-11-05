using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    public static partial class UiKit
    {
        public static Image Icon(Transform parent, string iconId, Vector2 size)
        {
            Image image = Circle(parent, "Icon", new Color(0.90f, 0.72f, 0.36f, 1f));
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = size;
            Sprite sprite = UiSkin.Icon(iconId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            return image;
        }

        public static Button Card(Transform parent, string iconId, string title, string body, Vector2 size)
        {
            Button button = Button(parent, string.Empty, size, Color.white);
            button.gameObject.name = "Card";
            Image background = button.GetComponent<Image>();
            background.sprite = UiSkin.Ui("card") ?? UiSkin.Ui("panel");
            if (background.sprite == null) background.sprite = RoundedSprite();
            background.type = Image.Type.Sliced;
            background.color = background.sprite == RoundedSprite()
                ? new Color(0.15f, 0.14f, 0.23f, 0.98f) : Color.white;
            Image icon = Icon(button.transform, iconId, new Vector2(200, 200));
            SetRect(icon.rectTransform, new Vector2(0.025f, 0.16f), new Vector2(0.24f, 0.84f));
            Text heading = Text(button.transform, title, 52, Color.white, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(0.27f, 0.56f), new Vector2(0.96f, 0.96f));
            Text details = Text(button.transform, body, 38, Color.white, TextAnchor.UpperLeft);
            SetRect(details.rectTransform, new Vector2(0.27f, 0.10f), new Vector2(0.96f, 0.58f));
            return button;
        }

        public static Image Background(Transform parent, string uiId)
        {
            Image image = Circle(parent, "Background", Color.white);
            image.sprite = UiSkin.Ui(uiId);
            image.color = image.sprite == null ? new Color(0.15f, 0.12f, 0.18f, 1f) : Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;
            SetRect(image.rectTransform, Vector2.zero, Vector2.one);
            image.transform.SetAsFirstSibling();
            return image;
        }

        static void AddPressFeedback(Button button)
        {
            button.gameObject.AddComponent<UiPressFeedback>();
        }

        static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    }

    public sealed class UiPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        RectTransform rect;
        Vector3 originalScale;
        float restoreAt;

        void Awake()
        {
            rect = transform as RectTransform;
            originalScale = rect.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            restoreAt = Time.unscaledTime + 0.08f;
            rect.localScale = originalScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        void Update()
        {
            if (Time.unscaledTime < restoreAt) return;
            rect.localScale = Vector3.Lerp(rect.localScale, originalScale, Time.unscaledDeltaTime * 18f);
            if ((rect.localScale - originalScale).sqrMagnitude < 0.0001f) rect.localScale = originalScale;
        }
    }
}
