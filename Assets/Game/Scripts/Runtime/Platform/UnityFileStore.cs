using System.IO;
using System.Text;
using RHMG.Core;

namespace RHMG.Runtime.Platform
{
    public sealed class UnityFileStore : IFileStore
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }

        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void Move(string source, string destination)
        {
            File.Move(source, destination);
        }

        public void EnsureDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }
    }
}
