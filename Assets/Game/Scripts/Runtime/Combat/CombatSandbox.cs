using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Combat
{
    public sealed class CombatSandbox : MonoBehaviour
    {
        public bool AutoRunOnStart;
        public CombatSetup Setup;

        private CombatSession session;
        private int floor;
        private float floorElapsed;
        private float runElapsed;
        private bool stopped;

        private void Start()
        {
            if (AutoRunOnStart)
            {
                Begin();
            }
        }

        public void Begin()
        {
            if (session != null)
            {
                return;
            }

            if (Setup.Stats == null)
            {
                Setup.Stats = StatsCalculator.Compute(null, null, 0, false);
            }

            if (Setup.SpiritId == null)
            {
                Setup.SpiritId = "spirit.dew";
            }

            session = CombatSession.Create(transform, Setup);
            session.FloorCleared += NextFloor;
            session.PlayerDied += HandlePlayerDied;
            floor = 0;
            runElapsed = 0f;
            BeginFloor();
        }

        private void Update()
        {
            if (session == null || !session.Running || stopped)
            {
                return;
            }

            floorElapsed += Time.deltaTime;
            runElapsed += Time.deltaTime;
            float time = runElapsed;
            Vector2 movement = new Vector2(Mathf.Sin(time * 0.83f), Mathf.Cos(time * 0.61f));
            session.SetMoveInput(movement.normalized);
            if (session.CommandCooldown01 <= 0f)
            {
                session.TryCommand();
            }
        }

        private void BeginFloor()
        {
            floor++;
            if (floor > 5)
            {
                Debug.Log("[CombatSandbox] all floors cleared");
                session.Running = false;
                return;
            }

            FloorDefinition definition = Catalog.GetFloor(floor);
            if (definition == null)
            {
                Debug.LogError("[CombatSandbox] Catalog.GetFloor(" + floor + ") returned null");
                session.Running = false;
                stopped = true;
                return;
            }

            floorElapsed = 0f;
            session.StartFloor(definition);
            session.Running = true;
        }

        private void NextFloor()
        {
            Debug.Log("[CombatSandbox] floor " + floor + " cleared t=" + floorElapsed.ToString("0.0") + "s");
            session.Running = false;
            BeginFloor();
        }

        private void HandlePlayerDied()
        {
            stopped = true;
            Debug.LogError("[CombatSandbox] player died on floor " + floor + " at t=" + floorElapsed.ToString("0.0") + "s");
        }
    }
}
