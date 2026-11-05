#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Opt-in player evidence for P06. Never loaded by a release build.</summary>
    public sealed class AnimationProbe : MonoBehaviour
    {
        private string directory;
        private readonly List<SpriteBillboard> actors = new List<SpriteBillboard>();
        private SpriteBillboard girl;
        private Camera view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-rhmgAnimationProbe");
            if (index < 0 || index + 1 >= args.Length) return;
            // Set before the first frame: a hidden player otherwise suspends before Start resumes.
            Application.runInBackground = true;
            AudioListener.volume = 0f;
            var probe = new GameObject("AnimationEvidence").AddComponent<AnimationProbe>();
            probe.directory = args[index + 1];
        }

        private IEnumerator Start()
        {
            // Let normal boot finish, then isolate the measurement from gameplay and UI.
            yield return null;
            Directory.CreateDirectory(directory);
            Application.logMessageReceived += OnLog;
            AudioListener.volume = 0f;
            Application.runInBackground = true;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root != gameObject) root.SetActive(false);
            foreach (var flow in FindObjectsByType<GameFlow>(FindObjectsSortMode.None))
                flow.gameObject.SetActive(false);
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                canvas.gameObject.SetActive(false);
            foreach (var camera in Camera.allCameras) camera.enabled = false;

            view = new GameObject("ProbeCamera").AddComponent<Camera>();
            view.tag = "MainCamera";
            view.orthographic = true;
            view.orthographicSize = 1.8f;
            view.transform.position = new Vector3(0f, 1.4f, -10f);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
            view.allowHDR = false;
            girl = Add("girl_journey_cloak_ash_hat_sprout", Vector3.zero, 2.8f);
            if (girl == null || girl.GetComponent<SpriteDeformer>() == null)
            {
                File.WriteAllText(Path.Combine(directory, "error.txt"), "Girl sprite/deformer missing");
                Application.Quit(1);
                yield break;
            }
            var step = Resources.Load<Sprite>("RHMG/Sprites/girl_journey_cloak_ash_hat_sprout_step");
            File.WriteAllText(Path.Combine(directory, "profile.txt"),
                "sprite=girl_journey_cloak_ash_hat_sprout\nstep=" + (step != null) +
                "\nframe_interval_seconds=0.06\nimage=512x512\nleg_roi=160,345,352,468\n");
            yield return CaptureSequence("frames_run", 1f);
            yield return CaptureSequence("frames_idle", 0f);

            // Real billboard/deformer work, including transforms, LOD ranking and mesh upload.
            for (int i = 0; i < 120; i++)
                Add(i % 3 == 0 ? "enemy_eye" : i % 3 == 1 ? "enemy_sentinel" : "enemy_beetle",
                    new Vector3(i % 12 - 6, 0f, i / 12 + 3), 1.8f);
            Add("dog_companion", new Vector3(2f, 0f, 0f), 1.5f);
            Add("spirit_ember", new Vector3(-2f, 1f, 0f), 1.1f);
            Add("spirit_dew", new Vector3(-3f, 1f, 0f), 1.1f);
            Add("spirit_bud", new Vector3(-4f, 1f, 0f), 1.1f);
            Add("boss_blue_hair", new Vector3(0f, 0f, 8f), 3.5f);
            foreach (var actor in actors) actor.SetMotion(1f);
            for (int i = 0; i < 30; i++)
            {
                AdvanceAll();
                yield return null;
            }
            var samples = new double[240];
            var watch = new Stopwatch();
            for (int i = 0; i < samples.Length; i++)
            {
                watch.Restart();
                AdvanceAll();
                watch.Stop();
                samples[i] = watch.Elapsed.TotalMilliseconds;
                yield return null;
            }
            int crowd = 0;
            for (int i = 1; i <= 120; i++)
                if (actors[i].GetComponent<SpriteDeformer>().MeshActive) crowd++;
            double sum = 0d;
            foreach (double sample in samples) sum += sample;
            Array.Sort(samples);
            double mean = sum / samples.Length;
            bool pass = mean <= 1.5d && crowd <= 20 && actors.Count == 126;
            string result = string.Format(CultureInfo.InvariantCulture,
                "actors={0}\nenemies=120\ncrowd_meshes={1}\nframes={2}\nmean_ms={3:F4}\np95_ms={4:F4}\nmax_ms={5:F4}\nlimit_mean_ms=1.5\nresult={6}\n",
                actors.Count, crowd, samples.Length, mean, samples[227], samples[239], pass ? "PASS" : "FAIL");
            File.WriteAllText(Path.Combine(directory, "perf.txt"), result);
            UnityEngine.Debug.Log("[AnimationProbe] " + result);
            Application.Quit(pass ? 0 : 1);
        }

        private SpriteBillboard Add(string id, Vector3 position, float height)
        {
            Sprite sprite = Resources.Load<Sprite>("RHMG/Sprites/" + id);
            if (sprite == null) throw new InvalidOperationException("Missing probe sprite " + id);
            var root = new GameObject(id);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var actor = SpriteBillboard.Create(root.transform, sprite, height);
            actor.enabled = false;
            actors.Add(actor);
            return actor;
        }

        private IEnumerator CaptureSequence(string name, float motion)
        {
            string path = Path.Combine(directory, name);
            Directory.CreateDirectory(path);
            girl.SetMotion(motion);
            for (int i = 0; i < 60; i++) girl.Advance(0.02f);
            var target = new RenderTexture(512, 512, 24);
            var texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            for (int i = 0; i < 8; i++)
            {
                for (int step = 0; step < 3; step++) girl.Advance(0.02f);
                yield return null;
                view.targetTexture = target;
                view.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(path, i.ToString("D2") + ".png"), texture.EncodeToPNG());
                RenderTexture.active = previous;
                view.targetTexture = null;
            }
            target.Release();
            Destroy(target);
            Destroy(texture);
        }

        private void AdvanceAll()
        {
            foreach (var actor in actors) actor.Advance(1f / 60f);
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            File.AppendAllText(Path.Combine(directory, "error.txt"), message + "\n" + stack + "\n");
            Application.Quit(1);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }
    }
}
#endif
