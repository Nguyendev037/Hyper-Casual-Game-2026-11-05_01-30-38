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
        private Vector3 facing = Vector3.forward;
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

        public void TakeDamage(float amount, Vector3 from)
        {
            if (invulnerabilityTimer > 0f)
            {
                return;
            }

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
            retargetTimer -= deltaTime;
            if (target == null || !target.Alive ||
                CombatMath.Flat(target.transform.position - transform.position).sqrMagnitude > Stats.Range * Stats.Range)
            {
                target = null;
            }

            if (target == null && retargetTimer <= 0f)
            {
                target = world.NearestEnemy(transform.position, Stats.Range);
            retargetTimer = CombatTuning.TargetRefreshInterval;
            }

            attackTimer -= deltaTime;
            if (target != null && attackTimer <= 0f)
            {
                Vector3 direction = CombatMath.Direction(Rig.StaffTip.position, target.transform.position);
                Rig.PlayCast();
                spellRing.transform.position = target.transform.position + Vector3.up * 0.08f;
                spellRing.SetActive(true);
                world.Fire(ProjectileOwner.Player, Rig.StaffTip.position, direction, Stats.StaffDamage,
                    CombatTuning.PlayerProjectileSpeed, target,
                    Palette.LightGold);
                CombatFeedback.Play("shoot");
                attackTimer = Stats.AttackInterval;
            }

            if (input.sqrMagnitude < 0.001f && target != null)
            {
                facing = CombatMath.Direction(transform.position, target.transform.position);
            }

            if (facing.sqrMagnitude > 0.001f)
            {
                Rig.Visual.rotation = Quaternion.LookRotation(facing);
            }
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
