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
                EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallRemove, new Rng(seed));
                Assert.IsTrue(choice.WallTargetCellIndex == 3 || choice.WallTargetCellIndex == 7);
            }
        }

        [Test]
        public void Choose_WallShift_MovesToAnUnoccupiedCell()
        {
            var state = MakeStateWithWalls();

            for (int seed = 0; seed < 30; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(state, EffectId.WallShift, new Rng(seed));
                Assert.IsTrue(choice.WallTargetCellIndex == 3 || choice.WallTargetCellIndex == 7);
                Assert.AreNotEqual(3, choice.WallDestinationCellIndex);
                Assert.AreNotEqual(7, choice.WallDestinationCellIndex);
                Assert.IsTrue(choice.WallDestinationCellIndex >= 0 && choice.WallDestinationCellIndex < GameConfig.WallGridCellCount);
            }
        }

        [Test]
        public void Choose_BounceBoard_ReturnsPositionWithinSafeRange()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                EffectChoice choice = CpuEffectChoiceSelector.Choose(new GameState(ValidDeck, ValidDeck), EffectId.BounceBoard, new Rng(seed));
                Assert.IsNotNull(choice.BouncePosition);
                Assert.IsTrue(choice.BouncePosition.Value.X >= 0f && choice.BouncePosition.Value.X <= 1f);
                Assert.IsTrue(choice.BouncePosition.Value.Y >= 0f && choice.BouncePosition.Value.Y <= 1f);
            }
        }

        [Test]
        public void Choose_WideGate_ReturnsAZone()
        {
            EffectChoice choice = CpuEffectChoiceSelector.Choose(new GameState(ValidDeck, ValidDeck), EffectId.WideGate, new Rng(1));
            Assert.IsNotNull(choice.WideGateZone);
        }
    }
}
