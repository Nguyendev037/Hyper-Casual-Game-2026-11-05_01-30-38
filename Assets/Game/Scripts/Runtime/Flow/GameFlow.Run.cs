using System;
using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        void ShowMenu()
        {
            StopCombat();
            SetMode(Mode.Menu);
            Time.timeScale = 1f;
            Clear();
            Text logo = UiKit.Text(root, "Red Hair\nMagic Girl", 110, Palette.HairRed, TextAnchor.MiddleCenter);
            SetRect(logo.rectTransform, new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.95f));
            var hold = logo.gameObject.AddComponent<HoldAction>();
            hold.Completed = () =>
            {
                seed = 7;
                Toast("Mẫu thử: mã 7");
            };
            Label("Bản thử nghiệm 5 tầng", 0.74f, 40);
            Label("Cùng cún leo tháp, cứu tinh linh cát", 0.68f, 40);
            Button("Chơi", 0.55f, StartRun);
            Button("Phòng trình diễn", 0.45f, TryOpenShowcase);
            Button("Cài đặt", 0.35f, ShowSettings);
            Button("Ghi công", 0.25f, ShowCredits);
        }

        void StartRun()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugAutoWin = false;
#endif
            try
            {
                run = new RunController(save, rewards, seed);
                run.Start();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }
            runStarted = Time.unscaledTime;
            clock.Reset();
            spiritId = "spirit.dew";
            Log("run_start", run.RunId);
            StartTower();
        }

        void StartTower()
        {
            SetMode(Mode.Tower);
            Time.timeScale = 1f;
            clock.Paused = false;
            if (combat != null)
            {
                Destroy(combat.gameObject);
                combat = null;
            }
            IEnumerable<string> equipped = wardrobe.EquippedIds();
            PlayerStats stats = StatsCalculator.Compute(equipped, run.Upgrades.Taken, save.dogSealMask, false);
            playerMaxHp = Mathf.CeilToInt(stats.MaxHp);
            string[] outfitIds = new List<string>(wardrobe.EquippedIds()).ToArray();
            combat = CombatSession.Create(modeRoot, new CombatSetup
            {
                Stats = stats,
                SpiritId = "spirit.dew",
                DogTier = DogProgress.GetTier(save.dogSealMask),
                Mounted = false,
                Showcase = false,
                Clock = clock,
                EquippedOutfitIds = outfitIds
            });
            hasPlayerVitals = false;
            combat.FloorCleared += OnFloorCleared;
            combat.PlayerDied += OnPlayerDied;
            combat.BossHealthChanged += OnBossHealthChanged;
            combat.BossDefeatedAt += ShowCage;
            AttachDayNightView();
            combat.StartFloor(Catalog.GetFloor(run.CurrentFloor));
            combat.Running = true;
            DrawHud();
            if (run.CurrentFloor == 1 && !save.tutorialSeen && !tutorialDisplayed)
            {
                Toast("Kéo nửa dưới màn hình để di chuyển — phép tự bắn");
                tutorialDisplayed = true;
            }
            if (run.CurrentFloor == 2)
            {
                Toast("Chạm nút tinh linh để ra lệnh");
            }
            if (run.CurrentFloor == 3)
            {
                Toast("Để ý vùng đỏ — tránh ra!");
            }
        }

        void ShowCage(Vector3 position)
        {
            GameObject cage = CharacterFactory.CreateCage(combat.transform);
            cage.transform.position = position;
        }

        void AttachDayNightView()
        {
            var view = modeRoot.gameObject.AddComponent<DayNightView>();
            view.Clock = clock;
            Camera target = FindAnyObjectByType<Camera>();
            view.TargetCamera = target;
            view.MainLight = FindAnyObjectByType<Light>();
        }

        void DrawHud()
        {
            Clear();
            var hpBar = UiKit.ProgressBar(root, "PlayerHealth", new Color(0.88f, 0.25f, 0.26f), out hpFill);
            SetRect(hpBar, new Vector2(0.05f, 0.895f), new Vector2(0.476f, 0.916f));
            HudLabel(
                "♥ " + Mathf.CeilToInt(combat.PlayerHp) + "/" + playerMaxHp + " · Tầng " +
                run.CurrentFloor + "/5 · " + save.gold + " vàng",
                new Vector2(0.05f, 0.93f),
                new Vector2(0.66f, 0.985f),
                40,
                Color.white);
            HudLabel(
                (clock.Phase == DayPhase.Day ? "☀ Ngày " : "☾ Đêm ") + Mathf.CeilToInt(clock.PhaseRemaining) + "s",
                new Vector2(0.68f, 0.93f),
                new Vector2(0.89f, 0.985f),
                40,
                Color.white);
            RectTransform clockRect = UiKit.Countdown(root, "PhaseCountdown", out Image clockFill);
            SetRect(clockRect, new Vector2(0.9f, 0.925f), new Vector2(0.99f, 0.975f));
            clockFill.fillAmount = clock.PhaseRemaining / DayNightClock.PhaseLength;
            HudLabel(
                "🐕 " + DogTierName(DogProgress.GetTier(save.dogSealMask)),
                new Vector2(0.05f, 0.85f),
                new Vector2(0.48f, 0.89f),
                40,
                Color.white);
            HudLabel(
                "Sương · Thủy · Bạn đồng hành dùng thử",
                new Vector2(0.05f, 0.80f),
                new Vector2(0.58f, 0.845f),
                40,
                Palette.WaterBlue);
            var bossBar = UiKit.ProgressBar(root, "BossHealth", Palette.WarnRed, out bossFill);
            SetRect(bossBar, new Vector2(0.287f, 0.73f), new Vector2(0.713f, 0.751f));
            bossBar.gameObject.SetActive(run.CurrentFloor == 5);
            Text bossLabel = HudLabel(
                "Hộ Vệ Lồng Cát",
                new Vector2(0.3f, 0.75f),
                new Vector2(0.7f, 0.79f),
                40,
                Color.white);
            bossLabel.gameObject.SetActive(run.CurrentFloor == 5);
            var pause = UiKit.CircleButton(root, "Ⅱ", new Color(0.22f, 0.18f, 0.22f, 0.96f));
            SetRect((RectTransform)pause.transform, new Vector2(0.89f, 0.83f), new Vector2(0.992f, 0.888f));
            pause.onClick.AddListener(Pause);
            HudLabel("Lệnh tinh linh", new Vector2(0.72f, 0.031f),
                new Vector2(0.944f, 0.06f), 40, CommandElementColor());
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
            var joystickArea = UiKit.Panel(root, "JoystickArea", Vector2.zero, new Vector2(0.72f, 0.55f), new Color(0, 0, 0, 0));
            joystickArea.gameObject.AddComponent<FloatingJoystick>().Changed += value => combat?.SetMoveInput(value);
        }

        static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Text HudLabel(string value, Vector2 min, Vector2 max, int size, Color color)
        {
            Text text = UiKit.Text(root, value, Mathf.Max(40, size), color, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, min, max);
            return text;
        }

        void OnBossHealthChanged(float value)
        {
            if (bossFill != null)
            {
                bossFill.fillAmount = value;
            }
        }

        void OnFloorCleared()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugAutoWin = false;
#endif
            try
            {
                int floor = run.CurrentFloor;
                int floorReward = run.CompleteFloor();
                Log("floor_clear", floor + ":" + floorReward);
                SaveNow();
                Toast("+" + floorReward + " vàng");
                if (floor < 5)
                {
                    ShowUpgrade();
                }
                else
                {
                    Log("run_complete", run.RunId);
                    ShowRescue();
                }
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }

        void OnPlayerDied()
        {
            try
            {
                run.PlayerDied();
                Log("player_death", run.CurrentFloor.ToString());
                DrawDeathResult();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
