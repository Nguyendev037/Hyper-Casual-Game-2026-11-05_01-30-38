using System;
using UnityEngine;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public enum ProjectileOwner
    {
        Player,
        Enemy
    }

    public sealed class Projectile : MonoBehaviour
    {
        public ProjectileOwner Owner { get; private set; }
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public float Life { get; private set; }
        public float Radius { get; private set; }
        public EnemyController Target { get; private set; }
        public Vector3 Direction { get; private set; }
        public long Born { get; set; }

        private CombatWorld world;
        private Action<Projectile> release;
        private float age;
        private float splashRadius;
        private float impactSlow;
        private float impactBurn;
        private float impactDuration;

        public void Init(CombatWorld ownerWorld, Action<Projectile> releaseAction)
        {
            world = ownerWorld;
            release = releaseAction;
        }

        public void Fire(ProjectileOwner owner, Vector3 position, Vector3 direction, float damage, float speed,
            EnemyController target = null, Color? color = null)
        {
            Owner = owner;
            transform.position = position;
            Direction = CombatMath.Flat(direction).normalized;
            Damage = damage;
            Speed = speed;
            Life = 4f;
            Radius = CombatTuning.ProjectileRadius;
            Target = target;
            age = 0f;
            splashRadius = 0f;
            impactSlow = 0f;
            impactBurn = 0f;
            impactDuration = 0f;
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null && color.HasValue)
            {
                renderer.sharedMaterial = MaterialLibrary.Unlit(color.Value);
            }

            gameObject.SetActive(true);
        }

        public void SetImpact(float radius, float slow, float burnDps, float duration)
        {
            splashRadius = Mathf.Max(0f, radius);
            impactSlow = slow;
            impactBurn = burnDps;
            impactDuration = duration;
        }

        public void Step(float deltaTime)
        {
            age += deltaTime;
            if (age >= Life)
            {
                release?.Invoke(this);
                return;
            }

            if (Owner == ProjectileOwner.Player && Target != null && Target.Alive)
            {
                Vector3 toTarget = CombatMath.Direction(transform.position, Target.transform.position);
                Direction = Vector3.RotateTowards(Direction, toTarget, deltaTime * 2f, 0f).normalized;
            }

            transform.position += Direction * Speed * deltaTime;
            if (Owner == ProjectileOwner.Player)
            {
                for (int i = 0; i < world.Enemies.Count; i++)
                {
                    EnemyController enemy = world.Enemies[i];
                    if (enemy != null && enemy.Alive &&
                        CombatMath.Overlap(transform.position, Radius, enemy.transform.position, enemy.Radius))
                    {
                        ResolveEnemyImpact(enemy);
                        release?.Invoke(this);
                        return;
                    }
                }

                EnemyController dummy = world.DummyTarget;
                if (dummy != null && dummy.Alive &&
                    CombatMath.Overlap(transform.position, Radius, dummy.transform.position, dummy.Radius))
                {
                    ResolveEnemyImpact(dummy);
                    release?.Invoke(this);
                    return;
                }
            }
            else if (CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                CombatTuning.PlayerRadius))
            {
                world.Player.TakeDamage(Damage, transform.position);
                release?.Invoke(this);
                return;
            }

            if (Mathf.Abs(transform.position.x) > 7f || Mathf.Abs(transform.position.z) > 9f)
            {
                release?.Invoke(this);
            }
        }

        private void ResolveEnemyImpact(EnemyController target)
        {
            if (splashRadius <= 0f)
            {
                target.TakeDamage(Damage, transform.position);
                target.ApplyEffects(impactSlow, impactBurn, impactDuration);
                return;
            }

            for (int i = world.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = world.Enemies[i];
                if (enemy != null && CombatMath.Overlap(enemy.transform.position, enemy.Radius,
                    target.transform.position, splashRadius))
                {
                    enemy.TakeDamage(Damage, transform.position);
                    enemy.ApplyEffects(impactSlow, impactBurn, impactDuration);
                }
            }

            EnemyController dummy = world.DummyTarget;
            if (dummy != null && dummy.Alive &&
                CombatMath.Overlap(dummy.transform.position, dummy.Radius, target.transform.position, splashRadius))
            {
                dummy.TakeDamage(Damage, transform.position);
                dummy.ApplyEffects(impactSlow, impactBurn, impactDuration);
            }
        }

        public void ResetPool()
        {
            Target = null;
            splashRadius = 0f;
            gameObject.SetActive(false);
        }
    }
}
