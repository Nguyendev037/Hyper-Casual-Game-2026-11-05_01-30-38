using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class EnemyController
    {
        private static readonly Color BoltColor = new Color(0.95f, 0.45f, 0.2f, 1f);

        // ---------- Shooter (SparkTurret fan of 3, BoneArcher single shot to 8 m) ----------

        private void StepShooter(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            bool archer = Kind == EnemyKind.BoneArcher;
            float minDistance = archer ? 5.5f : CombatTuning.EyeMinDistance;
            float maxDistance = archer ? Catalog.ArcherRange : CombatTuning.EyeMaxDistance;
            float tele = Mathf.Max(CombatTuning.EyeTelegraphTime, definition.TelegraphTime);
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                FillWarn(bTelegraph, tele);
                if (bTelegraph > 0f)
                {
                    return;
                }

                HideShapeWarn();
                int count = archer ? 1 : Catalog.TurretFanCount;
                float arc = archer ? 0f : Catalog.TurretFanArc;
                float speed = archer ? Catalog.ArcherProjectileSpeed : CombatTuning.EyeProjectileSpeed;
                for (int i = 0; i < count; i++)
                {
                    float angle = count == 1 ? 0f : Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (float)(count - 1));
                    Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * bAim;
                    world.Fire(ProjectileOwner.Enemy, transform.position + Vector3.up * 0.5f, dir,
                        definition.AttackDamage * damageMult, speed, null, archer ? Palette.Silver : BoltColor);
                }

                bState = Idle;
                bTimer = definition.AttackInterval;
                return;
            }

            float distance = DistanceToPlayer();
            if (distance < minDistance)
            {
                MoveBy(-direction * Speed(definition) * deltaTime);
            }
            else if (distance > maxDistance)
            {
                MoveBy(direction * Speed(definition) * deltaTime);
            }

            bTimer -= deltaTime;
            if (bTimer > 0f || distance > maxDistance + 0.5f)
            {
                return;
            }

            bAim = direction;
            if (archer)
            {
                ShowLine(transform.position, bAim, 0.4f, Catalog.ArcherRange);
            }
            else
            {
                ShowShapeWarn(WarnShape.Fan60, transform.position, bAim, Vector3.one);
            }

            StartTelegraph(tele);
        }

        // ---------- Teleporter (MirrorGhost): vanish, ring 0.6 s at the exit point 4 m from the girl, appear, shoot ----------

        private void StepTeleporter(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                FillWarn(bTelegraph, Catalog.TeleportTelegraph);
                if (bTelegraph <= 0f)
                {
                    HideShapeWarn();
                    transform.position = bPoint;
                    vanished = false;
                    if (visual != null)
                    {
                        visual.SetActive(true);
                    }

                    skipMotionSample = true;
                    world.PlayBurst(Palette.Silver, 0.9f, bPoint);
                    PlayWindup(0.3f);
                    bState = Act;
                    bTimer = 0.3f;
                }

                return;
            }

            if (bState == Act)
            {
                bTimer -= deltaTime;
                if (bTimer <= 0f)
                {
                    Vector3 dir = CombatMath.Direction(transform.position, world.Player.transform.position);
                    world.Fire(ProjectileOwner.Enemy, transform.position + Vector3.up * 0.5f, dir,
                        definition.AttackDamage * damageMult, CombatTuning.EyeProjectileSpeed, null, Palette.Silver);
                    bState = Idle;
                    bTimer = Catalog.TeleportInterval;
                }

                return;
            }

            float distance = DistanceToPlayer();
            if (distance < CombatTuning.EyeMinDistance)
            {
                MoveBy(-direction * Speed(definition) * deltaTime);
            }
            else if (distance > CombatTuning.EyeMaxDistance)
            {
                MoveBy(direction * Speed(definition) * deltaTime);
            }

            bTimer -= deltaTime;
            if (bTimer > 0f)
            {
                return;
            }

            bPoint = PickReappearPoint();
            ShowDisc(bPoint, 0.9f);
            vanished = true;
            if (visual != null)
            {
                visual.SetActive(false);
            }

            transform.position = new Vector3(1000f, 0f, 1000f);
            skipMotionSample = true;
            bState = Warn;
            bTelegraph = Catalog.TeleportTelegraph;
        }

        private Vector3 PickReappearPoint()
        {
            Vector3 player = world.Player.transform.position;
            float start = Random.value * Mathf.PI * 2f;
            Vector3 best = player;
            float bestDistance = -1f;
            for (int i = 0; i < 8; i++)
            {
                float angle = start + i * Mathf.PI * 0.25f;
                Vector3 candidate = CombatMath.ClampArena(player +
                    new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * Catalog.TeleportDistance);
                float d = CombatMath.Flat(candidate - player).magnitude;
                if (d >= Catalog.TeleportDistance - 0.25f)
                {
                    return candidate;
                }

                if (d > bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>Brings a vanished teleporter back (death, pooling) so its gems and visual are not left far away.</summary>
        private void UnVanish()
        {
            if (!vanished)
            {
                return;
            }

            vanished = false;
            transform.position = bPoint;
            if (visual != null)
            {
                visual.SetActive(true);
            }
        }

        // ---------- Orbiter (StarWisp): circles the girl at R5, ring of 6 every 3 s with a 0.6 s flash ----------

        private void StepOrbiter(float deltaTime, EnemyDefinition definition)
        {
            Vector3 player = world.Player.transform.position;
            float radius = Catalog.WispOrbitRadius;
            bAngle += bDir * Speed(definition) / radius * deltaTime;
            Vector3 target = CombatMath.ClampArena(player + new Vector3(Mathf.Sin(bAngle), 0f, Mathf.Cos(bAngle)) * radius);
            float reach = CombatMath.Flat(target - transform.position).magnitude;
            float speed = Mathf.Min(Speed(definition) * 3f, Mathf.Max(Speed(definition), reach * 2f));
            transform.position = Vector3.MoveTowards(transform.position, target, speed * deltaTime);
            transform.position = CombatMath.ClampArena(transform.position);

            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                shapeWarn.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                FillWarn(bTelegraph, definition.TelegraphTime);
                if (bTelegraph <= 0f)
                {
                    HideShapeWarn();
                    float offset = Random.value * 360f;
                    for (int i = 0; i < Catalog.WispRingCount; i++)
                    {
                        Vector3 dir = Quaternion.AngleAxis(offset + i * 360f / Catalog.WispRingCount, Vector3.up) * Vector3.forward;
                        world.Fire(ProjectileOwner.Enemy, transform.position + Vector3.up * 0.5f, dir,
                            definition.AttackDamage * damageMult, Catalog.WispRingSpeed, null, Palette.LightGold);
                    }

                    bState = Idle;
                    bTimer = definition.AttackInterval;
                }

                return;
            }

            bTimer -= deltaTime;
            if (bTimer <= 0f)
            {
                ShowDisc(transform.position, 0.9f);
                StartTelegraph(Mathf.Max(CombatTuning.EyeTelegraphTime, definition.TelegraphTime));
            }
        }

        // ---------- Beam (VoidEye): stop, 9 m lane for 0.9 s, then a 0.4 s damaging beam on the same lane ----------

        private void StepBeam(float deltaTime, EnemyDefinition definition, Vector3 direction)
        {
            if (bState == Warn)
            {
                bTelegraph -= deltaTime;
                FillWarn(bTelegraph, definition.TelegraphTime);
                if (bTelegraph <= 0f)
                {
                    bState = Act;
                    bTimer = Catalog.VoidBeamDuration;
                    bHit = false;
                    if (shapeWarn != null)
                    {
                        shapeWarn.SetFill(1f);
                    }

                    CombatFeedback.Shake(0.1f);
                }

                return;
            }

            if (bState == Act)
            {
                bTimer -= deltaTime;
                Vector3 toPlayer = CombatMath.Flat(world.Player.transform.position - transform.position);
                float along = Vector3.Dot(toPlayer, bAim);
                float side = Mathf.Abs(toPlayer.x * bAim.z - toPlayer.z * bAim.x);
                if (!bHit && along > 0f && along < Catalog.VoidBeamLength && side < 0.7f)
                {
                    bHit = true;
                    world.Player.TakeDamage(definition.AttackDamage * damageMult, transform.position);
                }

                if (bTimer <= 0f)
                {
                    HideShapeWarn();
                    bState = Idle;
                    bTimer = definition.AttackInterval;
                }

                return;
            }

            float distance = DistanceToPlayer();
            if (distance < CombatTuning.EyeMinDistance)
            {
                MoveBy(-direction * Speed(definition) * deltaTime);
            }
            else if (distance > 7f)
            {
                MoveBy(direction * Speed(definition) * deltaTime);
            }

            bTimer -= deltaTime;
            if (bTimer <= 0f && distance <= Catalog.VoidBeamLength)
            {
                bAim = direction;
                ShowLine(transform.position, bAim, 0.5f, Catalog.VoidBeamLength);
                StartTelegraph(Mathf.Max(0.9f, definition.TelegraphTime));
            }
        }
    }
}
