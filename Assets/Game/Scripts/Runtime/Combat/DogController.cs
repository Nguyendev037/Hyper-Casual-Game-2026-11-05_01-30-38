using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed class DogController : MonoBehaviour
    {
        public PlayerController Owner { get; private set; }
        public DogRig Rig { get; private set; }
        public DogTier Tier { get; private set; }

        private CombatWorld world;
        private DayNightClock clock;
        private bool mounted;
        private DayPhase phase;
        private float biteTimer;
        private float signatureTimer = CombatTuning.LionSignatureInterval;
        private float biteFacingHold;

        public void Init(CombatWorld combatWorld, PlayerController owner, DayNightClock dayNightClock)
        {
            world = combatWorld;
            Owner = owner;
            clock = dayNightClock;
            Rig = CharacterFactory.CreateDog(transform);
            if (clock != null)
            {
                clock.PhaseChanged += SetForm;
            }
        }

        public void SetTier(DogTier tier)
        {
            Tier = tier;
            Rig.SetTier(tier);
        }

        public void SetMounted(bool value)
        {
            mounted = value;
            Owner.Rig.SetMounted(value ? Rig.Seat : null);
            if (!value)
            {
                Owner.Rig.transform.SetParent(Owner.transform, false);
                Owner.Rig.transform.localPosition = Vector3.zero;
                Owner.Rig.transform.localRotation = Quaternion.identity;
                Owner.Rig.transform.localScale = Vector3.one;
            }
        }

        public void SetForm(DayPhase value)
        {
            phase = value;
            Rig.SetLionForm(value);
        }

        public void Step(float deltaTime)
        {
            if (mounted && Tier == DogTier.SunMoonLion)
            {
                StepLion(deltaTime);
                return;
            }

            FollowOwner(deltaTime);
            biteTimer -= deltaTime;
            if (biteTimer <= 0f)
            {
                EnemyController enemy = FindBiteTarget();
                if (enemy != null)
                {
                    enemy.TakeDamage(Owner.Stats.DogDamage, transform.position);
                    Rig.SetFacing(enemy.transform.position - transform.position);
                    biteFacingHold = 0.25f; // keep facing the bitten enemy for the lunge
                    Rig.PlayBite();
                    CombatFeedback.Play("bite");
                    biteTimer = Owner.Stats.DogInterval;
                }
            }
        }

        private EnemyController FindBiteTarget()
        {
            EnemyController nearest = null;
            float nearestDistance = CombatTuning.DogTargetRadius * CombatTuning.DogTargetRadius;
            foreach (EnemyController enemy in world.Enemies)
            {
                if (enemy == null || !enemy.Alive ||
                    !CombatMath.Overlap(enemy.transform.position, 0f, Owner.transform.position,
                        CombatTuning.DogTargetRadius) ||
                    !CombatMath.Overlap(enemy.transform.position, enemy.Radius, transform.position,
                        CombatTuning.DogBiteRadius))
                {
                    continue;
                }

                float distance = CombatMath.Flat(enemy.transform.position - Owner.transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void OnDestroy()
        {
            if (clock != null)
            {
                clock.PhaseChanged -= SetForm;
            }
        }

        private void FollowOwner(float deltaTime)
        {
            Vector3 ownerForward = Owner.Rig.FacingForward;
            Vector3 behind = Owner.transform.position - ownerForward * CombatTuning.DogFollowOffset;
            behind = CombatMath.ClampArena(behind);
            Vector3 before = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, behind,
                CombatTuning.DogFollowSpeed * deltaTime);
            float distance = (transform.position - Owner.transform.position).sqrMagnitude;
            if (distance > CombatTuning.DogReturnDistance * CombatTuning.DogReturnDistance)
            {
                transform.position = CombatMath.ClampArena(Owner.transform.position - ownerForward);
                world.PlayBurst(Palette.Sand, CombatTuning.DogReturnBurstRadius, transform.position);
            }

            // Face where it runs; when it has caught up, look the same way as the girl.
            Vector3 moved = transform.position - before;
            biteFacingHold -= deltaTime;
            if (biteFacingHold <= 0f)
            {
                Rig.SetFacing(Facing.HasDirection(moved) && moved.sqrMagnitude > 0.0004f ? moved : ownerForward);
            }
            Rig.SetMoving((behind - transform.position).magnitude);
        }

        private void StepLion(float deltaTime)
        {
            transform.position = Owner.transform.position;
            Rig.SetFacing(Owner.Rig.FacingForward);
            signatureTimer -= deltaTime;
            if (signatureTimer > 0f)
            {
                return;
            }

            EnemyController target = world.NearestEnemy(Owner.transform.position, Catalog.BaseRange);
            if (target != null)
            {
                if (phase == DayPhase.Day)
                {
                    Vector3 center = target.transform.position;
                    for (int index = world.Enemies.Count - 1; index >= 0; index--)
                    {
                        EnemyController enemy = world.Enemies[index];
                        if (enemy != null && CombatMath.Overlap(enemy.transform.position, enemy.Radius, center,
                            CombatTuning.LionSunRadius))
                        {
                            enemy.TakeDamage(CombatTuning.LionSunDamage, center);
                        }
                    }

                    world.PlayBurst(Palette.FireOrange, CombatTuning.LionSunRadius, center);
                }
                else
                {
                    Vector3 dash = CombatMath.Direction(Owner.transform.position, target.transform.position);
                    Owner.transform.position = CombatMath.ClampArena(Owner.transform.position +
                        dash * CombatTuning.LionMoonDashDistance);
                    Owner.Health.AddShield(CombatTuning.LionMoonShield * Owner.Stats.ShieldPower);
                }
            }

            signatureTimer = CombatTuning.LionSignatureInterval;
        }
    }
}
