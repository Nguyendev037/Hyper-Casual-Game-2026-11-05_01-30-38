using UnityEngine;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        Camera menuCamera;
        Light gameLight;
        DayNightView dayNightView;
        PostFxSetup postFxSetup;

        void CreateGameEnvironment()
        {
            GameObject cameraObject = new GameObject("MenuCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 12f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            menuCamera = cameraObject.AddComponent<Camera>();
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 8f;
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.depth = -1f;
            cameraObject.AddComponent<AudioListener>();

            GameObject lightObject = new GameObject("GameDirectionalLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            gameLight = lightObject.AddComponent<Light>();
            gameLight.type = LightType.Directional;
            gameLight.color = new Color(1f, 0.89f, 0.69f);
            gameLight.intensity = 1.1f;
            gameLight.shadows = LightShadows.Soft;

            postFxSetup = gameObject.AddComponent<PostFxSetup>();
            postFxSetup.CreateGlobalVolume();
            postFxSetup.EnableCamera(menuCamera);

            dayNightView = gameObject.AddComponent<DayNightView>();
            dayNightView.Bind(clock, menuCamera, gameLight);
        }

        void SyncGameEnvironment()
        {
            if (menuCamera == null)
            {
                return;
            }

            Camera combatCamera = combat == null ? null : combat.GetComponentInChildren<Camera>();
            menuCamera.enabled = combatCamera == null || !combatCamera.enabled;
            if (combatCamera != null)
            {
                combatCamera.clearFlags = CameraClearFlags.SolidColor;
                dayNightView.Bind(clock, combatCamera, gameLight);
                postFxSetup.EnableCamera(combatCamera);
            }
            else
            {
                dayNightView.Bind(clock, menuCamera, gameLight);
                postFxSetup.EnableCamera(menuCamera);
            }
            dayNightView.PhaseOnlyWhenPaused = mode == Mode.Showcase;
        }
    }
}
