using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    /// <summary>
    /// Title screen motion (SPEC-P01 section 1): key art breathes and floats, the logo bounces in, petals and
    /// sparkles drift (reused UI images, never more than <see cref="MaxParticles"/>). Pure maths is static so
    /// tests can sample it without a frame loop.
    /// </summary>
    public sealed class TitleFx : MonoBehaviour
    {
        public const int MaxParticles = 40;
        public const int DefaultParticles = 24;
        public const float BreathPeriod = 3f, BreathAmp = 0.015f;
        public const float BobPeriod = 3.4f, BobPixels = 8f;
        public const float LogoSeconds = 0.4f;
        public const float PressScale = 0.95f;

        struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public float X, Y, Speed, Sway, SwayRate, Phase, Spin, Size, Life, Age;
            public bool Twinkle;
        }

        RectTransform keyart, logo, layer;
        Vector2 keyBase;
        Vector3 keyScale = Vector3.one, logoScale = Vector3.one;
        float started;
        Particle[] particles = new Particle[0];

        /// <summary>Scale Y multiplier of the key art at time t (seconds): 1 +- 1.5% over 3 s.</summary>
        public static float Breathe(float t) => 1f + BreathAmp * Mathf.Sin(2f * Mathf.PI * t / BreathPeriod);

        /// <summary>Vertical float offset in pixels at time t: +-8 px.</summary>
        public static float Bob(float t) => BobPixels * Mathf.Sin(2f * Mathf.PI * t / BobPeriod + 1.1f);

        /// <summary>Logo scale 0..1+ over 0.4 s, ease-out-back (one small overshoot), exactly 1 afterwards.</summary>
        public static float LogoBounce(float t)
        {
            if (t >= LogoSeconds) return 1f;
            if (t <= 0f) return 0.01f;
            float u = t / LogoSeconds - 1f;
            const float c = 1.9f;
            return Mathf.Max(0.01f, 1f + (c + 1f) * u * u * u + c * u * u);
        }

        /// <summary>Particle budget from the json fx block ("count" / "density" / "max"), clamped 0..40.</summary>
        public static int ParticleCount(IDictionary<string, object> fx)
        {
            if (fx == null) return DefaultParticles;
            foreach (string key in new[] { "count", "density", "max" })
                if (fx.TryGetValue(key, out object value) && value is double number)
                    return Mathf.Clamp(Mathf.RoundToInt((float)number), 0, MaxParticles);
            return DefaultParticles;
        }

        /// <summary>Sprite ids from the fx block ("sprites": [..] or "sprite"/"petal"/"sparkle": ".."). May be empty.</summary>
        public static List<string> SpriteIds(IDictionary<string, object> fx)
        {
            var ids = new List<string>();
            if (fx == null) return ids;
            if (fx.TryGetValue("sprites", out object list) && list is List<object> items)
                foreach (object item in items)
                    if (item is string text && text.Length > 0) ids.Add(text);
            foreach (string key in new[] { "sprite", "petal", "sparkle" })
                if (fx.TryGetValue(key, out object one) && one is string id && id.Length > 0 && !ids.Contains(id)) ids.Add(id);
            return ids;
        }

        public static TitleFx Attach(RectTransform canvasRoot, RectTransform keyArt, RectTransform logoRect,
            IDictionary<string, object> fx, Sprite[] sprites, int seed)
        {
            var host = new GameObject("TitleFx", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            var fxRoot = (RectTransform)host.transform;
            fxRoot.anchorMin = Vector2.zero;
            fxRoot.anchorMax = Vector2.one;
            fxRoot.offsetMin = fxRoot.offsetMax = Vector2.zero;
            TitleFx title = host.AddComponent<TitleFx>();
            title.keyart = keyArt;
            title.logo = logoRect;
            if (keyArt != null) { title.keyBase = keyArt.anchoredPosition; title.keyScale = keyArt.localScale; }
            if (logoRect != null) { title.logoScale = logoRect.localScale; logoRect.localScale = title.logoScale * 0.01f; }
            title.layer = fxRoot;
            // Particles sit just above the key art and below the buttons so they never cover a control.
            if (keyArt != null && keyArt.parent == canvasRoot) fxRoot.SetSiblingIndex(keyArt.GetSiblingIndex() + 1);
            title.BuildParticles(ParticleCount(fx), sprites, seed, canvasRoot.rect);
            title.started = Time.unscaledTime;
            return title;
        }

        void BuildParticles(int count, Sprite[] sprites, int seed, Rect area)
        {
            if (sprites == null || sprites.Length == 0) sprites = new[] { SoftDot() };
            var random = new System.Random(seed);
            particles = new Particle[count];
            float width = Mathf.Max(100f, area.width), height = Mathf.Max(100f, area.height);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("P" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(layer, false);
                Image image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.sprite = sprites[i % sprites.Length];
                image.preserveAspect = true;
                bool twinkle = i % 2 == 1;
                float size = twinkle ? 26f + (float)random.NextDouble() * 22f : 30f + (float)random.NextDouble() * 26f;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                image.color = twinkle ? new Color(1f, 0.93f, 0.62f, 0f) : new Color(1f, 0.78f, 0.86f, 0f);
                particles[i] = new Particle
                {
                    Rect = rect, Image = image, Twinkle = twinkle, Size = size,
                    X = (float)random.NextDouble() * width, Y = -(float)random.NextDouble() * height,
                    Speed = 55f + (float)random.NextDouble() * 85f, Sway = 20f + (float)random.NextDouble() * 40f,
                    SwayRate = 0.6f + (float)random.NextDouble() * 1.2f, Phase = (float)random.NextDouble() * 6.283f,
                    Spin = (float)random.NextDouble() * 80f - 40f, Life = 1.6f + (float)random.NextDouble() * 1.6f,
                    Age = (float)random.NextDouble() * 2f
                };
            }
        }

        void Update()
        {
            float t = Time.unscaledTime - started;
            if (keyart != null)
            {
                keyart.localScale = new Vector3(keyScale.x, keyScale.y * Breathe(t), keyScale.z);
                keyart.anchoredPosition = keyBase + new Vector2(0f, Bob(t));
            }
            if (logo != null) logo.localScale = logoScale * LogoBounce(t);
            if (layer == null) return;
            float width = Mathf.Max(100f, layer.rect.width), height = Mathf.Max(100f, layer.rect.height);
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                Particle p = particles[i];
                p.Age += dt;
                float alpha;
                if (p.Twinkle)
                {
                    // Sparkles pulse in place and re-seed their spot when the life ends.
                    float u = p.Age / p.Life;
                    if (u >= 1f)
                    {
                        p.Age = 0f;
                        p.X = Mathf.Repeat(p.X * 1.7f + 211f, width);
                        p.Y = -Mathf.Repeat(-p.Y * 1.3f + 97f, height * 0.7f);
                        u = 0f;
                    }
                    alpha = Mathf.Sin(u * Mathf.PI);
                    p.Rect.localScale = Vector3.one * (0.6f + 0.6f * alpha);
                }
                else
                {
                    p.Y -= p.Speed * dt;
                    if (p.Y < -height - p.Size) { p.Y = p.Size; p.X = Mathf.Repeat(p.X + 313f, width); }
                    alpha = Mathf.Clamp01(Mathf.Min((-p.Y) / 80f, (height + p.Y) / 120f + 0.2f)) * 0.9f;
                }
                float sway = p.Twinkle ? 0f : Mathf.Sin(t * p.SwayRate + p.Phase) * p.Sway;
                p.Rect.anchoredPosition = new Vector2(p.X + sway, p.Y);
                p.Rect.localRotation = Quaternion.Euler(0f, 0f, p.Spin * t);
                Color c = p.Image.color;
                c.a = alpha;
                p.Image.color = c;
                particles[i] = p;
            }
        }

        static Sprite softDot;

        /// <summary>Code-made soft round dot used when the fx sprites are not installed yet (C3 fallback).</summary>
        static Sprite SoftDot()
        {
            if (softDot != null) return softDot;
            const int n = 32;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d * 0.3f)));
                }
            texture.Apply(false, true);
            softDot = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return softDot;
        }

        /// <summary>Adds the 0.95 press shrink to a title button.</summary>
        public static void AddPress(GameObject button) => button.AddComponent<TitlePress>();

        sealed class TitlePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            RectTransform rect;
            Vector3 normal;
            bool down;

            void Awake() { rect = (RectTransform)transform; normal = rect.localScale; }
            public void OnPointerDown(PointerEventData eventData) { down = true; }
            public void OnPointerUp(PointerEventData eventData) { down = false; }
            public void OnPointerExit(PointerEventData eventData) { down = false; }

            void Update()
            {
                Vector3 goal = down ? normal * PressScale : normal;
                rect.localScale = Vector3.Lerp(rect.localScale, goal, Mathf.Clamp01(Time.unscaledDeltaTime * 22f));
            }

            void OnDisable() { if (rect != null) rect.localScale = normal; }
        }
    }
}
