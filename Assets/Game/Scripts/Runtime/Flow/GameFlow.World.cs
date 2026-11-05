using RHMG.Runtime.Visuals;
using UnityEngine;

namespace RHMG.Runtime
{
    // SPEC-P04: the arena is reused between floors, so the chapter floor and props are re-applied on every floor.
    public sealed partial class GameFlow
    {
        void ApplyWorldTheme()
        {
            if (combat == null || run == null || combat.IsStation) return;
            try
            {
                ArenaBuilder.ApplyChapter(combat.transform.Find("TowerArena"), run.CurrentFloor);
            }
            catch (System.Exception error)
            {
                // Scenery must never stop a floor from starting.
                Debug.LogWarning("[RHMG.World] Chapter theme skipped: " + error.Message);
            }
        }
    }
}
