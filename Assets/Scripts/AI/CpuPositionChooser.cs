using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUの発射ポジション決定に関する判断（GAME_RULES.md 9章）。
    /// 実際の的配置はGame層にしかないため、狙いの巧拙は主にCpuShotAimPlannerのノイズで表現し、
    /// ここでは端の出目を避けたがる程度の簡単な傾向づけに留める。
    /// </summary>
    public static class CpuPositionChooser
    {
        public static bool ChooseReroll(int rolledPosition, CpuDifficulty difficulty, Rng rng)
        {
            bool isEdge = rolledPosition == 1 || rolledPosition == GameConfig.LaunchPositionCount;
            if (!isEdge)
            {
                return false;
            }

            double rerollChance = difficulty == CpuDifficulty.Strong ? 0.7 : 0.2;
            return rng.NextFloat01() < rerollChance;
        }

        /// <summary>
        /// POSITION_CHOICE発動時の自由選択。Weakは完全ランダム、Strongは中央寄りのポジションを選好する
        /// (2つ候補を引いて中央に近い方を採用する、シンプルな中央寄せサンプリング)。
        /// </summary>
        public static int ChoosePosition(CpuDifficulty difficulty, Rng rng)
        {
            int a = rng.NextInt(1, GameConfig.LaunchPositionCount + 1);

            if (difficulty == CpuDifficulty.Weak)
            {
                return a;
            }

            int b = rng.NextInt(1, GameConfig.LaunchPositionCount + 1);
            float center = (GameConfig.LaunchPositionCount + 1) / 2f;
            return System.Math.Abs(a - center) <= System.Math.Abs(b - center) ? a : b;
        }
    }
}
