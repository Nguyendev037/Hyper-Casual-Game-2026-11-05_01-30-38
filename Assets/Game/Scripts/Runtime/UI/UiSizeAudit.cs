using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    public sealed class UiSizeAudit : MonoBehaviour
    {
        bool captureEnabled;
        Transform pendingRoot;
        string pendingScreen;
        int requestedFrame;
        int generation;
        int capturedGeneration;

        void Awake()
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] == "-rhmgCapture") captureEnabled = true;
            }
        }

        public void Capture(Transform root, string screen)
        {
            if (!captureEnabled) return;
            pendingRoot = root;
            pendingScreen = screen;
            requestedFrame = Time.frameCount;
            generation++;
        }

        void LateUpdate()
        {
            if (!captureEnabled || pendingRoot == null || Time.frameCount <= requestedFrame ||
                capturedGeneration == generation) return;
            Canvas.ForceUpdateCanvases();
            Button[] buttons = pendingRoot.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                RectTransform rect = buttons[index].transform as RectTransform;
                if (rect == null) continue;
                string name = ButtonName(buttons[index]);
                Debug.Log("[RHMG.Size] " + pendingScreen + "/" + name + " " +
                    Mathf.RoundToInt(rect.rect.width) + "x" + Mathf.RoundToInt(rect.rect.height));
            }
            capturedGeneration = generation;
        }

        static string ButtonName(Button button)
        {
            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                if (!string.IsNullOrWhiteSpace(labels[index].text)) return labels[index].text.Replace('\n', ' ');
            }
            return button.name;
        }
    }
}
