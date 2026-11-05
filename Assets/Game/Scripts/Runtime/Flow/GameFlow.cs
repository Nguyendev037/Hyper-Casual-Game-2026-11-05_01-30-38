using System;
using System.Collections.Generic;
using System.IO;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow : MonoBehaviour
    {
        enum Mode
        {
            Menu,
            Tower,
            Upgrade,
            Rescue,
            Rest,
            Showcase,
            Pause,
            Result,
            Credits
        }

        SaveData save;
        SaveService saves;
        RewardService rewards;
        RunController run;
        FusionService fusion;
        WardrobeService wardrobe;
        IEventSink events;
        CombatSession combat;
        DayNightClock clock;
        Mode mode;
        Mode pauseReturnMode;
        Mode showcaseReturnMode;
        bool pauseReturnRunning;
        bool pauseReturnClockPaused;
        Canvas canvas;
        RectTransform root;
        Transform modeRoot;
        RectTransform toastPanel;
        Text toastText;
        AudioHub audioHub;
        UiSizeAudit sizeAudit;
        bool choosing;
        bool deleteArmed;
        bool tutorialDisplayed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool debugAutoWin;
#endif
        float toastUntil;
        float runStarted;
        string spiritId; // null = no spirit travels with the girl (floor 1 of a run)
        int seed = 20261105;
        Image hpFill;
        Image bossFill;
        Image commandFill;
        Image clockFill;
        Text hpText;
        Text clockText;
        int displayedHp = int.MinValue;
        int playerMaxHp;
        int displayedClock = int.MinValue;
        DayPhase displayedPhase = (DayPhase)(-1);
        CombatSession syncedEnvironmentCombat;
        float lastPlayerHp;
        float lastPlayerShield;
        bool hasPlayerVitals;
        readonly Queue<string> toastQueue = new Queue<string>();

        public void Initialize(SaveData data, SaveService saveService, IEventSink eventSink)
        {
            save = data;
            Loc.Current = Loc.Parse(save.language);
            saves = saveService;
            events = eventSink;
            rewards = new RewardService(save);
            fusion = new FusionService(save, Catalog.DemoTrialSpiritIds);
            wardrobe = new WardrobeService(save);
            audioHub = gameObject.AddComponent<AudioHub>();
            audioHub.MusicOn = save.musicOn;
            audioHub.SfxOn = save.sfxOn;
            audioHub.VibrationOn = save.vibrationOn;
            audioHub.StartMusic();
            clock = new DayNightClock();
            clock.PhaseChanged += OnPhaseChanged;
            BuildCanvas();
            CreateGameEnvironment();
            ShowMenu();
        }

        void BuildCanvas()
        {
            var canvasObject = new GameObject("FlowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            sizeAudit = canvasObject.AddComponent<UiSizeAudit>();
            root = UiKit.Panel(canvasObject.transform, "SafeRoot", Vector2.zero, Vector2.one, Color.clear);
            root.gameObject.AddComponent<SafeAreaFitter>();
            toastPanel = UiKit.Panel(root, "ToastPanel", Vector2.zero, Vector2.zero,
                new Color(0.10f, 0.08f, 0.12f, 0.96f));
            // Below the 3-row top HUD and the floor-5 boss bar.
            toastPanel.anchorMin = new Vector2(0.5f, 0.70f);
            toastPanel.anchorMax = new Vector2(0.5f, 0.70f);
            toastPanel.sizeDelta = new Vector2(900, 120);
            toastPanel.GetComponent<Image>().sprite = UiSkin.Ui("toast") ??
                toastPanel.GetComponent<Image>().sprite;
            if (UiSkin.Ui("toast") != null) toastPanel.GetComponent<Image>().color = Color.white;
            toastPanel.GetComponent<Image>().raycastTarget = false;
            toastText = UiKit.Text(toastPanel, string.Empty, 40, Color.white, TextAnchor.MiddleCenter);
            toastText.rectTransform.anchorMin = Vector2.zero;
            toastText.rectTransform.anchorMax = Vector2.one;
            toastText.raycastTarget = false;
            toastPanel.gameObject.SetActive(false);
            toastText.gameObject.SetActive(false);
        }

        void Clear()
        {
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                if (child != toastText.transform && child != toastPanel.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            // The canvas is screen-space overlay: an opaque backdrop here hides the 3D world.
            // World screens (tower, rest, showcase) get nothing; paused-over-world screens get a dim
            // tint; only pure menu screens get the painted bg_tower.
            bool worldScreen = mode == Mode.Tower || mode == Mode.Showcase || (mode == Mode.Rest && !clock.Paused);
            bool dimmedWorld = mode == Mode.Pause || (mode == Mode.Rest && clock.Paused);
            if (worldScreen || mode == Mode.Menu)
            {
                return;
            }
            RectTransform tint = UiKit.Panel(root, "TowerTint", Vector2.zero, Vector2.one,
                new Color(0.15f, 0.20f, 0.32f, dimmedWorld ? 0.6f : 0.35f));
            tint.GetComponent<Image>().raycastTarget = false;
            tint.SetAsFirstSibling();
            if (!dimmedWorld)
            {
                Image backdrop = UiKit.Background(root, "bg_tower");
                backdrop.transform.SetAsFirstSibling();
            }
        }

        void Label(string value, float y, int size = 42, Color? color = null)
        {
            int fontSize = Mathf.Max(40, size);
            Text text = UiKit.Text(root, value, fontSize, color ?? Color.white, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = new Vector2(0.06f, y);
            text.rectTransform.anchorMax = new Vector2(0.94f, y + 0.09f);
            if (fontSize >= 64)
            {
                text.font = UiSkin.Title;
                text.fontSize = Mathf.Clamp(fontSize, 72, 96);
                Shadow shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shadow.effectDistance = new Vector2(0f, -4f);
            }
        }

        void Button(string value, float y, Action action, float height = 0.06f)
        {
            Color color = value == "Chơi" ? RHMG.Runtime.Visuals.Palette.HairRed :
                new Color(0.22f, 0.18f, 0.22f, 0.96f);
            float actualHeight = value == "Chơi" ? 168f :
                value == "Mở lồng" || value == "Tiếp" ? 150f : Mathf.Max(132f, height * 1920f);
            float actualWidth = value == "Xóa dữ liệu" ? 360f : 840f;
            var button = UiKit.Button(root, value, new Vector2(actualWidth, actualHeight), color);
            RectTransform rect = (RectTransform)button.transform;
            float centerY = y + height * 0.5f;
            rect.anchorMin = new Vector2(0.5f, centerY);
            rect.anchorMax = new Vector2(0.5f, centerY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(actualWidth, actualHeight);
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
            sizeAudit.Capture(root, mode.ToString());
        }

        void Log(string name, string payload = "")
        {
            try
            {
                events?.Log(name, payload);
            }
            catch (IOException exception)
            {
                Debug.LogException(exception);
            }
        }

        void SetMode(Mode next)
        {
            deleteArmed = false;
            ClearNotifications();
            Transform previousRoot = modeRoot;
            mode = next;
            modeRoot = new GameObject(next + "Root").transform;
            modeRoot.SetParent(transform, false);
            if (combat != null)
            {
                combat.transform.SetParent(modeRoot, false);
                AttachDayNightView();
            }
            SyncGameEnvironment();
            if (previousRoot != null)
            {
                Destroy(previousRoot.gameObject);
            }
            Debug.Log("[Flow] " + next);
            sizeAudit.Capture(root, next.ToString());
        }

        public void SaveNow()
        {
            try
            {
                saves.Save(save);
            }
            catch (IOException exception)
            {
                Debug.LogException(exception);
                Toast("Không lưu được");
            }
        }

        void Update()
        {
            if (syncedEnvironmentCombat != combat)
            {
                SyncGameEnvironment();
                syncedEnvironmentCombat = combat;
            }
            if (toastText.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
            {
                toastText.gameObject.SetActive(false);
                toastPanel.gameObject.SetActive(false);
                ShowNextToast();
            }
            if (mode == Mode.Tower || mode == Mode.Rest || mode == Mode.Showcase)
            {
                ReadKeyboard();
            }
            if (combat != null && combat.Running)
            {
                clock.Tick(Time.deltaTime);
                UpdateHudBars();
            }
            else if (mode == Mode.Rest)
            {
                UpdateHudBars();
            }
        }

        void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Pause();
            }
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                combat?.TryCommand();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (keyboard.f9Key.wasPressedThisFrame && mode == Mode.Tower)
            {
                debugAutoWin = true;
                Toast("F9: dọn địch tầng này");
            }
            if (debugAutoWin && mode == Mode.Tower && combat != null)
            {
                DebugFloorHelper.ClearSpawnedEnemies(combat);
            }
#endif
        }

    }
}
