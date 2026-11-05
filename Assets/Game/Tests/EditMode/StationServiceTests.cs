using System.Collections.Generic;
using NUnit.Framework;

namespace RHMG.Core.Tests
{
    // T5: StationService.Roll (SPEC-P02 §4).
    public class StationServiceTests
    {
        private static RunSpirits WithPair()
        {
            RunSpirits s = new RunSpirits();
            s.Add("spirit.dew");
            s.Add("spirit.ember");
            s.LevelUp("spirit.dew");
            s.LevelUp("spirit.dew");
            s.LevelUp("spirit.ember");
            s.LevelUp("spirit.ember");
            return s;
        }

        [Test]
        public void Roll_ThreeDistinctTiles_ForManySeedsAndFloors()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                for (int floor = 5; floor <= 95; floor += 5)
                {
                    List<StationTile> tiles = StationService.Roll(new SeededRng(seed), WithPair(), floor);
                    Assert.AreEqual(3, tiles.Count);
                    Assert.AreEqual(3, new HashSet<StationTile>(tiles).Count);
                }
            }
        }

        [Test]
        public void Roll_FusionIsMandatoryWhenAPairExists_AndAbsentOtherwise()
        {
            RunSpirits plain = new RunSpirits();
            plain.Add("spirit.dew");
            for (int seed = 1; seed <= 200; seed++)
            {
                CollectionAssert.Contains(StationService.Roll(new SeededRng(seed), WithPair(), 15), StationTile.Fusion);
                CollectionAssert.DoesNotContain(StationService.Roll(new SeededRng(seed), plain, 15), StationTile.Fusion);
            }
        }

        [Test]
        public void Roll_HealIsMandatoryAfterFloorsDivisibleByTen()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                foreach (int floor in new[] { 10, 20, 50, 90 })
                {
                    CollectionAssert.Contains(StationService.Roll(new SeededRng(seed), WithPair(), floor), StationTile.Heal);
                }
            }
        }

        [Test]
        public void Roll_HealCanBeMissingAfterFloorFive()
        {
            bool missing = false;
            for (int seed = 1; seed <= 200 && !missing; seed++)
            {
                missing = !StationService.Roll(new SeededRng(seed), WithPair(), 5).Contains(StationTile.Heal);
            }

            Assert.IsTrue(missing);
        }

        [Test]
        public void Roll_SpiritUpOnlyWhenSomethingCanLevel()
        {
            RunSpirits maxed = new RunSpirits();
            maxed.Add("spirit.dew");
            while (maxed.LevelUp("spirit.dew"))
            {
            }

            for (int seed = 1; seed <= 100; seed++)
            {
                CollectionAssert.DoesNotContain(StationService.Roll(new SeededRng(seed), maxed, 5), StationTile.SpiritUp);
            }

            Assert.AreEqual(3, StationService.Roll(new SeededRng(1), maxed, 5).Count);
        }

        [Test]
        public void Roll_TwoArgumentOverload_StillGivesThreeTiles()
        {
            Assert.AreEqual(3, StationService.Roll(new SeededRng(4), WithPair()).Count);
            Assert.AreEqual(3, StationService.Roll(new SeededRng(4), null).Count);
        }
    }
}
