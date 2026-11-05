using System;
using System.Collections.Generic;
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
        /// <summary>False while a boss is shielded or changing phase: auto-aim and bullets ignore it.</summary>
        public bool Targetable => Alive && !vanished && (brain == null || !brain.Untargetable);
        public bool Elite { get; private set; }
        public bool IsAnchor { get; private set; }
        public bool IsBoss => !IsAnchor && !isDummy && EnemyKinds.IsBoss(Kind);
        /// <summary>Index of the pool this controller belongs to (EnemyKind value, or AnchorPoolIndex).</summary>
        public int PoolIndex { get; private set; }
        public float HpFraction => health == null ? 0f : health.Current / health.Max;
        public float MaxHealth => health == null ? 0f : health.Max;
        public float CurrentHealth => health == null ? 0f : health.Current;
        public event Action<EnemyController> OnKilled;
        /// <summary>Boss phase 1..3 (0 for non-bosses). Debug/autopilot read this.</summary>
        public int BossPhase => brain != null ? brain.Phase : 0;
        public bool BossPurifying => brain != null && brain.Purifying;


        public const int AnchorPoolIndex = 6;

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
        private float damageMult = 1f;
        private Vector3 lockedTarget;
        private Vector3 chargeDirection;
        private bool charging;
        private bool sentinelTelegraph;
        private TelegraphView warning;
        private bool warningDisc;
        private EnemyKind warningPoolKind;
        private Vector3 eyeShotDirection;
        private GameObject visual;
        private SpriteBillboard billboard;
        private GameObject eliteRing;
        private Facing facing;
        private bool isDummy;
        private TrainingDummy dummyDisplay;
        private BossBrain brain;
        private Vector3 visualBaseScale = Vector3.one;
        private Action<EnemyController> anchorKilled;
        private readonly Dictionary<EnemyKind, GameObject> visuals = new Dictionary<EnemyKind, GameObject>();
        private EnemyArchetype archetype;
        private bool vanished;
        private bool isSplitChild;

        /// <summary>Set by EnemySpawner just before CombatWorld.SpawnEnemy so a pooled controller can host a chapter kind.</summary>
        internal static EnemyKind? PendingKind;

        public void Init(CombatWorld ownerWorld, Action<EnemyController> releaseAction, EnemyKind kind, int poolIndex)
        {
            world = ownerWorld;
            release = releaseAction;
            Kind = kind;
            PoolIndex = poolIndex;
            IsAnchor = poolIndex == AnchorPoolIndex;
            visual = IsAnchor ? CharacterFactory.CreateAnchor(transform) : CharacterFactory.CreateEnemy(kind, transform);
            visuals[kind] = visual;
            billboard = visual.GetComponentInChildren<SpriteBillboard>();
            visualBaseScale = visual.transform.localScale;
            eliteRing = VfxFactory.EliteRing(0.9f);
            eliteRing.transform.SetParent(transform, false);
            eliteRing.SetActive(false);
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

        public void Spawn(EnemyKind kind, Vector3 position, float hpMult = 1f, float dmgMult = 1f, bool elite = false)
        {
            if (PendingKind.HasValue)
            {
                kind = PendingKind.Value;
                PendingKind = null;
            }

            Kind = kind;
            transform.localScale = Vector3.one;
            vanished = false;
            isSplitChild = false;
            UseVisual(kind);
            if (billboard != null)
            {
                billboard.Revive();
                billboard.SetMotion(0f);
            }
            transform.position = position;
            facing = default; // pooled enemies must not keep the previous life's yaw
            EnemyDefinition definition = Catalog.GetEnemy(kind);
            Radius = definition.Radius;
            archetype = definition.Archetype;
            ResetBrainState(definition);
            Elite = elite;
            damageMult = dmgMult;
            float hp = definition.MaxHp * hpMult * (elite ? CombatTuning.EliteHpMult : 1f);
            health = new Health(hp);
            health.Died += Die;
            transform.localScale = elite ? Vector3.one * CombatTuning.EliteScale : Vector3.one;
            if (visual != null)
            {
                visual.transform.localScale = visualBaseScale;
            }

            if (elite)
            {
                Radius *= CombatTuning.EliteScale;
            }

            if (eliteRing != null)
            {
                eliteRing.SetActive(elite);
            }

            slowAmount = 0f;
            slowRemaining = 0f;
            burnDps = 0f;
            burnRemaining = 0f;
            contactTimer = 0f;
            attackTimer = definition.AttackInterval;
            charging = false;
            sentinelTelegraph = false;
            warningDisc = false;
            brain = null;
            if (IsAnchor)
            {
                Radius = 0.8f;
            }
            else if (EnemyKinds.IsBoss(kind))
            {
                brain = new BossBrain(this, world, kind, world.CurrentFloor, dmgMult);
            }

            gameObject.SetActive(true);
            if (brain != null)
            {
                world.Session.NotifyBoss(1f);
            }
        }

        /// <summary>Anchor of the Blue Hair Guy: stationary target with the given HP.</summary>
        public void SpawnAnchor(Vector3 position, float hp, Action<EnemyController> onKilled)
        {
            Kind = EnemyKind.SandBeetle;
            transform.position = position;
            transform.localScale = Vector3.one;
            Radius = 0.8f;
            Elite = false;
            damageMult = 1f;
            health = new Health(hp);
            health.Died += Die;
            slowAmount = 0f;
            slowRemaining = 0f;
            burnDps = 0f;
            burnRemaining = 0f;
            brain = null;
            anchorKilled = onKilled;
            if (eliteRing != null)
            {
                eliteRing.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        /// <summary>Blue Hair Guy kneels instead of dying (SPEC-P03 §2b).</summary>
        public void KneelVisual()
        {
            if (visual != null)
            {
                Vector3 s = visualBaseScale;
                visual.transform.localScale = new Vector3(s.x, s.y * 0.7f, s.z);
            }
        }

        public void PlayWindup(float seconds)
        {
            if (billboard != null) billboard.PlayWindup(seconds);
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (!Alive)
            {
                return;
            }

            if (brain != null)
            {
                amount = brain.FilterDamage(amount, health);
                if (amount <= 0f)
                {
                    return;
                }
            }

            amount = ApplyShield(amount, from);

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
            if (brain != null && health != null)
            {
                world.Session.NotifyBoss(health.Current / health.Max);
                brain.AfterDamage(health);
            }
        }

        /// <summary>Kills regardless of the boss shield (floor end, debug).</summary>
        public void ForceKill()
        {
            if (Alive)
            {
                health.Damage(health.Max * 10f + 1f, transform.position);
            }
        }

        public void ApplyEffects(float slow, float burn, float duration)
        {
            float resist = 1f;
            float cap = 1f;
            if (EnemyKinds.IsGuardian(Kind) && !IsAnchor)
            {
                resist = 0.5f;
            }
            else if ((Kind == EnemyKind.SandQueen || Kind == EnemyKind.BlueHairGuy) && !IsAnchor)
            {
                cap = 0.2f; // SPEC-P03 §2: bosses are not slowed beyond 20%
            }

            slowAmount = Mathf.Max(slowAmount, Mathf.Min(cap, Mathf.Clamp01(slow) * resist));
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

            TickEffects(deltaTime);
            if (!Alive)
            {
                return;
            }

            if (IsAnchor)
            {
                return;
            }

            EnemyDefinition definition = Catalog.GetEnemy(Kind);
            Vector3 beforeMove = transform.position;
            skipMotionSample = false;
            Vector3 toPlayer = CombatMath.Direction(transform.position, world.Player.transform.position);
            if (visual != null)
            {
                facing.Face(FaceDirection(toPlayer));
                facing.Step(deltaTime, IsBoss ? 120f : 360f);
                visual.transform.rotation = facing.Rotation;
            }

            if (brain != null)
            {
                brain.Step(deltaTime, toPlayer, definition.MoveSpeed, 1f - slowAmount);
            }
            else
            {
                StepRegular(deltaTime, definition, toPlayer);
            }

            if (!Alive)
            {
                return;
            }

            float moved = skipMotionSample ? 0f : CombatMath.Flat(transform.position - beforeMove).magnitude;
            float speed = moved / Mathf.Max(deltaTime, 0.0001f);
            if (billboard != null)
            {
                billboard.SetMotion(speed / Mathf.Max(0.01f, definition.MoveSpeed));
            }

            BossFootsteps(deltaTime, speed);
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
            transform.position = CombatMath.ClampArena(transform.position +
                direction * definition.MoveSpeed * (1f - slowAmount) * deltaTime);
            ContactTick(deltaTime, definition);
        }

        private void ContactTick(float deltaTime, EnemyDefinition definition)
        {
            if (!CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                CombatTuning.PlayerRadius))
            {
                return;
            }

            contactTimer -= deltaTime;
            if (contactTimer <= 0f)
            {
                world.Player.TakeDamage(definition.ContactDamage * damageMult, transform.position);
                contactTimer = CombatTuning.BeetleContactInterval;
            }
        }

        private void StepEye(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            float distance = CombatMath.Flat(world.Player.transform.position - transform.position).magnitude;
            float speed = definition.MoveSpeed * (1f - slowAmount);
            if (distance < CombatTuning.EyeMinDistance)
            {
                transform.position = CombatMath.ClampArena(transform.position - direction * speed * deltaTime);
            }
            else if (distance > CombatTuning.EyeMaxDistance)
            {
                transform.position = CombatMath.ClampArena(transform.position + direction * speed * deltaTime);
            }

            if (warning != null)
            {
                telegraphRemaining -= deltaTime;
                warning.SetFill(1f - telegraphRemaining / CombatTuning.EyeTelegraphTime);
                if (telegraphRemaining <= 0f)
                {
                    world.Fire(ProjectileOwner.Enemy, transform.position + Vector3.up * 0.5f, eyeShotDirection,
                        definition.AttackDamage * damageMult, CombatTuning.EyeProjectileSpeed, null,
                        Palette.EnemyPurple);
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
                PlayWindup(telegraphRemaining);
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
                transform.position = CombatMath.ClampArena(transform.position + chargeDirection * distance);
                chargeDistance -= distance;
                if (chargeDistance <= 0f)
                {
                    charging = false;
                    HideWarning();
                }

                if (CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                    CombatTuning.PlayerRadius))
                {
                    world.Player.TakeDamage(definition.AttackDamage * damageMult, transform.position);
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
            PlayWindup(telegraphRemaining);
            sentinelTelegraph = true;
            charging = false;
            attackTimer = definition.AttackInterval;
        }
    }
}
