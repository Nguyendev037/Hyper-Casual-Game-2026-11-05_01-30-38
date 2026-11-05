using RHMG.Core;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace RHMG.Runtime
{
    public sealed class DayNightView : MonoBehaviour
    {
        [System.NonSerialized]
        public DayNightClock Clock;

        [System.NonSerialized]
        public Camera TargetCamera;

        [System.NonSerialized]
        public Light MainLight;
        [System.NonSerialized]
        public bool PhaseOnlyWhenPaused;
        bool persistent;

        static readonly Color DayBackground = new Color(0.88f, 0.75f, 0.54f);
        static readonly Color NightBackground = new Color(0.18f, 0.24f, 0.37f);
        static readonly Color DayLight = new Color(1f, 0.89f, 0.69f);
        static readonly Color NightLight = new Color(0.56f, 0.65f, 0.85f);
        static readonly Color DayAmbient = new Color(0.65f, 0.58f, 0.45f);
        static readonly Color NightAmbient = new Color(0.35f, 0.4f, 0.55f);

        public void Bind(DayNightClock clock, Camera targetCamera, Light mainLight)
        {
            persistent = true;
            Clock = clock;
            TargetCamera = targetCamera;
            MainLight = mainLight;
            RenderSettings.ambientMode = AmbientMode.Trilight;
        }

        void LateUpdate()
        {
            if (!persistent || Clock == null)
            {
                return;
            }
            float blend = Clock.NightBlend;
            if (PhaseOnlyWhenPaused && Clock.Paused)
            {
                blend = Clock.Phase == DayPhase.Night ? 1f : 0f;
            }
            if (TargetCamera != null)
            {
                TargetCamera.backgroundColor = Color.Lerp(DayBackground, NightBackground, blend);
            }
            if (MainLight != null)
            {
                MainLight.color = Color.Lerp(DayLight, NightLight, blend);
                MainLight.intensity = Mathf.Lerp(1.1f, 0.75f, blend);
            }
            Color ambient = Color.Lerp(DayAmbient, NightAmbient, blend);
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = Color.Lerp(ambient, NightBackground, 0.25f);
            RenderSettings.ambientGroundColor = Color.Lerp(ambient, Palette.SandShade, 0.35f);
        }
    }
}
