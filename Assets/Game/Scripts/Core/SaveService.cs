using System;

namespace RHMG.Core
{
    public interface IFileStore
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        void Delete(string path);
        void Move(string source, string destination);
        void EnsureDirectory(string directory);
    }

    public interface ISaveSerializer
    {
        string Serialize(SaveData data);

        /// <summary>Throws when the json is damaged.</summary>
        SaveData Deserialize(string json);
    }

    public sealed class SaveService
    {
        private readonly IFileStore _fs;
        private readonly ISaveSerializer _serializer;
        private readonly string _directory;

        public SaveService(IFileStore fs, ISaveSerializer ser, string directory, string slot = "main")
        {
            _fs = fs;
            _serializer = ser;
            _directory = directory;
            string prefix = directory.TrimEnd('/', '\\') + "/" + slot;
            PrimaryPath = prefix + ".json";
            BackupPath = prefix + ".bak";
            TempPath = prefix + ".tmp";
        }

        public string PrimaryPath { get; }
        public string BackupPath { get; }
        public string TempPath { get; }

        public LoadOutcome Load(out SaveData data)
        {
            bool primaryExists = _fs.Exists(PrimaryPath);
            bool backupExists = _fs.Exists(BackupPath);

            if (primaryExists && TryRead(PrimaryPath, out data))
            {
                return LoadOutcome.LoadedPrimary;
            }

            if (backupExists && TryRead(BackupPath, out data))
            {
                TryRewritePrimary(data);
                return LoadOutcome.RestoredFromBackup;
            }

            data = SaveData.CreateDefault();
            return primaryExists || backupExists ? LoadOutcome.ResetCorrupt : LoadOutcome.Fresh;
        }

        /// <summary>Writes tmp, rotates primary into bak, then moves tmp into primary.</summary>
        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _fs.EnsureDirectory(_directory);
            _fs.WriteAllText(TempPath, _serializer.Serialize(data));
            if (_fs.Exists(PrimaryPath))
            {
                if (_fs.Exists(BackupPath))
                {
                    _fs.Delete(BackupPath);
                }

                _fs.Move(PrimaryPath, BackupPath);
            }

            _fs.Move(TempPath, PrimaryPath);
        }

        public void DeleteAll()
        {
            string[] paths = { PrimaryPath, BackupPath, TempPath };
            for (int i = 0; i < paths.Length; i++)
            {
                if (_fs.Exists(paths[i]))
                {
                    _fs.Delete(paths[i]);
                }
            }
        }

        private bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                SaveData parsed = _serializer.Deserialize(_fs.ReadAllText(path));
                if (parsed == null || parsed.schemaVersion < 1 || parsed.schemaVersion > SaveData.CurrentSchemaVersion)
                {
                    return false;
                }

                parsed.Normalize();
                data = parsed;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Restores primary from the (valid) backup WITHOUT rotating, so the good backup is not overwritten
        // by the damaged primary. A failure here is not fatal: the data is already loaded in memory.
        private void TryRewritePrimary(SaveData data)
        {
            try
            {
                _fs.EnsureDirectory(_directory);
                _fs.WriteAllText(TempPath, _serializer.Serialize(data));
                if (_fs.Exists(PrimaryPath))
                {
                    _fs.Delete(PrimaryPath);
                }

                _fs.Move(TempPath, PrimaryPath);
            }
            catch (Exception)
            {
            }
        }
    }
}
