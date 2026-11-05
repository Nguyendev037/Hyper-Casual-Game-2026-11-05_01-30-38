using UnityEngine;
using UnityEngine.Rendering;

namespace RHMG.Runtime.Visuals
{
    /// <summary>One camera-facing textured quad (mesh, not SpriteRenderer, so the URP Particles/Unlit material binds _BaseMap).</summary>
    public sealed class VfxSprite : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock block;

        private MeshRenderer meshRenderer;
        private Texture currentTexture;

        public static VfxSprite Create(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = VfxAssets.Quad();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            VfxSprite sprite = go.AddComponent<VfxSprite>();
            sprite.meshRenderer = renderer;
            go.SetActive(false);
            return sprite;
        }

        /// <summary>Use the glow (additive) or the alpha-blended material.</summary>
        public void SetMaterial(bool additive)
        {
            meshRenderer.sharedMaterial = additive ? VfxAssets.Additive() : VfxAssets.Trail();
        }

        public void SetTexture(Texture texture)
        {
            currentTexture = texture;
            Apply(Color.white);
        }

        public void SetColor(Color color)
        {
            Apply(color);
        }

        public void SetSortingOrder(int order)
        {
            meshRenderer.sortingOrder = order;
        }

        public void SetSize(float size)
        {
            transform.localScale = new Vector3(size, size, 1f);
        }

        public void SetSize(float width, float height)
        {
            transform.localScale = new Vector3(width, height, 1f);
        }

        /// <summary>Face the camera (rotation = camera rotation keeps the quad screen-aligned).</summary>
        public void Face(Camera camera)
        {
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }

        private Color lastColor;

        private void Apply(Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            block.Clear();
            if (currentTexture != null)
            {
                block.SetTexture(BaseMapId, currentTexture);
                block.SetTexture(MainTexId, currentTexture);
            }

            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(block);
            lastColor = color;
        }

        public Color CurrentColor => lastColor;
    }
}
