using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T6: RunController v2 phases, stations, checkpoint, resume (01-CONTRACTS §1-§2).
    public class RunControllerTests
    {
        private SaveData _data;
        private RunController _run;

        [SetUp]
        public void SetUp()
        {
            _data = SaveData.CreateDefault();
            _run = new RunController(_data, new RewardService(_data), 42);
            _run.Start();
        }

        private void ClearFloors(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _run.CompleteFloor();
                if (_run.Phase == RunPhase.Station)
                {
                    break;
                }
            }
        }

        [Test]
        public void Start_BeginsOnFloorOneWithTrialDew()
        {
            Assert.AreEqual(RunPhase.Combat, _run.Phase);
            Assert.AreEqual(1, _run.CurrentFloor);
            Assert.AreEqual(1, _run.Progress.Level);
            Assert.AreEqual(1, _run.Spirits.Slots.Count);
            Assert.AreEqual("spirit.dew", _run.Spirits.Slots[0].spiritId);
        }

        [Test]
        public void NormalFloors_StayInCombatAndAdvance()
        {
            List<int> floors = new List<int>();
            _run.FloorChanged += f => floors.Add(f);
            for (int i = 0; i < 4; i++)
            {
                _run.CompleteFloor();
                Assert.AreEqual(RunPhase.Combat, _run.Phase);
            }

            Assert.AreEqual(5, _run.CurrentFloor);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, floors);
        }

        [Test]
        public void LevelUp_CombatToLevelUpToCombat()
        {
            _run.Progress.AddXp(XpCurve.Required(1), 1f);
            List<RunPhase> seen = new List<RunPhase>();
            _run.PhaseChanged += p => seen.Add(p);
            _run.OpenLevelUp();
            Assert.AreEqual(RunPhase.LevelUp, _run.Phase);
            Assert.AreEqual(3, _run.Cards.Count);
            _run.ChooseCard(_run.Cards[0]);
            Assert.AreEqual(RunPhase.Combat, _run.Phase);
            Assert.AreEqual(0, _run.Progress.PendingLevelUps);
            CollectionAssert.AreEqual(new[] { RunPhase.LevelUp, RunPhase.Combat }, seen);
        }

        [Test]
        public void LevelUp_SeveralPending_StayInLevelUpUntilAllChosen()
        {
            _run.Progress.AddXp(XpCurve.Required(1) + XpCurve.Required(2), 1f);
            Assert.AreEqual(2, _run.Progress.PendingLevelUps);
            _run.OpenLevelUp();
            _run.ChooseCard(_run.Cards[0]);
            Assert.AreEqual(RunPhase.LevelUp, _run.Phase);
            Assert.AreEqual(3, _run.Cards.Count);
            _run.ChooseCard(_run.Cards[1]);
            Assert.AreEqual(RunPhase.Combat, _run.Phase);
        }

        [Test]
        public void OpenLevelUp_WithNothingPending_Throws_ChooseCardOutsideLevelUpThrows()
        {
            Assert.Throws<InvalidOperationException>(() => _run.OpenLevelUp());
            Assert.Throws<InvalidOperationException>(() => _run.ChooseCard(new LevelUpCard()));
        }

        [Test]
        public void ChooseCard_NotOffered_ThrowsAndKeepsPending()
        {
            _run.Progress.AddXp(XpCurve.Required(1), 1f);
            _run.OpenLevelUp();
            Assert.Throws<ArgumentException>(() => _run.ChooseCard(new LevelUpCard { Id = "upgrade.not_offered", Kind = CardKind.Stat, TargetId = "upgrade.range" }));
            Assert.Throws<ArgumentNullException>(() => _run.ChooseCard(null));
            Assert.AreEqual(1, _run.Progress.PendingLevelUps);
            Assert.AreEqual(RunPhase.LevelUp, _run.Phase);
        }

        [Test]
        public void FloorFive_GoesToStation_LeaveStationAdvancesToSix()
        {
            ClearFloors(5);
            Assert.AreEqual(RunPhase.Station, _run.Phase);
            Assert.AreEqual(5, _run.CurrentFloor);
            _run.LeaveStation();
            Assert.AreEqual(RunPhase.Combat, _run.Phase);
            Assert.AreEqual(6, _run.CurrentFloor);
        }

        [Test]
        public void LeaveStation_WritesCheckpointOfTheNextFloor()
        {
            _run.Progress.AddXp(XpCurve.Required(1) + 3, 1f);
            _run.OpenLevelUp();
            _run.ChooseCard(_run.Cards[0]);
            _run.Hp = 77f;
            ClearFloors(5);
            _run.LeaveStation();
            RunCheckpoint cp = _data.runCheckpoint;
            Assert.IsNotNull(cp);
            Assert.IsTrue(cp.valid);
            Assert.AreEqual(6, cp.floor);
            Assert.AreEqual(2, cp.level);
            Assert.AreEqual(3, cp.xp);
            Assert.AreEqual(77f, cp.hp, 1e-4f);
            Assert.AreEqual(42, cp.seed);
            Assert.AreEqual(_run.RunId, cp.runId);
            Assert.IsTrue(SaveData.IsCheckpointStructurallyValid(cp));
            Assert.GreaterOrEqual(_data.highestLevelReached, 2);
        }

        [Test]
        public void LeaveStation_WithPendingLevelUps_Throws()
        {
            ClearFloors(5);
            _run.OpenCardChest();
            Assert.Throws<InvalidOperationException>(() => _run.LeaveStation());
            _run.OpenLevelUp();
            Assert.AreEqual(RunPhase.LevelUp, _run.Phase);
            _run.ChooseCard(_run.Cards[0]);
            Assert.AreEqual(RunPhase.Station, _run.Phase); // returns to the station, not to Combat
            _run.LeaveStation();
        }

        [Test]
        public void StationXpChest_CanTriggerLevelUps()
        {
            ClearFloors(5);
            _run.Progress.AddXp(XpCurve.Required(1) - 1, 1f); // 1 XP short of level 2; the chest tips it over
            int ups = _run.OpenXpChest();
            Assert.GreaterOrEqual(ups, 1);
            Assert.AreEqual(ups, _run.Progress.PendingLevelUps);
        }

        [Test]
        public void Defeat_KeepsCheckpoint_ResumeRestoresTheRun()
        {
            _run.Progress.AddXp(XpCurve.Required(1), 1f);
            _run.OpenLevelUp();
            _run.ChooseCard(_run.Cards[0]);
            ClearFloors(5);
            _run.LeaveStation();
            int level = _run.Progress.Level;
            string runId = _run.RunId;
            _run.PlayerDied();
            Assert.AreEqual(RunPhase.Defeat, _run.Phase);
            Assert.IsTrue(_data.runCheckpoint.valid);

            RunController next = new RunController(_data, new RewardService(_data), 42);
            Assert.IsTrue(next.ResumeFrom(_data.runCheckpoint));
            Assert.AreEqual(RunPhase.Combat, next.Phase);
            Assert.AreEqual(6, next.CurrentFloor);
            Assert.AreEqual(level, next.Progress.Level);
            Assert.AreEqual(runId, next.RunId);
            CollectionAssert.AreEqual(_run.Upgrades.Taken, next.Upgrades.Taken);
            Assert.AreEqual(_run.Spirits.Slots.Count, next.Spirits.Slots.Count);
            Assert.AreEqual(1, _data.runCounter); // resuming is not a new run
        }

        [Test]
        public void ResumeFrom_DamagedCheckpoint_ReturnsFalseAndInvalidates()
        {
            RunCheckpoint bad = new RunCheckpoint
            {
                valid = true, runId = "run1-1", seed = 1, floor = 101, level = 3, xp = 0, hp = 50f,
                takenCards = new List<string>(), spirits = new List<RunSpiritEntry>()
            };
            RunController next = new RunController(_data, new RewardService(_data), 1);
            Assert.IsFalse(next.ResumeFrom(bad));
            Assert.IsFalse(bad.valid);
            Assert.AreEqual(RunPhase.None, next.Phase);
            Assert.IsFalse(next.ResumeFrom(null));
        }

        [Test]
        public void FloorHundred_GoesToRescue_AndClearsCheckpoint()
        {
            _data.runCheckpoint = new RunCheckpoint
            {
                valid = true, runId = "x", floor = 96, level = 1, takenCards = new List<string>(), spirits = new List<RunSpiritEntry>()
            };
            RunController run = new RunController(_data, new RewardService(_data), 3);
            Assert.IsTrue(run.ResumeFrom(_data.runCheckpoint));
            for (int f = 96; f < 100; f++)
            {
                run.CompleteFloor();
                if (run.Phase == RunPhase.Station)
                {
                    run.LeaveStation();
                }
            }

            Assert.AreEqual(100, run.CurrentFloor);
            run.CompleteFloor();
            Assert.AreEqual(RunPhase.Rescue, run.Phase);
            Assert.IsFalse(_data.runCheckpoint.valid);
        }

        [Test]
        public void StationRescue_FollowsTheScheduleAndUnlocksPermanently()
        {
            ClearFloors(5);
            Assert.AreEqual("spirit.ember", _run.StationRescueSpiritId);
            Assert.AreEqual("spirit.ember", _run.ClaimStationReward());
            CollectionAssert.Contains(_data.unlockedSpiritIds, "spirit.ember");
            Assert.IsTrue(_run.Spirits.Has("spirit.ember"));
            Assert.IsNull(_run.ClaimStationReward()); // idempotent
            _run.LeaveStation();

            ClearFloors(5);
            Assert.AreEqual("spirit.dew", _run.StationRescueSpiritId);
            _run.ClaimStationReward();
            CollectionAssert.Contains(_data.unlockedSpiritIds, "spirit.dew");
            Assert.AreEqual(1, System.Linq.Enumerable.Count(_run.Spirits.Slots, e => e.spiritId == "spirit.dew"));
            _run.LeaveStation();

            ClearFloors(5);
            Assert.AreEqual("spirit.bud", _run.ClaimStationReward());
            Assert.AreEqual(3, _run.Spirits.Slots.Count);
            _run.LeaveStation();
        }

        [Test]
        public void StationFromFloorTwenty_GivesTwentyFiveGoldInsteadOfRescue()
        {
            for (int station = 5; station <= 20; station += 5)
            {
                ClearFloors(5);
                int before = _data.gold;
                string rescued = _run.ClaimStationReward();
                if (station < 20)
                {
                    Assert.IsNotNull(rescued);
                }
                else
                {
                    Assert.IsNull(rescued);
                    Assert.AreEqual(before + 25, _data.gold);
                    Assert.IsNull(_run.StationRescueSpiritId);
                }

                _run.LeaveStation();
            }
        }

        [Test]
        public void PlayerDied_AllowedInLevelUpAndStation()
        {
            _run.Progress.AddXp(XpCurve.Required(1), 1f);
            _run.OpenLevelUp();
            _run.PlayerDied();
            Assert.AreEqual(RunPhase.Defeat, _run.Phase);
        }

        [Test]
        public void RollStationTiles_OnlyInStation()
        {
            Assert.Throws<InvalidOperationException>(() => _run.RollStationTiles(new SeededRng(1)));
            ClearFloors(5);
            Assert.AreEqual(3, _run.RollStationTiles(new SeededRng(1)).Count);
        }
    }
}
