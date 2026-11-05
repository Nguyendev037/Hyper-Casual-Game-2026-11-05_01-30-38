using System;
using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        void ShowUpgrade()
        {
            SetMode(Mode.Upgrade);
            SetPaused(true);
            choosing = false;
            Clear();
            AddListPanel();
            Label("Chọn một nâng cấp", 0.8f);
            var choices = run.Upgrades.RollChoices();
            for (int index = 0; index < choices.Count; index++)
            {
                string id = choices[index];
                UpgradeDefinition definition = Catalog.GetUpgrade(id);
                int stack = run.Upgrades.StackCount(id);
                Button(
                    definition.Title + "\n" + definition.Description + "  " + stack + "/2",
                    0.63f - index * 0.14f,
                    () => ChooseUpgrade(id),
                    0.1f);
            }
        }

        void ChooseUpgrade(string id)
        {
            if (choosing)
            {
                return;
            }
            choosing = true;
            try
            {
                run.ChooseUpgrade(id);
                if (id == Catalog.HealFallbackId)
                {
                    combat?.Heal(Catalog.HealFallbackAmount);
                }
                StartTower();
                Toast("Tầng " + run.CurrentFloor);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }

        void ShowRescue()
        {
            SetMode(Mode.Rescue);
            SetPaused(true);
            Clear();
            Label("Lồng của Ember · Hỏa", 0.69f, 64);
            Label("Hộ vệ đã gục ngã. Mở lồng để giải cứu Ember.", 0.54f, 32);
            Button("Mở lồng", 0.39f, RescueEmber);
        }

        void RescueEmber()
        {
            bool rescued = rewards.GrantSpiritRescue("spirit.ember", Element.Fire);
            if (rescued)
            {
                Log("spirit_rescue", "spirit.ember");
                combat.SetDogTier(DogProgress.GetTier(save.dogSealMask));
                audioHub.Play("rescue");
                SaveNow();
            }
            Clear();
            Label("Ember được giải cứu!\nCún nhận ấn Hỏa — vòng cổ phát sáng", 0.57f, 64);
            Button("Tiếp", 0.25f, CompleteRescue);
            Toast("Ember được giải cứu!");
        }

        void CompleteRescue()
        {
            try
            {
                run.CompleteRescue();
                OpenRest();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }

        void OpenRest()
        {
            SetMode(Mode.Rest);
            SetPaused(false);
            combat.Running = true;
            combat.SetDogTier(DogProgress.GetTier(save.dogSealMask));
            combat.SpawnDummy(combat.PlayerPosition + Vector3.forward * 3f);
            DrawRest();
        }

        void DrawRest()
        {
            Clear();
            Label("Nơi nghỉ", 0.84f);
            Label("Đi lại tự do · đồng hồ ngày/đêm tiếp tục", 0.77f, 24);
            RestGridButton("Dung hợp", 0.27f, 0.05f, DrawFusion);
            RestGridButton("Tủ đồ", 0.27f, 0.37f, DrawWardrobe);
            RestGridButton("Kết quả", 0.18f, 0.05f, DrawResult);
            RestGridButton("Xem tiến hóa tương lai", 0.18f, 0.37f, OpenShowcase);
            RestGridButton("Chơi lại", 0.09f, 0.05f, StartRun);
            RestGridButton("Về menu", 0.09f, 0.37f, ShowMenu);
            DrawRestHud();
        }

        void RestGridButton(string value, float y, float x, Action action)
        {
            var button = UiKit.Button(root, value, new Vector2(700, 100), new Color(0.22f, 0.18f, 0.22f, 0.96f));
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + 0.29f, y + 0.07f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
        }

        void DrawResult()
        {
            if (run == null)
            {
                DrawRest();
                return;
            }
            SetMode(Mode.Result);
            SetPaused(true);
            Clear();
            AddListPanel();
            Label("Chiến thắng · kết quả chuyến đi", 0.67f, 64);
            Label(
                "Tầng đạt: " + save.highestClearedFloor +
                "\nVàng nhận: " + (run == null ? 0 : run.GoldEarnedThisRun) +
                "\nTổng vàng: " + save.gold +
                (run == null ? string.Empty :
                    "\nThời gian: " + Mathf.FloorToInt(Time.unscaledTime - runStarted) + " giây"),
                0.42f,
                32);
            Button("Quay lại", 0.23f, ReturnToRest);
        }

        void ReturnToRest()
        {
            SetMode(Mode.Rest);
            SetPaused(false);
            DrawRest();
        }

        void DrawDeathResult()
        {
            SetMode(Mode.Result);
            SetPaused(true);
            Clear();
            AddListPanel();
            Label("Thất bại", 0.68f, 64);
            Label(
                "Tầng đạt: " + save.highestClearedFloor +
                "\nVàng nhận: " + run.GoldEarnedThisRun +
                "\nTổng vàng: " + save.gold,
                0.48f,
                32);
            Button("Chơi lại", 0.31f, StartRun);
            Button("Về menu", 0.21f, ShowMenu);
        }

        void Pause()
        {
            if (mode == Mode.Pause)
            {
                ContinuePause();
                return;
            }
            if (mode == Mode.Tower || mode == Mode.Rest || mode == Mode.Showcase)
            {
                pauseReturnMode = mode;
                pauseReturnRunning = combat != null && combat.Running;
                pauseReturnClockPaused = clock.Paused;
                OpenPause();
            }
        }

        void ContinuePause()
        {
            Mode target = pauseReturnMode;
            SetMode(target);
            if (combat != null) combat.Running = pauseReturnRunning;
            clock.Paused = pauseReturnClockPaused;
            Time.timeScale = pauseReturnClockPaused ? 0f : 1f;
            if (target == Mode.Tower && combat != null && run != null) DrawHud();
            else if (target == Mode.Rest) DrawRest();
            else if (target == Mode.Showcase) DrawShowcaseScreen();
            else ShowMenu();
        }

        void OpenPause()
        {
            if (mode != Mode.Pause)
            {
                pauseReturnMode = mode;
            }
            SetMode(Mode.Pause);
            SetPaused(true);
            Clear();
            AddListPanel();
            Label("Tạm dừng", 0.68f, 64);
            Button("Tiếp tục", 0.5f, Pause);
            Button("Cài đặt", 0.4f, ShowSettings);
            Button("Về menu", 0.3f, ConfirmLeaveRun);
        }

        void DrawRestHud()
        {
            var hpBar = UiKit.ProgressBar(root, "RestHealth", new Color(0.88f, 0.25f, 0.26f), out hpFill);
            SetRect(hpBar, new Vector2(0.05f, 0.895f), new Vector2(0.476f, 0.916f));
            hpText = HudLabel("♥ " + Mathf.CeilToInt(combat.PlayerHp) + "/" + playerMaxHp +
                " · Tầng " + (run == null ? 0 : run.CurrentFloor) + "/5 · " + save.gold + " vàng",
                new Vector2(0.05f, 0.94f), new Vector2(0.66f, 0.99f), 40, Color.white);
            HudLabel("🐕 " + DogTierName(DogProgress.GetTier(save.dogSealMask)),
                new Vector2(0.05f, 0.85f), new Vector2(0.48f, 0.89f), 40, Color.white);
            clockText = HudLabel("", new Vector2(0.68f, 0.93f), new Vector2(0.89f, 0.985f), 40, Color.white);
            RectTransform phase = UiKit.Countdown(root, "PhaseCountdown", out clockFill);
            SetRect(phase, new Vector2(0.9f, 0.925f), new Vector2(0.99f, 0.975f));
            var pause = UiKit.CircleButton(root, "Ⅱ",
                new Color(0.22f, 0.18f, 0.22f, 0.96f));
            SetRect((RectTransform)pause.transform, new Vector2(0.89f, 0.83f), new Vector2(0.992f, 0.888f));
            pause.onClick.AddListener(Pause);
            DrawRestControls();
            displayedHp = int.MinValue;
            displayedClock = int.MinValue;
        }

        void DrawRestControls()
        {
            HudLabel("Lệnh tinh linh", new Vector2(0.72f, 0.031f), new Vector2(0.944f, 0.06f),
                40, CommandElementColor());
            var command = UiKit.CircleButton(root, CommandElementName(), CommandElementColor());
            SetRect((RectTransform)command.transform, new Vector2(0.74f, 0.065f),
                new Vector2(0.944f, 0.18f));
            command.onClick.AddListener(() => combat?.TryCommand());
            Image radial = UiKit.Circle(command.transform, "Cooldown", new Color(0.04f, 0.07f, 0.1f, 0.62f));
            radial.type = Image.Type.Filled;
            radial.fillMethod = Image.FillMethod.Radial360;
            radial.fillOrigin = (int)Image.Origin360.Top;
            radial.fillClockwise = false;
            radial.raycastTarget = false;
            commandFill = radial;
            RectTransform cooldown = (RectTransform)radial.transform;
            SetRect(cooldown, Vector2.zero, Vector2.one);
            command.transform.GetChild(0).SetAsLastSibling();
            var area = UiKit.Panel(root, "JoystickArea", Vector2.zero, new Vector2(0.72f, 0.55f),
                new Color(0, 0, 0, 0));
            area.gameObject.AddComponent<RHMG.Runtime.UI.FloatingJoystick>().Changed +=
                value => combat?.SetMoveInput(value);
        }

        void ConfirmLeaveRun()
        {
            Clear();
            Label("Tiến độ chuyến sẽ mất, vàng đã nhận được giữ", 0.55f, 30);
            Button("Xác nhận về menu", 0.38f, ShowMenu);
            Button("Quay lại", 0.27f, OpenPause);
        }
    }
}
