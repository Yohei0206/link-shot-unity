using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuShotAimPlannerTests
    {
        private static readonly string[] ValidDeck =
        {
            "WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA", "CURVE_SHOT_BETA", "SAFETY_NET_BETA", "RANGE_BOOST_GAMMA",
        };

        private static GameState MakeState(int launchPosition)
        {
            var state = new GameState(ValidDeck, ValidDeck);
            state.Field.LaunchPosition = launchPosition;
            return state;
        }

        private static double MeanAngleOffset(GameState state, CpuDifficulty difficulty, int trials)
        {
            double sum = 0;
            for (int seed = 0; seed < trials; seed++)
            {
                (float angleOffset, _) = CpuShotAimPlanner.GetAim(state, difficulty, new Rng(seed));
                sum += angleOffset;
            }

            return sum / trials;
        }

        [Test]
        public void GetAim_Weak_HasNoCenterBias_EvenFromEdgePosition()
        {
            double mean = MeanAngleOffset(MakeState(1), CpuDifficulty.Weak, 500);
            Assert.Less(System.Math.Abs(mean), 0.05, "Weak should not systematically bias toward center");
        }

        [Test]
        public void GetAim_Strong_BiasesTowardCenter_FromLeftEdgePosition()
        {
            double mean = MeanAngleOffset(MakeState(1), CpuDifficulty.Strong, 500);
            // 左端(position=1)から撃つ場合、右(正の角度)へ補正されるはず。
            Assert.Greater(mean, 0.1, "Strong should bias rightward (positive angle) when launching from the left edge");
        }

        [Test]
        public void GetAim_Strong_BiasesTowardCenter_FromRightEdgePosition()
        {
            double mean = MeanAngleOffset(MakeState(GameConfig.LaunchPositionCount), CpuDifficulty.Strong, 500);
            Assert.Less(mean, -0.1, "Strong should bias leftward (negative angle) when launching from the right edge");
        }

        [Test]
        public void GetAim_Strong_NoBias_FromCenterPosition()
        {
            // LaunchPositionCountが偶数(6)の場合、真の中心(3.5)に一致する整数ポジションは無いため、
            // 最も近いポジション(3)でもわずかな補正が入る。最大補正(端の場合)よりずっと小さいことだけ確認する。
            int centerPosition = (GameConfig.LaunchPositionCount + 1) / 2;
            double mean = MeanAngleOffset(MakeState(centerPosition), CpuDifficulty.Strong, 500);
            Assert.Less(System.Math.Abs(mean), GameConfig.CpuCenterAimBiasRadians / 2, "Launching from near the center should need much less correction than from the edge");
        }

        [Test]
        public void GetAim_Strong_BiasesTowardStarPosition_NotJustFieldCenter()
        {
            GameState state = MakeState(1); // 発射は左端から
            state.Field.StarNearestLaunchPosition = GameConfig.LaunchPositionCount; // 星は右端

            double mean = MeanAngleOffset(state, CpuDifficulty.Strong, 500);

            // 星が右端にあるので、フィールド中央より強く右へ補正されるはず。
            double meanWithoutStarHint = MeanAngleOffset(MakeState(1), CpuDifficulty.Strong, 500);
            Assert.Greater(mean, meanWithoutStarHint);
        }

        [Test]
        public void GetAim_Power_IsAlwaysWithinValidRange()
        {
            GameState state = MakeState(3);
            for (int seed = 0; seed < 200; seed++)
            {
                (_, float power) = CpuShotAimPlanner.GetAim(state, CpuDifficulty.Strong, new Rng(seed));
                Assert.IsTrue(power >= 0.2f && power <= 1.0f);
            }
        }
    }
}
