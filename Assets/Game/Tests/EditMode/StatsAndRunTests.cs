using System;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    public class StatsAndRunTests
    {
        private static readonly string[] DefaultOutfits =
        {
            "outfit.head.ash_hat", "outfit.body.journey_cloak", "outfit.staff.sprout"
        };

        [Test]
        public void Stats_BaseValuesWithoutAnything()
        {
            PlayerStats s = StatsCalculator.Compute(new string[0], new string[0], 0, false);
            Assert.AreEqual(100f, s.MaxHp, 1e-4f);
            Assert.AreEqual(4f, s.MoveSpeed, 1e-4f);
            Assert.AreEqual(10f, s.StaffDamage, 1e-4f);
            Assert.AreEqual(0.65f, s.AttackInterval, 1e-4f);
            Assert.AreEqual(6f, s.Range, 1e-4f);
            Assert.AreEqual(6f, s.DogDamage, 1e-4f);
            Assert.AreEqual(1.2f, s.DogInterval, 1e-4f);
            Assert.AreEqual(12f, s.CommandCooldown, 1e-4f);
        }

        [Test]
        public void Stats_DefaultOutfitsApplyModifiers()
        {
            PlayerStats s = StatsCalculator.Compute(DefaultOutfits, new string[0], 0, false);
            Assert.AreEqual(4.2f, s.MoveSpeed, 1e-4f);
            Assert.AreEqual(9.5f, s.StaffDamage, 1e-4f);
            Assert.AreEqual(1.08f, s.BurnPower, 1e-4f);
            Assert.AreEqual(1.10f, s.WoodHealPower, 1e-4f);
        }

        [Test]
        public void Stats_OutfitBonusClampedToTwentyPercent()
        {
            Assert.AreEqual(0.20f, StatsCalculator.ClampOutfitBonus(0.55f), 1e-6f);
            Assert.AreEqual(-0.20f, StatsCalculator.ClampOutfitBonus(-0.55f), 1e-6f);
            Assert.AreEqual(0.08f, StatsCalculator.ClampOutfitBonus(0.08f), 1e-6f);
        }

        [Test]
        public void Stats_MountedMultipliesMoveSpeedOnly()
        {
            PlayerStats foot = StatsCalculator.Compute(new string[0], new string[0], 0, false);
            PlayerStats ride = StatsCalculator.Compute(new string[0], new string[0], 0, true);
            Assert.AreEqual(foot.MoveSpeed * 1.15f, ride.MoveSpeed, 1e-4f);
            Assert.AreEqual(foot.MaxHp, ride.MaxHp, 1e-4f);
        }

        [Test]
        public void Stats_UpgradeStacksAddAndCapAtMaxStacks()
        {
            string[] twice = { "upgrade.staff_dmg", "upgrade.staff_dmg" };
            PlayerStats two = StatsCalculator.Compute(new string[0], twice, 0, false);
            Assert.AreEqual(13f, two.StaffDamage, 1e-4f);

            string[] thrice = { "upgrade.staff_dmg", "upgrade.staff_dmg", "upgrade.staff_dmg" };
            PlayerStats three = StatsCalculator.Compute(new string[0], thrice, 0, false);
            Assert.AreEqual(13f, three.StaffDamage, 1e-4f);
        }

        [Test]
        public void Stats_FlatHpAttackSpeedAndDogMultiplier()
        {
            string[] ups = { "upgrade.max_hp", "upgrade.atk_speed", "upgrade.dog_dmg" };
            PlayerStats s = StatsCalculator.Compute(new string[0], ups, 0x03, false);
            Assert.AreEqual(115f, s.MaxHp, 1e-4f);
            Assert.AreEqual(0.585f, s.AttackInterval, 1e-4f);
            Assert.AreEqual(6f * 1.2f * 1.10f, s.DogDamage, 1e-4f);
        }

        [Test]
        public void Stats_AttackIntervalAndCooldownHaveFloors()
        {
            string[] ups =
            {
                "upgrade.atk_speed", "upgrade.atk_speed", "upgrade.cmd_cd", "upgrade.cmd_cd"
            };
            PlayerStats s = StatsCalculator.Compute(new string[0], ups, 0, false);
            Assert.AreEqual(0.52f, s.AttackInterval, 1e-4f);
            Assert.AreEqual(8.4f, s.CommandCooldown, 1e-4f);
            Assert.GreaterOrEqual(s.AttackInterval, 0.25f);
            Assert.GreaterOrEqual(s.CommandCooldown, 4f);
        }

        [Test]
        public void Run_FullValidFlowReachesRest()
        {
            SaveData data = SaveData.CreateDefault();
            RunController run = new RunController(data, new RewardService(data), 42);
            run.Start();
            Assert.AreEqual("run1-42", run.RunId);
            Assert.AreEqual(RunPhase.Combat, run.Phase);
            for (int floor = 1; floor <= 4; floor++)
            {
                Assert.AreEqual(floor, run.CurrentFloor);
                run.CompleteFloor();
                Assert.AreEqual(RunPhase.Upgrade, run.Phase);
                run.ChooseUpgrade(run.Upgrades.RollChoices()[0]);
            }

            Assert.AreEqual(5, run.CurrentFloor);
            Assert.AreEqual(40, run.CompleteFloor());
            Assert.AreEqual(RunPhase.Rescue, run.Phase);
            run.CompleteRescue();
            Assert.AreEqual(RunPhase.Rest, run.Phase);
            Assert.AreEqual(4 * 15 + 40, run.GoldEarnedThisRun);
            Assert.AreEqual(data.gold, run.GoldEarnedThisRun);
        }

        [Test]
        public void Run_InvalidTransitionsThrow()
        {
            SaveData data = SaveData.CreateDefault();
            RunController run = new RunController(data, new RewardService(data), 1);
            Assert.Throws<InvalidOperationException>(() => run.CompleteFloor());
            run.Start();
            Assert.Throws<InvalidOperationException>(() => run.Start());
            Assert.Throws<InvalidOperationException>(() => run.ChooseUpgrade("upgrade.max_hp"));
            Assert.Throws<InvalidOperationException>(() => run.CompleteRescue());
        }

        [Test]
        public void Run_DeathKeepsEarnedGoldAndBlocksFurtherProgress()
        {
            SaveData data = SaveData.CreateDefault();
            RunController run = new RunController(data, new RewardService(data), 9);
            run.Start();
            run.CompleteFloor();
            run.ChooseUpgrade("upgrade.max_hp");
            run.PlayerDied();
            Assert.AreEqual(RunPhase.Defeat, run.Phase);
            Assert.AreEqual(15, data.gold);
            Assert.Throws<InvalidOperationException>(() => run.PlayerDied());
            Assert.Throws<InvalidOperationException>(() => run.CompleteFloor());
        }

        [Test]
        public void Run_InvalidUpgradeIdDoesNotAdvanceFloor()
        {
            SaveData data = SaveData.CreateDefault();
            RunController run = new RunController(data, new RewardService(data), 3);
            run.Start();
            run.CompleteFloor();
            Assert.Throws<ArgumentException>(() => run.ChooseUpgrade("upgrade.bogus"));
            Assert.AreEqual(RunPhase.Upgrade, run.Phase);
            Assert.AreEqual(1, run.CurrentFloor);
        }

        [Test]
        public void Run_SecondRunGetsNewIdAndNoFirstClearBonus()
        {
            SaveData data = SaveData.CreateDefault();
            RewardService rewards = new RewardService(data);
            RunController first = new RunController(data, rewards, 5);
            first.Start();
            first.CompleteFloor();
            RunController second = new RunController(data, rewards, 5);
            second.Start();
            Assert.AreEqual("run2-5", second.RunId);
            Assert.AreEqual(10, second.CompleteFloor());
        }

        [Test]
        public void Run_PhaseChangedEventFiresInOrder()
        {
            SaveData data = SaveData.CreateDefault();
            RunController run = new RunController(data, new RewardService(data), 2);
            System.Collections.Generic.List<RunPhase> seen = new System.Collections.Generic.List<RunPhase>();
            run.PhaseChanged += p => seen.Add(p);
            run.Start();
            run.CompleteFloor();
            run.PlayerDied();
            CollectionAssert.AreEqual(new[] { RunPhase.Combat, RunPhase.Upgrade, RunPhase.Defeat }, seen);
        }
    }
}
