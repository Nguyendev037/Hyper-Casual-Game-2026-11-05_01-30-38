using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed class SpiritController : MonoBehaviour
    {
        private CombatWorld world;
        private PlayerController owner;
        private DayNightClock clock;
        private SpiritDefinition definition;
        private readonly Dictionary<string, GameObject> spiritVisuals = new Dictionary<string, GameObject>();
        private float autoTimer;
        private float commandTimer;

        public float CommandCooldown01 => owner == null || owner.Stats.CommandCooldown <= 0f
            ? 0f
            : Mathf.Clamp01(commandTimer / owner.Stats.CommandCooldown);

        public void Init(CombatWorld combatWorld, PlayerController player, DayNightClock dayNightClock)
        {
            world = combatWorld;
            owner = player;
            clock = dayNightClock;
            spiritVisuals.Add("spirit.dew", CharacterFactory.CreateSpirit("spirit.dew", transform));
            spiritVisuals.Add("spirit.ember", CharacterFactory.CreateSpirit("spirit.ember", transform));
            spiritVisuals.Add("spirit.steam", CharacterFactory.CreateSpirit("spirit.steam", transform));
            foreach (GameObject visual in spiritVisuals.Values)
            {
                visual.SetActive(false);
            }
        }

        public void SetSpirit(string spiritId)
        {
            definition = string.IsNullOrEmpty(spiritId) ? null : Catalog.GetSpirit(spiritId);
            foreach (GameObject visual in spiritVisuals.Values)
            {
                visual.SetActive(false);
            }

            if (definition != null && spiritVisuals.TryGetValue(definition.Id, out GameObject spiritVisual))
            {
                spiritVisual.SetActive(true);
            }
        }

        public void Step(float deltaTime)
        {
            commandTimer = Mathf.Max(0f, commandTimer - deltaTime);
            transform.position = owner.transform.position +
                new Vector3(Mathf.Sin(Time.time * 2f) * 0.6f, 1.4f, Mathf.Cos(Time.time * 2f) * 0.6f);
            if (definition == null)
            {
                return;
            }

            autoTimer -= deltaTime;
            EnemyController target = world.NearestEnemy(owner.transform.position, owner.Stats.Range);
            if (target == null || autoTimer > 0f)
            {
                return;
            }

            float multiplier = ElementMultiplier();
            Projectile projectile = world.Fire(ProjectileOwner.Player, transform.position,
                CombatMath.Direction(transform.position, target.transform.position), definition.AutoDamage * multiplier,
                CombatTuning.SpiritProjectileSpeed, target, Palette.ForElement(definition.Element));
            projectile.SetImpact(definition.AutoRadius, definition.AutoSlow, definition.AutoBurnDps * owner.Stats.BurnPower,
                CombatTuning.AutoEffectDuration);
            autoTimer = definition.AutoInterval;
        }

        public bool TryCommand()
        {
            if (definition == null || commandTimer > 0f)
            {
                return false;
            }

            EnemyController target = world.NearestEnemy(owner.transform.position, owner.Stats.Range);
            Vector3 center = target != null ? target.transform.position : owner.transform.position;
            float multiplier = ElementMultiplier();
            for (int i = world.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = world.Enemies[i];
                if (enemy != null && CombatMath.Overlap(enemy.transform.position, enemy.Radius, center,
                    definition.CommandRadius))
                {
                    enemy.TakeDamage(definition.CommandDamage * multiplier, center);
                    enemy.ApplyEffects(definition.CommandSlow, 0f, CombatTuning.CommandEffectDuration);
                }
            }

            EnemyController dummy = world.DummyTarget;
            if (dummy != null && dummy.Alive &&
                CombatMath.Overlap(dummy.transform.position, dummy.Radius, center, definition.CommandRadius))
            {
                dummy.TakeDamage(definition.CommandDamage * multiplier, center);
                dummy.ApplyEffects(definition.CommandSlow, 0f, CombatTuning.CommandEffectDuration);
            }

            owner.Health.AddShield(definition.CommandShield * owner.Stats.ShieldPower);
            world.PlayBurst(Palette.ForElement(definition.Element), Mathf.Max(0.8f, definition.CommandRadius), center);
            commandTimer = owner.Stats.CommandCooldown;
            return true;
        }

        private float ElementMultiplier()
        {
            return DayNightClock.ElementMultiplier(definition.Element, clock == null ? DayPhase.Day : clock.Phase);
        }
    }
}
