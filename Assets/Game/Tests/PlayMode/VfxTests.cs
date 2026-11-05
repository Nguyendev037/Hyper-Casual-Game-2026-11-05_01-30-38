using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.TestTools;

namespace RHMG.PlayTests
{
    /// <summary>SPEC-P03 section 3: pooled VFX stay stable under continuous fire; fallbacks never null; hit-stop is capped.</summary>
    public class VfxTests
    {
        private GameObject root;
        private CombatWorld world;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("VfxRegression");
            CombatSession session = CombatSession.Create(root.transform, new CombatSetup
            {
                Stats = StatsCalculator.Compute(null, null, 0, false),
                Station = true,
                StationTiles = new List<StationTile>()
            });
            world = session.GetComponentInChildren<CombatWorld>();
            world.Running = true;
            world.Player.GodMode = true;
            world.Player.transform.position = Vector3.zero;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator ContinuousFire_TenSeconds_NoNewVfxObjectsAfterFiveSeconds()
        {
            world.SpawnEnemy(EnemyKind.SandBeetle, new Vector3(6f, 0f, 0f), 1000f, 0f);
            VfxStyle[] spiritStyles = { VfxStyle.Water, VfxStyle.Fire, VfxStyle.Steam };
            int createdAtFive = -1;
            float start = Time.time;
            float nextShot = 0f;
            while (Time.time - start < 10f)
            {
                float elapsed = Time.time - start;
                if (elapsed >= nextShot)
                {
                    nextShot = elapsed + 0.12f;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 dir = Quaternion.AngleAxis((i - 1) * 8f, Vector3.up) * Vector3.right;
                        world.Fire(ProjectileOwner.Player, Vector3.up * 0.8f, dir, 12f, 14f, null, null, VfxStyle.Arcane);
                    }

                    for (int i = 0; i < spiritStyles.Length; i++)
                    {
                        world.Fire(ProjectileOwner.Player, Vector3.up * 0.8f + Vector3.forward * (i - 1),
                            Vector3.right, 9f, 11f, null, null, spiritStyles[i]);
                    }

                    world.Fire(ProjectileOwner.Enemy, new Vector3(8f, 0.8f, 3f), Vector3.left, 5f, 6f, null, null,
                        VfxStyle.Boss);
                }

                if (createdAtFive < 0 && elapsed >= 5f)
                {
                    createdAtFive = VfxPool.Created;
                }

                yield return null;
            }

            Assert.GreaterOrEqual(createdAtFive, 0, "never reached the 5 s sample");
            Assert.AreEqual(createdAtFive, VfxPool.Created, "VfxPool.Created grew after the first 5 s (pool not stable)");
            Assert.IsNotNull(VfxHub.Instance);
            Assert.LessOrEqual(VfxHub.ParticleBudget, 300);
        }

        [Test]
        public void MissingArt_FallsBackToGeneratedTextures_NeverNull()
        {
            string[] ids =
            {
                "vfx_core_arcane", "vfx_core_water", "vfx_core_fire", "vfx_core_wood", "vfx_core_steam",
                "vfx_core_growth", "vfx_core_wildfire", "vfx_core_enemy", "vfx_core_boss", "vfx_trail",
                "vfx_impact_0", "vfx_impact_1", "vfx_impact_2", "vfx_impact_3", "vfx_impact_4", "vfx_impact_5",
                "vfx_muzzle", "vfx_spark", "vfx_ring", "vfx_crit", "vfx_does_not_exist"
            };
            foreach (string id in ids)
            {
                Assert.IsNotNull(VfxAssets.Texture(id), id);
            }

            Assert.IsNotNull(VfxAssets.Additive());
            Assert.IsNotNull(VfxAssets.Trail());
        }

        [Test]
        public void Styles_ResolveByOwnerAndSpirit()
        {
            Assert.AreEqual(VfxStyle.Arcane, VfxStyles.Resolve(VfxStyle.Auto, ProjectileOwner.Player));
            Assert.AreEqual(VfxStyle.Enemy, VfxStyles.Resolve(VfxStyle.Auto, ProjectileOwner.Enemy));
            Assert.AreEqual(VfxStyle.Boss, VfxStyles.Resolve(VfxStyle.Boss, ProjectileOwner.Enemy));
            Assert.AreEqual(VfxStyle.Steam, VfxStyles.ForSpirit(Catalog.GetSpirit("spirit.steam")));
            Assert.AreEqual(VfxStyle.Growth, VfxStyles.ForSpirit(Catalog.GetSpirit("spirit.growth")));
            Assert.AreEqual(VfxStyle.Wildfire, VfxStyles.ForSpirit(Catalog.GetSpirit("spirit.wildfire")));
            Assert.AreEqual(VfxStyle.Water, VfxStyles.ForSpirit(Catalog.GetSpirit("spirit.dew")));
        }

        [UnityTest]
        public IEnumerator HitStop_OnlyBigHits_CappedAndRestored()
        {
            VfxHub hub = VfxHub.Instance;
            Assert.IsNotNull(hub);
            VfxHub.HitStop(10f, false);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "small hit must not freeze");
            VfxHub.HitStop(30f, false);
            Assert.Less(Time.timeScale, 0.2f, "30 damage must freeze");
            Assert.IsTrue(hub.HitStopRunning);
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "freeze must end by itself");
            VfxHub.HitStop(30f, true);
            Assert.AreEqual(1f, Time.timeScale, 0.0001f, "second hit inside 0.25 s must be ignored");
            yield return new WaitForSecondsRealtime(0.3f);
            VfxHub.HitStop(1f, true);
            Assert.Less(Time.timeScale, 0.2f, "crit freezes regardless of damage");
        }
    }
}
