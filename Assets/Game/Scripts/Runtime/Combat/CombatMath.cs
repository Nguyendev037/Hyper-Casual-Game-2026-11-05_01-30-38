using UnityEngine;

namespace RHMG.Runtime.Combat
{
    public static class CombatMath
    {
        public static bool Overlap(Vector3 a, float radiusA, Vector3 b, float radiusB)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            float radius = radiusA + radiusB;
            return dx * dx + dz * dz <= radius * radius;
        }

        public static Vector3 ClampArena(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -CombatTuning.ArenaLimitX, CombatTuning.ArenaLimitX);
            position.z = Mathf.Clamp(position.z, -CombatTuning.ArenaLimitZ, CombatTuning.ArenaLimitZ);
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
