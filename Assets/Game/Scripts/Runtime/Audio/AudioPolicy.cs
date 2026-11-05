using System;
using System.Collections.Generic;

namespace RHMG.Runtime.Audio
{
    /// <summary>Voice limiting rules (SPEC-P07 §2), plain C# so the burst mix can be measured outside Unity.
    /// Survivor combat fires dozens of shots/hits per second; without these rules the one-shots stack past 0 dBFS.</summary>
    public sealed class AudioPolicy
    {
        public struct Rule
        {
            public float Volume, MinInterval, PitchJitter;
            public int MaxVoices;
            public Rule(float volume, float minInterval, int maxVoices, float pitchJitter)
            {
                Volume = volume; MinInterval = minInterval; MaxVoices = maxVoices; PitchJitter = pitchJitter;
            }
        }

        private static readonly Dictionary<string, Rule> s_Rules = new Dictionary<string, Rule>
        {
            { "shoot", new Rule(0.30f, 0.07f, 3, 0.05f) },
            { "hit", new Rule(0.32f, 0.05f, 4, 0.06f) },
            { "enemy_die", new Rule(0.35f, 0.06f, 3, 0.06f) },
            { "gem", new Rule(0.40f, 0.035f, 4, 0f) },
            { "hurt", new Rule(0.70f, 0.25f, 1, 0.03f) },
            { "bite", new Rule(0.45f, 0.12f, 2, 0.05f) },
            { "spirit_cast", new Rule(0.50f, 0.20f, 2, 0.04f) },
            { "ui", new Rule(0.55f, 0.05f, 2, 0f) },
            { "station_tile", new Rule(0.60f, 0.20f, 1, 0f) },
            { "anchor_break", new Rule(0.75f, 0.20f, 2, 0.03f) },
            { "levelup", new Rule(0.80f, 0.30f, 1, 0f) },
            { "rescue", new Rule(0.80f, 0.50f, 1, 0f) },
            { "fusion", new Rule(0.80f, 0.50f, 1, 0f) },
            { "transform", new Rule(0.80f, 0.50f, 1, 0f) },
            { "boss_warn", new Rule(0.85f, 1.00f, 1, 0f) },
            { "boss_phase", new Rule(0.90f, 1.00f, 1, 0f) },
            { "victory", new Rule(0.90f, 1.00f, 1, 0f) },
            { "defeat", new Rule(0.90f, 1.00f, 1, 0f) },
        };
        private static readonly Rule s_Default = new Rule(0.50f, 0.10f, 2, 0.03f);
        private static readonly int[] GemSteps = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

        public const float MusicVolume = 0.35f;
        public const int SfxVoices = 10;
        public const float GemStreakReset = 0.6f;

        private readonly Dictionary<string, float> m_Last = new Dictionary<string, float>();
        private readonly Dictionary<string, List<float>> m_Ends = new Dictionary<string, List<float>>();
        private readonly Random m_Rng;
        private int m_GemStreak;
        private float m_LastGem = -10f;

        public AudioPolicy(int seed = 12345)
        {
            m_Rng = new Random(seed);
        }

        public static Rule For(string id)
        {
            return id != null && s_Rules.TryGetValue(id, out Rule r) ? r : s_Default;
        }

        /// <summary>Decides whether a one-shot may start at <paramref name="now"/> (seconds). On success returns
        /// the volume and pitch to use and books the voice until now + clipSeconds / pitch.</summary>
        public bool TryStart(string id, float now, float clipSeconds, out float volume, out float pitch)
        {
            Rule rule = For(id);
            volume = 0f;
            pitch = 1f;
            if (m_Last.TryGetValue(id, out float last) && now - last < rule.MinInterval) return false;
            if (!m_Ends.TryGetValue(id, out List<float> ends))
            {
                ends = new List<float>();
                m_Ends[id] = ends;
            }
            ends.RemoveAll(e => e <= now);
            if (ends.Count >= rule.MaxVoices) return false;

            if (id == "gem")
            {
                m_GemStreak = now - m_LastGem <= GemStreakReset ? Math.Min(m_GemStreak + 1, GemSteps.Length - 1) : 0;
                m_LastGem = now;
                pitch = (float)Math.Pow(2.0, GemSteps[m_GemStreak] / 12.0);
            }
            else if (rule.PitchJitter > 0f)
            {
                pitch = 1f + (float)(m_Rng.NextDouble() * 2.0 - 1.0) * rule.PitchJitter;
            }
            volume = rule.Volume;
            m_Last[id] = now;
            ends.Add(now + clipSeconds / Math.Max(0.1f, pitch));
            return true;
        }
    }
}
