using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class CombatWorld : MonoBehaviour
    {
        public readonly List<EnemyController> Enemies = new List<EnemyController>(12);
        public PlayerController Player { get; set; }
        public DogController Dog { get; set; }
        public SpiritController Spirit { get; set; }
        public EnemyController DummyTarget { get; set; }
        public CombatSession Session { get; set; }
        public bool Running { get; set; }

        private readonly List<Projectile> projectiles = new List<Projectile>(40);
        private ObjectPool<Projectile> projectilePool;
        private readonly ObjectPool<EnemyController>[] enemyPools = new ObjectPool<EnemyController>[4];
        private ObjectPool<TelegraphView> discWarnings;
        private ObjectPool<TelegraphView> fanWarnings;
        private ObjectPool<TelegraphView> dashWarnings;
        private ObjectPool<GameObject> burstPool;
        private Mesh dashMesh;
        private readonly List<BurstLife> activeBursts = new List<BurstLife>(8);
        private WaveDirector waves;
        private long birthSequence;

        public void Init()
        {
            projectilePool = new ObjectPool<Projectile>(CreateProjectile, OnGetProjectile, OnReleaseProjectile,
                OnDestroyProjectile, false, 40, 40);
            for (int i = 0; i < enemyPools.Length; i++)
            {
                EnemyKind kind = (EnemyKind)i;
                enemyPools[i] = new ObjectPool<EnemyController>(() => CreateEnemy(kind), OnGetEnemy,
                    OnReleaseEnemy, OnDestroyEnemy, false, 12, 12);
            }
            discWarnings = CreateWarningPool(MeshBuilder.Disc(Catalog.WardenRingRadius, 32), 12);
            fanWarnings = CreateWarningPool(MeshBuilder.Fan(CombatTuning.WardenFanRadius,
                CombatTuning.WardenFanAngle, 24), 12);
            dashMesh = CreateDashWarningMesh();
            dashWarnings = CreateWarningPool(dashMesh, 12);
            burstPool = new ObjectPool<GameObject>(CreateBurst, item => item.SetActive(true),
                item => item.SetActive(false), item => Destroy(item), false, 8, 8);
            PrewarmPools();
            PrewarmEffects();
        }

        private void Update()
        {
            if (!Running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Player?.Step(deltaTime);
            Dog?.Step(deltaTime);
            Spirit?.Step(deltaTime);
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i] != null)
                {
                    Enemies[i].Step(deltaTime);
                }
            }

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = projectiles[i];
                if (projectile != null && projectile.gameObject.activeSelf)
                {
                    projectile.Step(deltaTime);
                }
            }

            for (int i = activeBursts.Count - 1; i >= 0; i--)
            {
                BurstLife burst = activeBursts[i];
                burst.Remaining -= deltaTime;
                if (burst.Remaining <= 0f)
                {
                    burstPool.Release(burst.Object);
                    activeBursts.RemoveAt(i);
                }
                else
                {
                    activeBursts[i] = burst;
                }
            }

            waves?.Step(deltaTime);
        }

        public EnemyController NearestEnemy(Vector3 position, float range)
        {
            EnemyController nearest = null;
            float bestDistance = range * range;
            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null || !enemy.Alive)
                {
                    continue;
                }

                float distance = CombatMath.Flat(enemy.transform.position - position).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    nearest = enemy;
                    bestDistance = distance;
                }
            }

            if (DummyTarget != null && DummyTarget.Alive)
            {
                float distance = CombatMath.Flat(DummyTarget.transform.position - position).sqrMagnitude;
                if (distance <= bestDistance)
                {
                    nearest = DummyTarget;
                }
            }

            return nearest;
        }

        public EnemyController SpawnEnemy(RHMG.Core.EnemyKind kind, Vector3 position)
        {
            EnemyController enemy = enemyPools[(int)kind].Get();
            enemy.Spawn(kind, position);
            Enemies.Add(enemy);
            return enemy;
        }

        public Projectile Fire(ProjectileOwner owner, Vector3 position, Vector3 direction, float damage, float speed,
            EnemyController target = null, Color? color = null)
        {
            while (projectiles.Count >= 40)
            {
                Projectile oldest = projectiles[0];
                ReleaseProjectile(oldest);
            }

            Projectile projectile = projectilePool.Get();
            projectile.Born = ++birthSequence;
            projectile.Fire(owner, position, direction, damage, speed, target, color);
            projectiles.Add(projectile);
            return projectile;
        }

        public TelegraphView GetWarning(EnemyKind kind, bool disc = false)
        {
            ObjectPool<TelegraphView> pool = kind == EnemyKind.Sentinel ? dashWarnings :
                (kind == EnemyKind.CageWarden && !disc ? fanWarnings : discWarnings);
            TelegraphView view = pool.Get();
            view.transform.localScale = Vector3.one;
            view.SetFill(0f);
            return view;
        }

        public void ReturnWarning(TelegraphView view, EnemyKind kind, bool disc = false)
        {
            if (view == null || !view.gameObject.activeSelf)
            {
                return;
            }

            ObjectPool<TelegraphView> pool = kind == EnemyKind.Sentinel ? dashWarnings :
                (kind == EnemyKind.CageWarden && !disc ? fanWarnings : discWarnings);
            view.transform.localScale = Vector3.one;
            pool.Release(view);
        }

        public void PlayBurst(Color color, float radius, Vector3 position)
        {
            GameObject burst = burstPool.Get();
            burst.transform.position = position;
            BurstAnimator animator = burst.GetComponent<BurstAnimator>();
            animator.TargetRadius = Mathf.Max(0.1f, radius);
            animator.BaseColor = color;
            burst.transform.localScale = Vector3.one * (animator.TargetRadius * 0.2f);

            activeBursts.Add(new BurstLife(burst, 0.3f));
        }

        public void SetFloor(RHMG.Core.FloorDefinition floor)
        {
            waves = new WaveDirector(this, () => Session.ClearFloor());
            waves.Start(floor);
        }

        public void ClearProjectiles()
        {
            while (projectiles.Count > 0)
            {
                ReleaseProjectile(projectiles[0]);
            }
        }

        public void ClearEnemies()
        {
            while (Enemies.Count > 0)
            {
                EnemyController enemy = Enemies[Enemies.Count - 1];
                Enemies.RemoveAt(Enemies.Count - 1);
                if (enemy != null && enemy.gameObject.activeSelf)
                {
                    enemyPools[(int)enemy.Kind].Release(enemy);
                }
            }
        }

        private void ReleaseProjectile(Projectile projectile)
        {
            if (projectile == null || !projectile.gameObject.activeSelf)
            {
                return;
            }

            projectiles.Remove(projectile);
            projectilePool.Release(projectile);
        }

        private void ReleaseEnemy(EnemyController enemy)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemyPools[(int)enemy.Kind].Release(enemy);
            }
        }

    }
}
