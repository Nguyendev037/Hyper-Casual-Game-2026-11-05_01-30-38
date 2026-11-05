using System;
using System.IO;
using System.Text;
using RHMG.Core;
using UnityEngine;

namespace RHMG.Runtime.Platform
{
    public sealed class CsvEventSink : IEventSink
    {
        readonly string path;

        public CsvEventSink(string filePath = null)
        {
            path = filePath ?? Path.Combine(Application.persistentDataPath, "events.csv");
        }

        public void Log(string eventName, string payload)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;
            using (var writer = new StreamWriter(path, true, new UTF8Encoding(false)))
            {
                if (needsHeader)
                {
                    writer.WriteLine("iso_time,event,payload");
                }
                writer.Write(Escape(DateTime.UtcNow.ToString("o")));
                writer.Write(',');
                writer.Write(Escape(eventName));
                writer.Write(',');
                writer.WriteLine(Escape(payload));
            }
        }

        static string Escape(string value)
        {
            string safe = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("\"", "\"\"");
            return "\"" + safe + "\"";
        }
    }
}
