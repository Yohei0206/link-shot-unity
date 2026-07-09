using System;
using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuPositionChooserTests
    {
        [Test]
        public void ChoosePosition_AlwaysReturnsAValidPosition()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                int weak = CpuPositionChooser.ChoosePosition(CpuDifficulty.Weak, new Rng(seed));
                int strong = CpuPositionChooser.ChoosePosition(CpuDifficulty.Strong, new Rng(seed));

                Assert.IsTrue(weak >= 1 && weak <= GameConfig.LaunchPositionCount);
                Assert.IsTrue(strong >= 1 && strong <= GameConfig.LaunchPositionCount);
            }
        }

        [Test]
        public void ChoosePosition_Strong_IsCloserToCenterOnAverageThanWeak()
        {
            float center = (GameConfig.LaunchPositionCount + 1) / 2f;
            double weakDistanceSum = 0;
            double strongDistanceSum = 0;
            const int trials = 500;

            for (int seed = 0; seed < trials; seed++)
            {
                int weak = CpuPositionChooser.ChoosePosition(CpuDifficulty.Weak, new Rng(seed));
                int strong = CpuPositionChooser.ChoosePosition(CpuDifficulty.Strong, new Rng(seed));

                weakDistanceSum += Math.Abs(weak - center);
                strongDistanceSum += Math.Abs(strong - center);
            }

            Assert.Less(strongDistanceSum / trials, weakDistanceSum / trials);
        }
    }
}
