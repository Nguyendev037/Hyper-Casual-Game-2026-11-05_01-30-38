using UnityEngine;

namespace RHMG.Runtime.Combat
{
    /// <summary>Orthographic camera that follows the girl (lerp 8/s) and never shows past the arena walls (SPEC-P01 §2).</summary>
    public sealed class CombatCamera : MonoBehaviour
    {
        private const float Pitch = 50f;

        private Camera cameraComponent;
        private Transform target;
        private Vector3 center;
        private bool hasCenter;

        public static CombatCamera Create(Transform parent, Transform follow)
        {
            GameObject root = new GameObject("CombatCamera");
            root.transform.SetParent(parent, false);
            CombatCamera controller = root.AddComponent<CombatCamera>();
            controller.cameraComponent = root.AddComponent<Camera>();
            controller.cameraComponent.orthographic = true;
            // Billboards are transparent: sort by distance along the view axis, not by camera position.
            controller.cameraComponent.transparencySortMode = TransparencySortMode.Orthographic;
            controller.cameraComponent.transform.rotation = Quaternion.Euler(Pitch, 0f, 0f);
            controller.target = follow;
            controller.RefreshSize();
            controller.Snap();
            return controller;
        }

        private void LateUpdate()
        {
            RefreshSize();
            FollowTarget(Time.unscaledDeltaTime);
        }

        public void Snap()
        {
            hasCenter = false;
            FollowTarget(1f);
        }

        private void FollowTarget(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = DesiredCenter();
            if (!hasCenter)
            {
                center = desired;
                hasCenter = true;
            }
            else
            {
                center = Vector3.Lerp(center, desired, 1f - Mathf.Exp(-CombatTuning.CameraFollowRate * deltaTime));
            }

            float back = CombatTuning.CameraHeight / Mathf.Tan(Pitch * Mathf.Deg2Rad);
            transform.position = new Vector3(center.x, CombatTuning.CameraHeight, center.z - back);
        }

        // Ground point shown at screen centre: the girl, kept inside the walls (the view is centred when the arena is smaller).
        private Vector3 DesiredCenter()
        {
            float halfWidth = cameraComponent == null ? 0f : cameraComponent.orthographicSize * cameraComponent.aspect;
            float halfDepth = cameraComponent == null ? 0f : cameraComponent.orthographicSize / Mathf.Sin(Pitch * Mathf.Deg2Rad);
            float wallX = CombatMath.LimitX + 0.8f;
            float wallZ = CombatMath.LimitZ + 0.8f;
            float limitX = Mathf.Max(0f, wallX - halfWidth);
            float limitZ = Mathf.Max(0f, wallZ - halfDepth);
            return new Vector3(Mathf.Clamp(target.position.x, -limitX, limitX), 0f,
                Mathf.Clamp(target.position.z, -limitZ, limitZ));
        }

        private void RefreshSize()
        {
            if (cameraComponent == null)
            {
                return;
            }

            // Fixed view width (15 m) on every aspect: taller phones simply see more depth.
            float size = CombatTuning.CameraViewWidth / (2f * Mathf.Max(0.3f, cameraComponent.aspect));
            cameraComponent.orthographicSize = Mathf.Max(CombatTuning.CameraMinSize, size);
        }
    }
}
