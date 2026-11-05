using System.Collections.Generic;
using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    public static class WorldSkin
    {
        private static readonly Dictionary<string, Texture2D> s_Cache = new Dictionary<string, Texture2D>();
        private static readonly HashSet<string> s_LoggedMissing = new HashSet<string>();

        public static Texture2D Tex(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            if (s_Cache.TryGetValue(id, out Texture2D texture) && texture != null)
            {
                return texture;
            }

            texture = Resources.Load<Texture2D>("RHMG/World/" + id);
            if (texture == null)
            {
                if (s_LoggedMissing.Add(id))
                {
                    Debug.LogWarning("[WorldSkin] Missing texture Resources.Load<Texture2D>(\"RHMG/World/" + id + "\").");
                }
                return null;
            }

            s_Cache[id] = texture;
            return texture;
        }
    }
}
