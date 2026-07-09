using System;
using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuPositionChooserTests
    {
        private static readonly string[] ValidDeck =
        {
            "WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA", "CURVE_SHOT_BETA", "SAFETY_NET_BETA", "RANGE_BOOST_GAMMA",
        };

        [Test]
        public void ChoosePosition_AlwaysReturnsAValidPosition()
        {
            var state = new GameState(ValidDeck, ValidDeck);

            for (int seed = 0; seed < 200; seed++)
            {
                int weak = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Weak, new Rng(seed));
                int strong = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Strong, new Rng(seed));

                Assert.IsTrue(weak >= 1 && weak <= GameConfig.LaunchPositionCount);
                Assert.IsTrue(strong >= 1 && strong <= GameConfig.LaunchPositionCount);
            }
        }

        [Test]
        public void ChoosePosition_Strong_IsCloserToCenterOnAverageThanWeak_WhenStarUnknown()
        {
            var state = new GameState(ValidDeck, ValidDeck); // StarNearestLaunchPosition未設定 = 中央にフォールバック
            float center = (GameConfig.LaunchPositionCount + 1) / 2f;
            double weakDistanceSum = 0;
            double strongDistanceSum = 0;
            const int trials = 500;

            for (int seed = 0; seed < trials; seed++)
            {
                int weak = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Weak, new Rng(seed));
                int strong = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Strong, new Rng(seed));

                weakDistanceSum += Math.Abs(weak - center);
                strongDistanceSum += Math.Abs(strong - center);
            }

            Assert.Less(strongDistanceSum / trials, weakDistanceSum / trials);
        }

        [Test]
        public void ChoosePosition_Strong_PrefersTheStarsPosition()
        {
            var state = new GameState(ValidDeck, ValidDeck);
            state.Field.StarNearestLaunchPosition = 1; // 星は一番左

            double strongDistanceSum = 0;
            double weakDistanceSum = 0;
            const int trials = 500;

            for (int seed = 0; seed < trials; seed++)
            {
                int strong = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Strong, new Rng(seed));
                int weak = CpuPositionChooser.ChoosePosition(state, CpuDifficulty.Weak, new Rng(seed));

                strongDistanceSum += Math.Abs(strong - 1);
                weakDistanceSum += Math.Abs(weak - 1);
            }

            Assert.Less(strongDistanceSum / trials, weakDistanceSum / trials);
        }

        [Test]
        public void ChooseReroll_Strong_RerollsWhenFarFromStar_KeepsWhenClose()
        {
            var state = new GameState(ValidDeck, ValidDeck);
            state.Field.StarNearestLaunchPosition = 3;

            // 星のポジション(3)そのものなら振り直さない。
            Assert.IsFalse(CpuPositionChooser.ChooseReroll(state, 3, CpuDifficulty.Strong, new Rng(1)));

            // 星から大きく離れたポジション(6)なら、高確率で振り直しを選ぶはず。
            int rerollCount = 0;
            const int trials = 200;
            for (int seed = 0; seed < trials; seed++)
            {
                if (CpuPositionChooser.ChooseReroll(state, 6, CpuDifficulty.Strong, new Rng(seed)))
                {
                    rerollCount++;
                }
            }

            Assert.Greater(rerollCount, trials / 2);
        }
    }
}
