using System.Linq;
using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuWallPlannerTests
    {
        private static readonly string[] ValidDeck =
        {
            "WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA", "CURVE_SHOT_BETA", "SAFETY_NET_BETA", "RANGE_BOOST_GAMMA",
        };

        [Test]
        public void PlanWalls_NeverReturnsDuplicateOrOutOfRangeCells()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var state = new GameState(ValidDeck, ValidDeck, rng: new Rng(seed));
                var (defaultCell, disposableCells) = CpuWallPlanner.PlanWalls(state, state.CurrentDefender, CpuDifficulty.Strong, new Rng(seed));

                var allCells = new[] { defaultCell }.Concat(disposableCells).ToList();

                Assert.AreEqual(allCells.Count, allCells.Distinct().Count(), $"seed={seed}: duplicate cell");
                Assert.IsTrue(allCells.All(c => c >= 0 && c < GameConfig.WallGridCellCount), $"seed={seed}: out of range cell");
            }
        }

        [Test]
        public void PlanWalls_NeverSpendsMoreDisposableCardsThanRemaining()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var state = new GameState(ValidDeck, ValidDeck, rng: new Rng(seed));
                int defender = state.CurrentDefender;
                state.Players[defender].DisposableWallCardsRemaining = seed % (GameConfig.DisposableWallCardCount + 1);

                var (_, disposableCells) = CpuWallPlanner.PlanWalls(state, defender, CpuDifficulty.Strong, new Rng(seed));

                Assert.LessOrEqual(disposableCells.Count, state.Players[defender].DisposableWallCardsRemaining);
            }
        }

        [Test]
        public void PlanWalls_Strong_ClustersAroundTheStarsColumn()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var state = new GameState(ValidDeck, ValidDeck, rng: new Rng(seed));
                int defender = state.CurrentDefender;
                state.Players[defender].DisposableWallCardsRemaining = GameConfig.DisposableWallCardCount;
                state.Field.StarWallColumn = 0; // 星は一番左の列

                var (defaultCell, disposableCells) = CpuWallPlanner.PlanWalls(state, defender, CpuDifficulty.Strong, new Rng(seed));

                int defaultColumn = defaultCell % GameConfig.WallGridColumns;
                Assert.LessOrEqual(defaultColumn, 1, $"seed={seed}: default wall should land near column 0");

                foreach (int cell in disposableCells)
                {
                    int column = cell % GameConfig.WallGridColumns;
                    Assert.LessOrEqual(column, GameConfig.DisposableWallCardCount + 1, $"seed={seed}: disposable wall should extend from near column 0");
                }
            }
        }
    }
}
