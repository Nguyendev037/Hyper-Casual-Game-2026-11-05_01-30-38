using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.UI;
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
            string message = toastQueue.Dequeue();
            toastText.font = UiSkin.Body;
            toastText.text = Loc.T(message);
            // Screens are rebuilt under the same root: keep the toast on top of everything drawn later.
            toastPanel.SetAsLastSibling();
            // Toast goes in each screen's free band: Rest (grid above, wardrobe rows 0.55/0.46) → 0.505;
            // Upgrade (cards 0.19–0.73) → bottom; tower HUD → 0.70.
            float y = mode == Mode.Rest ? 0.505f : mode == Mode.LevelUp ? 0.08f : 0.70f;
            toastPanel.anchorMin = new Vector2(0.5f, y);
            toastPanel.anchorMax = new Vector2(0.5f, y);
            toastPanel.gameObject.SetActive(true);
            toastText.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 2.5f;
            if (message == "Kéo nửa dưới màn hình để di chuyển — phép tự bắn" &&
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
            if (toastPanel != null) toastPanel.gameObject.SetActive(false);
        }
    }
}
