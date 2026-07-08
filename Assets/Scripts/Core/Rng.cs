using System;

namespace LinkShot.Core
{
    /// <summary>
    /// サイコロ（発射ポジション決定）用の乱数生成器。seedを注入できるためテストで決定的に扱える。
    /// WebGLでも動く System.Random ベース（System.Threading非依存）。
    /// </summary>
    public class Rng
    {
        private readonly Random _random;

        public Rng(int seed)
        {
            _random = new Random(seed);
        }

        public Rng() : this(Environment.TickCount)
        {
        }

        /// <summary>1〜GameConfig.LaunchPositionCount の一様乱数（GAME_RULES.md 6章）。</summary>
        public int RollPosition()
        {
            return _random.Next(1, GameConfig.LaunchPositionCount + 1);
        }

        /// <summary>min以上max未満の一様整数乱数（AI/の意思決定用）。</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        /// <summary>0.0以上1.0未満の一様乱数。</summary>
        public double NextFloat01()
        {
            return _random.NextDouble();
        }

        /// <summary>Box-Muller法による正規分布乱数（CPUのショット精度ノイズ等に使用）。</summary>
        public double NextGaussian(double mean, double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = _random.NextDouble();
            double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * standardNormal;
        }
    }
}
