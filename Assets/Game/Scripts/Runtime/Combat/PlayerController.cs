using System;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed class PlayerController : MonoBehaviour
    {
        public PlayerStats Stats { get; private set; }
        public Health Health { get; private set; }
        public GirlRig Rig { get; private set; }
        public Vector2 MoveInput { get; set; }
        public event Action OnDied;

        private CombatWorld world;
        private EnemyController target;
        private float retargetTimer;
        private float attackTimer;
        private float invulnerabilityTimer;
        private Vector3 facing = Vector3.back; // toward the camera at spawn
        private GameObject spellRing;

        public void Init(CombatWorld combatWorld, PlayerStats stats, string[] outfitIds)
        {
            world = combatWorld;
            Stats = stats;
            Rig = CharacterFactory.CreateGirl(transform);
            spellRing = VfxFactory.SpellRing(1.4f, Palette.LightGold);
            spellRing.transform.SetParent(transform, false);
            spellRing.AddComponent<AutoDisable>().Lifetime = 0.3f;
            spellRing.SetActive(false);
            Health = new Health(stats.MaxHp);
            Health.Died += () => OnDied?.Invoke();
            if (outfitIds != null)
            {
                ApplyOutfits(outfitIds);
            }
        }

        public void SetStats(PlayerStats stats)
        {
            float oldMaximum = Health.Max;
            Stats = stats;
            Health.SetMax(stats.MaxHp, stats.MaxHp > oldMaximum);
        }

        public void RestoreHealth(float hp)
        {
            Health.Restore(hp);
        }

        /// <summary>Debug only (autopilot boss test): the girl cannot be hurt.</summary>
        public bool GodMode { get; set; }

        /// <summary>Damage over time (storm): ignores the hit-invulnerability window, still respects armor.</summary>
        public void TakeContinuousDamage(float amount)
        {
            if (GodMode || amount <= 0f)
            {
                return;
            }

            Health.Damage(amount * (1f - Mathf.Clamp01(Stats.Armor)), transform.position);
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (invulnerabilityTimer > 0f || GodMode)
            {
                return;
            }

            amount *= 1f - Mathf.Clamp01(Stats.Armor);
            Health.Damage(amount, from);
            invulnerabilityTimer = CombatTuning.PlayerInvulnerability;
            CombatFeedback.Play("hurt");
            Rig.Flash(Palette.WarnRed, 0.12f);
        }

        public void Step(float deltaTime)
        {
            invulnerabilityTimer = Mathf.Max(0f, invulnerabilityTimer - deltaTime);
            Vector3 input = new Vector3(MoveInput.x, 0f, MoveInput.y);
            input = Vector3.ClampMagnitude(input, 1f);
            if (input.sqrMagnitude > 0.001f)
            {
                facing = input.normalized;
                transform.position = CombatMath.ClampArena(transform.position + input * Stats.MoveSpeed * deltaTime);
            }

            Rig.SetMoving(input.magnitude);
            if (Stats.Regen > 0f)
            {
                Health.Heal(Stats.Regen * deltaTime);
            }

            retargetTimer -= deltaTime;
            if (target == null || !target.Targetable ||
                CombatMath.Flat(target.transform.position - transform.position).sqrMagnitude > Stats.Range * Stats.Range)
            {
                target = null;
            }

            if (retargetTimer <= 0f)
            {
                target = world.NearestEnemy(transform.position, Stats.Range);
                retargetTimer = CombatTuning.TargetRefreshInterval;
            }

            attackTimer -= deltaTime;
            if (target != null && attackTimer <= 0f)
            {
                Vector3 direction = CombatMath.Direction(Rig.StaffTip.position, target.transform.position);
                Rig.PlayCast();
                // User 2026-11-06: no dashed circle on the targeted enemy when shooting (spellRing stays hidden).
                int count = Mathf.Max(1, Stats.ProjectileCount);
                VfxHub.Muzzle(Rig.StaffTip.position, VfxStyles.Tint(VfxStyle.Arcane));
                for (int i = 0; i < count; i++)
                {
                    float offset = (i - (count - 1) * 0.5f) * CombatTuning.MultishotSpreadDegrees;
                    Vector3 shotDirection = Quaternion.AngleAxis(offset, Vector3.up) * direction;
                    bool center = Mathf.Abs(offset) < 0.01f;
                    Projectile shot = world.Fire(ProjectileOwner.Player, Rig.StaffTip.position, shotDirection,
                        Stats.StaffDamage, CombatTuning.PlayerProjectileSpeed, center ? target : null,
                        Palette.LightGold, VfxStyle.Arcane);
                    shot.SetRange(Stats.Range);
                    shot.SetPierce(Stats.Pierce);
                }

                CombatFeedback.Play("shoot");
                attackTimer = Stats.AttackInterval;
            }

            if (input.sqrMagnitude < 0.001f && target != null)
            {
                facing = CombatMath.Direction(transform.position, target.transform.position);
            }

            // GirlRig turns smoothly; it owns the Visual rotation (it used to overwrite this every frame).
            Rig.SetFacing(facing);
        }

        private void ApplyOutfits(string[] outfitIds)
        {
            foreach (string outfitId in outfitIds)
            {
                OutfitDefinition outfit = Catalog.GetOutfit(outfitId);
                if (outfit != null)
                {
                    Rig.ApplyOutfit(outfit.Slot, outfit.AppearanceId);
                }
            }
        }
    }
}
