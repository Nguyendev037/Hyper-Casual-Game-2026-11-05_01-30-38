using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.TestTools;

namespace RHMG.PlayTests
{
    // Plan 05 WE (SPEC-P02 section 4). Written for the Gemini tester: not run by the author.
    public class ChapterEnemyPlayTests
    {
        private GameObject root;
        private CombatSession session;
        private CombatWorld world;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ChapterEnemyPlayTests");
            session = CombatSession.Create(root.transform, new CombatSetup
            {
                Stats = StatsCalculator.Compute(null, null, 0, false)
            });
            world = session.GetComponentInChildren<CombatWorld>();
            world.Running = false;
            world.Player.GodMode = true;
            world.Player.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        private static object Field(object owner, string name)
        {
            return owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                .GetValue(owner);
        }

        private static void SetField(object owner, string name, object value)
        {
            owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
        }

        // ---------- chapter floors: 20 s, no exception, at least two chapter kinds ----------

        private IEnumerator RunFloor(int floor)
        {
            session.StartFloor(floor);
            session.GodMode = true;
            session.Running = true;
            HashSet<EnemyKind> seen = new HashSet<EnemyKind>();
            float end = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < end)
            {
                for (int i = 0; i < world.Enemies.Count; i++)
                {
                    if (world.Enemies[i] != null && world.Enemies[i].Alive)
                    {
                        seen.Add(world.Enemies[i].Kind);
                    }
                }

                yield return new WaitForSeconds(0.25f);
            }

            int chapter = FloorPlan.Chapter(floor);
            Assert.GreaterOrEqual(seen.Count, 2, "floor " + floor + " saw " + string.Join(",", seen));
            foreach (EnemyKind kind in seen)
            {
                Assert.AreEqual(chapter, Catalog.GetEnemy(kind).Chapter, "floor " + floor + " kind " + kind);
                Assert.AreNotEqual(0, Catalog.GetEnemy(kind).Chapter, "must differ from chapter 0: " + kind);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Floor21_Runs20Seconds_WithEmberFamily() { yield return RunFloor(21); }

        [UnityTest, Timeout(60000)]
        public IEnumerator Floor41_Runs20Seconds_WithMirrorFamily() { yield return RunFloor(41); }

        [UnityTest, Timeout(60000)]
        public IEnumerator Floor61_Runs20Seconds_WithPlagueFamily() { yield return RunFloor(61); }

        [UnityTest, Timeout(60000)]
        public IEnumerator Floor81_Runs20Seconds_WithShadowFamily() { yield return RunFloor(81); }

        // ---------- archetype behaviours ----------

        [Test]
        public void Splitter_DeathSpawnsTwoSmallSlimes_ThatDoNotSplitAgain()
        {
            session.StartFloor(41);
            world.Running = false;
            // The parent must carry the floor's HP scaling, exactly like FloorDirector spawns it (children use the same budget).
            SpawnBudget budget = FloorPlan.Budget(41);
            EnemyController slime = EnemySpawner.Spawn(world, EnemyKind.InkSlime, new Vector3(0f, 0f, 6f),
                budget.HpMult, budget.DmgMult, false);
            float parentHp = slime.MaxHealth;
            slime.ForceKill();
            List<EnemyController> children = new List<EnemyController>();
            foreach (EnemyController e in world.Enemies)
            {
                if (e != null && e.Alive && e.Kind == EnemyKind.InkSlime)
                {
                    children.Add(e);
                }
            }

            Assert.AreEqual(2, children.Count);
            foreach (EnemyController child in children)
            {
                Assert.IsTrue(child.IsSplitChild);
                Assert.AreEqual(parentHp * Catalog.SlimeChildHpFraction, child.MaxHealth, 0.01f);
            }

            children[0].ForceKill();
            children[1].ForceKill();
            int alive = 0;
            foreach (EnemyController e in world.Enemies)
            {
                alive += e != null && e.Alive && e.Kind == EnemyKind.InkSlime ? 1 : 0;
            }

            Assert.AreEqual(0, alive, "children must not split");
        }

        [Test]
        public void Exploder_WarnsThenBlasts_HurtingNearbyEnemiesForHalf()
        {
            session.StartFloor(61);
            world.Running = false;
            EnemyController mite = EnemySpawner.Spawn(world, EnemyKind.BombMite, new Vector3(0f, 0f, 1.2f));
            EnemyController bystander = EnemySpawner.Spawn(world, EnemyKind.SandBeetle, new Vector3(0.5f, 0f, 1.2f));
            float before = bystander.CurrentHealth;
            mite.Step(0.05f);
            Assert.IsTrue(mite.Alive, "must wait out the fuse");
            for (int i = 0; i < 6; i++)
            {
                mite.Step(0.1f);
            }

            Assert.IsTrue(mite.Alive, "0.6 s is still inside the 0.7 s fuse");
            for (int i = 0; i < 3 && mite.Alive; i++)
            {
                mite.Step(0.1f);
            }

            Assert.IsFalse(mite.Alive, "blast after 0.7 s");
            Assert.Less(bystander.CurrentHealth, before, "bystander in R1.5 takes 50%");
        }

        [Test]
        public void Teleporter_VanishesUntargetable_ThenReappearsFourMetresAway()
        {
            session.StartFloor(41);
            world.Running = false;
            EnemyController ghost = EnemySpawner.Spawn(world, EnemyKind.MirrorGhost, new Vector3(0f, 0f, 5f));
            SetField(ghost, "bTimer", 0f);
            ghost.Step(0.02f);
            Assert.IsFalse(ghost.Targetable, "vanished");
            Assert.IsTrue(ghost.Alive);
            for (int i = 0; i < 7; i++)
            {
                ghost.Step(0.1f);
            }

            Assert.IsTrue(ghost.Targetable, "back after the 0.6 s ring");
            float d = Vector3.Distance(ghost.transform.position, world.Player.transform.position);
            Assert.AreEqual(Catalog.TeleportDistance, d, 0.6f);
        }

        [Test]
        public void PlagueKnight_FrontShieldCutsDamageBySixtyPercent()
        {
            session.StartFloor(61);
            world.Running = false;
            EnemyController knight = EnemySpawner.Spawn(world, EnemyKind.PlagueKnight, new Vector3(0f, 0f, 6f));
            knight.Step(0.05f); // faces the girl at the origin
            float hp = knight.CurrentHealth;
            knight.TakeDamage(10f, Vector3.zero);
            Assert.AreEqual(hp - 4f, knight.CurrentHealth, 0.01f, "frontal hit x0.4");
            hp = knight.CurrentHealth;
            knight.TakeDamage(10f, new Vector3(0f, 0f, 12f));
            Assert.AreEqual(hp - 10f, knight.CurrentHealth, 0.01f, "rear hit full");
        }

        // ---------- movement animation: Speed01 > 0.3 while walking ----------

        private IEnumerator WalkCheck(EnemyKind kind, float z)
        {
            session.StartFloor(41);
            world.Running = false;
            EnemyController enemy = EnemySpawner.Spawn(world, kind, new Vector3(0f, 0f, z));
            SpriteBillboard billboard = enemy.GetComponentInChildren<SpriteBillboard>();
            Assert.IsNotNull(billboard, kind + " has a billboard (sprite or fallback)");
            float end = Time.realtimeSinceStartup + 0.6f;
            float best = 0f;
            while (Time.realtimeSinceStartup < end)
            {
                enemy.Step(Time.deltaTime);
                yield return null;
                best = Mathf.Max(best, billboard.Speed01);
            }

            Assert.Greater(best, 0.3f, kind + " Speed01 while moving");
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator EveryChapterEnemy_WalkingDrivesSpeed01()
        {
            EnemyKind[] kinds =
            {
                EnemyKind.EmberImp, EnemyKind.SparkTurret, EnemyKind.AnvilGolem, EnemyKind.BookBat, EnemyKind.MirrorGhost,
                EnemyKind.InkSlime, EnemyKind.PlagueKnight, EnemyKind.BoneArcher, EnemyKind.BombMite, EnemyKind.ShadowWolf,
                EnemyKind.StarWisp, EnemyKind.VoidEye
            };
            foreach (EnemyKind kind in kinds)
            {
                yield return WalkCheck(kind, 13f);
            }
        }

        // ---------- guardians enter their own attack ----------

        [Test]
        public void ChapterGuardians_ReachTheirOwnAttack_WithAtLeastSixTenthsWarning()
        {
            (EnemyKind kind, int floor, string attack)[] cases =
            {
                (EnemyKind.ForgeGolem, 25, "rain"), (EnemyKind.MirrorWitch, 45, "mirrors"),
                (EnemyKind.CorruptKnight, 65, "triple_dash"), (EnemyKind.TwilightDrake, 85, "breath")
            };
            foreach ((EnemyKind kind, int floor, string attack) c in cases)
            {
                session.StartFloor(c.floor);
                world.Running = false;
                world.Player.GodMode = true;
                world.Player.transform.position = new Vector3(0f, 0f, -8f);
                EnemyController boss = null;
                foreach (EnemyController e in world.Enemies)
                {
                    if (e != null && e.Kind == c.kind) { boss = e; }
                }

                if (boss == null)
                {
                    world.Director.Step(0.1f);
                    foreach (EnemyController e in world.Enemies)
                    {
                        if (e != null && e.Kind == c.kind) { boss = e; }
                    }
                }

                Assert.IsNotNull(boss, c.kind + " spawned on floor " + c.floor);
                object brain = Field(boss, "brain");
                bool reached = false;
                for (int i = 0; i < 700 && !reached; i++)
                {
                    boss.Step(0.1f);
                    object current = Field(brain, "current");
                    if (current != null && (string)Field(current, "Name") == c.attack)
                    {
                        reached = true;
                        Assert.GreaterOrEqual((float)Field(brain, "telegraphTotal"), 0.6f, c.kind + " warning");
                    }
                }

                Assert.IsTrue(reached, c.kind + " never reached " + c.attack);
                Assert.IsTrue(boss.IsBoss);
            }
        }

        [Test]
        public void ChapterGuardian_RoutesThroughBossPool_AndKeepsBossFlags()
        {
            session.StartFloor(25);
            world.Running = false;
            EnemyController boss = EnemySpawner.Spawn(world, EnemyKind.ForgeGolem, new Vector3(0f, 0f, 12f));
            Assert.AreEqual(EnemyKind.ForgeGolem, boss.Kind);
            Assert.AreEqual((int)EnemyKind.CageWarden, boss.PoolIndex);
            Assert.IsTrue(boss.IsBoss);
            Assert.AreEqual(1, boss.BossPhase);
        }
    }
}
