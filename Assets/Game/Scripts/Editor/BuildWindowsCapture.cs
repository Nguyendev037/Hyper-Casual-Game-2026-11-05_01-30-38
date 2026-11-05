using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RHMG.EditorTools
{
    public static class BuildWindowsCapture
    {
        const string OutputPath = "Builds/WindowsCapture/RHMG.exe";

        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                Fail("Windows Standalone Build Support is unavailable.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception exception)
            {
                Fail("Build exception: " + exception);
                return;
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail("Build " + report.summary.result + " errors=" + report.summary.totalErrors);
                return;
            }

            Debug.Log("[RHMG.BuildWindowsCapture] OK " + OutputPath + " size=" + report.summary.totalSize);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Fail(string message)
        {
            Debug.LogError("[RHMG.BuildWindowsCapture] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
