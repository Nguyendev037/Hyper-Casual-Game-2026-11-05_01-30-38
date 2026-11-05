using System;
using System.Collections.Generic;
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
        private int pierceLeft;
        private float healOnHit;
        private VfxProjectileView view;
        private VfxStyle lookStyle;
        private readonly List<EnemyController> hitList = new List<EnemyController>(4);

        public void Init(CombatWorld ownerWorld, Action<Projectile> releaseAction)
        {
            world = ownerWorld;
            release = releaseAction;
        }

        public void Fire(ProjectileOwner owner, Vector3 position, Vector3 direction, float damage, float speed,
            EnemyController target = null, Color? color = null, VfxStyle style = VfxStyle.Auto)
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
            pierceLeft = 0;
            healOnHit = 0f;
            hitList.Clear();
            gameObject.SetActive(true);
            ApplyLook(VfxStyles.Resolve(style, owner), damage);
        }

        // Dmg 5..60 maps to a 0.85x..1.5x core; the colour argument of old callers is replaced by the style.
        private void ApplyLook(VfxStyle style, float damage)
        {
            if (view == null)
            {
                view = GetComponent<VfxProjectileView>();
            }

            if (view == null)
            {
                return;
            }

            lookStyle = style;
            view.SetStyle(style, Mathf.Lerp(0.85f, 1.5f, Mathf.InverseLerp(5f, 60f, damage)));
            view.Begin();
        }

        public void SetImpact(float radius, float slow, float burnDps, float duration)
        {
            splashRadius = Mathf.Max(0f, radius);
            impactSlow = slow;
            impactBurn = burnDps;
            impactDuration = duration;
        }

        /// <summary>Flies exactly to the end of the range: lifetime = range / speed + 0.1 s (SPEC-P01 §2).</summary>
        public void SetRange(float range)
        {
            Life = range / Mathf.Max(0.1f, Speed) + 0.1f;
        }

        public void SetPierce(int extraTargets)
        {
            pierceLeft = Mathf.Max(0, extraTargets);
        }

        public void SetHeal(float amount)
        {
            healOnHit = Mathf.Max(0f, amount);
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
                    if (enemy != null && enemy.Targetable && !hitList.Contains(enemy) &&
                        CombatMath.Overlap(transform.position, Radius, enemy.transform.position, enemy.Radius))
                    {
                        if (HitAndCheckEnd(enemy))
                        {
                            release?.Invoke(this);
                            return;
                        }

                        break;
                    }
                }

                EnemyController dummy = world.DummyTarget;
                if (dummy != null && dummy.Alive && !hitList.Contains(dummy) &&
                    CombatMath.Overlap(transform.position, Radius, dummy.transform.position, dummy.Radius))
                {
                    if (HitAndCheckEnd(dummy))
                    {
                        release?.Invoke(this);
                        return;
                    }
                }
            }
            else if (CombatMath.Overlap(transform.position, Radius, world.Player.transform.position,
                CombatTuning.PlayerRadius))
            {
                world.Player.TakeDamage(Damage, transform.position);
                VfxHub.Impact(transform.position, VfxStyles.TrailTint(lookStyle));
                release?.Invoke(this);
                return;
            }

            if (Mathf.Abs(transform.position.x) > CombatMath.LimitX + 2f ||
                Mathf.Abs(transform.position.z) > CombatMath.LimitZ + 2f)
            {
                release?.Invoke(this);
            }
        }

        // True when the projectile is spent after this hit.
        private bool HitAndCheckEnd(EnemyController enemy)
        {
            hitList.Add(enemy);
            ResolveEnemyImpact(enemy);
            Target = null;
            if (pierceLeft > 0)
            {
                pierceLeft--;
                return false;
            }

            return true;
        }

        private void ResolveEnemyImpact(EnemyController target)
        {
            bool hit = false;
            if (splashRadius <= 0f)
            {
                target.TakeDamage(Damage, transform.position);
                target.ApplyEffects(impactSlow, impactBurn, impactDuration);
                hit = true;
            }
            else
            {
                Vector3 center = target.transform.position;
                for (int i = world.Enemies.Count - 1; i >= 0; i--)
                {
                    EnemyController enemy = world.Enemies[i];
                    if (enemy != null && enemy.Alive && CombatMath.Overlap(enemy.transform.position, enemy.Radius,
                        center, splashRadius))
                    {
                        enemy.TakeDamage(Damage, transform.position);
                        enemy.ApplyEffects(impactSlow, impactBurn, impactDuration);
                        hit = true;
                    }
                }

                EnemyController dummy = world.DummyTarget;
                if (dummy != null && dummy.Alive &&
                    CombatMath.Overlap(dummy.transform.position, dummy.Radius, center, splashRadius))
                {
                    dummy.TakeDamage(Damage, transform.position);
                    dummy.ApplyEffects(impactSlow, impactBurn, impactDuration);
                    hit = true;
                }
            }

            if (hit)
            {
                PlayImpactFx();
            }

            if (hit && healOnHit > 0f && world.Player != null)
            {
                world.Player.Health.Heal(healOnHit);
            }
        }

        private void PlayImpactFx()
        {
            Color tint = VfxStyles.Tint(lookStyle);
            // User 2026-11-06: no circle drawn on the enemy when shots land — explosion sprite + sparks only.
            VfxHub.Impact(transform.position, tint, splashRadius > 0f ? 1.4f : 1f);
        }

        public void ResetPool()
        {
            if (view != null)
            {
                view.End();
            }

            Target = null;
            splashRadius = 0f;
            pierceLeft = 0;
            healOnHit = 0f;
            hitList.Clear();
            gameObject.SetActive(false);
        }
    }
}
