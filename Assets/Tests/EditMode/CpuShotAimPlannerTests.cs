using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuShotAimPlannerTests
    {
        [Test]
        public void GetAim_Weak_HasNoCenterBias_EvenFromEdgePosition()
        {
            double sum = 0;
            const int trials = 500;
            for (int seed = 0; seed < trials; seed++)
            {
                (float angleOffset, _) = CpuShotAimPlanner.GetAim(CpuDifficulty.Weak, 1, new Rng(seed));
                sum += angleOffset;
            }

            double mean = sum / trials;
            Assert.Less(System.Math.Abs(mean), 0.05, "Weak should not systematically bias toward center");
        }

        [Test]
        public void GetAim_Strong_BiasesTowardCenter_FromLeftEdgePosition()
        {
            double sum = 0;
            const int trials = 500;
            for (int seed = 0; seed < trials; seed++)
            {
                (float angleOffset, _) = CpuShotAimPlanner.GetAim(CpuDifficulty.Strong, 1, new Rng(seed));
                sum += angleOffset;
            }

            double mean = sum / trials;
            // 左端(position=1)から撃つ場合、右(正の角度)へ補正されるはず。
            Assert.Greater(mean, 0.1, "Strong should bias rightward (positive angle) when launching from the left edge");
        }

        [Test]
        public void GetAim_Strong_BiasesTowardCenter_FromRightEdgePosition()
        {
            double sum = 0;
            const int trials = 500;
            for (int seed = 0; seed < trials; seed++)
            {
                (float angleOffset, _) = CpuShotAimPlanner.GetAim(CpuDifficulty.Strong, GameConfig.LaunchPositionCount, new Rng(seed));
                sum += angleOffset;
            }

            double mean = sum / trials;
            Assert.Less(mean, -0.1, "Strong should bias leftward (negative angle) when launching from the right edge");
        }

        [Test]
        public void GetAim_Strong_NoBias_FromCenterPosition()
        {
            // LaunchPositionCountが偶数(6)の場合、真の中心(3.5)に一致する整数ポジションは無いため、
            // 最も近いポジション(3)でもわずかな補正が入る。最大補正(端の場合)よりずっと小さいことだけ確認する。
            int centerPosition = (GameConfig.LaunchPositionCount + 1) / 2;

            double sum = 0;
            const int trials = 500;
            for (int seed = 0; seed < trials; seed++)
            {
                (float angleOffset, _) = CpuShotAimPlanner.GetAim(CpuDifficulty.Strong, centerPosition, new Rng(seed));
                sum += angleOffset;
            }

            double mean = sum / trials;
            Assert.Less(System.Math.Abs(mean), GameConfig.CpuCenterAimBiasRadians / 2, "Launching from near the center should need much less correction than from the edge");
        }

        [Test]
        public void GetAim_Power_IsAlwaysWithinValidRange()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                (_, float power) = CpuShotAimPlanner.GetAim(CpuDifficulty.Strong, 3, new Rng(seed));
                Assert.IsTrue(power >= 0.2f && power <= 1.0f);
            }
        }
    }
}
