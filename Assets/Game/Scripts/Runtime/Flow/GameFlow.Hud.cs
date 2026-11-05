using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    // Shared in-world HUD (Tower + Station + Rest). Elements are pinned to screen corners with fixed pixel
    // sizes so nothing drifts or overlaps on tall (20:9) phones. The spirit command button is gone (SPEC-P01 §1):
    // the girl, the dog and the spirits act by themselves, the player only moves.
    public sealed partial class GameFlow
    {
        const float HudEdge = 24f;

        Text floorText;
        Text goldText;
        Text levelText;
        Text timeText;
        Text bossLabel;
        RectTransform xpFill;
        Image clockIcon;
        int displayedGold = int.MinValue;
        int displayedFloor = int.MinValue;
        int displayedLevel = int.MinValue;
        int displayedTime = int.MinValue;
        float displayedXp = -1f;

        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);

        void DrawTopHud(string healthName, bool bossFloor)
        {
            if (DrawLayoutHud(bossFloor)) return;
            // Row 1 left: health frame (heart baked in) with the number inside the channel.
            RectTransform hpBar = UiKit.ProgressBar(root, healthName, new Color(0.88f, 0.25f, 0.26f), out hpFill);
            Place(hpBar, TopLeft, TopLeft, new Vector2(-60f, -16f), new Vector2(512f, 128f));
            hpText = UiKit.Text(hpBar, string.Empty, 36, Color.white, TextAnchor.MiddleCenter);
            hpText.name = "HudHp";
            hpText.verticalOverflow = VerticalWrapMode.Overflow;
            hpText.raycastTarget = false;
            hpText.rectTransform.anchorMin = UiKit.BarChannelMin;
            hpText.rectTransform.anchorMax = UiKit.BarChannelMax;
            hpText.rectTransform.offsetMin = Vector2.zero;
            hpText.rectTransform.offsetMax = Vector2.zero;

            // Row 2 left: floor and gold, icon + number pairs.
            Image floorIcon = UiKit.Icon(root, "icon_floor", new Vector2(64f, 64f));
            Place(floorIcon.rectTransform, TopLeft, TopLeft, new Vector2(HudEdge, -140f), new Vector2(64f, 64f));
            floorText = HudText("HudFloor", TopLeft, new Vector2(96f, -140f), new Vector2(220f, 64f),
                TextAnchor.MiddleLeft, Color.white);
            Image goldIcon = UiKit.Icon(root, "icon_gold", new Vector2(64f, 64f));
            Place(goldIcon.rectTransform, TopLeft, TopLeft, new Vector2(320f, -140f), new Vector2(64f, 64f));
            goldText = HudText("HudGold", TopLeft, new Vector2(392f, -140f), new Vector2(180f, 64f),
                TextAnchor.MiddleLeft, Palette.LightGold);

            // Row 3 left: level + XP bar (spirit stones).
            levelText = HudText("HudLevel", TopLeft, new Vector2(HudEdge, -212f), new Vector2(150f, 56f),
                TextAnchor.MiddleLeft, Palette.LightGold);
            RectTransform xpBack = SolidRect(root, "XpBar", new Color(0.05f, 0.05f, 0.09f, 0.85f));
            Place(xpBack, TopLeft, TopLeft, new Vector2(176f, -226f), new Vector2(380f, 28f));
            xpFill = SolidRect(xpBack, "XpFill", new Color(0.45f, 0.88f, 1f, 1f));
            xpFill.anchorMin = new Vector2(0f, 0f);
            xpFill.anchorMax = new Vector2(0f, 1f);
            xpFill.offsetMin = new Vector2(3f, 3f);
            xpFill.offsetMax = new Vector2(0f, -3f);

            // Row 1 right: pause (smallest, rarely used) and the day/night clock left of it.
            DrawPauseButton();
            RectTransform phase = UiKit.Countdown(root, "PhaseCountdown", out clockFill);
            Place(phase, TopRight, TopRight, new Vector2(-(HudEdge + 132f + 20f), -HudEdge - 18f),
                new Vector2(96f, 96f));
            phase.GetComponent<Image>().raycastTarget = false;
            clockFill.raycastTarget = false;
            clockIcon = UiKit.Icon(phase, "icon_sun", new Vector2(60f, 60f));
            clockIcon.raycastTarget = false;
            clockText = HudText("HudClock", TopRight, new Vector2(-(HudEdge + 132f + 20f + 96f + 12f), -HudEdge - 18f),
                new Vector2(210f, 96f), TextAnchor.MiddleRight, Color.white);

            // Row 2 right: time left on the floor.
            timeText = HudText("HudTime", TopRight, new Vector2(-HudEdge, -150f), new Vector2(300f, 80f),
                TextAnchor.MiddleRight, Color.white);
            timeText.fontSize = 60;

            // Boss floor: name + bar centred below the left column.
            bossLabel = HudText("HudBoss", TopCenter, new Vector2(0f, -290f), new Vector2(640f, 56f),
                TextAnchor.MiddleCenter, Color.white);
            bossLabel.text = BossNameFor(run == null ? 0 : run.CurrentFloor);
            RectTransform bossBar = UiKit.ProgressBar(root, "BossHealth", Palette.WarnRed, out bossFill);
            Place(bossBar, TopCenter, TopCenter, new Vector2(0f, -330f), new Vector2(512f, 128f));
            bossLabel.gameObject.SetActive(bossFloor);
            bossBar.gameObject.SetActive(bossFloor);
            bossFill.fillAmount = bossFloor ? 1f : 0f;

            displayedHp = int.MinValue;
            displayedClock = int.MinValue;
            displayedPhase = (DayPhase)(-1);
            displayedGold = int.MinValue;
            displayedFloor = int.MinValue;
            displayedLevel = int.MinValue;
            displayedTime = int.MinValue;
            displayedXp = -1f;
        }

        static string BossNameFor(int floor)
        {
            switch (FloorPlan.Kind(floor))
            {
                case FloorKind.MidBoss: return Loc.T("Nữ Hoàng Cát Lồng");
                case FloorKind.FinalBoss: return "Blue Hair Guy";
                default: return Loc.T("Hộ Vệ Lồng Cát");
            }
        }

        static RectTransform SolidRect(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        void DrawPauseButton()
        {
            Button pause = UiKit.CircleButton(root, "Ⅱ", new Color(0.22f, 0.18f, 0.22f, 0.96f));
            pause.gameObject.name = "PauseButton";
            Place((RectTransform)pause.transform, TopRight, TopRight, new Vector2(-HudEdge, -HudEdge),
                new Vector2(132f, 132f));
            Image pauseIcon = UiKit.Icon(pause.transform, "icon_pause", new Vector2(72f, 72f));
            pauseIcon.raycastTarget = false;
            if (pauseIcon.sprite != null) pause.transform.GetChild(0).gameObject.SetActive(false);
            else pause.transform.GetChild(0).SetAsLastSibling();
            pause.onClick.AddListener(() => audioHub.Play("ui"));
            pause.onClick.AddListener(Pause);
        }

        // Invisible drag area for the floating joystick; drawn FIRST so buttons stay on top of it.
        // The whole lower part of the screen is the stick: there is no command button any more.
        void DrawJoystickArea(float top = 0.62f)
        {
            RectTransform area = UiKit.Panel(root, "JoystickArea", Vector2.zero, new Vector2(1f, top),
                new Color(0, 0, 0, 0));
            area.gameObject.AddComponent<FloatingJoystick>().Changed += value => combat?.SetMoveInput(value);
        }

        Text HudText(string name, Vector2 anchor, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
        {
            Text text = UiKit.Text(root, string.Empty, 40, color, alignment);
            text.name = name;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow; // CJK line height > box: never drop the line
            Place(text.rectTransform, anchor, anchor, position, size);
            return text;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
