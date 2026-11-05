using System.Collections.Generic;
using System.IO;

namespace RHMG.Core.Tests
{
    public sealed class InMemoryFileStore : IFileStore
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();

        /// <summary>When true, WriteAllText throws IOException (simulates a full disk).</summary>
        public bool FailWrites;

        public bool Exists(string path)
        {
            return Files.ContainsKey(path);
        }

        public string ReadAllText(string path)
        {
            string text;
            if (!Files.TryGetValue(path, out text))
            {
                throw new FileNotFoundException(path);
            }

            return text;
        }

        public void WriteAllText(string path, string contents)
        {
            if (FailWrites)
            {
                throw new IOException("disk full");
            }

            Files[path] = contents;
        }

        public void Delete(string path)
        {
            Files.Remove(path);
        }

        public void Move(string source, string destination)
        {
            if (!Files.ContainsKey(source))
            {
                throw new FileNotFoundException(source);
            }

            if (Files.ContainsKey(destination))
            {
                throw new IOException("destination exists: " + destination);
            }

            Files[destination] = Files[source];
            Files.Remove(source);
        }

        public void EnsureDirectory(string directory)
        {
        }
    }
}
