using System;
using System.Collections.Generic;

namespace RHMG.Runtime.Audio
{
    /// <summary>Plain C# synthesizer (no UnityEngine) for every game sound (SPEC-P07). 44.1 kHz mono floats.
    /// Instruments: Karplus-Strong pluck (gayageum colour), FM bell, janggu/buk drums, jing gong, open-fifth pad.
    /// Every clip is click-free (3 ms edge fades), never hard-clipped, and normalized to a 0.89 peak.
    /// Music loops are mixed circularly (tails wrap to the start) so they loop without a seam.</summary>
    public static class AudioSynth
    {
        public const int SampleRate = 44100;
        private const float Peak = 0.89f;
        private const double TwoPi = Math.PI * 2.0;

        public static readonly string[] MusicIds = { "music_loop", "music_boss", "music_calm" };

        public static readonly string[] SfxIds =
        {
            "shoot", "hit", "hurt", "bite", "ui", "rescue", "fusion", "transform", "gem", "levelup", "enemy_die",
            "spirit_cast", "station_tile", "boss_warn", "boss_phase", "anchor_break", "victory", "defeat"
        };

        public static bool IsMusic(string id)
        {
            return Array.IndexOf(MusicIds, id) >= 0;
        }

        public static bool IsKnown(string id)
        {
            return IsMusic(id) || Array.IndexOf(SfxIds, id) >= 0;
        }

        /// <summary>Renders a clip; unknown ids render the quiet "tok" fallback (caller warns once).</summary>
        public static float[] Render(string id)
        {
            float[] d;
            switch (id)
            {
                case "music_loop": d = MusicRun(); break;
                case "music_boss": d = MusicBoss(); break;
                case "music_calm": d = MusicCalm(); break;
                case "shoot": d = Shoot(); break;
                case "hit": d = Hit(); break;
                case "hurt": d = Hurt(); break;
                case "bite": d = Bite(); break;
                case "ui": d = Ui(1f); break;
                case "rescue": d = Rescue(); break;
                case "fusion": d = Fusion(); break;
                case "transform": d = Transform(); break;
                case "gem": d = Gem(); break;
                case "levelup": d = LevelUp(); break;
                case "enemy_die": d = EnemyDie(); break;
                case "spirit_cast": d = SpiritCast(); break;
                case "station_tile": d = StationTile(); break;
                case "boss_warn": d = BossWarn(); break;
                case "boss_phase": d = BossPhase(); break;
                case "anchor_break": d = AnchorBreak(); break;
                case "victory": d = Victory(); break;
                case "defeat": d = Defeat(); break;
                default: d = Ui(0.5f); break;
            }
            if (!IsMusic(id)) FadeEdges(d, 0.003f);
            Normalize(d, Peak);
            return d;
        }

        // ------------------------------------------------------------------ helpers

        private static int Len(double seconds) { return Math.Max(1, (int)(seconds * SampleRate)); }

        private static float Midi(double m) { return (float)(440.0 * Math.Pow(2.0, (m - 69.0) / 12.0)); }

        /// <summary>Adds src into dst at start (samples); when wrap, samples past the end wrap to the beginning.</summary>
        private static void Mix(float[] dst, float[] src, int start, float gain, bool wrap = false)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int j = start + i;
                if (j >= dst.Length)
                {
                    if (!wrap) break;
                    j %= dst.Length;
                }
                if (j < 0) continue;
                dst[j] += src[i] * gain;
            }
        }

        private static void LowPass(float[] d, float cutoff)
        {
            float a = (float)(1.0 - Math.Exp(-TwoPi * cutoff / SampleRate));
            float y = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                y += a * (d[i] - y);
                d[i] = y;
            }
        }

        private static void FadeEdges(float[] d, float seconds)
        {
            int n = Math.Min(d.Length / 2, Len(seconds));
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                d[i] *= k;
                d[d.Length - 1 - i] *= k;
            }
        }

        private static void Normalize(float[] d, float peak)
        {
            float max = 0f;
            for (int i = 0; i < d.Length; i++) max = Math.Max(max, Math.Abs(d[i]));
            if (max < 1e-6f) return;
            float g = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        /// <summary>Gentle tanh glue for dense mixes (never a hard clip).</summary>
        private static void SoftSaturate(float[] d, float drive)
        {
            float norm = (float)Math.Tanh(drive);
            for (int i = 0; i < d.Length; i++) d[i] = (float)Math.Tanh(d[i] * drive) / norm;
        }

        private static float[] Noise(int length, int seed)
        {
            Random rng = new Random(seed);
            float[] d = new float[length];
            for (int i = 0; i < length; i++) d[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
            return d;
        }

        /// <summary>Karplus-Strong plucked string: warm, decaying, gayageum-like.</summary>
        private static float[] Pluck(float freq, double seconds, float brightness, float sustain, int seed)
        {
            int n = Math.Max(2, (int)Math.Round(SampleRate / freq));
            float[] ring = Noise(n, seed);
            // Pre-filter the excitation: darker pluck when brightness is low.
            float a = 0.08f + 0.5f * brightness, y = 0f;
            for (int i = 0; i < n; i++) { y += a * (ring[i] - y); ring[i] = y; }
            float[] d = new float[Len(seconds)];
            for (int i = 0, p = 0; i < d.Length; i++)
            {
                int q = (p + 1) % n;
                float v = ring[p];
                d[i] = v;
                ring[p] = sustain * 0.5f * (v + ring[q]);
                p = q;
            }
            LowPass(d, 1800f + 4200f * brightness); // warm the string (no fizzy top end)
            int atk = Math.Min(d.Length, Len(0.004));
            for (int i = 0; i < atk; i++) d[i] *= (float)i / atk;
            int rel = Math.Min(d.Length, Len(0.03));
            for (int i = 0; i < rel; i++) d[d.Length - 1 - i] *= (float)i / rel;
            return d;
        }

        /// <summary>Sine voice with optional pitch glide, exponential decay and soft attack.</summary>
        private static float[] Tone(float f0, float f1, double seconds, float attack, float decay, float h2 = 0f, float h3 = 0f,
            float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            float[] d = new float[Len(seconds)];
            double ph = 0;
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SampleRate, u = (double)i / d.Length;
                double f = f0 * Math.Pow(f1 / f0, u);
                if (vibratoHz > 0f) f *= 1.0 + vibratoDepth * Math.Sin(TwoPi * vibratoHz * t);
                ph += TwoPi * f / SampleRate;
                double env = Math.Min(1.0, t / Math.Max(0.003, attack)) * Math.Exp(-t * decay);
                d[i] = (float)((Math.Sin(ph) + h2 * Math.Sin(2 * ph) + h3 * Math.Sin(3 * ph)) * env);
            }
            return d;
        }

        /// <summary>FM bell (inharmonic 1.4 ratio): crystal / chime colour.</summary>
        private static float[] Bell(float freq, double seconds, float decay, float index = 2.5f)
        {
            float[] d = new float[Len(seconds)];
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SampleRate;
                double mod = index * Math.Exp(-t * 7.0) * Math.Sin(TwoPi * freq * 1.4 * t);
                double env = Math.Min(1.0, t / 0.004) * Math.Exp(-t * decay);
                d[i] = (float)(Math.Sin(TwoPi * freq * t + mod) * env);
            }
            return d;
        }

        /// <summary>Hand drum: pitch-dropping body + low-passed noise skin.</summary>
        private static float[] Drum(float fStart, float fEnd, double seconds, float noiseAmount, float noiseCutoff, int seed)
        {
            float[] body = Tone(fStart, fEnd, seconds, 0.002f, (float)(5.0 / seconds));
            float[] skin = Noise(body.Length, seed);
            LowPass(skin, noiseCutoff);
            LowPass(skin, noiseCutoff);
            for (int i = 0; i < body.Length; i++)
            {
                double t = (double)i / SampleRate;
                body[i] += skin[i] * noiseAmount * (float)(Math.Exp(-t * 40.0) * Math.Min(1.0, t / 0.002)) * 3f;
            }
            return body;
        }

        /// <summary>Jing (Korean gong): inharmonic partials, slow bloom, long shimmer.</summary>
        private static float[] Gong(float f0, double seconds)
        {
            float[] ratios = { 1f, 1.47f, 2.09f, 2.56f, 3.14f, 4.2f };
            float[] amps = { 1f, 0.6f, 0.45f, 0.3f, 0.2f, 0.12f };
            float[] d = new float[Len(seconds)];
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SampleRate;
                double bend = 1.0 + 0.012 * (1.0 - Math.Exp(-t * 3.0)); // the jing's slight upward glide
                double s = 0;
                for (int k = 0; k < ratios.Length; k++)
                {
                    double f = f0 * ratios[k] * bend;
                    s += amps[k] * Math.Sin(TwoPi * f * t) * Math.Exp(-t * (1.2 + k * 0.6))
                         * (1.0 + 0.15 * Math.Sin(TwoPi * (0.7 + k * 0.3) * t));
                }
                d[i] = (float)(s * Math.Min(1.0, t / 0.015));
            }
            return d;
        }

        /// <summary>Soft open-fifth pad: two slightly detuned sines per note, slow attack and release.</summary>
        private static float[] Pad(float[] freqs, double seconds, float attack = 0.35f, float release = 0.45f)
        {
            float[] d = new float[Len(seconds)];
            for (int i = 0; i < d.Length; i++)
            {
                double t = (double)i / SampleRate;
                double env = Math.Min(1.0, t / attack) * Math.Min(1.0, (seconds - t) / release);
                double s = 0;
                foreach (float f in freqs)
                    s += Math.Sin(TwoPi * f * 0.9985 * t) + Math.Sin(TwoPi * f * 1.0015 * t + 1.0);
                d[i] = (float)(s * Math.Max(0.0, env) / (2 * freqs.Length));
            }
            LowPass(d, 2200f);
            return d;
        }

        // ------------------------------------------------------------------ music

        private struct Note
        {
            public int Degree;  // scale degree (−1 = rest)
            public int Eighths;
            public Note(int degree, int eighths) { Degree = degree; Eighths = eighths; }
        }

        private static readonly int[] MajorPenta = { 0, 2, 4, 7, 9 };
        private static readonly int[] MinorPenta = { 0, 3, 5, 7, 10 };

        private static float DegreeFreq(int degree, int[] scale, int baseMidi)
        {
            int octave = (int)Math.Floor(degree / (double)scale.Length);
            int step = degree - octave * scale.Length;
            return Midi(baseMidi + 12 * octave + scale[step]);
        }

        private static Note[] N(params int[] pairs)
        {
            Note[] notes = new Note[pairs.Length / 2];
            for (int i = 0; i < notes.Length; i++) notes[i] = new Note(pairs[2 * i], pairs[2 * i + 1]);
            return notes;
        }

        private static void Melody(float[] mix, Note[] phrase, int startEighth, double eighth, int[] scale, int baseMidi,
            float gain, float brightness, int seed, float fluteGain = 0f)
        {
            int pos = startEighth;
            foreach (Note n in phrase)
            {
                if (n.Degree >= 0)
                {
                    float f = DegreeFreq(n.Degree, scale, baseMidi);
                    double dur = n.Eighths * eighth + 0.6;
                    Mix(mix, Pluck(f, dur, brightness, 0.9985f, seed + pos), (int)(pos * eighth * SampleRate), gain, true);
                    if (fluteGain > 0f)
                    {
                        float[] flute = Tone(f * 2f, f * 2f, n.Eighths * eighth, 0.06f, 1.2f, 0.12f, 0f, 5f, 0.004f);
                        int rel = Math.Min(flute.Length, Len(0.08));
                        for (int i = 0; i < rel; i++) flute[flute.Length - 1 - i] *= (float)i / rel;
                        Mix(mix, flute, (int)(pos * eighth * SampleRate), fluteGain, true);
                    }
                }
                pos += n.Eighths;
            }
        }

        private static float[] MusicRun()
        {
            const double bpm = 90;
            double beat = 60.0 / bpm, eighth = beat / 2;
            const int bars = 16;
            float[] mix = new float[Len(bars * 4 * beat)];
            int[] roots = { 50, 47, 43, 45 }; // D3, B2, G2, A2 (two bars each)
            bool[] minor = { false, true, false, false };

            for (int bar = 0; bar < bars; bar++)
            {
                int chord = (bar / 2) % 4;
                int root = roots[chord];
                int third = root + (minor[chord] ? 3 : 4);
                double barStart = bar * 4 * beat;
                if (bar % 2 == 0)
                    Mix(mix, Pad(new[] { Midi(root + 12), Midi(third + 12), Midi(root + 19) }, 8 * beat + 0.4), (int)(barStart * SampleRate), 0.22f, true);
                // gayageum arpeggio on eighths
                int[] arp = { root + 12, root + 19, root + 24, root + 19 };
                for (int e = 0; e < 8; e++)
                    Mix(mix, Pluck(Midi(arp[e % 4]), 0.9, 0.45f, 0.997f, 1000 + bar * 8 + e),
                        (int)((barStart + e * eighth) * SampleRate), 0.16f, true);
                // bass on beats 1 and 3
                for (int bt = 0; bt < 4; bt += 2)
                    Mix(mix, Tone(Midi(root), Midi(root), 0.7, 0.012f, 3.2f, 0.35f), (int)((barStart + bt * beat) * SampleRate), 0.38f, true);
                // janggu: deong (1), kung (3), deok on the off-beats
                Mix(mix, Drum(150f, 85f, 0.35, 0.25f, 1200f, 7 + bar), (int)(barStart * SampleRate), 0.42f, true);
                Mix(mix, Drum(115f, 70f, 0.3, 0.2f, 1000f, 50 + bar), (int)((barStart + 2 * beat) * SampleRate), 0.3f, true);
                foreach (int off in new[] { 3, 7 })
                    Mix(mix, Drum(420f, 380f, 0.08, 0.6f, 3500f, 90 + bar + off), (int)((barStart + off * eighth) * SampleRate), 0.12f, true);
            }

            Note[] a = N(5, 2, 4, 1, 3, 1, 4, 2, 2, 2, 3, 3, 2, 1, 1, 2, 0, 2, 1, 2, 2, 1, 3, 1, 4, 2, 5, 2, 4, 6, -1, 2);
            Note[] b = N(6, 2, 5, 2, 4, 1, 5, 1, 6, 2, 7, 3, 6, 1, 5, 4, 4, 2, 3, 2, 2, 1, 3, 1, 4, 2, 2, 2, 1, 2, 0, 4);
            Melody(mix, a, 0, eighth, MajorPenta, 62, 0.42f, 0.7f, 11);
            Melody(mix, b, 32, eighth, MajorPenta, 62, 0.42f, 0.7f, 22);
            Melody(mix, a, 64, eighth, MajorPenta, 62, 0.42f, 0.75f, 33, 0.07f);
            Melody(mix, b, 96, eighth, MajorPenta, 62, 0.42f, 0.75f, 44, 0.07f);
            SoftSaturate(mix, 1.1f);
            return mix;
        }

        private static float[] MusicBoss()
        {
            const double bpm = 128;
            double beat = 60.0 / bpm, eighth = beat / 2;
            const int bars = 16;
            float[] mix = new float[Len(bars * 4 * beat)];
            int[] roots = { 50, 46, 48, 45 }; // D, Bb, C, A

            for (int bar = 0; bar < bars; bar++)
            {
                int root = roots[(bar / 2) % 4];
                double barStart = bar * 4 * beat;
                if (bar % 2 == 0)
                    Mix(mix, Pad(new[] { Midi(root + 12), Midi(root + 15), Midi(root + 19) }, 8 * beat + 0.3, 0.2f, 0.3f), (int)(barStart * SampleRate), 0.2f, true);
                for (int e = 0; e < 8; e++)
                {
                    int note = e % 2 == 0 ? root : root + 7;
                    Mix(mix, Pluck(Midi(note), 0.35, 0.35f, 0.994f, 3000 + bar * 8 + e), (int)((barStart + e * eighth) * SampleRate), 0.26f, true);
                }
                for (int bt = 0; bt < 4; bt++)
                    Mix(mix, Drum(105f, 52f, 0.4, 0.3f, 900f, 200 + bar * 4 + bt), (int)((barStart + bt * beat) * SampleRate), bt == 0 ? 0.55f : 0.38f, true);
                Mix(mix, Drum(105f, 52f, 0.3, 0.3f, 900f, 400 + bar), (int)((barStart + 7 * eighth) * SampleRate), 0.3f, true);
                Mix(mix, Drum(480f, 420f, 0.07, 0.7f, 4000f, 500 + bar), (int)((barStart + 3 * eighth) * SampleRate), 0.12f, true);
                if (bar % 8 == 0) Mix(mix, Gong(98f, 3.0), (int)(barStart * SampleRate), 0.35f, true);
            }

            Note[] a = N(5, 4, 4, 2, 3, 2, 2, 6, 3, 2, 4, 4, 5, 2, 7, 2, 6, 8);
            Note[] b = N(7, 4, 6, 2, 5, 2, 4, 3, 3, 1, 2, 4, 3, 2, 4, 2, 2, 4, 1, 4, 0, 4);
            Melody(mix, a, 0, eighth, MinorPenta, 62, 0.4f, 0.8f, 61);
            Melody(mix, b, 32, eighth, MinorPenta, 62, 0.4f, 0.8f, 62);
            Melody(mix, a, 64, eighth, MinorPenta, 74, 0.32f, 0.85f, 63, 0.05f);
            Melody(mix, b, 96, eighth, MinorPenta, 74, 0.32f, 0.85f, 64, 0.05f);
            SoftSaturate(mix, 1.2f);
            return mix;
        }

        private static float[] MusicCalm()
        {
            const double bpm = 72;
            double beat = 60.0 / bpm, eighth = beat / 2;
            const int bars = 8;
            float[] mix = new float[Len(bars * 4 * beat)];
            int[] roots = { 50, 43, 47, 45 };
            for (int bar = 0; bar < bars; bar++)
            {
                int root = roots[(bar / 2) % 4];
                double barStart = bar * 4 * beat;
                if (bar % 2 == 0)
                    Mix(mix, Pad(new[] { Midi(root + 12), Midi(root + 19), Midi(root + 24) }, 8 * beat + 0.8, 0.8f, 0.9f), (int)(barStart * SampleRate), 0.3f, true);
                for (int e = 0; e < 8; e += 2)
                    Mix(mix, Pluck(Midi(root + 12 + MajorPenta[(bar + e) % 5]), 1.6, 0.35f, 0.998f, 700 + bar * 8 + e), (int)((barStart + e * eighth) * SampleRate), 0.14f, true);
                if (bar % 2 == 1) Mix(mix, Bell(Midi(86 + MajorPenta[bar % 5]), 1.5, 2.5f, 1.5f), (int)((barStart + 2 * beat) * SampleRate), 0.08f, true);
            }
            Note[] a = N(5, 4, 4, 2, 3, 2, 2, 8, 3, 4, 4, 4, 5, 8, -1, 8, 4, 4, 3, 4, 2, 4, 1, 4, 0, 8, -1, 8);
            Melody(mix, a, 0, eighth, MajorPenta, 62, 0.34f, 0.55f, 81);
            SoftSaturate(mix, 1.05f);
            return mix;
        }

        // ------------------------------------------------------------------ sfx

        private static float[] Shoot()
        {
            float[] d = Tone(1400f, 650f, 0.13, 0.002f, 28f, 0.25f);
            Mix(d, Bell(2600f, 0.12, 30f, 1.2f), 0, 0.18f);
            LowPass(d, 5200f);
            return d;
        }

        private static float[] Hit()
        {
            float[] d = Tone(230f, 85f, 0.12, 0.002f, 30f, 0.2f);
            float[] n = Noise(Len(0.05), 3);
            LowPass(n, 900f);
            LowPass(n, 900f);
            for (int i = 0; i < n.Length; i++) n[i] *= (float)Math.Exp(-i / (double)SampleRate * 80.0) * 3f;
            Mix(d, n, 0, 0.5f);
            return d;
        }

        private static float[] EnemyDie()
        {
            float[] d = Tone(620f, 170f, 0.2, 0.002f, 16f, 0.3f);
            Mix(d, Bell(1320f, 0.2, 18f, 1.5f), Len(0.02), 0.25f);
            LowPass(d, 4500f);
            return d;
        }

        private static float[] Hurt()
        {
            float[] d = Tone(190f, 105f, 0.28, 0.006f, 9f, 0.45f, 0.15f);
            float[] b = Tone(197f, 110f, 0.28, 0.006f, 9f);
            Mix(d, b, 0, 0.6f);
            float[] n = Noise(d.Length, 5);
            LowPass(n, 700f);
            LowPass(n, 700f);
            for (int i = 0; i < n.Length; i++) n[i] *= (float)Math.Exp(-i / (double)SampleRate * 14.0) * 2f;
            Mix(d, n, 0, 0.35f);
            return d;
        }

        private static float[] Bite()
        {
            float[] d = new float[Len(0.2)];
            foreach (double at in new[] { 0.0, 0.075 })
            {
                float[] snap = Noise(Len(0.05), 9 + (int)(at * 1000));
                LowPass(snap, 1500f);
                LowPass(snap, 1500f);
                for (int i = 0; i < snap.Length; i++) snap[i] *= (float)(Math.Exp(-i / (double)SampleRate * 90.0) * Math.Min(1.0, i / (SampleRate * 0.002))) * 2.5f;
                Mix(d, snap, Len(at), 0.6f);
                Mix(d, Tone(330f, 240f, 0.06, 0.001f, 60f), Len(at), 0.5f);
            }
            return d;
        }

        private static float[] Ui(float gain)
        {
            float[] d = Tone(880f, 860f, 0.07, 0.001f, 55f, 0.35f);
            for (int i = 0; i < d.Length; i++) d[i] *= gain;
            return d;
        }

        private static float[] Gem()
        {
            float[] d = Bell(1318.5f, 0.3, 10f, 1.1f);           // E6 crystal ting
            Mix(d, Bell(1975.5f, 0.22, 14f, 0.6f), Len(0.015), 0.3f);
            LowPass(d, 5000f);
            return d;
        }

        private static float[] Arpeggio(int[] midis, double spacing, double tail, float brightness, int seed)
        {
            float[] d = new float[Len(spacing * midis.Length + tail)];
            for (int i = 0; i < midis.Length; i++)
                Mix(d, Pluck(Midi(midis[i]), tail + spacing * (midis.Length - i), brightness, 0.9988f, seed + i), Len(i * spacing), 0.7f);
            return d;
        }

        private static float[] LevelUp()
        {
            float[] d = Arpeggio(new[] { 74, 78, 81, 86, 90 }, 0.06, 0.9, 0.8f, 300);
            Mix(d, Pad(new[] { Midi(62), Midi(69), Midi(74) }, 1.1, 0.15f, 0.5f), 0, 0.5f);
            Mix(d, Bell(Midi(93), 0.8, 5f, 0.8f), Len(0.3), 0.22f);
            LowPass(d, 7000f);
            return d;
        }

        private static float[] Rescue()
        {
            float[] d = Arpeggio(new[] { 62, 64, 66, 69, 71, 74, 76, 78 }, 0.07, 0.9, 0.7f, 400);
            Mix(d, Bell(Midi(93), 0.9, 4f, 1.6f), Len(0.5), 0.25f);
            Mix(d, Bell(Midi(98), 0.9, 4f, 1.6f), Len(0.58), 0.18f);
            return d;
        }

        private static float[] Fusion()
        {
            float[] d = new float[Len(1.3)];
            Mix(d, Tone(Midi(69), Midi(74), 0.6, 0.05f, 2f, 0.2f, 0f, 6f, 0.01f), 0, 0.4f);
            Mix(d, Tone(Midi(81), Midi(74), 0.6, 0.05f, 2f, 0.2f, 0f, 6f, 0.01f), 0, 0.4f);
            float[] whoosh = Noise(Len(0.6), 13);
            LowPass(whoosh, 1500f);
            for (int i = 0; i < whoosh.Length; i++) whoosh[i] *= (float)Math.Sin(Math.PI * i / whoosh.Length) * 1.5f;
            Mix(d, whoosh, 0, 0.3f);
            Mix(d, Pad(new[] { Midi(62), Midi(69), Midi(74), Midi(78) }, 0.8, 0.02f, 0.6f), Len(0.55), 0.6f);
            Mix(d, Bell(Midi(86), 0.7, 5f), Len(0.55), 0.3f);
            return d;
        }

        private static float[] Transform()
        {
            float[] d = Tone(220f, 880f, 0.75, 0.05f, 1.5f, 0.3f, 0.1f, 7f, 0.01f);
            Mix(d, Gong(147f, 0.9), Len(0.6), 0.25f);
            LowPass(d, 6000f);
            return d;
        }

        private static float[] SpiritCast()
        {
            float[] n = Noise(Len(0.35), 17);
            float a = 0f, y = 0f;
            for (int i = 0; i < n.Length; i++)
            {
                double u = (double)i / n.Length;
                a = (float)(1.0 - Math.Exp(-TwoPi * (300.0 + 1300.0 * Math.Sin(Math.PI * u)) / SampleRate));
                y += a * (n[i] - y);
                n[i] = y * (float)Math.Sin(Math.PI * u) * 2f;
            }
            Mix(n, Bell(1320f, 0.3, 9f, 1.0f), Len(0.05), 0.4f);
            LowPass(n, 4500f);
            LowPass(n, 3500f);
            return n;
        }

        private static float[] StationTile()
        {
            float[] d = Bell(Midi(86), 0.7, 5f, 1.0f);
            Mix(d, Bell(Midi(93), 0.6, 6f, 0.8f), Len(0.09), 0.7f);
            LowPass(d, 7000f);
            return d;
        }

        private static float[] BossWarn() { return Gong(98f, 2.6); }

        private static float[] BossPhase()
        {
            float[] d = new float[Len(1.8)];
            Mix(d, Drum(90f, 45f, 0.6, 0.35f, 800f, 21), 0, 0.9f);
            Mix(d, Gong(110f, 1.7), Len(0.05), 0.6f);
            Mix(d, Tone(110f, 440f, 1.2, 0.2f, 1.5f, 0.2f, 0f, 6f, 0.01f), Len(0.3), 0.3f);
            return d;
        }

        private static float[] AnchorBreak()
        {
            float[] d = Noise(Len(0.7), 23);
            LowPass(d, 2500f);
            LowPass(d, 3000f);
            for (int i = 0; i < d.Length; i++) d[i] *= (float)(Math.Exp(-i / (double)SampleRate * 9.0) * Math.Min(1.0, i / (SampleRate * 0.003))) * 0.6f;
            Random rng = new Random(29);
            for (int k = 0; k < 7; k++)
                Mix(d, Bell(1400f + (float)rng.NextDouble() * 1800f, 0.5, 8f + k, 0.6f), Len(k * 0.025), 0.35f);
            LowPass(d, 7000f);
            return d;
        }

        private static float[] Victory()
        {
            float[] d = Arpeggio(new[] { 74, 78, 81, 86 }, 0.13, 1.1, 0.75f, 600);
            Mix(d, Pad(new[] { Midi(62), Midi(66), Midi(69), Midi(74) }, 1.6, 0.3f, 0.6f), Len(0.4), 0.55f);
            Mix(d, Gong(147f, 1.4), Len(0.4), 0.2f);
            return d;
        }

        private static float[] Defeat()
        {
            float[] d = Arpeggio(new[] { 74, 70, 67, 62 }, 0.28, 1.2, 0.4f, 700);
            Mix(d, Pad(new[] { Midi(50), Midi(53), Midi(57) }, 1.9, 0.5f, 0.8f), 0, 0.45f);
            return d;
        }
    }
}
