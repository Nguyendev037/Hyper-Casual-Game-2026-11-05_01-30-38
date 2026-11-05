using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Combat
{
    /// <summary>Scene-less combat test bench: walks a fixed path and plays the first floors of the survival loop.</summary>
    public sealed class CombatSandbox : MonoBehaviour
    {
        public bool AutoRunOnStart;
        public CombatSetup Setup;
        public int LastFloor = 5;

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

            if (Setup.SpiritId == null && Setup.Spirits == null)
            {
                Setup.SpiritId = "spirit.dew";
            }

            session = CombatSession.Create(transform, Setup);
            session.FloorCleared += NextFloor;
            session.PlayerDied += HandlePlayerDied;
            session.XpCollected += _ => { };
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
        }

        private void BeginFloor()
        {
            floor++;
            if (floor > LastFloor)
            {
                Debug.Log("[CombatSandbox] all floors cleared");
                session.Running = false;
                return;
            }

            floorElapsed = 0f;
            session.StartFloor(floor);
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
