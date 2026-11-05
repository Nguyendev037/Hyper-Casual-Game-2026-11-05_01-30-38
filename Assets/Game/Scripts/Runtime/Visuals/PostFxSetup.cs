using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RHMG.Runtime.Visuals
{
    internal sealed class PostFxSetup : MonoBehaviour
    {
        public void CreateGlobalVolume()
        {
            GameObject volumeObject = new GameObject("PostFx");
            volumeObject.transform.SetParent(transform, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.name = "RuntimePostFx";

            Bloom bloom = volume.profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.4f);

            Tonemapping tonemapping = volume.profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
            colorAdjustments.saturation.Override(10f);

            Vignette vignette = volume.profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
        }

        public void EnableCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            // URP strips UberPost from builds that do not look like they use post-processing (this Volume is
            // created in code, so Android builds strip it). Post on + missing UberPost = no 3D frame on screen.
            // Same test URP uses before logging "is not supported or has been stripped".
            cameraData.renderPostProcessing = UberPostAvailable();
        }

        static bool? s_UberPostAvailable;

        static bool UberPostAvailable()
        {
            if (s_UberPostAvailable == null)
            {
                // Capability probe of a URP internal shader (not material loading, so not stripped by our use).
                Shader uber = Shader.Find("Hidden/Universal Render Pipeline/UberPost");
                s_UberPostAvailable = uber != null && uber.isSupported;
                if (s_UberPostAvailable == false)
                {
                    Debug.LogWarning("[RHMG.PostFx] UberPost unavailable on this build/device: post-processing off");
                }
            }
            return s_UberPostAvailable.Value;
        }
    }
}
