using RHMG.Core;
using UnityEngine;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Sand storm of phase 3 (SPEC-P03 §2): safe circle shrinks R14 -> R8 over 20 s; outside it the girl loses
    /// 8% of her max HP per second. The edge is shown 2 s before it starts to hurt.</summary>
    public sealed class StormZone
    {
        private readonly CombatWorld world;
        private GameObject ring;
        private bool active;
        private float warnLeft;
        private float shrink;

        public bool Active => active;
        public bool Hurting => active && warnLeft <= 0f;
        public float Radius => Mathf.Lerp(SurvivorTuning.StormStartRadius, SurvivorTuning.StormEndRadius,
            Mathf.Clamp01(shrink / SurvivorTuning.StormShrinkSeconds));

        public StormZone(CombatWorld owner)
        {
            world = owner;
        }

        public void Begin()
        {
            EnsureRing();
            active = true;
            warnLeft = SurvivorTuning.StormWarnSeconds;
            shrink = 0f;
            ring.SetActive(true);
            Apply(1f);
        }

        public void End()
        {
            active = false;
            if (ring != null)
            {
                ring.SetActive(false);
            }
        }

        public void Step(float deltaTime)
        {
            if (!active)
            {
                return;
            }

            float pulse = 1f;
            if (warnLeft > 0f)
            {
                warnLeft -= deltaTime;
                pulse = Mathf.Sin(world.SimTime * 18f) > 0f ? 1f : 0.35f;
            }
            else
            {
                shrink += deltaTime;
                Vector3 offset = CombatMath.Flat(world.Player.transform.position);
                if (offset.magnitude > Radius)
                {
                    world.Player.TakeContinuousDamage(world.Player.Health.Max *
                        SurvivorTuning.StormDamageFractionPerSecond * deltaTime);
                }
            }

            Apply(pulse);
        }

        public void Dispose()
        {
            End();
        }

        private void Apply(float pulse)
        {
            float radius = Radius;
            ring.transform.position = new Vector3(0f, 0.06f, 0f);
            ring.transform.localScale = new Vector3(radius, 1f, radius);
            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = pulse > 0.5f;
            }
        }

        private void EnsureRing()
        {
            if (ring != null)
            {
                return;
            }

            ring = VfxFactory.UnitRing(Palette.EnemyPurple, 0.04f, false);
            ring.name = "StormEdge";
            ring.transform.SetParent(world.transform, false);
            ring.SetActive(false);
        }
    }
}
