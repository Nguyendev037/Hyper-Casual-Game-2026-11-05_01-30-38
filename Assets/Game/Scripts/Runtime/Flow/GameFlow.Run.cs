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
        bool HasCheckpoint => save != null && save.runCheckpoint != null && save.runCheckpoint.valid;

        void ShowMenu()
        {
            StopCombat();
            SetMode(Mode.Menu);
            audioHub.PlayMusic("music_calm");
            Time.timeScale = 1f;
            Clear();
            if (DrawLayoutTitle() || DrawLayoutMenu()) return;
            Image backdrop = UiKit.Background(root, "bg_menu");
            backdrop.transform.SetAsFirstSibling();
            Image logoImage = UiKit.Circle(root, "Logo", Color.white);
            logoImage.sprite = UiSkin.Ui("logo");
            logoImage.color = logoImage.sprite == null ? Color.clear : Color.white;
            logoImage.preserveAspect = true;
            SetRect(logoImage.rectTransform, new Vector2(0.083f, 0.68f), new Vector2(0.917f, 0.914f));
            Text logo = UiKit.Text(root, "Red Hair\nMagic Girl", 110, Palette.HairRed, TextAnchor.MiddleCenter);
            logo.font = UiSkin.Title;
            logo.fontSize = 96;
            SetRect(logo.rectTransform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f));
            logo.color = logoImage.sprite == null ? Palette.HairRed : new Color(0, 0, 0, 0);
            var hold = logo.gameObject.AddComponent<HoldAction>();
            hold.Completed = () =>
            {
                seed = 7;
                Toast("Mẫu thử: mã 7");
            };
            Label("Bản thử nghiệm 100 tầng", 0.60f, 40);
            Label("Cùng cún leo tháp, cứu tinh linh cát", 0.54f, 40);
            if (HasCheckpoint)
            {
                Button(Loc.F("Tiếp tục (Tầng {0})", save.runCheckpoint.floor), 0.38f, ResumeRun);
            }
            Button("Chơi", 0.27f, StartRun);
            MenuAction("Trình diễn", "icon_showcase", 0.20f, TryOpenShowcase);
            MenuAction("Cài đặt", "icon_settings", 0.50f, ShowSettings);
            MenuAction("Ghi công", "icon_credits", 0.80f, ShowCredits);
        }

        // Icon tile + caption underneath. The whole 240x220 rect is the touch target; the tile art
        // holds only the icon so the caption never sits on the tile's gold rim.
        void MenuAction(string label, string iconId, float x, Action action)
        {
            var button = UiKit.Button(root, label, new Vector2(240, 220), Color.clear);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(x, 0.13f);
            rect.anchorMax = new Vector2(x, 0.13f);
            rect.sizeDelta = new Vector2(240, 220);
            Image hit = button.GetComponent<Image>();
            hit.sprite = null;
            hit.color = Color.clear;
            Image tile = UiKit.Circle(button.transform, "Tile", new Color(0.22f, 0.18f, 0.22f, 0.96f));
            tile.sprite = UiSkin.Ui("button_secondary") ?? tile.sprite;
            tile.type = Image.Type.Sliced;
            if (UiSkin.Ui("button_secondary") != null) tile.color = Color.white;
            tile.raycastTarget = false;
            SetRect(tile.rectTransform, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 1f));
            Image icon = UiKit.Icon(button.transform, iconId, new Vector2(96, 96));
            icon.raycastTarget = false;
            Place(icon.rectTransform, new Vector2(0.5f, 0.67f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(104, 104));
            Text labelText = button.GetComponentInChildren<Text>();
            labelText.transform.SetAsLastSibling();
            labelText.fontSize = 38;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(1f, 0.32f);
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
            sizeAudit.Capture(root, mode.ToString());
        }

        /// <summary>Menu "Chơi": a brand new run from floor 1 (an old checkpoint is dropped).</summary>
        void StartRun()
        {
            if (HasCheckpoint)
            {
                save.runCheckpoint.valid = false;
            }
            BeginRun(false);
        }

        /// <summary>Menu "Tiếp tục" and defeat "Chơi lại": rebuild the run from the last station checkpoint (full HP).</summary>
        void ResumeRun()
        {
            BeginRun(true);
        }

        void RestartAfterDefeat()
        {
            BeginRun(HasCheckpoint);
        }

        void BeginRun(bool resume)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugAutoWin = false;
#endif
            try
            {
                run = new RunController(save, rewards, seed);
                if (resume)
                {
                    if (!run.ResumeFrom(save.runCheckpoint))
                    {
                        Debug.LogWarning("[Flow] Checkpoint rejected, starting a new run");
                        SaveNow();
                        run = new RunController(save, rewards, seed);
                        run.Start();
                    }
                }
                else
                {
                    run.Start();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }
            runStarted = Time.unscaledTime;
            clock.Reset();
            LevelUpsThisRun = 0;
            GemPickupsThisRun = 0;
            StationsLeftThisRun = 0;
            floorClearPending = false;
            run.Hp = 0f; // 0 = full HP at the start of a run / after a defeat
            debugStatsOverride = null;
            Log("run_start", run.RunId);
            if (resume) Log("run_resume", run.CurrentFloor.ToString());
            EnterCombat(true);
        }

        void ShowCage(Vector3 position)
        {
            if (combat == null) return;
            GameObject cage = CharacterFactory.CreateCage(combat.transform);
            cage.transform.position = position;
        }

        void AttachDayNightView()
        {
            var view = modeRoot.GetComponent<DayNightView>();
            if (view == null) view = modeRoot.gameObject.AddComponent<DayNightView>();
            view.Clock = clock;
            Camera target = FindAnyObjectByType<Camera>();
            view.TargetCamera = target;
            view.MainLight = FindAnyObjectByType<Light>();
        }

        void DrawHud()
        {
            Clear();
            DrawJoystickArea();
            DrawTopHud("PlayerHealth", FloorPlan.Kind(run.CurrentFloor) != FloorKind.Normal);
            UpdateHudBars();
        }

        static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
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

        void OnPlayerDied()
        {
            audioHub.Play("defeat");
            try
            {
                if (run.Phase != RunPhase.Combat && run.Phase != RunPhase.LevelUp && run.Phase != RunPhase.Station)
                {
                    return;
                }
                run.PlayerDied();
                Log("player_death", run.CurrentFloor.ToString());
                SaveNow();
                DrawDeathResult();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
