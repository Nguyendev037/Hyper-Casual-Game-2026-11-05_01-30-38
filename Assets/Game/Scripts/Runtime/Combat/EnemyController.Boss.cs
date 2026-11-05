using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class EnemyController
    {
        private void StepBoss(float deltaTime, Vector3 direction, float movementSpeed)
        {
            transform.position += direction * Mathf.Min(movementSpeed, 1f) * (1f - slowAmount) * deltaTime;
            if (telegraphRemaining > 0f)
            {
                telegraphRemaining -= deltaTime;
                if (warning != null)
                {
                    warning.SetFill(1f - telegraphRemaining / Catalog.WardenFanTelegraph);
                }

                if (telegraphRemaining > 0f)
                {
                    return;
                }

                ResolveBossAttack();
                return;
            }

            bossRest -= deltaTime;
            if (bossRest > 0f)
            {
                return;
            }

            nextSlam = !nextSlam;
            lockedTarget = world.Player.transform.position;
            attackDirection = direction;
            warningDisc = nextSlam;
            warningPoolKind = Kind;
            warning = world.GetWarning(Kind, warningDisc);
            warning.transform.position = nextSlam ? lockedTarget : transform.position;
            warning.transform.rotation = Quaternion.LookRotation(attackDirection);
            telegraphRemaining = Mathf.Max(Catalog.WardenFanTelegraph,
                Catalog.GetEnemy(EnemyKind.CageWarden).TelegraphTime);
        }

        private void ResolveBossAttack()
        {
            if (warning == null)
            {
                return;
            }

            if (nextSlam)
            {
                if (CombatMath.Overlap(world.Player.transform.position, 0f, lockedTarget, Catalog.WardenRingRadius))
                {
                    world.Player.TakeDamage(Catalog.GetEnemy(EnemyKind.CageWarden).AttackDamage, lockedTarget);
                }
            }
            else
            {
                for (int i = 0; i < Catalog.WardenFanProjectiles; i++)
                {
                    float halfAngle = CombatTuning.WardenFanAngle / 2f;
                    float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)(Catalog.WardenFanProjectiles - 1));
                    Vector3 shotDirection = Quaternion.AngleAxis(angle, Vector3.up) * attackDirection;
                    world.Fire(ProjectileOwner.Enemy, transform.position, shotDirection, Catalog.WardenFanDamage,
                        CombatTuning.WardenProjectileSpeed, null, Palette.EnemyPurple);
                }
            }

            HideWarning();
            bossRest = CombatTuning.WardenRecovery;
            if (health.Current / health.Max < Catalog.WardenEnrageHpFraction)
            {
                bossRest /= Catalog.WardenEnrageRate;
            }
        }

        private void HideWarning()
        {
            if (warning == null)
            {
                return;
            }

            world.ReturnWarning(warning, warningPoolKind, warningDisc);
            warning = null;
        }

        private void Die()
        {
            HideWarning();
            if (Kind == EnemyKind.CageWarden)
            {
                world.Session.NotifyBossDefeated(transform.position);
            }

            world.Enemies.Remove(this);
            OnKilled?.Invoke(this);
            release?.Invoke(this);
        }

        public void ReturnToPool()
        {
            HideWarning();
            health = null;
            gameObject.SetActive(false);
        }
    }
}
