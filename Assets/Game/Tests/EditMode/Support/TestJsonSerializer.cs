#if !UNITY_5_3_OR_NEWER
using System.Text.Json;
#else
using UnityEngine;
#endif

namespace RHMG.Core.Tests
{
    /// <summary>JSON serializer for tests: System.Text.Json under dotnet, JsonUtility inside Unity.</summary>
    public sealed class TestJsonSerializer : ISaveSerializer
    {
#if !UNITY_5_3_OR_NEWER
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };

        public string Serialize(SaveData d)
        {
            return JsonSerializer.Serialize(d, Options);
        }

        public SaveData Deserialize(string json)
        {
            return JsonSerializer.Deserialize<SaveData>(json, Options);
        }
#else
        public string Serialize(SaveData d)
        {
            return JsonUtility.ToJson(d);
        }

        public SaveData Deserialize(string json)
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
#endif
    }
}
