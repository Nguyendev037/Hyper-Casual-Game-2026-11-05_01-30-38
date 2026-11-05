using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime.UI
{
    /// <summary>Live character preview rendered into the design's 540 by 960 container.</summary>
    public sealed class WardrobePreview : MonoBehaviour
    {
        GameObject stage;
        RenderTexture texture;
        SpriteBillboard[] billboards;
        Camera previewCamera;
        public void Initialize(string head, string body, string staff)
        {
            texture = new RenderTexture(540, 960, 24, RenderTextureFormat.ARGB32);
            texture.Create();
            RawImage image = gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            stage = new GameObject("WardrobePreviewStage");
            stage.transform.position = new Vector3(1000, 1000, 1000);
            Quaternion viewRotation = Quaternion.identity;
            float highestDepth = float.NegativeInfinity;
            foreach (Camera active in Camera.allCameras)
            {
                if (!active.enabled || active.targetTexture != null || active.depth <= highestDepth) continue;
                viewRotation = active.transform.rotation;
                highestDepth = active.depth;
            }
            Camera camera = new GameObject("WardrobePreviewCamera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.transform.rotation = viewRotation;
            camera.transform.localPosition = new Vector3(0, 1.4f, 0) - viewRotation * Vector3.forward * 6f;
            camera.orthographic = true;
            camera.orthographicSize = 1.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 30;
            camera.targetTexture = texture;
            previewCamera = camera;
            camera.depth = -10;
            GirlRig rig = CharacterFactory.CreateGirl(stage.transform);
            rig.ApplyOutfit(RHMG.Core.OutfitSlot.Head, head);
            rig.ApplyOutfit(RHMG.Core.OutfitSlot.Body, body);
            rig.ApplyOutfit(RHMG.Core.OutfitSlot.Staff, staff);
            foreach (Transform child in rig.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            billboards = rig.GetComponentsInChildren<SpriteBillboard>();
            foreach (SpriteBillboard billboard in billboards) billboard.enabled = false;
        }
        void LateUpdate()
        {
            if (billboards == null) return;
            foreach (SpriteBillboard billboard in billboards) billboard.Advance(Time.unscaledDeltaTime);
        }
        public void RenderNow()
        {
            if (previewCamera != null) previewCamera.Render();
        }
        void OnDestroy()
        {
            if (stage != null) Destroy(stage);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
