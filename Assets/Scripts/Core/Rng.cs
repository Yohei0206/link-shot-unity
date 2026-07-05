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
    }
}
