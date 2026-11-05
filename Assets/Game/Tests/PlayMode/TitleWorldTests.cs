using System.Collections.Generic;
using NUnit.Framework;
using RHMG.Runtime;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;

namespace RHMG.PlayTests
{
    /// <summary>Plan 05 pack WT: title motion maths, chapter world, HUD border levels. Run by the tester, not by the worker.</summary>
    public class TitleWorldTests
    {
        [Test]
        public void TitleMotion_StaysInsideSpecBounds()
        {
            for (float t = 0f; t < 12f; t += 0.05f)
            {
                Assert.That(TitleFx.Breathe(t), Is.InRange(1f - TitleFx.BreathAmp - 1e-4f, 1f + TitleFx.BreathAmp + 1e-4f));
                Assert.That(Mathf.Abs(TitleFx.Bob(t)), Is.LessThanOrEqualTo(TitleFx.BobPixels + 1e-4f));
            }
            Assert.AreEqual(1f, TitleFx.LogoBounce(TitleFx.LogoSeconds), 1e-6f);
            Assert.AreEqual(1f, TitleFx.LogoBounce(5f), 1e-6f);
            Assert.Less(TitleFx.LogoBounce(0.01f), 0.5f);
            Assert.Greater(TitleFx.LogoBounce(0.2f), 0.5f);
        }

        [Test]
        public void TitleMotion_FramesHalfSecondApartDiffer()
        {
            // P01 §4: four captures 0.5 s apart must show a different pose. The pose is breathe scale + bob offset
            // together (breathing alone is symmetric around its crest, e.g. t=0.5 and t=1.0 at a 3 s period).
            float[] times = { 0.2f, 0.7f, 1.2f, 1.7f };
            for (int i = 1; i < times.Length; i++)
            {
                float poseA = TitleFx.Breathe(times[i - 1]) * 1000f + TitleFx.Bob(times[i - 1]);
                float poseB = TitleFx.Breathe(times[i]) * 1000f + TitleFx.Bob(times[i]);
                Assert.Greater(Mathf.Abs(poseA - poseB), 0.5f, "pose unchanged between " + times[i - 1] + "s and " + times[i] + "s");
            }
        }

        [Test]
        public void ParticleBudget_IsClampedTo40()
        {
            Assert.AreEqual(TitleFx.DefaultParticles, TitleFx.ParticleCount(null));
            Assert.AreEqual(40, TitleFx.ParticleCount(new Dictionary<string, object> { { "count", 999d } }));
            Assert.AreEqual(0, TitleFx.ParticleCount(new Dictionary<string, object> { { "density", -5d } }));
            Assert.AreEqual(12, TitleFx.ParticleCount(new Dictionary<string, object> { { "count", 12d } }));
        }

        [Test]
        public void FxSprites_ReadFromListOrSingleKeys()
        {
            var fx = new Dictionary<string, object>
            {
                { "sprites", new List<object> { "fx_petal", "fx_sparkle" } }, { "sprite", "fx_petal" }
            };
            CollectionAssert.AreEqual(new[] { "fx_petal", "fx_sparkle" }, TitleFx.SpriteIds(fx));
            Assert.AreEqual(0, TitleFx.SpriteIds(null).Count);
        }

        [Test]
        public void MissingTitleScreen_IsNotAnError()
        {
            Assert.DoesNotThrow(() => UiLayout.Has("no_such_screen"));
            Assert.IsFalse(UiLayout.Has("no_such_screen"));
        }

        [TestCase(1, 0)]
        [TestCase(20, 0)]
        [TestCase(21, 1)]
        [TestCase(41, 2)]
        [TestCase(61, 3)]
        [TestCase(81, 4)]
        [TestCase(100, 4)]
        public void ChapterOf_MatchesContract(int floor, int chapter)
        {
            Assert.AreEqual(chapter, WorldDecor.ChapterOf(floor));
        }

        [Test]
        public void DecorPlacement_ObeysSpacingCentreClearAndSeed()
        {
            for (int floor = 1; floor <= 100; floor++)
            {
                List<Vector2> points = WorldDecor.Place(floor, 36f, 56f);
                Assert.That(points.Count, Is.InRange(WorldDecor.MinCount, WorldDecor.MaxCount), "floor " + floor);
                for (int i = 0; i < points.Count; i++)
                {
                    Assert.GreaterOrEqual(points[i].magnitude, WorldDecor.CenterClear, "floor " + floor);
                    Assert.LessOrEqual(Mathf.Abs(points[i].x), 18f);
                    Assert.LessOrEqual(Mathf.Abs(points[i].y), 28f);
                    for (int j = i + 1; j < points.Count; j++)
                        Assert.GreaterOrEqual(Vector2.Distance(points[i], points[j]), WorldDecor.MinSpacing - 1e-4f);
                }
                CollectionAssert.AreEqual(points, WorldDecor.Place(floor, 36f, 56f), "seed must reproduce floor " + floor);
            }
        }

        [Test]
        public void ChangingFloors_AppliesChapterWithoutException_AndKeepsOneDecorRoot()
        {
            GameObject arena = CharacterFactory.CreateArena(null, 36f, 56f, false);
            try
            {
                foreach (int floor in new[] { 1, 21, 41, 61, 81, 100, 1 })
                {
                    Assert.DoesNotThrow(() => ArenaBuilder.ApplyChapter(arena.transform, floor), "floor " + floor);
                    int roots = 0, props = 0;
                    foreach (Transform child in arena.transform)
                    {
                        if (child.name == WorldDecor.RootName) { roots++; props = child.childCount; }
                    }
                    Assert.LessOrEqual(roots, 1, "floor " + floor);
                    if (roots == 1) Assert.That(props, Is.InRange(WorldDecor.MinCount, WorldDecor.MaxCount));
                    Assert.IsNotNull(arena.transform.Find("FloorBase"));
                }
            }
            finally { Object.DestroyImmediate(arena); }
        }

        [Test]
        public void SpiritBorder_TokenByLevel()
        {
            Assert.AreEqual("card_common_border", GameFlow.LevelBorderToken(1));
            Assert.AreEqual("card_common_border", GameFlow.LevelBorderToken(2));
            Assert.AreEqual("card_rare_border", GameFlow.LevelBorderToken(3));
            Assert.AreEqual("card_rare_border", GameFlow.LevelBorderToken(4));
            Assert.AreEqual("accent_gold_bright", GameFlow.LevelBorderToken(5));
        }
    }
}
