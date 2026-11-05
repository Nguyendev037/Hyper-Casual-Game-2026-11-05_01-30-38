using System.IO;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class SaveServiceTests
    {
        private InMemoryFileStore _fs;
        private SaveService _svc;

        [SetUp]
        public void SetUp()
        {
            _fs = new InMemoryFileStore();
            _svc = new SaveService(_fs, new TestJsonSerializer(), "save");
        }

        [Test]
        public void Load_NoFiles_ReturnsFreshDefault()
        {
            SaveData data;
            Assert.AreEqual(LoadOutcome.Fresh, _svc.Load(out data));
            Assert.IsNotNull(data);
            Assert.AreEqual(0, data.gold);
            Assert.AreEqual(3, data.ownedOutfitIds.Count);
        }

        [Test]
        public void Paths_UseSlotName()
        {
            Assert.AreEqual("save/main.json", _svc.PrimaryPath);
            Assert.AreEqual("save/main.bak", _svc.BackupPath);
            Assert.AreEqual("save/main.tmp", _svc.TempPath);
        }

        [Test]
        public void SaveThenLoad_RoundTripsData()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 77;
            data.highestClearedFloor = 3;
            _svc.Save(data);

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.LoadedPrimary, _svc.Load(out loaded));
            Assert.AreEqual(77, loaded.gold);
            Assert.AreEqual(3, loaded.highestClearedFloor);
            Assert.IsFalse(_fs.Exists(_svc.TempPath));
        }

        [Test]
        public void SaveTwice_BackupHoldsPreviousVersion()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 10;
            _svc.Save(data);
            data.gold = 20;
            _svc.Save(data);

            Assert.IsTrue(_fs.ReadAllText(_svc.BackupPath).Contains("10"));
            Assert.IsTrue(_fs.ReadAllText(_svc.PrimaryPath).Contains("20"));
        }

        [Test]
        public void CorruptPrimary_RestoresFromBackupAndRewritesPrimary()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 10;
            _svc.Save(data);
            data.gold = 20;
            _svc.Save(data);
            _fs.Files[_svc.PrimaryPath] = "{ not json";

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.RestoredFromBackup, _svc.Load(out loaded));
            Assert.AreEqual(10, loaded.gold);

            SaveData again;
            Assert.AreEqual(LoadOutcome.LoadedPrimary, _svc.Load(out again));
            Assert.AreEqual(10, again.gold);
            Assert.IsTrue(_fs.Exists(_svc.BackupPath));
        }

        [Test]
        public void BothCorrupt_ReturnsResetCorruptWithDefault()
        {
            _fs.Files[_svc.PrimaryPath] = "garbage";
            _fs.Files[_svc.BackupPath] = "also garbage";

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.ResetCorrupt, _svc.Load(out loaded));
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.gold);
        }

        [Test]
        public void CorruptPrimaryWithoutBackup_ReturnsResetCorrupt()
        {
            _fs.Files[_svc.PrimaryPath] = "";
            SaveData loaded;
            Assert.AreEqual(LoadOutcome.ResetCorrupt, _svc.Load(out loaded));
            Assert.IsNotNull(loaded);
        }

        [Test]
        public void WrongSchemaVersion_TreatedAsCorrupt()
        {
            SaveData data = SaveData.CreateDefault();
            data.schemaVersion = 99;
            _fs.Files[_svc.PrimaryPath] = new TestJsonSerializer().Serialize(data);

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.ResetCorrupt, _svc.Load(out loaded));
            Assert.AreEqual(1, loaded.schemaVersion);
        }

        [Test]
        public void Save_IoFailure_ThrowsIoExceptionAndKeepsPrimary()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 5;
            _svc.Save(data);

            _fs.FailWrites = true;
            data.gold = 6;
            Assert.Throws<IOException>(() => _svc.Save(data));

            _fs.FailWrites = false;
            SaveData loaded;
            Assert.AreEqual(LoadOutcome.LoadedPrimary, _svc.Load(out loaded));
            Assert.AreEqual(5, loaded.gold);
        }

        [Test]
        public void CrashBetweenRotateAndCommit_PrimaryMissing_RecoversFromBackup()
        {
            SaveData data = SaveData.CreateDefault();
            data.gold = 42;
            _svc.Save(data);
            _fs.Move(_svc.PrimaryPath, _svc.BackupPath);

            SaveData loaded;
            Assert.AreEqual(LoadOutcome.RestoredFromBackup, _svc.Load(out loaded));
            Assert.AreEqual(42, loaded.gold);
        }

        [Test]
        public void DeleteAll_RemovesEveryFile()
        {
            SaveData data = SaveData.CreateDefault();
            _svc.Save(data);
            _svc.Save(data);
            _svc.DeleteAll();
            Assert.AreEqual(0, _fs.Files.Count);
        }
    }
}
