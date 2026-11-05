using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RHMG.EditorTools
{
    /// <summary>Screenshot helper for evidence capture.</summary>
    public static class CaptureTools
    {
        const string MediaDir = "H:/KITTS_Games/planning/01_2026-11-05_rhmg-demo-v01/evidence/media";

        [MenuItem("RHMG/Capture Game View")]
        public static void CaptureGameView()
        {
            Directory.CreateDirectory(MediaDir);
            string path = MediaDir + "/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[RHMG.Capture] " + path + " (chi chup khi Game View dang render, can Play Mode)");
        }
    }
}
