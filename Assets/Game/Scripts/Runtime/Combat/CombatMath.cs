using UnityEngine;

namespace RHMG.Runtime.Combat
{
    public static class CombatMath
    {
        /// <summary>Half extents of the walkable area; CombatSession sets them for the combat or station arena.</summary>
        public static float LimitX = CombatTuning.ArenaLimitX;
        public static float LimitZ = CombatTuning.ArenaLimitZ;

        public static void SetLimits(float x, float z)
        {
            LimitX = x;
            LimitZ = z;
        }

        public static bool Overlap(Vector3 a, float radiusA, Vector3 b, float radiusB)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            float radius = radiusA + radiusB;
            return dx * dx + dz * dz <= radius * radius;
        }

        public static Vector3 ClampArena(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -LimitX, LimitX);
            position.z = Mathf.Clamp(position.z, -LimitZ, LimitZ);
            position.y = 0f;
            return position;
        }

        public static Vector3 Flat(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        public static Vector3 Direction(Vector3 from, Vector3 to)
        {
            return Flat(to - from).normalized;
        }
    }
}
