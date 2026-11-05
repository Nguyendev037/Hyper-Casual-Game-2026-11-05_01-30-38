using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RHMG.Runtime.UI
{
    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public System.Action<Vector2> Changed;

        RectTransform area;
        RectTransform baseRect;
        RectTransform knob;
        int pointerId = int.MinValue;
        Vector2 origin;

        void Awake()
        {
            area = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue || eventData.position.x > Screen.width * 0.72f)
            {
                return;
            }
            pointerId = eventData.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area,
                eventData.position,
                eventData.pressEventCamera,
                out origin);
            EnsureVisuals();
            baseRect.anchoredPosition = origin;
            baseRect.gameObject.SetActive(true);
            knob.anchoredPosition = Vector2.zero;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
            {
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                area,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 position);
            Vector2 value = Vector2.ClampMagnitude((position - origin) / 140f, 1f);
            knob.anchoredPosition = value * 140f;
            Changed?.Invoke(value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId)
            {
                return;
            }
            pointerId = int.MinValue;
            baseRect.gameObject.SetActive(false);
            Changed?.Invoke(Vector2.zero);
        }

        void Update()
        {
            if (pointerId != int.MinValue || Keyboard.current == null)
            {
                return;
            }
            var keyboard = Keyboard.current;
            Vector2 value = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                value.y += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                value.y -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                value.x += 1f;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                value.x -= 1f;
            }
            Changed?.Invoke(Vector2.ClampMagnitude(value, 1f));
        }

        void EnsureVisuals()
        {
            if (baseRect != null)
            {
                return;
            }
            var baseImage = UiKit.Circle(area, "JoystickBase", new Color(0.1f, 0.12f, 0.18f, 0.6f));
            baseRect = (RectTransform)baseImage.transform;
            baseRect.sizeDelta = Vector2.one * 280f;
            baseImage.raycastTarget = false;
            var knobImage = UiKit.Circle(baseRect, "JoystickKnob", new Color(0.9f, 0.82f, 0.68f, 0.85f));
            knob = (RectTransform)knobImage.transform;
            knob.sizeDelta = Vector2.one * 120f;
            knobImage.raycastTarget = false;
            baseRect.gameObject.SetActive(false);
        }
    }
}
