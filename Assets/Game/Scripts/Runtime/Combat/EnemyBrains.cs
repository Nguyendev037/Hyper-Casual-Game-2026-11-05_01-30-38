using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Spawns any EnemyKind. CombatWorld keeps seven pools indexed by legacy kind, so chapter kinds are hosted by the
    /// pool of their role (EnemyDefinition.PoolKind) and re-skinned on spawn. Always spawn through this, never SpawnEnemy directly.</summary>
    public static class EnemySpawner
    {
        public static EnemyController Spawn(CombatWorld world, EnemyKind kind, Vector3 position, float hpMult = 1f,
            float dmgMult = 1f, bool elite = false)
        {
            EnemyKind pool = Catalog.GetEnemy(kind).PoolKind;
            EnemyController.PendingKind = kind;
            try
            {
                return world.SpawnEnemy(pool, position, hpMult, dmgMult, elite);
            }
            finally
            {
                EnemyController.PendingKind = null;
            }
        }
    }

    /// <summary>Chapter archetype brains (SPEC-P02 section 1). Every attack is announced by a pooled TelegraphView.</summary>
    public sealed partial class EnemyController
    {
        private const int Idle = 0, Warn = 1, Act = 2;

        private int bState;
        private float bTimer;
        private float bTelegraph;
        private float bClock;
        private float bAngle;
        private float bDir = 1f;
        private float bTravel;
        private float bBlink;
        private bool bHit;
        private Vector3 bAim;
        private Vector3 bPoint;
        private TelegraphView shapeWarn;
        private WarnShape shapeWarnShape;
        private bool skipMotionSample;
        private float stepClock;

        private void ResetBrainState(EnemyDefinition definition)
        {
            bState = Idle;
            bTimer = definition.AttackInterval > 0f ? Mathf.Min(definition.AttackInterval, 1f + Random.value * 1.5f) : 0f;
            bTelegraph = 0f;
            bClock = Random.value * 6f;
            bDir = Random.value < 0.5f ? -1f : 1f;
            bHit = false;
            bBlink = 0f;
            stepClock = 0f;
            HideShapeWarn();
            if (archetype == EnemyArchetype.Orbiter && world != null && world.Player != null)
            {
                Vector3 offset = transform.position - world.Player.transform.position;
                bAngle = Mathf.Atan2(offset.x, offset.z);
            }
        }

        /// <summary>Shows (building on first use) the visual of the kind and hides the others hosted by this pooled controller.</summary>
        private void UseVisual(EnemyKind kind)
        {
            if (IsAnchor)
            {
                return;
            }

            GameObject target;
            if (!visuals.TryGetValue(kind, out target) || target == null)
            {
                target = CharacterFactory.CreateEnemy(kind, transform);
                visuals[kind] = target;
            }

            if (target != visual)
            {
                if (visual != null)
                {
                    visual.SetActive(false);
                }

                visual = target;
                billboard = visual.GetComponentInChildren<SpriteBillboard>();
                visualBaseScale = visual.transform.localScale;
            }

            visual.SetActive(true);
        }

        private Vector3 FaceDirection(Vector3 toPlayer)
        {
            bool dashing = (archetype == EnemyArchetype.Charger || archetype == EnemyArchetype.Beam) && bState != Idle &&
                Kind != EnemyKind.Sentinel;
            return dashing && bAim.sqrMagnitude > 0.0001f ? bAim : toPlayer;
        }

        // ---------- shape warnings (separate from the legacy single warning of Eye/Sentinel) ----------

        private void ShowShapeWarn(WarnShape shape, Vector3 position, Vector3 forward, Vector3 scale)
        {
            HideShapeWarn();
            shapeWarnShape = shape;
            shapeWarn = world.GetWarningShape(shape);
            shapeWarn.transform.position = new Vector3(position.x, 0.05f, position.z);
            shapeWarn.transform.rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward) : Quaternion.identity;
            shapeWarn.transform.localScale = scale;
        }

        private void ShowDisc(Vector3 position, float radius)
        {
            ShowShapeWarn(WarnShape.Disc, position, Vector3.forward, Vector3.one * (radius / Catalog.WardenRingRadius));
        }

        private void ShowLine(Vector3 position, Vector3 forward, float width, float length)
        {
            ShowShapeWarn(WarnShape.Line, position, forward, new Vector3(width / CombatTuning.SentinelTelegraphWidth, 1f,
                length / CombatTuning.SentinelChargeDistance));
        }

        private void HideShapeWarn()
        {
            if (shapeWarn == null || world == null)
            {
                shapeWarn = null;
                return;
            }

            world.ReturnWarningShape(shapeWarn, shapeWarnShape);
            shapeWarn = null;
        }

        private void FillWarn(float remaining, float total)
        {
            if (shapeWarn != null)
            {
                shapeWarn.SetFill(1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, total)));
            }
        }

        private void StartTelegraph(float seconds)
        {
            bState = Warn;
            bTelegraph = seconds;
            PlayWindup(seconds);
        }

        private float Speed(EnemyDefinition definition)
        {
            return definition.MoveSpeed * (1f - slowAmount);
        }

        private void MoveBy(Vector3 delta)
        {
            transform.position = CombatMath.ClampArena(transform.position + delta);
        }

        private float DistanceToPlayer()
        {
            return CombatMath.Flat(world.Player.transform.position - transform.position).magnitude;
        }

        // ---------- shield (PlagueKnight) and boss footsteps ----------

        private float ApplyShield(float amount, Vector3 from)
        {
            if (Kind != EnemyKind.PlagueKnight || isDummy || visual == null)
            {
                return amount;
            }

            Vector3 toSource = CombatMath.Flat(from - transform.position);
            if (toSource.sqrMagnitude < 0.0001f)
            {
                return amount;
            }

            return Vector3.Dot(facing.Forward, toSource.normalized) > 0.35f ? amount * (1f - Catalog.KnightShieldReduction) : amount;
        }

        private void BossFootsteps(float deltaTime, float speed)
        {
            if (!IsBoss || speed < 0.3f || DistanceToPlayer() > 8f)
            {
                return;
            }

            stepClock += deltaTime;
            if (stepClock >= 0.5f)
            {
                stepClock = 0f;
                CombatFeedback.Shake(0.05f);
            }
        }

        // ---------- dispatch ----------

        private void StepRegular(float deltaTime, EnemyDefinition definition, Vector3 toPlayer)
        {
            switch (archetype)
            {
                case EnemyArchetype.Shooter:
                    if (Kind == EnemyKind.SandEye) { StepEye(deltaTime, definition, toPlayer); }
                    else if (Kind == EnemyKind.MirrorGhost) { StepTeleporter(deltaTime, definition, toPlayer); }
                    else if (Kind == EnemyKind.StarWisp) { StepOrbiter(deltaTime, definition); }
                    else { StepShooter(deltaTime, definition, toPlayer); }
                    break;
                case EnemyArchetype.Teleporter: StepTeleporter(deltaTime, definition, toPlayer); break;
                case EnemyArchetype.Orbiter: StepOrbiter(deltaTime, definition); break;
                case EnemyArchetype.Beam: StepBeam(deltaTime, definition, toPlayer); break;
                case EnemyArchetype.Tank: StepTank(deltaTime, definition, toPlayer); break;
                case EnemyArchetype.Flyer: StepFlyer(deltaTime, definition, toPlayer); break;
                case EnemyArchetype.Exploder: StepExploder(deltaTime, definition, toPlayer); break;
                case EnemyArchetype.Charger:
                    if (Kind == EnemyKind.Sentinel) { StepSentinel(deltaTime, definition, toPlayer); }
                    else { StepKnight(deltaTime, definition, toPlayer); }
                    break;
                default: StepBeetle(deltaTime, definition, toPlayer); break;
            }
        }

        // ---------- Flyer (BookBat): zigzag with a 1.5 m sine amplitude ----------

        private void StepFlyer(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            bClock += deltaTime;
            float w = Mathf.PI * 2f / Catalog.FlyerZigzagPeriod;
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            float lateral = Mathf.Cos(bClock * w) * Catalog.FlyerZigzagAmplitude * w * (1f - slowAmount);
            MoveBy(direction * Speed(definition) * deltaTime + side * lateral * deltaTime);
            ContactTick(deltaTime, definition);
        }

        // ---------- Tank (AnvilGolem): slow walk, 1.8 m ring slam every 3.5 s ----------

        private void StepTank(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                FillWarn(bTelegraph, definition.TelegraphTime);
                if (bTelegraph <= 0f)
                {
                    Vector3 center = transform.position;
                    if (CombatMath.Overlap(world.Player.transform.position, 0f, center, Catalog.GolemSlamRadius))
                    {
                        world.Player.TakeDamage(definition.AttackDamage * damageMult, center);
                    }

                    world.PlayBurst(Palette.WarnRed, Catalog.GolemSlamRadius, center);
                    CombatFeedback.Shake(0.08f);
                    HideShapeWarn();
                    bState = Idle;
                    bTimer = definition.AttackInterval;
                }

                return;
            }

            MoveBy(direction * Speed(definition) * deltaTime);
            bTimer -= deltaTime;
            if (bTimer <= 0f && DistanceToPlayer() <= Catalog.GolemSlamRadius + 1.2f)
            {
                ShowDisc(transform.position, Catalog.GolemSlamRadius);
                StartTelegraph(definition.TelegraphTime);
            }
        }

        // ---------- Charger (PlagueKnight): walks, 0.7 s lane warning, 7 m dash; front shield in ApplyShield ----------

        private void StepKnight(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                FillWarn(bTelegraph, definition.TelegraphTime);
                if (bTelegraph <= 0f)
                {
                    HideShapeWarn();
                    bState = Act;
                    bTravel = Catalog.KnightChargeDistance;
                    bHit = false;
                }

                return;
            }

            if (bState == Act)
            {
                float step = Mathf.Min(Catalog.KnightDashSpeed * deltaTime, bTravel);
                MoveBy(bAim * step);
                bTravel -= step;
                if (!bHit && CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                    CombatTuning.PlayerRadius))
                {
                    bHit = true;
                    world.Player.TakeDamage(definition.AttackDamage * damageMult, transform.position);
                }

                if (bTravel <= 0f)
                {
                    bState = Idle;
                    bTimer = definition.AttackInterval;
                }

                return;
            }

            MoveBy(direction * Speed(definition) * deltaTime);
            bTimer -= deltaTime;
            if (bTimer <= 0f && DistanceToPlayer() <= 10f)
            {
                bAim = direction;
                ShowLine(transform.position, bAim, CombatTuning.SentinelTelegraphWidth, Catalog.KnightChargeDistance);
                StartTelegraph(Mathf.Max(CombatTuning.SentinelTelegraphMinimum, definition.TelegraphTime));
            }
        }

        // ---------- Exploder (BombMite): within 1.6 m stop, blink 0.7 s, blast R1.5 ----------

        private void StepExploder(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                bBlink -= deltaTime;
                if (bBlink <= 0f && billboard != null)
                {
                    billboard.Flash(new Color(1f, 0.45f, 0.35f, 1f), 0.08f);
                    bBlink = 0.14f;
                }

                FillWarn(bTelegraph, definition.TelegraphTime);
                if (bTelegraph <= 0f)
                {
                    Explode(definition);
                }

                return;
            }

            MoveBy(direction * Speed(definition) * deltaTime);
            if (DistanceToPlayer() <= Catalog.MiteTriggerRadius)
            {
                ShowDisc(transform.position, Catalog.MiteBlastRadius);
                StartTelegraph(Mathf.Max(Catalog.MiteFuse, definition.TelegraphTime));
                bBlink = 0f;
            }
        }

        private void Explode(EnemyDefinition definition)
        {
            Vector3 center = transform.position;
            float damage = definition.AttackDamage * damageMult;
            HideShapeWarn();
            world.PlayBurst(Palette.FireOrange, Catalog.MiteBlastRadius, center);
            CombatFeedback.Shake(0.1f);
            if (CombatMath.Overlap(world.Player.transform.position, 0f, center, Catalog.MiteBlastRadius))
            {
                world.Player.TakeDamage(damage, center);
            }

            for (int i = world.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController other = i < world.Enemies.Count ? world.Enemies[i] : null;
                if (other == null || other == this || !other.Alive || other.IsAnchor)
                {
                    continue;
                }

                if (CombatMath.Overlap(other.transform.position, other.Radius, center, Catalog.MiteBlastRadius))
                {
                    other.TakeDamage(damage * Catalog.MiteBlastEnemyFraction, center);
                }
            }

            ForceKill();
        }

        // ---------- Splitter (InkSlime): two small slimes (35% HP, 0.6 size, no XP, no further split) ----------

        private void SplitOnDeath(Vector3 position)
        {
            if (archetype != EnemyArchetype.Splitter || isSplitChild || isDummy || world.Director == null ||
                world.Director.Ending)
            {
                return;
            }

            SpawnBudget budget = FloorPlan.Budget(world.CurrentFloor);
            for (int i = 0; i < 2; i++)
            {
                if (world.RegularAlive >= SurvivorTuning.MaxAliveEnemies)
                {
                    return;
                }

                float angle = (i == 0 ? 0.5f : 3.64f) + Random.value;
                Vector3 spot = CombatMath.ClampArena(position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.8f);
                EnemyController child = EnemySpawner.Spawn(world, Kind, spot,
                    budget.HpMult * Catalog.SlimeChildHpFraction, budget.DmgMult, false);
                child.MakeSplitChild();
            }
        }

        internal void MakeSplitChild()
        {
            isSplitChild = true;
            transform.localScale = Vector3.one * Catalog.SlimeChildScale;
            Radius *= Catalog.SlimeChildScale;
        }

        /// <summary>True for the small slimes created by a Splitter death.</summary>
        public bool IsSplitChild => isSplitChild;
    }
}
