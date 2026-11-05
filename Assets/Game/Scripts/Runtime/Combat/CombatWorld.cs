using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using RHMG.Core;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Shapes of the pooled attack warnings (disc, 60 degree fan, 90 degree fan, line).</summary>
    public enum WarnShape
    {
        Disc,
        Fan60,
        BossFan,
        Line
    }

    public sealed partial class CombatWorld : MonoBehaviour
    {
        public readonly List<EnemyController> Enemies = new List<EnemyController>(64);
        public PlayerController Player { get; set; }
        public DogController Dog { get; set; }
        public SpiritController Spirit { get; set; }
        public EnemyController DummyTarget { get; set; }
        public CombatSession Session { get; set; }
        public bool Running { get; set; }
        public GemField Gems { get; private set; }
        public FloorDirector Director { get; private set; }
        public StormZone Storm { get; private set; }
        public StationView Station { get; set; }
        public int CurrentFloor { get; private set; } = 1;
        /// <summary>Simulation seconds since the world was built (unaffected by pauses).</summary>
        public float SimTime { get; private set; }

        private readonly List<Projectile> projectiles = new List<Projectile>(80);
        private ObjectPool<Projectile> projectilePool;
        private readonly ObjectPool<EnemyController>[] enemyPools = new ObjectPool<EnemyController>[7];
        private ObjectPool<TelegraphView> discWarnings;
        private ObjectPool<TelegraphView> fanWarnings;
        private ObjectPool<TelegraphView> dashWarnings;
        private ObjectPool<TelegraphView> bossFanWarnings;
        private ObjectPool<GameObject> burstPool;
        private Mesh dashMesh;
        private readonly List<BurstLife> activeBursts = new List<BurstLife>(8);
        private long birthSequence;

        public void Init()
        {
            projectilePool = new ObjectPool<Projectile>(CreateProjectile, OnGetProjectile, OnReleaseProjectile,
                OnDestroyProjectile, false, 60, CombatTuning.MaxProjectiles + 20);
            for (int i = 0; i < enemyPools.Length; i++)
            {
                EnemyKind kind = i < 6 ? (EnemyKind)i : EnemyKind.SandBeetle;
                int index = i;
                enemyPools[i] = new ObjectPool<EnemyController>(() => CreateEnemy(kind, index), OnGetEnemy,
                    OnReleaseEnemy, OnDestroyEnemy, false, 12, 140);
            }
            discWarnings = CreateWarningPool(MeshBuilder.Disc(Catalog.WardenRingRadius, 32), 12);
            fanWarnings = CreateWarningPool(MeshBuilder.Fan(CombatTuning.WardenFanRadius,
                CombatTuning.WardenFanAngle, 24), 12);
            Mesh bossFan = MeshBuilder.Fan(CombatTuning.BossFanRadius, CombatTuning.BossFanAngle, 24);
            bossFan.name = "BossFan";
            bossFanWarnings = CreateWarningPool(bossFan, 6);
            dashMesh = CreateDashWarningMesh();
            dashWarnings = CreateWarningPool(dashMesh, 12);
            burstPool = new ObjectPool<GameObject>(CreateBurst, item => item.SetActive(true),
                item => item.SetActive(false), item => Destroy(item), false, 8, 8);
            Gems = new GemField(this);
            Storm = new StormZone(this);
            if (!Session.IsStation)
            {
                PrewarmPools();
            }

            PrewarmEffects();
        }

        private void Update()
        {
            if (!Running)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            SimTime += deltaTime;
            Player?.Step(deltaTime);
            Dog?.Step(deltaTime);
            Spirit?.Step(deltaTime);
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (i < Enemies.Count && Enemies[i] != null)
                {
                    Enemies[i].Step(deltaTime);
                }
            }

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (i >= projectiles.Count)
                {
                    continue;
                }

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

            if (Player != null)
            {
                Gems.Step(deltaTime, Player.transform.position, Player.Stats.MagnetRadius);
            }

            Storm.Step(deltaTime);
            Station?.Step(deltaTime);
            Director?.Step(deltaTime);
        }

        public EnemyController NearestEnemy(Vector3 position, float range)
        {
            EnemyController nearest = null;
            float bestDistance = range * range;
            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null || !enemy.Targetable)
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

        /// <summary>Normal enemies alive (bosses and anchors are not counted against MaxAliveEnemies).</summary>
        public int RegularAlive
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Enemies.Count; i++)
                {
                    EnemyController enemy = Enemies[i];
                    if (enemy != null && enemy.Alive && !enemy.IsBoss && !enemy.IsAnchor)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public EnemyController SpawnEnemy(RHMG.Core.EnemyKind kind, Vector3 position, float hpMult = 1f,
            float dmgMult = 1f, bool elite = false)
        {
            EnemyController enemy = enemyPools[(int)kind].Get();
            enemy.Spawn(kind, position, hpMult, dmgMult, elite);
            Enemies.Add(enemy);
            return enemy;
        }

        public EnemyController SpawnAnchor(Vector3 position, float hp, Action<EnemyController> onKilled)
        {
            EnemyController anchor = enemyPools[EnemyController.AnchorPoolIndex].Get();
            anchor.SpawnAnchor(position, hp, onKilled);
            Enemies.Add(anchor);
            return anchor;
        }

        public Projectile Fire(ProjectileOwner owner, Vector3 position, Vector3 direction, float damage, float speed,
            EnemyController target = null, Color? color = null)
        {
            while (projectiles.Count >= CombatTuning.MaxProjectiles)
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

        private ObjectPool<TelegraphView> PoolFor(WarnShape shape)
        {
            switch (shape)
            {
                case WarnShape.Fan60: return fanWarnings;
                case WarnShape.BossFan: return bossFanWarnings;
                case WarnShape.Line: return dashWarnings;
                default: return discWarnings;
            }
        }

        /// <summary>Boss warnings: the caller scales/positions the view, and returns it with ReturnWarningShape.</summary>
        public TelegraphView GetWarningShape(WarnShape shape)
        {
            TelegraphView view = PoolFor(shape).Get();
            view.transform.localScale = Vector3.one;
            view.SetFill(0f);
            return view;
        }

        public void ReturnWarningShape(TelegraphView view, WarnShape shape)
        {
            if (view == null || !view.gameObject.activeSelf)
            {
                return;
            }

            view.transform.localScale = Vector3.one;
            PoolFor(shape).Release(view);
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

        public void SetFloor(int floor)
        {
            CurrentFloor = floor;
            Director = new FloorDirector(this, floor, () => Session.ClearFloor());
            Director.Start();
        }

        /// <summary>Debug / floor-end: every enemy dies now (drops its gems).</summary>
        public void DissolveEnemies()
        {
            List<EnemyController> snapshot = new List<EnemyController>(Enemies);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = snapshot[i];
                if (enemy != null && enemy.Alive)
                {
                    enemy.ForceKill();
                }
            }
        }

        public void ClearProjectiles()
        {
            while (projectiles.Count > 0)
            {
                ReleaseProjectile(projectiles[0]);
            }
        }

        /// <summary>Phase change of a boss: only the boss bullets disappear (SPEC-P03 §2).</summary>
        public void ClearEnemyProjectiles()
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (i < projectiles.Count && projectiles[i] != null && projectiles[i].Owner == ProjectileOwner.Enemy)
                {
                    ReleaseProjectile(projectiles[i]);
                }
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
                    enemyPools[enemy.PoolIndex].Release(enemy);
                }
            }
        }

        /// <summary>Spawn point 11-14 m from the girl, inside the arena, never inside the safety radius (SPEC-P01 §2/§7).</summary>
        public Vector3 PickSpawnPosition()
        {
            Vector3 center = Player != null ? Player.transform.position : Vector3.zero;
            for (int attempt = 0; attempt < CombatTuning.SpawnTries; attempt++)
            {
                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                float distance = UnityEngine.Random.Range(CombatTuning.SpawnMinDistance, CombatTuning.SpawnMaxDistance);
                Vector3 candidate = CombatMath.ClampArena(center +
                    new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * distance);
                if (CombatMath.Flat(candidate - center).sqrMagnitude >=
                    CombatTuning.SpawnSafetyRadius * CombatTuning.SpawnSafetyRadius)
                {
                    return candidate;
                }
            }

            Vector3 best = new Vector3(CombatMath.LimitX, 0f, CombatMath.LimitZ);
            float bestDistance = -1f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = new Vector3(sx * CombatMath.LimitX, 0f, sz * CombatMath.LimitZ);
                    float d = CombatMath.Flat(corner - center).sqrMagnitude;
                    if (d > bestDistance)
                    {
                        bestDistance = d;
                        best = corner;
                    }
                }
            }

            return best;
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
                enemyPools[enemy.PoolIndex].Release(enemy);
            }
        }
    }
}
