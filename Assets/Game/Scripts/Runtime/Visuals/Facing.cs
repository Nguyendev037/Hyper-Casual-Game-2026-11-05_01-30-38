using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Smooth yaw toward a flat (XZ) direction. Shared by the girl, dog and enemy visuals.</summary>
    public struct Facing
    {
        public float Yaw;
        public float TargetYaw;
        bool initialized;

        public static bool HasDirection(Vector3 direction)
        {
            return direction.x * direction.x + direction.z * direction.z > 0.0001f;
        }

        public void Face(Vector3 direction)
        {
            if (!HasDirection(direction)) return;
            TargetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (!initialized)
            {
                Yaw = TargetYaw;
                initialized = true;
            }
        }

        /// <summary>Advances toward the target; returns the signed remaining turn (deg) for banking.</summary>
        public float Step(float deltaTime, float degreesPerSecond)
        {
            Yaw = Mathf.MoveTowardsAngle(Yaw, TargetYaw, degreesPerSecond * deltaTime);
            return Mathf.DeltaAngle(Yaw, TargetYaw);
        }

        public Quaternion Rotation => Quaternion.Euler(0f, Yaw, 0f);

        public Vector3 Forward => Rotation * Vector3.forward;
    }
}
