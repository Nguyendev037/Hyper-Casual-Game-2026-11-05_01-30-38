using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RHMG.EditorTools
{
    /// <summary>Android APK build entry points (callable via -executeMethod).</summary>
    public static class BuildAndroid
    {
        const string OutDir = "Builds/Android";
        const string ApkName = "RedHairMagicGirl_demo_v0.1.apk";
        const string DevApkName = "RedHairMagicGirl_demo_v0.1_dev.apk";

        [MenuItem("RHMG/Build Android APK")]
        public static void Build() { Run(false); }

        [MenuItem("RHMG/Build Android APK (Development)")]
        public static void BuildDevelopment() { Run(true); }

        static void Fail(int code, string msg)
        {
            Debug.LogError("[RHMG.Build] " + msg);
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void Run(bool dev)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Fail(2, "Android Build Support chua cai (cai module android + android-sdk-ndk-tools + android-open-jdk cho dung Editor version).");
                return;
            }
            ProjectSetup.EnsureAll();
            Directory.CreateDirectory(OutDir);
            string apk = OutDir + "/" + (dev ? DevApkName : ApkName);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = dev ? BuildOptions.Development : BuildOptions.None
            };
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(opts); }
            catch (Exception e) { Fail(1, "Build exception: " + e); return; }

            var s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Fail(1, "Build " + s.result + " errors=" + s.totalErrors);
                return;
            }
            WriteInfo(apk, dev);
            Debug.Log("[RHMG.Build] OK " + apk + " size=" + s.totalSize);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void WriteInfo(string apk, bool dev)
        {
            string sha;
            using (var sm = SHA256.Create())
            using (var fs = File.OpenRead(apk))
                sha = BitConverter.ToString(sm.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            string text =
                "file=" + Path.GetFileName(apk) + "\n" +
                "development=" + dev + "\n" +
                "version=" + PlayerSettings.bundleVersion + "\n" +
                "versionCode=" + PlayerSettings.Android.bundleVersionCode + "\n" +
                "sha256=" + sha + "\n" +
                "time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                "editor=" + Application.unityVersion + "\n";
            string name = dev ? "build_info_dev.txt" : "build_info.txt";
            File.WriteAllText(OutDir + "/" + name, text);
        }
    }
}
