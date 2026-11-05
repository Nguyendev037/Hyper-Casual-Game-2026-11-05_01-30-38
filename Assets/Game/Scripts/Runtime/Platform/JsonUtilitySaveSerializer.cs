using RHMG.Core;
using UnityEngine;

namespace RHMG.Runtime.Platform
{
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        public string Serialize(SaveData data)
        {
            return JsonUtility.ToJson(data);
        }

        public SaveData Deserialize(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                throw new System.FormatException("Save JSON không hợp lệ.");
            }
            return data;
        }
    }
}
