using System;
using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Combat
{
    /// <summary>Spawns enemies continuously from FloorPlan.Budget and ends the floor (SPEC-P01 §3).
    /// Timed floors end at Duration; guardian/boss floors end when the boss is down.</summary>
    public sealed class FloorDirector
    {
        private enum State
        {
            Fighting,
            Collecting,
            Done
        }

        private const float BossFloorSpawnWindow = 60f;
        private const float BossSpawnZ = 14f;
        private const float CollectTimeout = 3f;

        private readonly CombatWorld world;
        private readonly int floor;
        private readonly Action onClear;
        private readonly SpawnBudget budget;
        private readonly FloorKind kind;
        private State state;
        private float elapsed;
        private float collectElapsed;
        private int spawned;
        private bool bossDown;
        private bool bossSpawned;

        public int Floor => floor;
        public bool Timed => budget.Duration > 0f;
        public float TimeLeft => Timed ? Mathf.Max(0f, budget.Duration - elapsed) : 0f;
        public float Elapsed => elapsed;
        public bool Finished => state == State.Done;
        public bool Ending => state != State.Fighting;
        public SpawnBudget Budget => budget;

        public FloorDirector(CombatWorld ownerWorld, int floorIndex, Action clearCallback)
        {
            world = ownerWorld;
            floor = floorIndex;
            onClear = clearCallback;
            budget = FloorPlan.Budget(floorIndex);
            kind = FloorPlan.Kind(floorIndex);
        }

        public void Start()
        {
            state = State.Fighting;
            elapsed = 0f;
            spawned = 0;
            bossDown = false;
            bossSpawned = false;
            world.Gems.StopPullAll();
        }

        public void BossDefeated()
        {
            bossDown = true;
        }

        /// <summary>Debug: end the floor now (stones are still collected).</summary>
        public void ForceEnd()
        {
            if (state == State.Fighting)
            {
                BeginEnd();
            }
        }

        public void Step(float deltaTime)
        {
            if (state == State.Done)
            {
                return;
            }

            if (state == State.Collecting)
            {
                collectElapsed += deltaTime;
                if (world.Gems.Count == 0 || collectElapsed >= CollectTimeout)
                {
                    world.Gems.AbsorbAll();
                    world.Gems.StopPullAll();
                    state = State.Done;
                    onClear?.Invoke();
                }

                return;
            }

            elapsed += deltaTime;
            if (kind != FloorKind.Normal && !bossSpawned)
            {
                SpawnBoss();
            }

            SpawnDue();
            if (Timed && elapsed >= budget.Duration)
            {
                BeginEnd();
            }
            else if (kind != FloorKind.Normal && bossDown)
            {
                BeginEnd();
            }
        }

        private void BeginEnd()
        {
            state = State.Collecting;
            collectElapsed = 0f;
            world.ClearEnemyProjectiles();
            world.Storm.End();
            world.DissolveEnemies();
            world.Gems.PullAll();
        }

        private void SpawnBoss()
        {
            bossSpawned = true;
            EnemyKind? boss = FloorPlan.BossKind(floor);
            if (!boss.HasValue)
            {
                return;
            }

            Vector3 position = new Vector3(0f, 0f, BossSpawnZ);
            EnemySpawner.Spawn(world, boss.Value, position, budget.HpMult, budget.DmgMult, false);
        }

        // Even spread over 80% of the floor time (boss floors: over a fixed 60 s window).
        private void SpawnDue()
        {
            float window = Timed ? budget.Duration * SurvivorTuning.SpawnTimeFraction : BossFloorSpawnWindow;
            int target = Mathf.Min(budget.Total, Mathf.FloorToInt(budget.Total * (elapsed / window)) + 1);
            int guard = 0;
            while (spawned < target && guard++ < 64)
            {
                if (world.RegularAlive >= SurvivorTuning.MaxAliveEnemies)
                {
                    return;
                }

                spawned += SpawnOne();
            }
        }

        /// <summary>Spawns one weighted enemy of the chapter roster; returns how many bodies it counted (wolves come in pairs).</summary>
        private int SpawnOne()
        {
            float total = budget.BeetleW + budget.EyeW + budget.SentinelW;
            float roll = UnityEngine.Random.value * total;
            EnemyKind enemyKind = budget.ChaserKind;
            if (roll >= budget.BeetleW)
            {
                enemyKind = roll < budget.BeetleW + budget.EyeW ? budget.ShooterKind : budget.SpecialKind;
            }

            bool elite = budget.EliteChance > 0f && UnityEngine.Random.value < budget.EliteChance;
            Vector3 spot = world.PickSpawnPosition();
            EnemySpawner.Spawn(world, enemyKind, spot, budget.HpMult, budget.DmgMult, elite);
            if (enemyKind == EnemyKind.ShadowWolf && world.RegularAlive < SurvivorTuning.MaxAliveEnemies)
            {
                Vector3 mate = CombatMath.ClampArena(spot + new Vector3(Catalog.WolfPairOffset, 0f, 0f));
                EnemySpawner.Spawn(world, enemyKind, mate, budget.HpMult, budget.DmgMult, false);
                return 2;
            }

            return 1;
        }
    }
}
