using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T7: save schema v1 -> v2 and checkpoint validation (01-CONTRACTS §2).
    public class SaveMigrationTests
    {
        private InMemoryFileStore _fs;
        private SaveService _svc;

        [SetUp]
        public void SetUp()
        {
            _fs = new InMemoryFileStore();
            _svc = new SaveService(_fs, new TestJsonSerializer(), "save");
        }

        private static RunCheckpoint GoodCheckpoint()
        {
            return new RunCheckpoint
            {
                valid = true, runId = "run1-7", seed = 7, floor = 11, level = 9, xp = 12, hp = 88f,
                takenCards = new List<string> { "upgrade.staff_dmg", "upgrade.staff_dmg", "upgrade.range" },
                spirits = new List<RunSpiritEntry>
                {
                    new RunSpiritEntry { spiritId = "spirit.steam", level = 3 },
                    new RunSpiritEntry { spiritId = "spirit.bud", level = 2 }
                }
            };
        }

        [Test]
        public void SchemaVersion_IsTwo()
        {
            Assert.AreEqual(2, SaveData.CurrentSchemaVersion);
            Assert.AreEqual(2, SaveData.CreateDefault().schemaVersion);
        }

        [Test]
        public void V1Save_LoadsAsV2_KeepingEveryOldField()
        {
            SaveData old = SaveData.CreateDefault();
            old.gold = 321;
            old.highestClearedFloor = 5;
            old.language = "ko";
            old.unlockedSpiritIds.Add("spirit.ember");
            old.activeFusions.Add(new FusionLink { recipeId = "recipe.steam", ingredientA = "spirit.dew", ingredientB = "spirit.ember" });
            old.schemaVersion = 1;
            string json = new TestJsonSerializer().Serialize(old);
            json = Regex.Replace(json, ",\"runCheckpoint\":[^,}]*", "");
            json = Regex.Replace(json, ",\"highestLevelReached\":[^,}]*", "");
            Assert.That(json, Does.Contain("\"schemaVersion\":1"));
            _fs.Files[_svc.PrimaryPath] = json;

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.LoadedPrimary, _svc.Load(out loaded));
            Assert.AreEqual(2, loaded.schemaVersion);
            Assert.AreEqual(321, loaded.gold);
            Assert.AreEqual(5, loaded.highestClearedFloor);
            Assert.AreEqual("ko", loaded.language);
            CollectionAssert.AreEqual(new[] { "spirit.ember" }, loaded.unlockedSpiritIds);
            Assert.AreEqual(1, loaded.activeFusions.Count);
            Assert.AreEqual("recipe.steam", loaded.activeFusions[0].recipeId);
            Assert.AreEqual(0, loaded.highestLevelReached);
            Assert.IsTrue(loaded.runCheckpoint == null || !loaded.runCheckpoint.valid);
        }

        [Test]
        public void FutureSchema_StillTreatedAsCorrupt()
        {
            SaveData data = SaveData.CreateDefault();
            data.schemaVersion = 3;
            _fs.Files[_svc.PrimaryPath] = new TestJsonSerializer().Serialize(data);
            SaveData loaded;
            Assert.AreEqual(LoadOutcome.ResetCorrupt, _svc.Load(out loaded));
        }

        [Test]
        public void Checkpoint_SurvivesSaveLoadAndDeepClone()
        {
            SaveData data = SaveData.CreateDefault();
            data.runCheckpoint = GoodCheckpoint();
            data.highestLevelReached = 17;
            _svc.Save(data);

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.LoadedPrimary, _svc.Load(out loaded));
            Assert.IsTrue(loaded.runCheckpoint.valid);
            Assert.AreEqual(11, loaded.runCheckpoint.floor);
            Assert.AreEqual(9, loaded.runCheckpoint.level);
            Assert.AreEqual(88f, loaded.runCheckpoint.hp, 1e-4f);
            CollectionAssert.AreEqual(data.runCheckpoint.takenCards, loaded.runCheckpoint.takenCards);
            Assert.AreEqual("spirit.steam", loaded.runCheckpoint.spirits[0].spiritId);
            Assert.AreEqual(17, loaded.highestLevelReached);

            SaveData clone = loaded.DeepClone();
            clone.runCheckpoint.spirits[0].level = 5;
            clone.runCheckpoint.takenCards.Add("upgrade.range");
            Assert.AreEqual(3, loaded.runCheckpoint.spirits[0].level);
            Assert.AreEqual(3, loaded.runCheckpoint.takenCards.Count);
        }

        private static void AssertInvalidAfterNormalize(RunCheckpoint cp, string why)
        {
            SaveData data = SaveData.CreateDefault();
            data.runCheckpoint = cp;
            data.Normalize();
            Assert.IsFalse(data.runCheckpoint.valid, why);
        }

        [Test]
        public void DamagedCheckpoints_BecomeInvalid()
        {
            RunCheckpoint cp = GoodCheckpoint(); cp.floor = 0; AssertInvalidAfterNormalize(cp, "floor 0");
            cp = GoodCheckpoint(); cp.floor = 101; AssertInvalidAfterNormalize(cp, "floor 101");
            cp = GoodCheckpoint(); cp.level = 0; AssertInvalidAfterNormalize(cp, "level 0");
            cp = GoodCheckpoint(); cp.xp = -1; AssertInvalidAfterNormalize(cp, "negative xp");
            cp = GoodCheckpoint(); cp.runId = null; AssertInvalidAfterNormalize(cp, "no run id");
            cp = GoodCheckpoint(); cp.spirits[0].spiritId = "spirit.ghost"; AssertInvalidAfterNormalize(cp, "unknown spirit");
            cp = GoodCheckpoint(); cp.spirits[0].level = 9; AssertInvalidAfterNormalize(cp, "spirit level");
            cp = GoodCheckpoint(); cp.spirits.Add(new RunSpiritEntry { spiritId = "spirit.dew", level = 1 });
            cp.spirits.Add(new RunSpiritEntry { spiritId = "spirit.ember", level = 1 }); AssertInvalidAfterNormalize(cp, "four spirits");
            cp = GoodCheckpoint(); cp.takenCards.Add("upgrade.ghost"); AssertInvalidAfterNormalize(cp, "unknown card");
            cp = GoodCheckpoint(); for (int i = 0; i < 6; i++) { cp.takenCards.Add("upgrade.armor"); } AssertInvalidAfterNormalize(cp, "over max stacks");
        }

        [Test]
        public void GoodCheckpoint_StaysValid_NullListsAreRepaired_NullCheckpointStaysNull()
        {
            SaveData data = SaveData.CreateDefault();
            data.runCheckpoint = GoodCheckpoint();
            data.Normalize();
            Assert.IsTrue(data.runCheckpoint.valid);

            RunCheckpoint empty = new RunCheckpoint { valid = true, runId = "r", floor = 6, level = 1 };
            data.runCheckpoint = empty;
            data.Normalize();
            Assert.IsNotNull(empty.takenCards);
            Assert.IsNotNull(empty.spirits);
            Assert.IsTrue(empty.valid);

            SaveData none = SaveData.CreateDefault();
            none.Normalize();
            Assert.IsNull(none.runCheckpoint);
        }

        [Test]
        public void NumericCheckpointDamage_InvalidatesOnlyCheckpoint()
        {
            foreach (int level in new[] { 94589, int.MaxValue })
            {
                RunCheckpoint cp = GoodCheckpoint(); cp.level = level;
                AssertInvalidAfterNormalize(cp, "unrepresentable XP level");
            }
            foreach (float hp in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                RunCheckpoint cp = GoodCheckpoint(); cp.hp = hp;
                AssertInvalidAfterNormalize(cp, "nonfinite HP");
            }
            foreach (int xp in new[] { XpCurve.Required(9), int.MaxValue })
            {
                SaveData data = SaveData.CreateDefault();
                data.gold = 321; data.language = "ko"; data.highestClearedFloor = 50;
                data.runCheckpoint = GoodCheckpoint(); data.runCheckpoint.xp = xp;
                data.Normalize();
                Assert.IsFalse(data.runCheckpoint.valid);
                Assert.AreEqual(321, data.gold);
                Assert.AreEqual("ko", data.language);
                Assert.AreEqual(50, data.highestClearedFloor);
            }
        }

        [Test]
        public void NumericCheckpointBoundary_RemainsValidAndResumesSafely()
        {
            RunCheckpoint cp = GoodCheckpoint();
            cp.level = 94588; cp.xp = XpCurve.Required(cp.level) - 1; cp.hp = float.MaxValue;
            Assert.IsTrue(SaveData.IsCheckpointStructurallyValid(cp));
            RunProgress progress = new RunProgress(cp.level, cp.xp);
            Assert.DoesNotThrow(() => progress.AddXp(int.MaxValue, 1f));
            Assert.AreEqual(94588, progress.Level);
            Assert.AreEqual(XpCurve.Required(progress.Level) - 1, progress.Xp);
        }

        [Test]
        public void NegativeHighestLevel_IsRepaired()
        {
            SaveData data = SaveData.CreateDefault();
            data.highestLevelReached = -4;
            data.Normalize();
            Assert.AreEqual(0, data.highestLevelReached);
        }
    }
}
