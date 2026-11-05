using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    // Shared in-world HUD (Tower + Rest). Elements are pinned to screen corners with fixed pixel
    // sizes so nothing drifts or overlaps on tall (20:9) phones. Layout table: SPEC-P02 §2b.
    public sealed partial class GameFlow
    {
        const float HudEdge = 24f;
        const float CommandSize = 300f;
        const float CommandMargin = 64f;
        const int SpiritJoinFloor = 2;

        Text floorText;
        Text goldText;
        Image clockIcon;
        int displayedGold = int.MinValue;
        int displayedFloor = int.MinValue;

        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        bool HasSpirit => !string.IsNullOrEmpty(spiritId);

        void DrawTopHud(string healthName, bool bossFloor)
        {
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

            // Row 3 left: dog tier.
            HudText("HudDog", TopLeft, new Vector2(HudEdge, -212f), new Vector2(520f, 56f),
                TextAnchor.MiddleLeft, Color.white).text = DogTierName(DogProgress.GetTier(save.dogSealMask));

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

            // Boss floor: name + bar centred below the left column.
            Text bossLabel = HudText("HudBoss", TopCenter, new Vector2(0f, -290f), new Vector2(640f, 56f),
                TextAnchor.MiddleCenter, Color.white);
            bossLabel.text = Loc.T("Hộ Vệ Lồng Cát");
            RectTransform bossBar = UiKit.ProgressBar(root, "BossHealth", Palette.WarnRed, out bossFill);
            Place(bossBar, TopCenter, TopCenter, new Vector2(0f, -330f), new Vector2(512f, 128f));
            bossLabel.gameObject.SetActive(bossFloor);
            bossBar.gameObject.SetActive(bossFloor);

            displayedHp = int.MinValue;
            displayedClock = int.MinValue;
            displayedPhase = (DayPhase)(-1);
            displayedGold = int.MinValue;
            displayedFloor = int.MinValue;
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

        // Spirit command: only exists while a spirit travels with the girl (none on floor 1 of a run).
        void DrawCommandControls()
        {
            commandFill = null;
            if (HasSpirit)
            {
                bool borrowed = false;
                foreach (string trialId in Catalog.DemoTrialSpiritIds) borrowed |= trialId == spiritId;
                // Right-aligned to the screen edge so long translations grow leftward, never off-screen.
                Text spiritLabel = HudText("HudSpirit", BottomRight,
                    new Vector2(-HudEdge, CommandMargin + CommandSize + 8f),
                    new Vector2(640f, 56f), TextAnchor.MiddleRight, CommandElementColor());
                spiritLabel.text = SpiritName(spiritId) + " · " + CommandElementName() +
                    (borrowed ? " " + Loc.T("(mượn)") : string.Empty);

                Button command = UiKit.CircleButton(root, CommandElementName(), CommandElementColor());
                command.gameObject.name = "CommandButton";
                Place((RectTransform)command.transform, BottomRight, BottomRight,
                    new Vector2(-CommandMargin, CommandMargin), new Vector2(CommandSize, CommandSize));
                Image commandIcon = UiKit.Icon(command.transform, UiSkin.IconIdFor(spiritId), new Vector2(150f, 150f));
                commandIcon.raycastTarget = false;
                if (commandIcon.sprite != null) command.transform.GetChild(0).gameObject.SetActive(false);
                else command.transform.GetChild(0).SetAsLastSibling();

                // Cooldown darkens the inner disc (inside the gold ring) on top of the icon.
                Image radial = UiKit.Circle(command.transform, "Cooldown", new Color(0.04f, 0.07f, 0.1f, 0.62f));
                radial.type = Image.Type.Filled;
                radial.fillMethod = Image.FillMethod.Radial360;
                radial.fillOrigin = (int)Image.Origin360.Top;
                radial.fillClockwise = false;
                radial.fillAmount = 0f;
                radial.raycastTarget = false;
                SetRect(radial.rectTransform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                radial.transform.SetAsLastSibling();
                commandFill = radial;

                command.onClick.AddListener(() => audioHub.Play("ui"));
                command.onClick.AddListener(() => combat?.TryCommand());
            }
            sizeAudit.Capture(root, mode.ToString());
        }

        // Invisible drag area for the floating joystick; drawn FIRST so buttons stay on top of it.
        // Right edge stops 32 px left of the command button on every width (SPEC-P02 §1 spacing).
        void DrawJoystickArea(float top = 0.55f)
        {
            RectTransform area = UiKit.Panel(root, "JoystickArea", Vector2.zero, new Vector2(1f, top),
                new Color(0, 0, 0, 0));
            area.offsetMax = new Vector2(-(CommandMargin + CommandSize + 32f), 0f);
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
