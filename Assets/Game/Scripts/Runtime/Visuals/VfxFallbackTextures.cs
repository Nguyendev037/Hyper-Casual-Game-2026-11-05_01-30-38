using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Code-made soft textures used when the Gemini art for an id is not installed yet (plan05 C3).</summary>
    public static class VfxFallbackTextures
    {
        private delegate float PixelAlpha(float u, float v);

        public static Texture2D Make(string id)
        {
            if (id.StartsWith("vfx_impact_"))
            {
                int frame = id.Length > 11 ? id[11] - '0' : 0;
                return Impact(Mathf.Clamp(frame, 0, 5));
            }

            if (id == "vfx_trail")
            {
                return Build(id, 64, 16, TrailPixel);
            }

            if (id == "vfx_ring")
            {
                return Build(id, 128, 128, (u, v) => Ring(u, v, 0.86f, 0.1f));
            }

            if (id == "vfx_crit")
            {
                return Build(id, 96, 96, StarPixel);
            }

            if (id == "vfx_spark")
            {
                return Build(id, 16, 16, (u, v) => Soft(u, v, 1.6f));
            }

            if (id == "vfx_muzzle")
            {
                return Build(id, 64, 64, (u, v) => Soft(u, v, 1.3f));
            }

            bool spiky = id == "vfx_core_boss";
            return Build(id, 64, 64, (u, v) => Core(u, v, spiky));
        }

        private static Texture2D Build(string name, int w, int h, PixelAlpha alpha)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "fallback_" + name };
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h));
                    byte b = (byte)Mathf.RoundToInt(a * 255f);
                    pixels[y * w + x] = new Color32(255, 255, 255, b);
                }
            }

            t.SetPixels32(pixels);
            t.Apply(false, true);
            return t;
        }

        private static float Dist(float u, float v)
        {
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        }

        private static float Soft(float u, float v, float power)
        {
            float d = Mathf.Clamp01(1f - Dist(u, v));
            return Mathf.Pow(d, power);
        }

        // Bright centre with a softer halo; tinted by the style colour at draw time.
        private static float Core(float u, float v, bool spiky)
        {
            float d = Dist(u, v);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 1.4f);
            if (spiky)
            {
                float angle = Mathf.Atan2(v - 0.5f, u - 0.5f);
                halo *= 0.75f + 0.25f * Mathf.Cos(angle * 6f);
            }

            return d < 0.55f ? Mathf.Max(halo, 0.95f) : halo;
        }

        private static float Ring(float u, float v, float radius, float width)
        {
            float k = 1f - Mathf.Abs(Dist(u, v) - radius) / width;
            return Mathf.Clamp01(k);
        }

        private static float StarPixel(float u, float v)
        {
            float dx = Mathf.Abs(u - 0.5f) * 2f;
            float dy = Mathf.Abs(v - 0.5f) * 2f;
            float arm = Mathf.Clamp01(1f - Mathf.Min(dx, dy) * 6f) * Mathf.Clamp01(1f - Mathf.Max(dx, dy));
            return Mathf.Max(arm, Soft(u, v, 2.2f) * 0.8f);
        }

        // 64 x 16: soft toward both edges of V; the along-trail fade comes from the trail colour gradient.
        private static float TrailPixel(float u, float v)
        {
            float edge = 1f - Mathf.Abs(v - 0.5f) * 2f;
            return Mathf.Clamp01(edge);
        }

        private static Texture2D Impact(int frame)
        {
            float t = frame / 5f;
            float radius = Mathf.Lerp(0.3f, 0.85f, t);
            float fade = 1f - t * 0.85f;
            return Build("vfx_impact_" + frame, 64, 64, (u, v) =>
                fade * Mathf.Max(Ring(u, v, radius, 0.22f), Soft(u, v, 2f) * (1f - t)));
        }
    }
}
