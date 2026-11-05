using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Generates synthetic procedural SFX and music loops at 22050 Hz mono.</summary>
    public static class ProceduralAudio
    {
        private const int SampleRate = 22050;
        private static readonly Dictionary<string, AudioClip> s_ClipCache = new Dictionary<string, AudioClip>();

        public static AudioClip Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (s_ClipCache.TryGetValue(id, out var cached) && cached != null)
                return cached;

            AudioClip clip = GenerateClip(id);
            if (clip != null)
            {
                s_ClipCache[id] = clip;
            }
            return clip;
        }

        private static AudioClip GenerateClip(string id)
        {
            float[] data = null;
            switch (id)
            {
                case "shoot":
                    data = GenShoot();
                    break;
                case "hit":
                    data = GenHit();
                    break;
                case "hurt":
                    data = GenHurt();
                    break;
                case "bite":
                    data = GenBite();
                    break;
                case "ui":
                    data = GenUi();
                    break;
                case "rescue":
                    data = GenRescue();
                    break;
                case "fusion":
                    data = GenFusion();
                    break;
                case "transform":
                    data = GenTransform();
                    break;
                case "music_loop":
                    data = GenMusicLoop(false);
                    break;
                case "music_boss":
                    data = GenMusicLoop(true);
                    break;
                default:
                    Debug.LogWarning($"[ProceduralAudio] Unknown clip id: {id}");
                    data = GenBeep(440f, 0.1f);
                    break;
            }

            if (data == null || data.Length == 0) return null;

            AudioClip clip = AudioClip.Create(id, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float[] GenShoot()
        {
            int len = (int)(0.15f * SampleRate);
            float[] d = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(880f, 320f, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Exp(-t * 8f);
                d[i] = Mathf.Sin(phase) * env * 0.45f;
            }
            return d;
        }

        private static float[] GenHit()
        {
            int len = (int)(0.12f * SampleRate);
            float[] d = new float[len];
            float phase = 0f;
            var rng = new System.Random(42);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(220f, 60f, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.3f;
                float env = Mathf.Exp(-t * 12f);
                d[i] = (Mathf.Sin(phase) + noise) * env * 0.5f;
            }
            return d;
        }

        private static float[] GenHurt()
        {
            int len = (int)(0.25f * SampleRate);
            float[] d = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(140f, 45f, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Exp(-t * 6f);
                float s = Mathf.Sin(phase);
                d[i] = Mathf.Clamp(s * 1.5f, -0.6f, 0.6f) * env;
            }
            return d;
        }

        private static float[] GenBite()
        {
            int len = (int)(0.20f * SampleRate);
            float[] d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float tSec = (float)i / SampleRate;
                float snap1 = Mathf.Exp(-tSec * 45f) * Mathf.Sin(2f * Mathf.PI * 350f * tSec);
                float t2 = tSec - 0.08f;
                float snap2 = (t2 > 0f) ? Mathf.Exp(-t2 * 45f) * Mathf.Sin(2f * Mathf.PI * 280f * t2) : 0f;
                d[i] = (snap1 + snap2) * 0.5f;
            }
            return d;
        }

        private static float[] GenUi()
        {
            int len = (int)(0.08f * SampleRate);
            float[] d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float tSec = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 16f);
                d[i] = Mathf.Sin(2f * Mathf.PI * 1200f * tSec) * env * 0.35f;
            }
            return d;
        }

        private static float[] GenRescue()
        {
            int len = (int)(0.50f * SampleRate);
            float[] d = new float[len];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
            float noteDur = 0.50f / notes.Length;

            for (int i = 0; i < len; i++)
            {
                float tSec = (float)i / SampleRate;
                int nIdx = Mathf.Clamp((int)(tSec / noteDur), 0, notes.Length - 1);
                float noteT = tSec - nIdx * noteDur;
                float freq = notes[nIdx];
                float env = Mathf.Exp(-noteT * 5f);
                float val = Mathf.Sin(2f * Mathf.PI * freq * noteT) * env;
                d[i] = val * 0.40f;
            }
            return d;
        }

        private static float[] GenFusion()
        {
            int len = (int)(0.55f * SampleRate);
            float[] d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float tSec = (float)i / SampleRate;
                float mod = Mathf.Sin(2f * Mathf.PI * 14f * tSec) * 120f;
                float carrierFreq = Mathf.Lerp(440f, 880f, t) + mod;
                float env = Mathf.Sin(t * Mathf.PI);
                d[i] = Mathf.Sin(2f * Mathf.PI * carrierFreq * tSec) * env * 0.4f;
            }
            return d;
        }

        private static float[] GenTransform()
        {
            int len = (int)(0.50f * SampleRate);
            float[] d = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(110f, 660f, t * t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Sin(t * Mathf.PI);
                d[i] = Mathf.Sin(phase) * env * 0.45f;
            }
            return d;
        }

        private static float[] GenMusicLoop(bool boss)
        {
            const float duration = 16.0f;
            int totalSamples = (int)(duration * SampleRate);
            float[] d = new float[totalSamples];

            // D minor pentatonic scale: D4 (293.66), F4 (349.23), G4 (392.00), A4 (440.00), C5 (523.25)
            float[] scale = boss
                ? new float[] { 220f, 261.63f, 293.66f, 329.63f, 392f } // A minor pentatonic for boss
                : new float[] { 293.66f, 349.23f, 392f, 440f, 523.25f };

            int barCount = 16;
            float barDur = duration / barCount; // 1.0 s per note/bar

            for (int i = 0; i < totalSamples; i++)
            {
                float tSec = (float)i / SampleRate;
                int barIdx = (int)(tSec / barDur) % barCount;
                float noteT = tSec - barIdx * barDur;

                // Melody note selection
                int noteIdx = (barIdx * 3 + (barIdx / 4)) % scale.Length;
                float freq = scale[noteIdx];
                float melEnv = Mathf.Sin(Mathf.Clamp01(noteT / barDur) * Mathf.PI);
                float mel = Mathf.Sin(2f * Mathf.PI * freq * noteT) * melEnv * 0.22f;

                // Bass drone / rhythm pulse
                float bassFreq = scale[0] * 0.5f;
                float pulseFreq = boss ? 4f : 2f; // beats per second
                float pulseT = (tSec * pulseFreq) % 1f;
                float pulseEnv = Mathf.Exp(-pulseT * 5f);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * tSec) * pulseEnv * (boss ? 0.28f : 0.16f);

                // Smooth loop seam (first and last 0.05s)
                float seam = 1f;
                if (tSec < 0.05f) seam = tSec / 0.05f;
                else if (tSec > duration - 0.05f) seam = (duration - tSec) / 0.05f;

                d[i] = (mel + bass) * seam;
            }
            return d;
        }

        private static float[] GenBeep(float freq, float duration)
        {
            int len = (int)(duration * SampleRate);
            float[] d = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float tSec = (float)i / SampleRate;
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * tSec) * (1f - t) * 0.3f;
            }
            return d;
        }
    }
}
