using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RHMG.Runtime.Audio;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Unity wrapper over <see cref="AudioSynth"/> (SPEC-P07): renders sample data on a worker thread at boot,
    /// creates AudioClips on the main thread on first use. 44.1 kHz mono.</summary>
    public static class ProceduralAudio
    {
        private static readonly Dictionary<string, AudioClip> s_Clips = new Dictionary<string, AudioClip>();
        private static readonly ConcurrentDictionary<string, float[]> s_Data = new ConcurrentDictionary<string, float[]>();
        private static readonly HashSet<string> s_Warned = new HashSet<string>();
        private static bool s_Prewarming;

        /// <summary>Starts rendering every known clip in the background (music first). Safe to call more than once.</summary>
        public static void PrewarmAll()
        {
            if (s_Prewarming) return;
            s_Prewarming = true;
            List<string> ids = new List<string>(AudioSynth.MusicIds);
            ids.AddRange(AudioSynth.SfxIds);
            Task.Run(() =>
            {
                foreach (string id in ids)
                {
                    if (!s_Data.ContainsKey(id)) s_Data[id] = AudioSynth.Render(id);
                }
            });
        }

        public static AudioClip Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (s_Clips.TryGetValue(id, out AudioClip cached) && cached != null) return cached;
            if (!AudioSynth.IsKnown(id) && s_Warned.Add(id)) Debug.LogWarning("[ProceduralAudio] Unknown clip id: " + id);

            if (!s_Data.TryGetValue(id, out float[] data))
            {
                data = AudioSynth.Render(id); // not prewarmed yet: render now (main thread)
                s_Data[id] = data;
            }
            AudioClip clip = AudioClip.Create(id, data.Length, 1, AudioSynth.SampleRate, false);
            clip.SetData(data, 0);
            s_Clips[id] = clip;
            return clip;
        }
    }
}
