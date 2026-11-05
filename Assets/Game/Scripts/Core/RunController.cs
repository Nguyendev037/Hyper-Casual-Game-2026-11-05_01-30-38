using System;
using System.Collections.Generic;

namespace RHMG.Core
{
    public sealed class RunController
    {
        private readonly SaveData _data;
        private readonly RewardService _rewards;
        private readonly int _seed;
        private readonly LevelUpService _levelUps;
        private RunPhase _levelUpReturn = RunPhase.Combat;
        private List<LevelUpCard> _cards = new List<LevelUpCard>();

        public RunController(SaveData data, RewardService rewards, int seed)
        {
            _data = data;
            _rewards = rewards;
            _seed = seed;
            Upgrades = new RunUpgrades(new SeededRng(seed));
            _levelUps = new LevelUpService(new SeededRng(seed + 7919));
            Progress = new RunProgress();
            Spirits = new RunSpirits(data.unlockedSpiritIds);
            Phase = RunPhase.None;
            RunId = null;
        }

        public string RunId { get; private set; }
        public int CurrentFloor { get; private set; }
        public RunPhase Phase { get; private set; }
        public RunUpgrades Upgrades { get; }
        public RunProgress Progress { get; private set; }
        public RunSpirits Spirits { get; private set; }
        public int GoldEarnedThisRun { get; private set; }

        /// <summary>Runtime writes the player's current HP here; LeaveStation stores it in the checkpoint.</summary>
        public float Hp { get; set; }

        /// <summary>Cards currently offered (valid in phase LevelUp).</summary>
        public IReadOnlyList<LevelUpCard> Cards
        {
            get { return _cards; }
        }

        /// <summary>Spirit rescued at the current station (floor 5/10/15), or null. Valid in phase Station.</summary>
        public string StationRescueSpiritId { get; private set; }

        public event Action<RunPhase> PhaseChanged;
        public event Action<int> FloorChanged;

        public void Start()
        {
            Require(Phase == RunPhase.None, "Start");
            _data.runCounter++;
            RunId = "run" + _data.runCounter + "-" + _seed;
            CurrentFloor = 1;
            Progress = new RunProgress();
            Spirits = new RunSpirits(_data.unlockedSpiritIds);
            Spirits.Add("spirit.dew"); // trial spirit at the start (SPEC-P02 §2)
            SetPhase(RunPhase.Combat);
        }

        /// <summary>Rebuilds a run from a station checkpoint. False (and cp.valid=false) when the checkpoint is damaged.</summary>
        public bool ResumeFrom(RunCheckpoint cp)
        {
            Require(Phase == RunPhase.None, "ResumeFrom");
            if (cp == null || !cp.valid)
            {
                return false;
            }

            if (!SaveData.IsCheckpointStructurallyValid(cp))
            {
                cp.valid = false;
                return false;
            }

            RunSpirits spirits = new RunSpirits(_data.unlockedSpiritIds);
            spirits.Restore(cp.spirits);
            for (int i = 0; i < cp.takenCards.Count; i++)
            {
                Upgrades.Take(cp.takenCards[i]);
            }

            RunId = cp.runId;
            CurrentFloor = cp.floor;
            Progress = new RunProgress(cp.level, cp.xp);
            Spirits = spirits;
            Hp = cp.hp;
            SetPhase(RunPhase.Combat);
            return true;
        }

        public int CompleteFloor()
        {
            Require(Phase == RunPhase.Combat, "CompleteFloor");
            int gold = _rewards.GrantFloorClear(RunId, CurrentFloor, FloorPlan.Kind(CurrentFloor) != FloorKind.Normal);
            GoldEarnedThisRun += gold;
            SyncStats();
            if (CurrentFloor >= SurvivorTuning.FloorCount)
            {
                if (_data.runCheckpoint != null)
                {
                    _data.runCheckpoint.valid = false;
                }

                SetPhase(RunPhase.Rescue);
            }
            else if (FloorPlan.StationAfter(CurrentFloor))
            {
                StationRescueSpiritId = SurvivorTuning.RescueSpiritAt(CurrentFloor);
                SetPhase(RunPhase.Station);
            }
            else
            {
                CurrentFloor++;
                RaiseFloorChanged();
            }

            return gold;
        }

        /// <summary>Combat or Station (chests) with Progress.PendingLevelUps &gt; 0. Rolls the first cards.</summary>
        public void OpenLevelUp()
        {
            Require(Phase == RunPhase.Combat || Phase == RunPhase.Station, "OpenLevelUp");
            Require(Progress.PendingLevelUps > 0, "OpenLevelUp (nothing pending)");
            _levelUpReturn = Phase;
            _cards = _levelUps.Roll(Upgrades, Spirits);
            SetPhase(RunPhase.LevelUp);
        }

        /// <summary>Applies one of the offered cards. Invalid card: throws, nothing changes.</summary>
        public void ChooseCard(LevelUpCard card)
        {
            Require(Phase == RunPhase.LevelUp, "ChooseCard");
            if (card == null)
            {
                throw new ArgumentNullException("card");
            }

            if (!_cards.Exists(c => c.Id == card.Id))
            {
                throw new ArgumentException("Card was not offered: " + card.Id, "card");
            }

            _levelUps.Apply(card, Upgrades, Spirits);
            Progress.ConsumeLevelUp();
            if (Progress.PendingLevelUps > 0)
            {
                _cards = _levelUps.Roll(Upgrades, Spirits);
                return;
            }

            _cards = new List<LevelUpCard>();
            SetPhase(_levelUpReturn);
        }

        /// <summary>Claims the station reward once: floor 5/10/15 rescue a spirit, floor 20+ gives gold. Returns the rescued spirit id or null.</summary>
        public string ClaimStationReward()
        {
            Require(Phase == RunPhase.Station, "ClaimStationReward");
            string txn = RunId + ":station:" + CurrentFloor;
            if (_data.rewardTransactionIds.Contains(txn))
            {
                return null;
            }

            _data.rewardTransactionIds.Add(txn);
            string spiritId = StationRescueSpiritId;
            if (spiritId != null)
            {
                SpiritDefinition def = Catalog.GetSpirit(spiritId);
                _rewards.GrantSpiritRescue(spiritId, def == null ? Element.None : def.Element);
                Spirits.Unlock(spiritId);
                Spirits.Add(spiritId);
                return spiritId;
            }

            if (CurrentFloor >= SurvivorTuning.FirstNoRescueStation)
            {
                _data.gold += SurvivorTuning.StationGoldNoRescue;
                GoldEarnedThisRun += SurvivorTuning.StationGoldNoRescue;
            }

            return null;
        }

        /// <summary>Station XP chest: returns the number of new level-ups (open them with OpenLevelUp).</summary>
        public int OpenXpChest()
        {
            Require(Phase == RunPhase.Station, "OpenXpChest");
            int ups = Progress.AddXp(XpCurve.ChestXp(Progress.Level), 1f);
            SyncStats();
            return ups;
        }

        /// <summary>Station card chest: one free choice (open it with OpenLevelUp).</summary>
        public void OpenCardChest()
        {
            Require(Phase == RunPhase.Station, "OpenCardChest");
            Progress.GrantFreeLevelUp();
        }

        public List<StationTile> RollStationTiles(IRng rng)
        {
            Require(Phase == RunPhase.Station, "RollStationTiles");
            return StationService.Roll(rng, Spirits, CurrentFloor);
        }

        /// <summary>Floor++, writes the checkpoint, back to Combat. Level-ups must be resolved first.</summary>
        public void LeaveStation()
        {
            Require(Phase == RunPhase.Station, "LeaveStation");
            Require(Progress.PendingLevelUps == 0, "LeaveStation (level-ups pending)");
            CurrentFloor++;
            StationRescueSpiritId = null;
            SyncStats();
            _data.runCheckpoint = BuildCheckpoint();
            SetPhase(RunPhase.Combat);
            RaiseFloorChanged();
        }

        public void CompleteRescue()
        {
            Require(Phase == RunPhase.Rescue, "CompleteRescue");
            SetPhase(RunPhase.Rest);
        }

        /// <summary>Defeat keeps the checkpoint: the next run resumes at the start of the current 5-floor block.</summary>
        public void PlayerDied()
        {
            Require(Phase == RunPhase.Combat || Phase == RunPhase.LevelUp || Phase == RunPhase.Station, "PlayerDied");
            SyncStats();
            SetPhase(RunPhase.Defeat);
        }

        private RunCheckpoint BuildCheckpoint()
        {
            RunCheckpoint cp = new RunCheckpoint();
            cp.valid = true;
            cp.runId = RunId;
            cp.seed = _seed;
            cp.floor = CurrentFloor;
            cp.level = Progress.Level;
            cp.xp = Progress.Xp;
            cp.hp = Hp;
            cp.takenCards = new List<string>(Upgrades.Taken);
            cp.spirits = new List<RunSpiritEntry>();
            for (int i = 0; i < Spirits.Slots.Count; i++)
            {
                RunSpiritEntry e = new RunSpiritEntry();
                e.spiritId = Spirits.Slots[i].spiritId;
                e.level = Spirits.Slots[i].level;
                cp.spirits.Add(e);
            }

            return cp;
        }

        private void SyncStats()
        {
            if (Progress.Level > _data.highestLevelReached)
            {
                _data.highestLevelReached = Progress.Level;
            }
        }

        private void RaiseFloorChanged()
        {
            if (FloorChanged != null)
            {
                FloorChanged(CurrentFloor);
            }
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
