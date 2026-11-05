using UnityEngine;

namespace RHMG.Runtime.Combat
{
    public sealed class CombatCamera : MonoBehaviour
    {
        private Camera cameraComponent;
        private Transform target;

        public static CombatCamera Create(Transform parent, Transform follow)
        {
            GameObject root = new GameObject("CombatCamera");
            root.transform.SetParent(parent, false);
            CombatCamera controller = root.AddComponent<CombatCamera>();
            controller.cameraComponent = root.AddComponent<Camera>();
            controller.cameraComponent.orthographic = true;
            // Billboards are transparent: sort by distance along the view axis, not by camera position.
            controller.cameraComponent.transparencySortMode = TransparencySortMode.Orthographic;
            controller.cameraComponent.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            controller.target = follow;
            controller.RefreshSize();
            controller.FollowTarget();
            return controller;
        }

        private void LateUpdate()
        {
            FollowTarget();
            RefreshSize();
        }

        private void FollowTarget()
        {
            if (target == null)
            {
                return;
            }

            float height = CombatTuning.CameraHeight;
            float back = height / Mathf.Tan(Pitch * Mathf.Deg2Rad);
            // Depth is fixed (fits the arena top-to-bottom); width depends on the phone, so follow X
            // and stop before the view leaves the arena walls.
            float halfView = cameraComponent == null ? 0f : cameraComponent.orthographicSize * cameraComponent.aspect;
            float limitX = Mathf.Max(0f, CombatTuning.ArenaWidth * 0.5f + 0.4f - halfView);
            float centerX = Mathf.Clamp(target.position.x, -limitX, limitX);
            transform.position = new Vector3(centerX, height, CombatTuning.CameraCenterZ - back);
        }

        private const float Pitch = 50f;

        private void RefreshSize()
        {
            if (cameraComponent == null)
            {
                return;
            }

            // Fit the arena DEPTH to the screen height on every aspect (it used to fit width, leaving
            // the bottom third of a portrait screen empty). Narrower phones follow the girl in X.
            cameraComponent.orthographicSize =
                CombatTuning.CameraVisibleDepth * Mathf.Sin(Pitch * Mathf.Deg2Rad) * 0.5f;
        }
    }
}
