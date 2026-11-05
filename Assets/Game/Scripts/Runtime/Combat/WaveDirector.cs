using System;
using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Combat
{
    public sealed class WaveDirector
    {
        private static readonly Vector3[] SpawnPoints =
        {
            new Vector3(-CombatTuning.SpawnEdgeX, 0f, -CombatTuning.SpawnEdgeZ),
            new Vector3(0f, 0f, -CombatTuning.SpawnEdgeZ),
            new Vector3(CombatTuning.SpawnEdgeX, 0f, -CombatTuning.SpawnEdgeZ),
            new Vector3(-CombatTuning.SpawnEdgeX, 0f, CombatTuning.SpawnEdgeZ),
            new Vector3(0f, 0f, CombatTuning.SpawnEdgeZ),
            new Vector3(CombatTuning.SpawnEdgeX, 0f, CombatTuning.SpawnEdgeZ)
        };

        private readonly CombatWorld world;
        private readonly Action onClear;
        private FloorDefinition floor;
        private int nextWave;
        private float waveElapsed;
        private float nextDelay;
        private bool firstWaveSpawned;
        private bool floorNotified;

        public WaveDirector(CombatWorld combatWorld, Action clearCallback)
        {
            world = combatWorld;
            onClear = clearCallback;
        }

        public void Start(FloorDefinition floorDefinition)
        {
            floor = floorDefinition;
            nextWave = 0;
            waveElapsed = 0f;
            nextDelay = 0f;
            firstWaveSpawned = false;
            floorNotified = false;
        }

        public void Step(float deltaTime)
        {
            if (floor == null || floorNotified)
            {
                return;
            }

            if (!firstWaveSpawned)
            {
                Spawn(floor.Waves[nextWave]);
                nextWave++;
                nextDelay = nextWave < floor.Waves.Count
                    ? floor.Waves[nextWave].DelayAfterPrevious
                    : 0f;
                firstWaveSpawned = true;
                return;
            }

            waveElapsed += deltaTime;
            bool previousWaveDead = world.Enemies.Count == 0;
            if (nextWave < floor.Waves.Count && (previousWaveDead || waveElapsed >= nextDelay))
            {
                Spawn(floor.Waves[nextWave]);
                nextWave++;
                nextDelay = nextWave < floor.Waves.Count
                    ? floor.Waves[nextWave].DelayAfterPrevious
                    : 0f;
                waveElapsed = 0f;
                return;
            }

            if (nextWave >= floor.Waves.Count && world.Enemies.Count == 0)
            {
                floorNotified = true;
                onClear?.Invoke();
            }
        }

        private void Spawn(WaveDefinition wave)
        {
            int index = 0;
            foreach ((EnemyKind kind, int count) in wave.Spawns)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 spawn = SpawnPoints[(index + nextWave * 2) % SpawnPoints.Length];
                    if (CombatMath.Flat(spawn - world.Player.transform.position).sqrMagnitude <
                        CombatTuning.SpawnSafetyRadius * CombatTuning.SpawnSafetyRadius)
                    {
                        spawn = -spawn;
                    }

                    world.SpawnEnemy(kind, spawn);
                    index++;
                }
            }
        }
    }
}
