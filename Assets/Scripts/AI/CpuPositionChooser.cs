using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUの発射ポジション決定に関する判断（GAME_RULES.md 9章）。
    /// Weakは端の出目を避けたがる程度の簡単な傾向づけに留めるが、Strongは
    /// `state.Field.StarNearestLaunchPosition`(最高得点の的「星」に最も近い発射ポジション。
    /// Game層がショットごとに算出しCoreへセットする)へどれだけ近いかで判断する。
    /// </summary>
    public static class CpuPositionChooser
    {
        public static bool ChooseReroll(GameState state, int rolledPosition, CpuDifficulty difficulty, Rng rng)
        {
            if (difficulty == CpuDifficulty.Weak)
            {
                bool isEdge = rolledPosition == 1 || rolledPosition == GameConfig.LaunchPositionCount;
                return isEdge && rng.NextFloat01() < 0.2;
            }

            float target = state.Field.StarNearestLaunchPosition ?? (GameConfig.LaunchPositionCount + 1) / 2f;
            if (System.Math.Abs(rolledPosition - target) <= 1f)
            {
                return false;
            }

            return rng.NextFloat01() < 0.7;
        }

        /// <summary>
        /// POSITION_CHOICE発動時の自由選択。Weakは完全ランダム、Strongは星に近いポジションを選好する
        /// (2つ候補を引いて星に近い方を採用する、シンプルな中央寄せサンプリング)。
        /// </summary>
        public static int ChoosePosition(GameState state, CpuDifficulty difficulty, Rng rng)
        {
            int a = rng.NextInt(1, GameConfig.LaunchPositionCount + 1);

            if (difficulty == CpuDifficulty.Weak)
            {
                return a;
            }

            int b = rng.NextInt(1, GameConfig.LaunchPositionCount + 1);
            float target = state.Field.StarNearestLaunchPosition ?? (GameConfig.LaunchPositionCount + 1) / 2f;
            return System.Math.Abs(a - target) <= System.Math.Abs(b - target) ? a : b;
        }
    }
}
