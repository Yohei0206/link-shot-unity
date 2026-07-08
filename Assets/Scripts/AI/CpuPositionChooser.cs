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

        public static int ChoosePosition(CpuDifficulty difficulty, Rng rng)
        {
            return rng.NextInt(1, GameConfig.LaunchPositionCount + 1);
        }
    }
}
