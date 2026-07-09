using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuEffectChoiceSelectorTests
    {
        private static readonly string[] ValidDeck =
        {
            "WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA", "CURVE_SHOT_BETA", "SAFETY_NET_BETA", "RANGE_BOOST_GAMMA",
        };

        private static GameState MakeStateWithWalls()
        {
            var state = new GameState(ValidDeck, ValidDeck);
            state.Field.DefenderWalls.Add(new WallPlacement(3, isDefaultWall: true));
            state.Field.DefenderWalls.Add(new WallPlacement(7, isDefaultWall: false));
            return state;
        }

        [Test]
        public void Choose_WallRemove_TargetsAnExistingWallCell()
        {
            var state = MakeStateWithWalls();

            for (int seed = 0; seed < 30; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallRemove, CpuDifficulty.Weak, new Rng(seed));
                Assert.IsTrue(choice.WallTargetCellIndex == 3 || choice.WallTargetCellIndex == 7);
            }
        }

        [Test]
        public void Choose_WallRemove_Strong_TargetsWallClosestToStarColumn()
        {
            var state = MakeStateWithWalls();
            state.Field.StarWallColumn = 7; // 列7(セル7と同じ列)に星がある想定

            EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallRemove, CpuDifficulty.Strong, new Rng(1));
            Assert.AreEqual(7, choice.WallTargetCellIndex);
        }

        [Test]
        public void Choose_WallShift_MovesToAnUnoccupiedCell()
        {
            var state = MakeStateWithWalls();

            for (int seed = 0; seed < 30; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallShift, CpuDifficulty.Weak, new Rng(seed));
                Assert.IsTrue(choice.WallTargetCellIndex == 3 || choice.WallTargetCellIndex == 7);
                Assert.AreNotEqual(3, choice.WallDestinationCellIndex);
                Assert.AreNotEqual(7, choice.WallDestinationCellIndex);
                Assert.IsTrue(choice.WallDestinationCellIndex >= 0 && choice.WallDestinationCellIndex < GameConfig.WallGridCellCount);
            }
        }

        [Test]
        public void Choose_WallShift_Strong_MovesWallAwayFromStarColumn()
        {
            var state = MakeStateWithWalls();
            state.Field.StarWallColumn = 7;

            EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallShift, CpuDifficulty.Strong, new Rng(1));

            Assert.AreEqual(7, choice.WallTargetCellIndex);
            int destinationColumn = choice.WallDestinationCellIndex.Value % GameConfig.WallGridColumns;
            Assert.Greater(System.Math.Abs(destinationColumn - 7), 2, "Strong should move the wall far from the star's column");
        }

        [Test]
        public void Choose_BounceBoard_ReturnsPositionWithinSafeRange()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(new GameState(ValidDeck, ValidDeck), EffectId.BounceBoard, CpuDifficulty.Weak, new Rng(seed));
                Assert.IsNotNull(choice.BouncePosition);
                Assert.IsTrue(choice.BouncePosition.Value.X >= 0f && choice.BouncePosition.Value.X <= 1f);
                Assert.IsTrue(choice.BouncePosition.Value.Y >= 0f && choice.BouncePosition.Value.Y <= 1f);
            }
        }

        [Test]
        public void Choose_WideGate_ReturnsAZone()
        {
            EffectChoice choice = CpuEffectChoiceSelector.Choose(new GameState(ValidDeck, ValidDeck), EffectId.WideGate, CpuDifficulty.Weak, new Rng(1));
            Assert.IsNotNull(choice.WideGateZone);
        }

        [Test]
        public void Choose_WideGate_Strong_AlwaysTargetsScore500()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(new GameState(ValidDeck, ValidDeck), EffectId.WideGate, CpuDifficulty.Strong, new Rng(seed));
                Assert.AreEqual(TargetZoneId.Score500, choice.WideGateZone);
            }
        }
    }
}
