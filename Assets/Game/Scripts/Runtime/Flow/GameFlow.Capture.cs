#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using RHMG.Core;
using RHMG.Runtime.Combat;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        string captureDirectory;

        public void SetCaptureDirectory(string directory)
        {
            captureDirectory = directory;
            if (!string.IsNullOrEmpty(captureDirectory)) Directory.CreateDirectory(captureDirectory);
        }

        IEnumerator Capture(string name)
        {
            if (string.IsNullOrEmpty(captureDirectory)) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, name + ".png"));
            yield return null;
            yield return null;
        }

        bool HasLiveEnemy()
        {
            EnemyController[] enemies = combat.GetComponentsInChildren<EnemyController>(true);
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null && enemy.Alive) return true;
            }
            return false;
        }

        bool HasLiveBoss()
        {
            EnemyController[] enemies = combat.GetComponentsInChildren<EnemyController>(true);
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null && enemy.Alive && enemy.Kind == EnemyKind.CageWarden) return true;
            }
            return false;
        }
    }
}
#endif
