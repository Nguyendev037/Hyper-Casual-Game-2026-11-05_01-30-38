using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Spark particles (pooled ParticleSystems, emitted by script) and the capped hit-stop.</summary>
    public sealed partial class VfxHub
    {
        public const int SparkSystems = 6;
        public const int SparksPerSystem = 50;
        public const float HitStopSeconds = 0.035f;
        public const float HitStopMinGap = 0.25f;
        public const float HitStopDamage = 25f;
        private const float HitStopScale = 0.05f;

        private ParticleSystem[] sparks;
        private int sparkCursor;
        private float hitStopUntil;
        private float hitStopLast = -10f;
        private float hitStopBase = 1f;
        private bool hitStopActive;

        /// <summary>Total particle capacity (budget 300, SPEC-P03 §2).</summary>
        public static int ParticleBudget => SparkSystems * SparksPerSystem;

        private void BuildSparks()
        {
            sparks = new ParticleSystem[SparkSystems];
            Texture texture = VfxAssets.Texture("vfx_spark");
            for (int i = 0; i < sparks.Length; i++)
            {
                GameObject go = new GameObject("Sparks" + i);
                go.transform.SetParent(transform, false);
                ParticleSystem ps = go.AddComponent<ParticleSystem>();
                ConfigureSparks(ps);
                ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
                Material material = new Material(VfxAssets.Additive());
                material.hideFlags = HideFlags.DontSave;
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }

                psr.sharedMaterial = material;
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                psr.receiveShadows = false;
                ps.Play();
                sparks[i] = ps;
                VfxPool.NoteCreated();
            }
        }

        private static void ConfigureSparks(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = SparksPerSystem;
            main.startLifetime = 0.4f;
            main.startSpeed = 0f;
            main.startSize = 0.18f;
            main.gravityModifier = 0.6f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }

        /// <summary>Emit a burst of sparks flying outward. Cheap: round-robin over the pooled systems.</summary>
        public void EmitSparks(Vector3 position, Color color, int count, float speed)
        {
            if (sparks == null)
            {
                return;
            }

            ParticleSystem ps = sparks[sparkCursor];
            sparkCursor = (sparkCursor + 1) % sparks.Length;
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            emit.position = position;
            emit.startColor = color;
            emit.startLifetime = Random.Range(0.25f, 0.45f);
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                emit.velocity = new Vector3(dir.x, Random.Range(0.4f, 1.1f), dir.y) * speed * Random.Range(0.5f, 1f);
                emit.startSize = Random.Range(0.12f, 0.22f);
                ps.Emit(emit, 1);
            }
        }

        /// <summary>One slow falling particle (merged-spirit shots drop glitter).</summary>
        public static void Drip(Vector3 position, Color color)
        {
            if (Instance == null || Instance.sparks == null)
            {
                return;
            }

            ParticleSystem ps = Instance.sparks[Instance.sparkCursor];
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            emit.position = position;
            emit.startColor = color;
            emit.startLifetime = 0.5f;
            emit.startSize = 0.14f;
            emit.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, Random.Range(-0.3f, 0.3f));
            ps.Emit(emit, 1);
        }

        /// <summary>Hit-stop: only for hits of 25+ damage or a crit, at most once per 0.25 s, and only at normal speed.</summary>
        public static void HitStop(float damage, bool crit)
        {
            if (Instance == null || (!crit && damage < HitStopDamage))
            {
                return;
            }

            Instance.BeginHitStop();
        }

        private void BeginHitStop()
        {
            float now = Time.unscaledTime;
            float scale = Time.timeScale;
            if (hitStopActive || now - hitStopLast < HitStopMinGap || scale < 0.5f || scale > 1.5f)
            {
                return;
            }

            hitStopBase = scale;
            hitStopLast = now;
            hitStopUntil = now + HitStopSeconds;
            hitStopActive = true;
            Time.timeScale = HitStopScale;
        }

        private void TickHitStop()
        {
            if (hitStopActive && Time.unscaledTime >= hitStopUntil)
            {
                RestoreHitStop();
            }
        }

        // Only restore when nobody else (pause, autopilot) has changed the time scale meanwhile.
        private void RestoreHitStop()
        {
            if (!hitStopActive)
            {
                return;
            }

            hitStopActive = false;
            if (Mathf.Approximately(Time.timeScale, HitStopScale))
            {
                Time.timeScale = hitStopBase;
            }
        }

        /// <summary>True while a hit-stop freeze is running (tests).</summary>
        public bool HitStopRunning => hitStopActive;
    }
}
