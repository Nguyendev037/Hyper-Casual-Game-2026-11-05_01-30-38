using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Up to 3 spirits circling the girl; each shoots by itself and casts its skill by itself (SPEC-P02 §2).</summary>
    public sealed class SpiritController : MonoBehaviour
    {
        private sealed class Instance
        {
            public string Id;
            public int Level;
            public SpiritDefinition Definition;
            public GameObject Visual;
            public float AutoTimer;
            public float SkillTimer;
        }

        private CombatWorld world;
        private PlayerController owner;
        private DayNightClock clock;
        private readonly List<Instance> spirits = new List<Instance>(SurvivorTuning.SpiritSlots);
        private readonly Dictionary<string, GameObject> visuals = new Dictionary<string, GameObject>();
        private float lastCast = -100f;
        private float orbit;

        public int Count => spirits.Count;
        /// <summary>Seconds of simulation at the last skill cast of any spirit (test/debug).</summary>
        public float LastCastTime => lastCast;
        public int CastsDone { get; private set; }
        public float CommandCooldown01 => 0f;

        public void Init(CombatWorld combatWorld, PlayerController player, DayNightClock dayNightClock)
        {
            world = combatWorld;
            owner = player;
            clock = dayNightClock;
        }

        /// <summary>Synchronises with the run's slots. Spirits that stay keep their timers; new ones start late.</summary>
        public void SetSpirits(IReadOnlyList<RunSpiritEntry> slots)
        {
            List<Instance> next = new List<Instance>(SurvivorTuning.SpiritSlots);
            int count = slots == null ? 0 : Mathf.Min(slots.Count, SurvivorTuning.SpiritSlots);
            for (int i = 0; i < count; i++)
            {
                RunSpiritEntry entry = slots[i];
                Instance existing = spirits.Find(s => s.Id == entry.spiritId);
                if (existing == null)
                {
                    SpiritDefinition def = Catalog.GetSpirit(entry.spiritId);
                    if (def == null)
                    {
                        continue;
                    }

                    existing = new Instance();
                    existing.Id = entry.spiritId;
                    existing.Definition = def;
                    existing.AutoTimer = def.AutoInterval;
                    // first skill: half a cooldown plus a per-slot offset so two spirits never fire together
                    existing.SkillTimer = def.CommandCooldown * 0.5f + i * CombatTuning.SpiritCastGap;
                }

                existing.Level = Mathf.Clamp(entry.level, 1, SurvivorTuning.SpiritMaxLevel);
                next.Add(existing);
            }

            foreach (Instance old in spirits)
            {
                if (!next.Contains(old) && old.Visual != null)
                {
                    old.Visual.SetActive(false);
                }
            }

            spirits.Clear();
            spirits.AddRange(next);
            foreach (Instance s in spirits)
            {
                s.Visual = VisualFor(s.Id);
                s.Visual.SetActive(true);
            }
        }

        public void SetSpirit(string spiritId)
        {
            List<RunSpiritEntry> one = new List<RunSpiritEntry>();
            if (!string.IsNullOrEmpty(spiritId) && Catalog.GetSpirit(spiritId) != null)
            {
                RunSpiritEntry e = new RunSpiritEntry();
                e.spiritId = spiritId;
                e.level = 1;
                one.Add(e);
            }

            SetSpirits(one);
        }

        private GameObject VisualFor(string id)
        {
            GameObject go;
            if (!visuals.TryGetValue(id, out go) || go == null)
            {
                go = CharacterFactory.CreateSpirit(id, transform);
                visuals[id] = go;
            }

            return go;
        }

        public void Step(float deltaTime)
        {
            orbit += deltaTime * 2f;
            Vector3 center = owner.transform.position;
            int n = spirits.Count;
            for (int i = 0; i < n; i++)
            {
                Instance s = spirits[i];
                float angle = orbit + i * Mathf.PI * 2f / Mathf.Max(1, n);
                Vector3 position = center + new Vector3(Mathf.Sin(angle) * CombatTuning.SpiritOrbitRadius,
                    CombatTuning.SpiritOrbitHeight, Mathf.Cos(angle) * CombatTuning.SpiritOrbitRadius);
                if (s.Visual != null)
                {
                    s.Visual.transform.position = position;
                }

                StepAuto(s, position, deltaTime);
                StepSkill(s, deltaTime);
            }
        }

        private void StepAuto(Instance s, Vector3 position, float deltaTime)
        {
            s.AutoTimer -= deltaTime;
            if (s.AutoTimer > 0f)
            {
                return;
            }

            EnemyController target = world.NearestEnemy(owner.transform.position, owner.Stats.Range);
            if (target == null)
            {
                return;
            }

            SpiritDefinition d = s.Definition;
            float damage = d.AutoDamage * SurvivorTuning.SpiritDamageMult(s.Level) * owner.Stats.SpiritPower *
                ElementMultiplier(d);
            Projectile projectile = world.Fire(ProjectileOwner.Player, position,
                CombatMath.Direction(position, target.transform.position), damage,
                CombatTuning.SpiritProjectileSpeed, target, Palette.ForElement(d.Element));
            projectile.SetRange(owner.Stats.Range);
            projectile.SetImpact(d.AutoRadius, d.AutoSlow, d.AutoBurnDps * owner.Stats.BurnPower,
                CombatTuning.AutoEffectDuration);
            projectile.SetHeal(d.AutoHeal * owner.Stats.WoodHealPower);
            s.AutoTimer = d.AutoInterval * SurvivorTuning.SpiritIntervalMult(s.Level);
        }

        private void StepSkill(Instance s, float deltaTime)
        {
            s.SkillTimer -= deltaTime;
            if (s.SkillTimer > 0f || world.SimTime - lastCast < CombatTuning.SpiritCastGap)
            {
                return;
            }

            EnemyController target = world.NearestEnemy(owner.transform.position, owner.Stats.Range);
            if (target == null)
            {
                return;
            }

            CombatFeedback.Play("spirit_cast");
            SpiritDefinition d = s.Definition;
            Vector3 center = target.transform.position;
            float damage = d.CommandDamage * SurvivorTuning.SpiritDamageMult(s.Level) * owner.Stats.SpiritPower *
                ElementMultiplier(d);
            for (int i = world.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = i < world.Enemies.Count ? world.Enemies[i] : null;
                if (enemy != null && enemy.Targetable && CombatMath.Overlap(enemy.transform.position, enemy.Radius,
                    center, d.CommandRadius))
                {
                    enemy.TakeDamage(damage, center);
                    enemy.ApplyEffects(d.CommandSlow, 0f, CombatTuning.CommandEffectDuration);
                }
            }

            EnemyController dummy = world.DummyTarget;
            if (dummy != null && dummy.Alive &&
                CombatMath.Overlap(dummy.transform.position, dummy.Radius, center, d.CommandRadius))
            {
                dummy.TakeDamage(damage, center);
                dummy.ApplyEffects(d.CommandSlow, 0f, CombatTuning.CommandEffectDuration);
            }

            if (d.CommandShield > 0f)
            {
                owner.Health.AddShield(d.CommandShield * owner.Stats.ShieldPower);
            }

            if (d.CommandHeal > 0f)
            {
                owner.Health.Heal(d.CommandHeal * owner.Stats.WoodHealPower);
            }

            world.PlayBurst(Palette.ForElement(d.Element), Mathf.Max(0.8f, d.CommandRadius), center);
            float cooldownScale = owner.Stats.CommandCooldown / Catalog.BaseCommandCooldown;
            s.SkillTimer = d.CommandCooldown * SurvivorTuning.SpiritCooldownMult(s.Level) * cooldownScale;
            lastCast = world.SimTime;
            CastsDone++;
        }

        private float ElementMultiplier(SpiritDefinition d)
        {
            return DayNightClock.ElementMultiplier(d.Element, clock == null ? DayPhase.Day : clock.Phase);
        }
    }
}
