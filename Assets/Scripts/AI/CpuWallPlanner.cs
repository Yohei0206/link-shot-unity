using System.Collections.Generic;
using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUの壁配置（GAME_RULES.md 9章）。常設壁1枚はランダムなセルに置き、
    /// 使い捨て壁カードは残りラウンド数と点差に応じて温存/消費を決める簡単なヒューリスティック。
    /// </summary>
    public static class CpuWallPlanner
    {
        public static (int DefaultCell, List<int> DisposableCells) PlanWalls(GameState state, int defenderPlayer, CpuDifficulty difficulty, Rng rng)
        {
            var occupied = new HashSet<int>();
            int defaultCell = rng.NextInt(0, GameConfig.WallGridCellCount);
            occupied.Add(defaultCell);

            PlayerState defender = state.Players[defenderPlayer];
            PlayerState opponent = state.Players[1 - defenderPlayer];

            int spendCount = DecideSpendCount(state, defender, opponent, difficulty, rng);
            var disposableCells = new List<int>();

            int attempts = 0;
            int maxAttempts = GameConfig.WallGridCellCount * 2;
            while (disposableCells.Count < spendCount && attempts < maxAttempts)
            {
                int cell = rng.NextInt(0, GameConfig.WallGridCellCount);
                if (occupied.Add(cell))
                {
                    disposableCells.Add(cell);
                }

                attempts++;
            }

            return (defaultCell, disposableCells);
        }

        private static int DecideSpendCount(GameState state, PlayerState defender, PlayerState opponent, CpuDifficulty difficulty, Rng rng)
        {
            int maxSpend = defender.DisposableWallCardsRemaining;
            if (maxSpend == 0)
            {
                return 0;
            }

            int scoreDiff = defender.Score - opponent.Score;
            int roundsRemaining = GameConfig.RoundCount - state.Round;
            bool earlyGame = roundsRemaining > GameConfig.RoundCount * GameConfig.CpuWallSpendBehindThreshold;

            if (scoreDiff < 0 && earlyGame)
            {
                // 負けていてまだ試合序盤なら、使い捨て壁を温存する。
                int conservativeCap = difficulty == CpuDifficulty.Strong ? 1 : 0;
                return System.Math.Min(maxSpend, conservativeCap);
            }

            int aggressiveCap = difficulty == CpuDifficulty.Strong ? 2 : 1;
            return System.Math.Min(maxSpend, rng.NextInt(0, aggressiveCap + 1));
        }
    }
}
