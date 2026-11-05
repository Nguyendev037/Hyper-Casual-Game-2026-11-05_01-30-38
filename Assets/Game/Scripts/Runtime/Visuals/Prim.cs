using UnityEngine;
using UnityEngine.Rendering;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Helper to instantiate and configure Unity primitive shapes.</summary>
    public static class Prim
    {
        public static GameObject Make(
            PrimitiveType t,
            Transform parent,
            Vector3 localPos,
            Vector3 localScale,
            Color c,
            string name,
            bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(t);
            go.name = string.IsNullOrEmpty(name) ? t.ToString() : name;

            Transform tr = go.transform;
            if (parent != null)
            {
                tr.SetParent(parent, false);
            }
            tr.localPosition = localPos;
            tr.localScale = localScale;
            tr.localRotation = Quaternion.identity;

            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(col);
                    else
                        Object.DestroyImmediate(col);
                }
            }

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = MaterialLibrary.Lit(c);

                float maxDim = Mathf.Max(localScale.x, Mathf.Max(localScale.y, localScale.z));
                if (maxDim < 0.3f)
                {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            return go;
        }
    }
}
