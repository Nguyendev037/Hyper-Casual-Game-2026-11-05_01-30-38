using System.IO;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
#endif
using RHMG.Core;
using RHMG.Runtime.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RHMG.Runtime
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static string SaveDirectoryOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureRoot()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null)
            {
                return;
            }
            new GameObject("GameRoot").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            string saveDirectory = GetArgument(commandLineArgs, "-rhmgSaveDir");
            if (!string.IsNullOrEmpty(saveDirectory)) SaveDirectoryOverride = saveDirectory;
#endif
            if (FindObjectsByType<GameBootstrap>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            EnsureEventSystem();
            string directory = string.IsNullOrEmpty(SaveDirectoryOverride)
                ? Path.Combine(Application.persistentDataPath, "save")
                : SaveDirectoryOverride;
            var saveService = new SaveService(
                new UnityFileStore(),
                new JsonUtilitySaveSerializer(),
                directory);
            SaveData data;
            LoadOutcome outcome = saveService.Load(out data);
            var flow = gameObject.AddComponent<GameFlow>();
            string eventPath = string.IsNullOrEmpty(SaveDirectoryOverride)
                ? null
                : Path.Combine(SaveDirectoryOverride, "events.csv");
            flow.Initialize(data, saveService, new CsvEventSink(eventPath));
            if (outcome == LoadOutcome.ResetCorrupt)
            {
                flow.Toast("Dữ liệu lưu bị hỏng, đã tạo hồ sơ mới");
                flow.SaveNow();
            }
            else if (outcome == LoadOutcome.RestoredFromBackup)
            {
                flow.Toast("Đã khôi phục bản lưu dự phòng");
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string captureDirectory = GetArgument(commandLineArgs, "-rhmgCapture");
            if (!string.IsNullOrEmpty(captureDirectory)) flow.SetCaptureDirectory(captureDirectory);
            if (HasArgument(commandLineArgs, "-rhmgAutopilot")) StartCoroutine(RunAutopilotAndQuit(flow));
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static bool HasArgument(string[] args, string value)
        {
            foreach (string arg in args)
            {
                if (arg == value) return true;
            }
            return false;
        }

        static string GetArgument(string[] args, string name)
        {
            for (int index = 0; index + 1 < args.Length; index++)
            {
                if (args[index] == name) return args[index + 1];
            }
            return null;
        }

        IEnumerator RunAutopilotAndQuit(GameFlow flow)
        {
            bool failed = false;
            string reason = null;
            yield return flow.RunAutopilot(true, message =>
            {
                failed = true;
                reason = message;
            }, 1);
            if (failed) Debug.LogError("[Autopilot] FAIL " + reason);
            else Debug.Log("[Autopilot] PASS");
            Application.Quit(failed ? 1 : 0);
        }
#endif

        static void EnsureEventSystem()
        {
            EventSystem current = FindAnyObjectByType<EventSystem>();
            if (current != null)
            {
                var legacyModule = current.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    legacyModule.enabled = false;
                    Destroy(legacyModule);
                }
                if (current.GetComponent<InputSystemUIInputModule>() == null)
                {
                    current.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }
            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
