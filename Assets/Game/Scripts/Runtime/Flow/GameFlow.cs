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
        Text toastText;
        AudioHub audioHub;
        bool choosing;
        bool deleteArmed;
        bool tutorialDisplayed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool debugAutoWin;
#endif
        float toastUntil;
        float runStarted;
        string spiritId = "spirit.dew";
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
            root = UiKit.Panel(canvasObject.transform, "SafeRoot", Vector2.zero, Vector2.one, Color.clear);
            root.gameObject.AddComponent<SafeAreaFitter>();
            toastText = UiKit.Text(root, string.Empty, 48, Color.white, TextAnchor.MiddleCenter);
            toastText.rectTransform.anchorMin = new Vector2(0.05f, 0.66f);
            toastText.rectTransform.anchorMax = new Vector2(0.95f, 0.72f);
            toastText.gameObject.SetActive(false);
        }

        void Clear()
        {
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                if (child != toastText.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        void Label(string value, float y, int size = 42, Color? color = null)
        {
            int fontSize = Mathf.Max(40, y >= 0.79f ? 64 : size);
            Text text = UiKit.Text(root, value, fontSize, color ?? Color.white, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = new Vector2(0.06f, y);
            text.rectTransform.anchorMax = new Vector2(0.94f, y + 0.09f);
        }

        void Button(string value, float y, Action action, float height = 0.06f)
        {
            Color color = value == "Chơi" ? RHMG.Runtime.Visuals.Palette.HairRed :
                new Color(0.22f, 0.18f, 0.22f, 0.96f);
            var button = UiKit.Button(root, value, new Vector2(700, 100), color);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.15f, y);
            rect.anchorMax = new Vector2(0.85f, y + height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
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
            if (mode == Mode.Tower && hpText == null)
            {
                BindHudFields();
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
