using UnityEngine;
using UnityEngine.Pool;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    public sealed partial class CombatWorld
    {
        private static Mesh CreateDashWarningMesh()
        {
            Mesh mesh = new Mesh { name = "Dash" };
            mesh.vertices = new[]
            {
                new Vector3(-CombatTuning.SentinelTelegraphWidth / 2f, 0f, 0f),
                new Vector3(CombatTuning.SentinelTelegraphWidth / 2f, 0f, 0f),
                new Vector3(CombatTuning.SentinelTelegraphWidth / 2f, 0f, CombatTuning.SentinelChargeDistance),
                new Vector3(-CombatTuning.SentinelTelegraphWidth / 2f, 0f, CombatTuning.SentinelChargeDistance)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private Projectile CreateProjectile()
        {
            GameObject view = VfxFactory.Projectile(Color.white, CombatTuning.ProjectileVisualRadius);
            view.name = "PooledProjectile";
            view.transform.SetParent(transform, false);
            Projectile projectile = view.AddComponent<Projectile>();
            projectile.Init(this, ReleaseProjectile);
            view.SetActive(false);
            return projectile;
        }

        private EnemyController CreateEnemy(EnemyKind kind)
        {
            GameObject root = new GameObject("PooledEnemy");
            root.transform.SetParent(transform, false);
            EnemyController enemy = root.AddComponent<EnemyController>();
            enemy.Init(this, ReleaseEnemy, kind);
            root.SetActive(false);
            return enemy;
        }

        private ObjectPool<TelegraphView> CreateWarningPool(Mesh shape, int capacity)
        {
            ObjectPool<TelegraphView> pool = new ObjectPool<TelegraphView>(() =>
            {
                GameObject warning = VfxFactory.Telegraph(shape);
                warning.transform.SetParent(transform, false);
                warning.SetActive(false);
                return warning.GetComponent<TelegraphView>();
            }, view => view.gameObject.SetActive(true), view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject), false, capacity, capacity);
            return pool;
        }

        private GameObject CreateBurst()
        {
            GameObject burst = VfxFactory.Burst(Color.white, 1f);
            burst.transform.SetParent(transform, false);
            burst.SetActive(false);
            return burst;
        }

        private void PrewarmPools()
        {
            Projectile[] shotItems = new Projectile[40];
            for (int i = 0; i < shotItems.Length; i++)
            {
                shotItems[i] = projectilePool.Get();
            }

            for (int i = 0; i < shotItems.Length; i++)
            {
                projectilePool.Release(shotItems[i]);
            }

            int[] counts = { 6, 2, 3, 1 };
            for (int kind = 0; kind < enemyPools.Length; kind++)
            {
                EnemyController[] enemyItems = new EnemyController[counts[kind]];
                for (int i = 0; i < enemyItems.Length; i++)
                {
                    enemyItems[i] = enemyPools[kind].Get();
                }

                for (int i = 0; i < enemyItems.Length; i++)
                {
                    enemyPools[kind].Release(enemyItems[i]);
                }
            }
        }

        private void PrewarmEffects()
        {
            PrewarmWarnings(discWarnings, 12);
            PrewarmWarnings(fanWarnings, 12);
            PrewarmWarnings(dashWarnings, 12);
            GameObject[] bursts = new GameObject[8];
            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i] = burstPool.Get();
            }

            for (int i = 0; i < bursts.Length; i++)
            {
                burstPool.Release(bursts[i]);
            }
        }

        private static void PrewarmWarnings(ObjectPool<TelegraphView> pool, int count)
        {
            TelegraphView[] values = new TelegraphView[count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = pool.Get();
            }

            for (int i = 0; i < values.Length; i++)
            {
                pool.Release(values[i]);
            }
        }

        private static void OnGetProjectile(Projectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private static void OnReleaseProjectile(Projectile projectile)
        {
            projectile.ResetPool();
        }

        private static void OnDestroyProjectile(Projectile projectile)
        {
            Destroy(projectile.gameObject);
        }

        private static void OnGetEnemy(EnemyController enemy)
        {
            enemy.gameObject.SetActive(true);
        }

        private static void OnReleaseEnemy(EnemyController enemy)
        {
            enemy.ReturnToPool();
        }

        private static void OnDestroyEnemy(EnemyController enemy)
        {
            Destroy(enemy.gameObject);
        }

        private void OnDestroy()
        {
            ClearProjectiles();
            ClearEnemies();
            projectilePool?.Dispose();
            foreach (ObjectPool<EnemyController> pool in enemyPools)
            {
                pool?.Dispose();
            }

            discWarnings?.Dispose();
            fanWarnings?.Dispose();
            dashWarnings?.Dispose();
            burstPool?.Dispose();
            if (dashMesh != null)
            {
                Destroy(dashMesh);
            }
        }

        private struct BurstLife
        {
            public readonly GameObject Object;
            public float Remaining;

            public BurstLife(GameObject gameObject, float remaining)
            {
                Object = gameObject;
                Remaining = remaining;
            }
        }
    }
}
