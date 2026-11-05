using System;
using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public struct CombatSetup
    {
        public PlayerStats Stats;
        /// <summary>Single-spirit shortcut (showcase). Ignored when Spirits is set.</summary>
        public string SpiritId;
        /// <summary>Spirits of the run (slots, level). null = use SpiritId.</summary>
        public IReadOnlyList<RunSpiritEntry> Spirits;
        public DogTier DogTier;
        public bool Mounted;
        public bool Showcase;
        /// <summary>Small station arena with three function tiles instead of the survival arena.</summary>
        public bool Station;
        public IList<StationTile> StationTiles;
        public DayNightClock Clock;
        public string[] EquippedOutfitIds;
        /// <summary>Starting HP; 0 or less = full.</summary>
        public float StartHp;
    }

    public sealed class CombatSession : MonoBehaviour
    {
        public bool Running
        {
            get => world != null && world.Running;
            set
            {
                if (world != null)
                {
                    world.Running = value;
                }
            }
        }

        public float PlayerHp01 => world != null && world.Player != null
            ? world.Player.Health.Current / world.Player.Health.Max
            : 0f;
        public float PlayerHp => world != null && world.Player != null ? world.Player.Health.Current : 0f;
        public float PlayerMaxHp => world != null && world.Player != null ? world.Player.Health.Max : 0f;
        public float PlayerShield => world != null && world.Player != null ? world.Player.Health.Shield : 0f;
        public float CommandCooldown01 => world != null && world.Spirit != null ? world.Spirit.CommandCooldown01 : 0f;
        public GirlRig Girl => world != null && world.Player != null ? world.Player.Rig : null;
        public DogRig Dog => world != null && world.Dog != null ? world.Dog.Rig : null;
        public StationView Station => world != null ? world.Station : null;
        public bool IsStation => setup.Station;
        public int CurrentFloor => world != null ? world.CurrentFloor : 0;
        /// <summary>Seconds left on a timed floor (0 on boss floors).</summary>
        public float TimeLeft => world != null && world.Director != null ? world.Director.TimeLeft : 0f;
        public bool FloorTimed => world != null && world.Director != null && world.Director.Timed;
        public bool FloorEnding => world != null && world.Director != null && world.Director.Ending;
        public int GemsPicked => world != null && world.Gems != null ? world.Gems.Picked : 0;
        public int GemsOnField => world != null && world.Gems != null ? world.Gems.Count : 0;
        public int EnemiesAlive => world != null ? world.RegularAlive : 0;
        public int SpiritCount => world != null && world.Spirit != null ? world.Spirit.Count : 0;
        public int SpiritCasts => world != null && world.Spirit != null ? world.Spirit.CastsDone : 0;
        public bool GodMode
        {
            get => world != null && world.Player != null && world.Player.GodMode;
            set
            {
                if (world != null && world.Player != null)
                {
                    world.Player.GodMode = value;
                }
            }
        }

        public event Action FloorCleared;
        public event Action PlayerDied;
        public event Action<float> BossHealthChanged;
        public event Action<Vector3> BossDefeatedAt;
        public event Action<int> XpCollected;
        public event Action<string> Noticed;
        public event Action DayNightForced;
        public event Action<int, StationTile> StationTileActivated;
        public event Action StationExit;

        private CombatWorld world;
        private CombatSetup setup;
        private bool floorComplete;
        private TrainingDummy dummy;
        private CombatCamera combatCamera;

        public Vector3 PlayerPosition => world != null && world.Player != null
            ? world.Player.transform.position
            : transform.position;

        public static CombatSession Create(Transform parent, CombatSetup combatSetup)
        {
            GameObject root = new GameObject("CombatSession");
            root.transform.SetParent(parent, false);
            CombatSession session = root.AddComponent<CombatSession>();
            session.setup = combatSetup;
            session.Build();
            return session;
        }

        private void Build()
        {
            float width = CombatTuning.ArenaWidth;
            float depth = CombatTuning.ArenaDepth;
            if (setup.Station)
            {
                width = CombatTuning.StationSize;
                depth = CombatTuning.StationSize;
                CombatMath.SetLimits(CombatTuning.StationLimit, CombatTuning.StationLimit);
            }
            else if (setup.Showcase)
            {
                width = 12f;
                depth = 20f;
                CombatMath.SetLimits(5.6f, 9.6f);
            }
            else
            {
                CombatMath.SetLimits(CombatTuning.ArenaLimitX, CombatTuning.ArenaLimitZ);
            }

            CharacterFactory.CreateArena(transform, width, depth, setup.Showcase);
            GameObject worldObject = new GameObject("CombatWorld");
            worldObject.transform.SetParent(transform, false);
            world = worldObject.AddComponent<CombatWorld>();
            world.Session = this;
            world.Init();
            CharacterFactory.CreateCage(transform, false);

            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(worldObject.transform, false);
            world.Player = playerObject.AddComponent<PlayerController>();
            world.Player.Init(world, setup.Stats, setup.EquippedOutfitIds);
            if (setup.StartHp > 0f)
            {
                world.Player.RestoreHealth(Mathf.Min(setup.StartHp, world.Player.Health.Max));
            }

            if (!setup.Showcase && !setup.Station)
            {
                playerObject.transform.position = new Vector3(0f, 0f, -8f);
            }

            GameObject dogObject = new GameObject("Dog");
            dogObject.transform.SetParent(worldObject.transform, false);
            dogObject.transform.position = playerObject.transform.position;
            world.Dog = dogObject.AddComponent<DogController>();
            world.Dog.Init(world, world.Player, setup.Clock);
            world.Dog.SetTier(setup.DogTier);
            world.Dog.SetMounted(setup.Mounted);
            world.Dog.SetForm(setup.Clock == null ? DayPhase.Day : setup.Clock.Phase);

            GameObject spiritObject = new GameObject("Spirits");
            spiritObject.transform.SetParent(worldObject.transform, false);
            world.Spirit = spiritObject.AddComponent<SpiritController>();
            world.Spirit.Init(world, world.Player, setup.Clock);
            if (setup.Spirits != null)
            {
                world.Spirit.SetSpirits(setup.Spirits);
            }
            else
            {
                world.Spirit.SetSpirit(setup.SpiritId);
            }

            world.Player.OnDied += HandlePlayerDied;

            combatCamera = CombatCamera.Create(transform, world.Player.transform);
            CombatFeedback.Create(transform, combatCamera.GetComponent<Camera>());

            if (setup.Station)
            {
                StationView station = new StationView(world, transform);
                station.Build(setup.StationTiles ?? new List<StationTile>(), combatCamera.GetComponent<Camera>());
                station.TileActivated += (index, kind) => StationTileActivated?.Invoke(index, kind);
                station.ExitReached += () => StationExit?.Invoke();
                world.Station = station;
            }
        }

        public void SetMoveInput(Vector2 value)
        {
            if (world != null && world.Player != null)
            {
                world.Player.MoveInput = Vector2.ClampMagnitude(value, 1f);
            }
        }

        /// <summary>Legacy hook of the old spirit button; spirits cast by themselves now.</summary>
        public bool TryCommand()
        {
            return false;
        }

        public void StartFloor(int floor)
        {
            world.ClearEnemies();
            world.ClearProjectiles();
            world.Gems.Clear();
            world.Storm.End();
            floorComplete = false;
            if (FloorPlan.Kind(floor) != FloorKind.Normal)
            {
                world.Player.transform.position = new Vector3(0f, 0f, -8f);
                combatCamera.Snap();
            }

            world.SetFloor(floor);
            BossHealthChanged?.Invoke(0f);
        }

        public void ApplyStats(PlayerStats stats)
        {
            if (world != null && world.Player != null)
            {
                world.Player.SetStats(stats);
            }
        }

        public void SetSpirits(IReadOnlyList<RunSpiritEntry> slots)
        {
            if (world != null && world.Spirit != null)
            {
                world.Spirit.SetSpirits(slots);
            }
        }

        public void SetSpirit(string spiritId)
        {
            if (world != null && world.Spirit != null)
            {
                world.Spirit.SetSpirit(spiritId);
            }
        }

        public void SetDogTier(DogTier tier)
        {
            if (world != null && world.Dog != null)
            {
                world.Dog.SetTier(tier);
            }
        }

        public void SetMounted(bool mounted)
        {
            if (world != null && world.Dog != null)
            {
                world.Dog.SetMounted(mounted);
            }
        }

        public void SetLionForm(DayPhase phase)
        {
            if (world != null && world.Dog != null)
            {
                world.Dog.SetForm(phase);
            }
        }

        public void SpawnDummy(Vector3 position)
        {
            if (dummy != null)
            {
                return;
            }

            GameObject dummyObject = CharacterFactory.CreateDummy(transform);
            dummyObject.transform.position = position;
            dummy = dummyObject.AddComponent<TrainingDummy>();
            dummy.Init();
            EnemyController target = dummyObject.AddComponent<EnemyController>();
            target.ConfigureDummy(world, dummy);
            world.DummyTarget = target;
        }

        public void Heal(float amount)
        {
            if (world != null && world.Player != null)
            {
                world.Player.Health.Heal(amount);
            }
        }

        public void HealFraction(float fractionOfMax)
        {
            if (world != null && world.Player != null)
            {
                world.Player.Health.Heal(world.Player.Health.Max * fractionOfMax);
            }
        }

        public void PlayBurstAtPlayer(Color color, float radius)
        {
            if (world != null && world.Player != null)
            {
                world.PlayBurst(color, radius, world.Player.transform.position);
            }
        }

        /// <summary>Nearest living anchor (Blue Hair Guy phase 3) to the girl, or null.</summary>
        public EnemyController FindAnchor()
        {
            if (world == null || world.Player == null)
            {
                return null;
            }

            EnemyController best = null;
            float bestDistance = float.MaxValue;
            Vector3 p = world.Player.transform.position;
            for (int i = 0; i < world.Enemies.Count; i++)
            {
                EnemyController e = world.Enemies[i];
                if (e == null || !e.Alive || !e.IsAnchor)
                {
                    continue;
                }

                float d = CombatMath.Flat(e.transform.position - p).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = e;
                }
            }

            return best;
        }

        /// <summary>Debug: the girl dies now (defeat flow test).</summary>
        public void DebugKillPlayer()
        {
            if (world != null && world.Player != null)
            {
                world.Player.Health.Damage(world.Player.Health.Max * 10f + world.Player.Health.Shield, world.Player.transform.position);
            }
        }

        /// <summary>Debug: ends the current floor as if its time ran out / the boss fell.</summary>
        public void DebugEndFloor()
        {
            if (world != null && world.Director != null)
            {
                world.Director.ForceEnd();
            }
        }

        /// <summary>Boss currently on the field (first one), or null.</summary>
        public EnemyController FindBoss()
        {
            if (world == null)
            {
                return null;
            }

            for (int i = 0; i < world.Enemies.Count; i++)
            {
                EnemyController e = world.Enemies[i];
                if (e != null && e.Alive && e.IsBoss)
                {
                    return e;
                }
            }

            return null;
        }

        /// <summary>Position of the nearest living gem-less target for the autopilot: enemy/anchor/boss; null if none.</summary>
        public EnemyController FindNearestEnemy(float range)
        {
            return world == null || world.Player == null ? null : world.NearestEnemy(world.Player.transform.position, range);
        }

        public Vector3? NearestGemPosition()
        {
            return world == null || world.Gems == null ? (Vector3?)null : world.Gems.NearestTo(PlayerPosition);
        }

        /// <summary>Position of an enemy within radius of the girl, summed for flee logic (autopilot).</summary>
        public Vector3 EnemyPressure(float radius)
        {
            Vector3 sum = Vector3.zero;
            if (world == null || world.Player == null)
            {
                return sum;
            }

            Vector3 p = world.Player.transform.position;
            for (int i = 0; i < world.Enemies.Count; i++)
            {
                EnemyController e = world.Enemies[i];
                if (e == null || !e.Alive || e.IsAnchor)
                {
                    continue;
                }

                Vector3 d = CombatMath.Flat(p - e.transform.position);
                float m = d.magnitude;
                if (m < radius && m > 0.01f)
                {
                    sum += d / m * (1f - m / radius);
                }
            }

            return sum;
        }

        internal void ClearFloor()
        {
            if (floorComplete)
            {
                return;
            }

            floorComplete = true;
            FloorCleared?.Invoke();
        }

        internal void NotifyBoss(float health01)
        {
            BossHealthChanged?.Invoke(Mathf.Clamp01(health01));
        }

        internal void NotifyBossDefeated(Vector3 position, EnemyKind kind)
        {
            BossHealthChanged?.Invoke(0f);
            BossDefeatedAt?.Invoke(position);
            if (world != null && world.Director != null)
            {
                world.Director.BossDefeated();
            }
        }

        internal void NotifyXp(int value)
        {
            XpCollected?.Invoke(value);
        }

        internal void Notice(string message)
        {
            Noticed?.Invoke(message);
        }

        internal void RequestDayNightToggle()
        {
            DayNightForced?.Invoke();
        }

        private void HandlePlayerDied()
        {
            Running = false;
            PlayerDied?.Invoke();
        }
    }
}
