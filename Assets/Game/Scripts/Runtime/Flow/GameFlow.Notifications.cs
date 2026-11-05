using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        public void Toast(string message)
        {
            if (toastText == null || IsHint(message) && mode != Mode.Tower) return;
            toastQueue.Enqueue(message);
            if (!toastText.gameObject.activeSelf) ShowNextToast();
        }

        void ShowNextToast()
        {
            if (toastQueue.Count == 0) return;
            toastText.text = toastQueue.Dequeue();
            toastText.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 1.2f;
            if (toastText.text == "Kéo nửa dưới màn hình để di chuyển — phép tự bắn" &&
                !save.tutorialSeen)
            {
                save.tutorialSeen = true;
                SaveNow();
            }
        }

        bool IsHint(string message)
        {
            return message.StartsWith("Kéo nửa dưới") || message.StartsWith("Chạm nút tinh linh") ||
                message.StartsWith("Để ý vùng đỏ");
        }

        void ClearNotifications()
        {
            toastQueue.Clear();
            if (toastText != null) toastText.gameObject.SetActive(false);
        }
    }
}
