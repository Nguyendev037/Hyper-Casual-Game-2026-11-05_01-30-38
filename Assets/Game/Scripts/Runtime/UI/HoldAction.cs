using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RHMG.Runtime.UI
{
    public sealed class HoldAction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action Completed;

        float heldSeconds;
        bool holding;
        bool triggered;

        public void OnPointerDown(PointerEventData eventData)
        {
            heldSeconds = 0f;
            holding = true;
            triggered = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            holding = false;
        }

        void Update()
        {
            if (!holding || triggered)
            {
                return;
            }
            heldSeconds += Time.unscaledDeltaTime;
            if (heldSeconds >= 3f)
            {
                triggered = true;
                Completed?.Invoke();
            }
        }
    }
}
