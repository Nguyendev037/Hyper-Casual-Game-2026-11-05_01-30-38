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
            float back = height / Mathf.Tan(50f * Mathf.Deg2Rad);
            float centerZ = Mathf.Clamp(target.position.z, -CombatTuning.CameraFollowLimitZ,
                CombatTuning.CameraFollowLimitZ);
            transform.position = new Vector3(0f, height, centerZ - back);
        }

        private void RefreshSize()
        {
            if (cameraComponent == null)
            {
                return;
            }

            float aspect = Mathf.Max(0.1f, cameraComponent.aspect);
            cameraComponent.orthographicSize = Mathf.Max(CombatTuning.CameraMinSize,
                CombatTuning.CameraViewWidth / aspect / 2f);
        }
    }
}
