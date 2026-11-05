using System;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public struct CombatSetup
    {
        public PlayerStats Stats;
        public string SpiritId;
        public DogTier DogTier;
        public bool Mounted;
        public bool Showcase;
        public DayNightClock Clock;
        public string[] EquippedOutfitIds;
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
        public float PlayerShield => world != null && world.Player != null ? world.Player.Health.Shield : 0f;
        public float CommandCooldown01 => world != null && world.Spirit != null
            ? world.Spirit.CommandCooldown01
            : 0f;
        public GirlRig Girl => world != null && world.Player != null ? world.Player.Rig : null;
        public DogRig Dog => world != null && world.Dog != null ? world.Dog.Rig : null;

        public event Action FloorCleared;
        public event Action PlayerDied;
        public event Action<float> BossHealthChanged;
        public event Action<Vector3> BossDefeatedAt;

        private CombatWorld world;
        private CombatSetup setup;
        private bool floorComplete;
        private TrainingDummy dummy;
        private CombatCamera combatCamera;
        private static CombatSession activeSession;
        private static float pendingHp;
        private static float pendingMaxHp;
        private static bool hasPendingHp;

        public Vector3 PlayerPosition => world != null && world.Player != null
            ? world.Player.transform.position
            : transform.position;

        public static CombatSession Create(Transform parent, CombatSetup combatSetup)
        {
            CapturePendingHealth();
            GameObject root = new GameObject("CombatSession");
            root.transform.SetParent(parent, false);
            CombatSession session = root.AddComponent<CombatSession>();
            session.setup = combatSetup;
            session.Build();
            activeSession = session;
            return session;
        }

        private void Build()
        {
            CharacterFactory.CreateArena(transform, CombatTuning.ArenaWidth, CombatTuning.ArenaDepth, setup.Showcase);
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

            GameObject dogObject = new GameObject("Dog");
            dogObject.transform.SetParent(worldObject.transform, false);
            world.Dog = dogObject.AddComponent<DogController>();
            world.Dog.Init(world, world.Player, setup.Clock);
            world.Dog.SetTier(setup.DogTier);
            world.Dog.SetMounted(setup.Mounted);
            world.Dog.SetForm(setup.Clock == null ? DayPhase.Day : setup.Clock.Phase);

            GameObject spiritObject = new GameObject("Spirit");
            spiritObject.transform.SetParent(worldObject.transform, false);
            world.Spirit = spiritObject.AddComponent<SpiritController>();
            world.Spirit.Init(world, world.Player, setup.Clock);
            world.Spirit.SetSpirit(setup.SpiritId);
            world.Player.OnDied += HandlePlayerDied;

            combatCamera = CombatCamera.Create(transform, world.Player.transform);
            CombatFeedback.Create(transform, combatCamera.GetComponent<Camera>());
        }

        public void SetMoveInput(Vector2 value)
        {
            if (world != null && world.Player != null)
            {
                world.Player.MoveInput = Vector2.ClampMagnitude(value, 1f);
            }
        }

        public bool TryCommand()
        {
            return world != null && world.Spirit != null && world.Spirit.TryCommand();
        }

        public void StartFloor(FloorDefinition floor)
        {
            if (floor.Index > 1 && hasPendingHp && world.Player != null)
            {
                float increase = Mathf.Max(0f, world.Player.Health.Max - pendingMaxHp);
                world.Player.RestoreHealth(pendingHp + increase);
                hasPendingHp = false;
            }
            else if (floor.Index == 1)
            {
                hasPendingHp = false;
            }
            world.ClearEnemies();
            world.ClearProjectiles();
            floorComplete = false;
            world.SetFloor(floor);
            BossHealthChanged?.Invoke(0f);
        }

        private static void CapturePendingHealth()
        {
            if (activeSession == null || activeSession.world == null || activeSession.world.Player == null)
            {
                return;
            }

            pendingHp = activeSession.world.Player.Health.Current;
            pendingMaxHp = activeSession.world.Player.Health.Max;
            hasPendingHp = true;
        }

        public void ApplyStats(PlayerStats stats)
        {
            if (world != null && world.Player != null)
            {
                world.Player.SetStats(stats);
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

        internal void NotifyBossDefeated(Vector3 position)
        {
            BossHealthChanged?.Invoke(0f);
            BossDefeatedAt?.Invoke(position);
        }

        private void HandlePlayerDied()
        {
            Running = false;
            PlayerDied?.Invoke();
        }
    }
}
