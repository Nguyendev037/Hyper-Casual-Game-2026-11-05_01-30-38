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
            Label("Chọn một nâng cấp", 0.8f, 80);
            var choices = run.Upgrades.RollChoices();
            for (int index = 0; index < choices.Count; index++)
            {
                string id = choices[index];
                UpgradeDefinition definition = Catalog.GetUpgrade(id);
                int stack = run.Upgrades.StackCount(id);
                var card = UiKit.Card(root, UiSkin.IconIdFor(id), Loc.T(definition.Title),
                    Loc.T(definition.Description) + "\n" + Loc.F("Cấp {0}/2", stack), new Vector2(880, 300));
                RectTransform cardRect = (RectTransform)card.transform;
                float centerY = 0.65f - index * 0.19f;
                // Stretch inside the list panel (0.07–0.93) instead of a fixed 960 that overflowed at 966 wide.
                cardRect.anchorMin = new Vector2(0.09f, centerY);
                cardRect.anchorMax = new Vector2(0.91f, centerY);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(0f, 300f);
                cardRect.anchoredPosition = Vector2.zero;
                card.onClick.AddListener(() => audioHub.Play("ui"));
                card.onClick.AddListener(() => ChooseUpgrade(id));
            }
            sizeAudit.Capture(root, mode.ToString());
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
                Toast(Loc.F("Tầng {0}", run.CurrentFloor));
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
            // Joystick stops at 50% height so the 3x2 grid (rows 0.68 / 0.59, ±66 px) never overlaps it.
            DrawJoystickArea(0.50f);
            DrawTopHud("RestHealth", false);
            Label("Nơi nghỉ", 0.74f, 72);
            RestGridButton("Dung hợp", 0, 0, DrawFusion);
            RestGridButton("Tủ đồ", 1, 0, DrawWardrobe);
            RestGridButton("Kết quả", 2, 0, DrawResult);
            RestGridButton("Tiến hóa", 0, 1, OpenShowcase);
            RestGridButton("Chơi lại", 1, 1, StartRun);
            RestGridButton("Về menu", 2, 1, ShowMenu);
            DrawCommandControls();
            UpdateHudBars();
        }

        // Columns stretch with screen width (gap stays ≥ 32 px on 20:9 phones); height fixed at 132.
        void RestGridButton(string value, int column, int row, Action action)
        {
            var button = UiKit.Button(root, value, new Vector2(300, 132), new Color(0.22f, 0.18f, 0.22f, 0.96f));
            RectTransform rect = (RectTransform)button.transform;
            float left = 0.04f + column * 0.32f;
            float centerY = 0.68f - row * 0.09f;
            rect.anchorMin = new Vector2(left, centerY);
            rect.anchorMax = new Vector2(left + 0.28f, centerY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 132f);
            rect.anchoredPosition = Vector2.zero;
            button.GetComponentInChildren<Text>().fontSize = 44;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
            sizeAudit.Capture(root, mode.ToString());
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
                Loc.F("Tầng đạt: {0}\nVàng nhận: {1}\nTổng vàng: {2}", save.highestClearedFloor,
                    run == null ? 0 : run.GoldEarnedThisRun, save.gold) +
                (run == null ? string.Empty :
                    "\n" + Loc.F("Thời gian: {0} giây", Mathf.FloorToInt(Time.unscaledTime - runStarted))),
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
                Loc.F("Tầng đạt: {0}\nVàng nhận: {1}\nTổng vàng: {2}", save.highestClearedFloor,
                    run.GoldEarnedThisRun, save.gold),
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
            else if (target == Mode.Rest)
            {
                // Pause may have been opened from Fusion/Wardrobe (world paused): Rest always resumes it.
                SetPaused(false);
                if (combat != null) combat.Running = true;
                DrawRest();
            }
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

        void ConfirmLeaveRun()
        {
            Clear();
            Label("Tiến độ chuyến sẽ mất, vàng đã nhận được giữ", 0.55f, 30);
            Button("Xác nhận về menu", 0.38f, ShowMenu);
            Button("Quay lại", 0.27f, OpenPause);
        }
    }
}
