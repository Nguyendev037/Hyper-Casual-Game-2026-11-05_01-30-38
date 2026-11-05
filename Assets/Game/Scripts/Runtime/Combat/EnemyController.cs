using System;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class EnemyController : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public float Radius { get; private set; }
        public bool Alive => health != null && !health.Dead;
        public event Action<EnemyController> OnKilled;

        private CombatWorld world;
        private Action<EnemyController> release;
        private Health health;
        private float slowAmount;
        private float slowRemaining;
        private float burnDps;
        private float burnRemaining;
        private float contactTimer;
        private float attackTimer;
        private float telegraphRemaining;
        private float chargeDistance;
        private float bossRest = 1.6f;
        private Vector3 lockedTarget;
        private Vector3 chargeDirection;
        private Vector3 attackDirection;
        private bool charging;
        private bool sentinelTelegraph;
        private bool nextSlam;
        private TelegraphView warning;
        private bool warningDisc;
        private EnemyKind warningPoolKind;
        private Vector3 eyeShotDirection;
        private GameObject visual;
        private Facing facing;
        private bool isDummy;
        private TrainingDummy dummyDisplay;

        public void Init(CombatWorld ownerWorld, Action<EnemyController> releaseAction, EnemyKind kind)
        {
            world = ownerWorld;
            release = releaseAction;
            Kind = kind;
            visual = CharacterFactory.CreateEnemy(kind, transform);
        }

        public void ConfigureDummy(CombatWorld combatWorld, TrainingDummy display)
        {
            world = combatWorld;
            dummyDisplay = display;
            isDummy = true;
            Kind = EnemyKind.SandBeetle;
            Radius = CombatTuning.DogBiteRadius;
            health = new Health(99999f);
        }

        public void Spawn(EnemyKind kind, Vector3 position)
        {
            Kind = kind;
            transform.position = position;
            facing = default; // pooled enemies must not keep the previous life's yaw
            EnemyDefinition definition = Catalog.GetEnemy(kind);
            Radius = definition.Radius;
            health = new Health(definition.MaxHp);
            health.Died += Die;
            slowAmount = 0f;
            slowRemaining = 0f;
            burnDps = 0f;
            burnRemaining = 0f;
            contactTimer = 0f;
            attackTimer = definition.AttackInterval;
            bossRest = CombatTuning.WardenRecovery;
            nextSlam = false;
            charging = false;
            sentinelTelegraph = false;
            warningDisc = false;
            gameObject.SetActive(true);
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (!Alive)
            {
                return;
            }

            health.Damage(amount, from);
            if (isDummy)
            {
                dummyDisplay.Record(amount);
            }
            else
            {
                CombatFeedback.ShowDamage(transform.position + Vector3.up, amount);
            }

            CombatFeedback.Play("hit");
            // Die() may already have returned this enemy to the pool (health == null).
            if (Kind == EnemyKind.CageWarden && health != null)
            {
                world.Session.NotifyBoss(health.Current / health.Max);
            }
        }

        public void ApplyEffects(float slow, float burn, float duration)
        {
            float resist = Kind == EnemyKind.CageWarden ? 0.5f : 1f;
            slowAmount = Mathf.Max(slowAmount, Mathf.Clamp01(slow) * resist);
            slowRemaining = Mathf.Max(slowRemaining, duration * resist);
            burnDps = Mathf.Max(burnDps, burn);
            burnRemaining = Mathf.Max(burnRemaining, duration);
        }

        public void Step(float deltaTime)
        {
            if (!Alive)
            {
                return;
            }

            EnemyDefinition definition = Catalog.GetEnemy(Kind);
            TickEffects(deltaTime);
            if (!Alive)
            {
                return;
            }
            Vector3 toPlayer = CombatMath.Direction(transform.position, world.Player.transform.position);
            if (visual != null)
            {
                facing.Face(toPlayer);
                facing.Step(deltaTime, Kind == EnemyKind.CageWarden ? 120f : 360f);
                visual.transform.rotation = facing.Rotation;
            }
            if (Kind == EnemyKind.CageWarden)
            {
                StepBoss(deltaTime, toPlayer, definition.MoveSpeed);
            }
            else if (Kind == EnemyKind.SandBeetle)
            {
                StepBeetle(deltaTime, definition, toPlayer);
            }
            else if (Kind == EnemyKind.SandEye)
            {
                StepEye(deltaTime, definition, toPlayer);
            }
            else
            {
                StepSentinel(deltaTime, definition, toPlayer);
            }
        }

        private void TickEffects(float deltaTime)
        {
            if (slowRemaining > 0f)
            {
                slowRemaining -= deltaTime;
                if (slowRemaining <= 0f)
                {
                    slowAmount = 0f;
                }
            }

            if (burnRemaining > 0f)
            {
                burnRemaining -= deltaTime;
                TakeDamage(burnDps * deltaTime, transform.position);
            }
        }

        private void StepBeetle(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            transform.position += direction * definition.MoveSpeed * (1f - slowAmount) * deltaTime;
            if (!CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                CombatTuning.PlayerRadius))
            {
                return;
            }

            contactTimer -= deltaTime;
            if (contactTimer <= 0f)
            {
                world.Player.TakeDamage(definition.ContactDamage, transform.position);
                contactTimer = CombatTuning.BeetleContactInterval;
            }
        }

        private void StepEye(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            float distance = CombatMath.Flat(world.Player.transform.position - transform.position).magnitude;
            float speed = definition.MoveSpeed * (1f - slowAmount);
            if (distance < CombatTuning.EyeMinDistance)
            {
                transform.position -= direction * speed * deltaTime;
            }
            else if (distance > CombatTuning.EyeMaxDistance)
            {
                transform.position += direction * speed * deltaTime;
            }

            if (warning != null)
            {
                telegraphRemaining -= deltaTime;
                warning.SetFill(1f - telegraphRemaining / CombatTuning.EyeTelegraphTime);
                if (telegraphRemaining <= 0f)
                {
                    world.Fire(ProjectileOwner.Enemy, transform.position + Vector3.up * 0.5f, eyeShotDirection,
                        definition.AttackDamage, CombatTuning.EyeProjectileSpeed, null, Palette.EnemyPurple);
                    HideWarning();
                    attackTimer = definition.AttackInterval;
                }

                return;
            }

            attackTimer -= deltaTime;
            if (attackTimer <= 0f)
            {
                eyeShotDirection = CombatMath.Direction(transform.position, world.Player.transform.position);
                warningPoolKind = Kind;
                warningDisc = true;
                warning = world.GetWarning(Kind, true);
                warning.transform.localScale = Vector3.one * 0.2f;
                warning.transform.position = transform.position;
                warning.transform.rotation = Quaternion.LookRotation(eyeShotDirection);
                telegraphRemaining = Mathf.Max(CombatTuning.EyeTelegraphTime, definition.TelegraphTime);
            }
        }

        private void StepSentinel(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (sentinelTelegraph)
            {
                telegraphRemaining -= deltaTime;
                if (warning != null)
                {
                    warning.SetFill(1f - telegraphRemaining / Mathf.Max(0.7f, definition.TelegraphTime));
                }

                if (telegraphRemaining <= 0f)
                {
                    HideWarning();
                    sentinelTelegraph = false;
                    charging = true;
                }

                return;
            }

            if (charging)
            {
                float distance = Mathf.Min(Catalog.SentinelDashSpeed * deltaTime, chargeDistance);
                transform.position += chargeDirection * distance;
                chargeDistance -= distance;
                if (chargeDistance <= 0f)
                {
                    charging = false;
                    HideWarning();
                }

                if (CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                    CombatTuning.PlayerRadius))
                {
                    world.Player.TakeDamage(definition.AttackDamage, transform.position);
                }

                return;
            }

            attackTimer -= deltaTime;
            if (attackTimer > 0f)
            {
                return;
            }

            lockedTarget = world.Player.transform.position;
            chargeDirection = CombatMath.Direction(transform.position, lockedTarget);
            chargeDistance = Mathf.Min(CombatTuning.SentinelChargeDistance,
                CombatMath.Flat(lockedTarget - transform.position).magnitude);
            warningPoolKind = Kind;
            warning = world.GetWarning(Kind);
            warning.transform.position = transform.position;
            warning.transform.rotation = Quaternion.LookRotation(chargeDirection);
            telegraphRemaining = Mathf.Max(CombatTuning.SentinelTelegraphMinimum, definition.TelegraphTime);
            sentinelTelegraph = true;
            charging = false;
            attackTimer = definition.AttackInterval;
        }

    }
}
