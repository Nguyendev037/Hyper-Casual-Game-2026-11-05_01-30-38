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

        [UnityTest, Timeout(300000)]
        public IEnumerator FullFlow_DebugClear()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            yield return flow.RunAutopilot(false, reason => failure = reason);
            Assert.IsNull(failure, failure);
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "events.csv")));
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator Floor1_RealCombat_Clears()
        {
            GameFlow flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "Main scene did not initialize GameFlow");
            string failure = null;
            yield return flow.RunFloor1RealCombat(reason => failure = reason);
            Assert.IsNull(failure, failure);
        }
    }
}
