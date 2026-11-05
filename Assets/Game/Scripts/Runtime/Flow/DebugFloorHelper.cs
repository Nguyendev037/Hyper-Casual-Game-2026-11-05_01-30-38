#if UNITY_EDITOR || DEVELOPMENT_BUILD
using RHMG.Runtime.Combat;
using UnityEngine;

namespace RHMG.Runtime
{
    static class DebugFloorHelper
    {
        public static void ClearSpawnedEnemies(CombatSession session)
        {
            EnemyController[] enemies = session.GetComponentsInChildren<EnemyController>(true);
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null && enemy.Alive)
                {
                    enemy.TakeDamage(float.MaxValue, enemy.transform.position);
                }
            }
        }
    }
}
#endif
