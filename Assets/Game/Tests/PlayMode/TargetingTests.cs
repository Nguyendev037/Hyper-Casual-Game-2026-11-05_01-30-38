using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RHMG.Core;
using RHMG.Runtime.Audio;
using RHMG.Runtime.Combat;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.TestTools;

namespace RHMG.PlayTests
{
    public class TargetingTests
    {
        private GameObject root;
        private CombatWorld world;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TargetingRegression");
            var session = CombatSession.Create(root.transform, new CombatSetup
            {
                Stats = StatsCalculator.Compute(null, null, 0, false),
                Station = true,
                StationTiles = new List<StationTile>()
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

        private EnemyController ShieldedBoss()
        {
            var boss = world.SpawnEnemy(EnemyKind.BlueHairGuy, new Vector3(0.5f, 0f, 0f));
            world.Player.Step(0.01f);
            Assert.AreSame(boss, Field(world.Player, "target"));
            boss.TakeDamage(boss.MaxHealth, Vector3.zero);
            Assert.AreEqual(2, boss.BossPhase);
            object brain = Field(boss, "brain");
            brain.GetType().GetField("invulnerable", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(brain, 0f);
            boss.TakeDamage(boss.MaxHealth, Vector3.zero);
            Assert.AreEqual(3, boss.BossPhase);
            Assert.IsTrue(boss.Alive);
            Assert.IsFalse(boss.Targetable);
            return boss;
        }

        [Test]
        public void Player_DropsCachedShieldedBoss_AndTargetsAnchor()
        {
            ShieldedBoss();
            var anchor = world.SpawnAnchor(new Vector3(0.6f, 0f, 0f), 50f, null);
            world.Player.Step(0.5f);
            Assert.AreSame(anchor, Field(world.Player, "target"));
        }

        [Test]
        public void Player_RefreshesNearestTarget_WhenCloserEnemyAppears()
        {
            var first = world.SpawnEnemy(EnemyKind.SandBeetle, new Vector3(0f, 0f, 5f));
            world.Player.Step(0.01f);
            Assert.AreSame(first, Field(world.Player, "target"));
            var closer = world.SpawnEnemy(EnemyKind.SandBeetle, new Vector3(0f, 0f, 1f));
            world.Player.Step(0.5f);
            Assert.AreSame(closer, Field(world.Player, "target"));
        }

        [Test]
        public void Dog_IgnoresCloserShieldedBoss_AndBitesAnchor()
        {
            ShieldedBoss();
            var anchor = world.SpawnAnchor(new Vector3(0.6f, 0f, 0f), 50f, null);
            world.Dog.transform.position = Vector3.zero;
            object selected = typeof(DogController).GetMethod("FindBiteTarget",
                BindingFlags.NonPublic | BindingFlags.Instance).Invoke(world.Dog, null);
            Assert.AreSame(anchor, selected);
        }

        [UnityTest]
        public IEnumerator EnemyAnimation_UsesChildMesh_TracksMotion_ResetsAfterPool()
        {
            var boss = world.SpawnEnemy(EnemyKind.SandQueen, new Vector3(0f, 0f, 10f));
            var billboard = boss.GetComponentInChildren<SpriteBillboard>();
            var deformer = billboard.GetComponent<SpriteDeformer>();
            Assert.IsNotNull(deformer);
            Assert.AreNotSame(billboard.gameObject, billboard.GetComponentInChildren<MeshRenderer>().gameObject);
            boss.Step(0.1f);
            yield return null;
            Assert.Greater(billboard.Speed01, 0f, "Actual enemy movement must drive the walk cycle");
            boss.PlayWindup(0.8f);
            Assert.Greater((float)Field(deformer, "m_Windup"), 0f);
            boss.ForceKill();
            Assert.IsFalse(boss.Alive);
            Assert.IsTrue(boss.gameObject.activeSelf, "The death pose must remain visible before pooling");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(boss.gameObject.activeSelf);
            var reused = world.SpawnEnemy(EnemyKind.SandQueen, new Vector3(0f, 0f, 10f));
            Assert.AreSame(boss, reused);
            Assert.AreEqual(-1f, (float)Field(deformer, "m_Death"));
            Assert.AreEqual(0f, (float)Field(deformer, "m_Windup"));
        }

        [Test]
        public void MasterLimiter_PreservesQuietSamples_AndBoundsDenseMix()
        {
            var listener = new GameObject("Listener", typeof(AudioListener), typeof(MasterAudioLimiter));
            listener.transform.SetParent(root.transform);
            float[] samples = { 0f, 0.2f, -0.2f, 1.374293f, -3f, float.NaN, float.PositiveInfinity };
            typeof(MasterAudioLimiter).GetMethod("OnAudioFilterRead", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(listener.GetComponent<MasterAudioLimiter>(), new object[] { samples, 1 });
            Assert.AreEqual(0.2f, samples[1]);
            Assert.AreEqual(-0.2f, samples[2]);
            foreach (float sample in samples)
            {
                Assert.IsFalse(float.IsNaN(sample) || float.IsInfinity(sample));
                Assert.LessOrEqual(Mathf.Abs(sample), AudioLimiter.Ceiling);
            }
        }

        private static object Field(object owner, string name)
        {
            return owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
        }
    }
}
