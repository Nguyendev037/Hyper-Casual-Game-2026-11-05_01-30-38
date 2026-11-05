using UnityEngine;
using UnityEngine.Rendering;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Fake contact shadow: a soft dark disc under a character's feet, no real shadow.</summary>
    internal static class BlobShadow
    {
        private const float GroundOffset = 0.04f;
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.30f);

        public static Transform Create(Transform parent, float radius)
        {
            GameObject go = new GameObject("BlobShadow");
            if (parent != null) go.transform.SetParent(parent, false);

            float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
            if (parentScale < 0.0001f) parentScale = 1f;

            Transform tr = go.transform;
            tr.localPosition = new Vector3(0f, GroundOffset / parentScale, 0f);
            tr.localRotation = Quaternion.identity;
            tr.localScale = new Vector3(radius / parentScale, 1f, radius / parentScale);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = MeshBuilder.Disc(1f, 20);

            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = MaterialLibrary.UnlitTransparent(ShadowColor);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return tr;
        }
    }
}
