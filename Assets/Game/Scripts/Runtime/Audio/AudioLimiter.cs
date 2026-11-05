using System;

namespace RHMG.Runtime.Audio
{
    /// <summary>Master-bus soft knee. Individual clip peaks and voice limits alone cannot bound a mix.</summary>
    public static class AudioLimiter
    {
        public const float Knee = 0.72f;
        public const float Ceiling = 0.90f;

        public static float Process(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) return 0f;
            float magnitude = Math.Abs(sample);
            if (magnitude <= Knee) return sample;
            // Continuous value and first derivative at the knee; no hard clipping or gain pumping.
            float excess = magnitude - Knee;
            float headroom = Ceiling - Knee;
            float output = Knee + headroom * (excess / (headroom + excess));
            return sample < 0f ? -output : output;
        }

        public static void Process(float[] samples)
        {
            for (int i = 0; i < samples.Length; i++) samples[i] = Process(samples[i]);
        }
    }
}
