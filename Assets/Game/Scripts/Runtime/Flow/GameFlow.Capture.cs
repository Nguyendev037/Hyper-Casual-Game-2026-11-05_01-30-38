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
            if (!string.IsNullOrEmpty(directory)) Application.runInBackground = true;
            if (!string.IsNullOrEmpty(captureDirectory)) Directory.CreateDirectory(captureDirectory);
        }

        bool captureFailed;

        IEnumerator Capture(string name)
        {
            if (string.IsNullOrEmpty(captureDirectory)) yield break;
            // Offscreen rendering remains valid when the automated player's window is hidden.
            yield return null;
            Camera target = null;
            foreach (Camera candidate in Camera.allCameras)
                if (candidate.enabled && candidate.targetTexture == null &&
                    (target == null || candidate.depth > target.depth)) target = candidate;
            RenderTexture render = null;
            Texture2D pixels = null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = target != null ? target.targetTexture : null;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            try
            {
                if (target == null) throw new System.InvalidOperationException("No active capture camera");
                foreach (UI.WardrobePreview preview in root.GetComponentsInChildren<UI.WardrobePreview>()) preview.RenderNow();
                render = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32);
                render.Create();
                target.targetTexture = render;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = target;
                canvas.planeDistance = Mathf.Max(1f, target.nearClipPlane + 0.1f);
                Canvas.ForceUpdateCanvases();
                target.Render();
                RenderTexture.active = render;
                pixels = new Texture2D(540, 960, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(captureDirectory, name + ".png"), pixels.EncodeToPNG());
            }
            catch (System.Exception error)
            {
                captureFailed = true;
                Debug.LogException(error);
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                if (target != null) target.targetTexture = previousTarget;
                RenderTexture.active = previous;
                if (render != null) { render.Release(); Destroy(render); }
                if (pixels != null) Destroy(pixels);
            }
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
                if (enemy != null && enemy.Alive && enemy.IsBoss) return true;
            }
            return false;
        }
    }
}
#endif
