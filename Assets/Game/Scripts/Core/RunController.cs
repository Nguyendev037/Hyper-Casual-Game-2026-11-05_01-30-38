using System;

namespace RHMG.Core
{
    public sealed class RunController
    {
        private readonly SaveData _data;
        private readonly RewardService _rewards;
        private readonly int _seed;

        public RunController(SaveData data, RewardService rewards, int seed)
        {
            _data = data;
            _rewards = rewards;
            _seed = seed;
            Upgrades = new RunUpgrades(new SeededRng(seed));
            Phase = RunPhase.None;
            RunId = null;
        }

        public string RunId { get; private set; }
        public int CurrentFloor { get; private set; }
        public RunPhase Phase { get; private set; }
        public RunUpgrades Upgrades { get; }
        public int GoldEarnedThisRun { get; private set; }

        public event Action<RunPhase> PhaseChanged;

        public void Start()
        {
            Require(Phase == RunPhase.None, "Start");
            _data.runCounter++;
            RunId = "run" + _data.runCounter + "-" + _seed;
            CurrentFloor = 1;
            SetPhase(RunPhase.Combat);
        }

        public int CompleteFloor()
        {
            Require(Phase == RunPhase.Combat, "CompleteFloor");
            FloorDefinition floor = Catalog.GetFloor(CurrentFloor);
            bool guardian = floor != null && floor.IsGuardianFloor;
            int gold = _rewards.GrantFloorClear(RunId, CurrentFloor, guardian);
            GoldEarnedThisRun += gold;
            SetPhase(CurrentFloor < Catalog.FloorCount ? RunPhase.Upgrade : RunPhase.Rescue);
            return gold;
        }

        public void ChooseUpgrade(string id)
        {
            Require(Phase == RunPhase.Upgrade, "ChooseUpgrade");
            Upgrades.Take(id);
            CurrentFloor++;
            SetPhase(RunPhase.Combat);
        }

        public void CompleteRescue()
        {
            Require(Phase == RunPhase.Rescue, "CompleteRescue");
            SetPhase(RunPhase.Rest);
        }

        public void PlayerDied()
        {
            Require(Phase == RunPhase.Combat || Phase == RunPhase.Upgrade, "PlayerDied");
            SetPhase(RunPhase.Defeat);
        }

        private void Require(bool condition, string action)
        {
            if (!condition)
            {
                throw new InvalidOperationException(action + " is not allowed in phase " + Phase);
            }
        }

        private void SetPhase(RunPhase phase)
        {
            Phase = phase;
            if (PhaseChanged != null)
            {
                PhaseChanged(phase);
            }
        }
    }
}
