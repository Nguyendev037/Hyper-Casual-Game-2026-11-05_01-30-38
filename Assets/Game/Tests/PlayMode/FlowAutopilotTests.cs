using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RHMG.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RHMG.PlayTests
{
    public class FlowAutopilotTests
    {
        string testDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), "RHMG_Play_" + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
            Directory.CreateDirectory(testDirectory);
            GameBootstrap.SaveDirectoryOverride = testDirectory;
            GameBootstrap[] oldBootstraps = UnityEngine.Object.FindObjectsByType<GameBootstrap>(
                FindObjectsSortMode.None);
            foreach (GameBootstrap oldBootstrap in oldBootstraps)
            {
                UnityEngine.Object.DestroyImmediate(oldBootstrap.gameObject);
            }
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameBootstrap.SaveDirectoryOverride = null;
            GameBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<GameBootstrap>(
                FindObjectsSortMode.None);
            foreach (GameBootstrap bootstrap in bootstraps)
            {
                UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);
            }
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
            yield return null;
        }

        const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance;

        static void Invoke(GameFlow flow, string name, params object[] args)
        {
            typeof(GameFlow).GetMethod(name, Private).Invoke(flow, args);
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator LevelUpWatchdog_AfterTwoSeconds_SelectsFirstCard()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Invoke(flow, "StartRun");
            var run = (RHMG.Core.RunController)typeof(GameFlow).GetField("run", Private).GetValue(flow);
            run.Progress.GrantFreeLevelUp();
            Invoke(flow, "OpenLevelUpScreen");
            typeof(GameFlow).GetField("levelUpShown", Private).SetValue(flow, false);
            LogAssert.Expect(LogType.Error, "[Flow] Level-up screen did not appear within 2 s, picking the first card");
            yield return new WaitForSecondsRealtime(2.15f);
            Assert.AreEqual(1, flow.LevelUpsThisRun);
            Assert.AreEqual(RHMG.Core.RunPhase.Combat, run.Phase);
            Assert.Greater(Time.timeScale, 0f);
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator Gems_OverThreeHundred_MergeWithoutLosingXp()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Invoke(flow, "StartRun");
            var world = UnityEngine.Object.FindFirstObjectByType<RHMG.Runtime.Combat.CombatWorld>();
            world.Running = false;
            for (int i = 0; i < 1000; i++) world.Gems.Drop(new Vector3(i % 30, 0, i / 30), 1);
            Assert.AreEqual(300, world.Gems.Count);
            Assert.AreEqual(1000L, world.Gems.TotalValueOnField);
            Debug.Log("[W3] gems drops=1000 count=" + world.Gems.Count + " xp=" + world.Gems.TotalValueOnField);
            yield return null;
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator FloorDirector_AtOneHundredTwenty_StopsSpawning()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Invoke(flow, "StartRun");
            var world = UnityEngine.Object.FindFirstObjectByType<RHMG.Runtime.Combat.CombatWorld>();
            world.Running = false;
            // Carry-over enemies can already fill the arena before the next floor scheduler ticks.
            for (int i = 0; i < 119; i++)
                world.SpawnEnemy(RHMG.Core.EnemyKind.SandBeetle, new Vector3(8, 0, i * 0.01f));
            var director = new RHMG.Runtime.Combat.FloorDirector(world, 99, () => { });
            director.Start();
            // Exercise the real scheduler for the full 80% spawn window, while enemies remain alive.
            for (int i = 0; i < 240; i++) director.Step(0.1f);
            Assert.AreEqual(120, world.RegularAlive);
            for (int i = 0; i < 10; i++) director.Step(0.01f);
            Assert.AreEqual(120, world.RegularAlive);
            Debug.Log("[W3] enemy cap=" + world.RegularAlive);
            yield return null;
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator ApprovedMenu_WardrobeButtons_PurchaseAndReturn()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            var data = (RHMG.Core.SaveData)typeof(GameFlow).GetField("save", Private).GetValue(flow);
            data.gold = 500;
            var ui = (RectTransform)typeof(GameFlow).GetField("root", Private).GetValue(flow);
            ui.Find("btn_menu_wardrobe").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(ui.Find("girl_preview_container").GetComponent<UnityEngine.UI.RawImage>().texture);
            Assert.IsFalse(ui.Find("item_3_card").gameObject.activeSelf, "Only two catalog outfits in each slot");
            ui.Find("item_2_card").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            string confirmation = ui.Find("btn_equip_action").GetComponentInChildren<UnityEngine.UI.Text>().text;
            StringAssert.Contains(RHMG.Core.Loc.T("Di chuyển chậm hơn 3%"), confirmation,
                "Tradeoff must be visible before purchase");
            ui.Find("btn_equip_action").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            var service = new RHMG.Core.WardrobeService(data);
            Assert.AreEqual("outfit.body.rose_dress", service.GetEquipped(RHMG.Core.OutfitSlot.Body));
            Assert.AreEqual(380, data.gold);
            ui.Find("btn_wardrobe_back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(ui.Find("btn_menu_start"));
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator ApprovedLevelUp_SelectSecondCard_ConfirmUsesThatCard()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Invoke(flow, "StartRun");
            var controller = (RHMG.Core.RunController)typeof(GameFlow).GetField("run", Private).GetValue(flow);
            controller.Progress.GrantFreeLevelUp();
            Invoke(flow, "OpenLevelUpScreen");
            string chosenId = controller.Cards[1].Id;
            var ui = (RectTransform)typeof(GameFlow).GetField("root", Private).GetValue(flow);
            yield return null;
            ui.Find("card_2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.AreEqual(0, flow.LevelUpsThisRun, "Selection waits for the confirm button");
            ui.Find("btn_confirm_card").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, flow.LevelUpsThisRun);
            Assert.AreEqual(RHMG.Core.RunPhase.Combat, controller.Phase);
            string csv = File.ReadAllText(Path.Combine(testDirectory, "events.csv"));
            StringAssert.Contains(chosenId, csv);
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator SpiritBook_FromMenu_FocusAndEscapePreserveBackRoute()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            var ui = (RectTransform)typeof(GameFlow).GetField("root", Private).GetValue(flow);
            ui.Find("btn_menu_spirits").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Invoke(flow, "ForcePause");
            Invoke(flow, "Pause");
            yield return null;
            Assert.IsNull(typeof(GameFlow).GetField("combat", Private).GetValue(flow));
            Assert.AreNotEqual("Pause", typeof(GameFlow).GetField("mode", Private).GetValue(flow).ToString());
            UnityEngine.UI.Button back = null;
            bool titleFound = false;
            foreach (UnityEngine.UI.Text label in ui.GetComponentsInChildren<UnityEngine.UI.Text>())
                if (label.text == RHMG.Core.Loc.T("Sổ tinh linh")) titleFound = true;
            foreach (UnityEngine.UI.Button button in ui.GetComponentsInChildren<UnityEngine.UI.Button>())
                if (button.GetComponentInChildren<UnityEngine.UI.Text>()?.text == RHMG.Core.Loc.T("Quay lại")) back = button;
            Assert.IsTrue(titleFound);
            Assert.IsNotNull(back);
            back.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Menu", typeof(GameFlow).GetField("mode", Private).GetValue(flow).ToString());
            Assert.IsNotNull(ui.Find("btn_menu_start"));
        }

        // Old test kept, adapted: the campaign is now 100 floors with stations, so the debug clear walks the whole
        // survival flow (rescues, station tiles, fusion, final rescue) and then the Rest-screen checks as before.
        [UnityTest, Timeout(600000)]
        public IEnumerator FullFlow_DebugClear()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            yield return flow.RunAutopilot(false, reason => failure = reason);
            Assert.IsNull(failure, failure);
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "events.csv")));
        }

        // Old test kept: floor 1 real combat. Now the floor is a 25 s timer with stones, not "kill every enemy".
        [UnityTest, Timeout(150000)]
        public IEnumerator Floor1_RealCombat_Clears()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            yield return flow.RunFloor1RealCombat(reason => failure = reason);
            Assert.IsNull(failure, failure);
        }

        static void ApplyShots(GameFlow flow)
        {
            string directory = Environment.GetEnvironmentVariable("RHMG_SHOTS");
            if (!string.IsNullOrEmpty(directory))
            {
                flow.SetCaptureDirectory(directory);
            }
        }

        // Defeat keeps the station checkpoint; "Chơi lại" resumes there; a new run drops it (SPEC-P01 §4, 01 §2).
        [UnityTest, Timeout(120000)]
        public IEnumerator Defeat_ResumesFromStationCheckpoint()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            AutopilotReport report = new AutopilotReport();
            yield return flow.RunCheckpointProbe(reason => failure = reason, report);
            Assert.IsNull(failure, failure);
            Debug.Log("[Checkpoint] " + report.Note);
        }

        // T9: real combat floors 1..6 through one station: >= 1 level-up, >= 20 stones, no exception.
        [UnityTest, Timeout(500000)]
        public IEnumerator T9_Survivor_Floors1To6_ThroughStation()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            AutopilotReport report = new AutopilotReport();
            ApplyShots(flow);
            yield return flow.RunSurvivorProbe(6, 2f, reason => failure = reason, report);
            TestContext.Progress.WriteLine("[T9] " + report);
            Debug.Log("[T9] " + report);
            Assert.IsNull(failure, failure);
            Assert.GreaterOrEqual(report.FloorsCleared, 6, "floors cleared");
            Assert.GreaterOrEqual(report.StationsPassed, 1, "stations passed");
            Assert.GreaterOrEqual(report.LevelUps, 1, "level-ups");
            Assert.GreaterOrEqual(report.GemPickups, 20, "stones picked up");
        }

        // T10: debug flag jumps to floor 100 with the reference build (BalanceProbe.BuildReference(40)); the final boss
        // must show all three phases, the purification window, and end in the rescue scene without hanging.
        // The girl is invulnerable here: the test measures the boss script, not the autopilot's dodging.
        [UnityTest, Timeout(700000)]
        public IEnumerator T10_BlueHairGuy_Floor100_ThreePhases()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            AutopilotReport report = new AutopilotReport();
            ApplyShots(flow);
            yield return flow.RunBossProbe(3f, 420f, reason => failure = reason, report);
            TestContext.Progress.WriteLine("[T10] " + report);
            Debug.Log("[T10] " + report);
            Assert.IsNull(failure, failure);
            Assert.IsTrue(report.BossPhase1, "phase 1 seen");
            Assert.IsTrue(report.BossPhase2, "phase 2 seen");
            Assert.IsTrue(report.BossPhase3, "phase 3 seen");
            Assert.IsTrue(report.AnchorsSeen, "anchors seen");
            Assert.IsTrue(report.Purified, "purification window seen");
            Assert.IsTrue(report.RescueReached, "rescue scene reached");
        }
    }
}
