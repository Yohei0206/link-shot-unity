using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUのショット精度(GAME_RULES.md 9章)。難易度別の標準偏差で、狙い角度とパワーに正規分布ノイズを加える。
    /// 基準の狙い方向(まっすぐ的帯へ)はGame層が持つため、ここでは角度オフセットとパワーだけを返す。
    /// 的の実座標はGame層にしかないが、高得点の的は中央付近に置かれやすいため、
    /// Strongは発射ポジションが端に寄っているほど軌道を中央寄りに補正する(端の発射位置ほど中央の的を外しやすいため)。
    /// </summary>
    public static class CpuShotAimPlanner
    {
        public static (float AngleOffsetRadians, float Power) GetAim(CpuDifficulty difficulty, int launchPosition, Rng rng)
        {
            float sigma = difficulty == CpuDifficulty.Strong
                ? GameConfig.CpuShotAimSigmaStrongRadians
                : GameConfig.CpuShotAimSigmaWeakRadians;

            float noise = (float)rng.NextGaussian(0.0, sigma);
            float bias = difficulty == CpuDifficulty.Strong ? CenterAimBias(launchPosition) : 0f;

            float power = (float)rng.NextGaussian(GameConfig.CpuShotPowerMean, GameConfig.CpuShotPowerSigma);
            power = System.Math.Clamp(power, 0.2f, 1.0f);

            return (noise + bias, power);
        }

        /// <summary>
        /// 発射ポジションが中央から離れているほど、中央へ向けて軌道を傾ける補正角(ラジアン)を返す。
        /// 例えば一番左(position=1)なら右へ、一番右なら左へ補正する。中央付近ならほぼ0。
        /// </summary>
        private static float CenterAimBias(int launchPosition)
        {
            float center = (GameConfig.LaunchPositionCount + 1) / 2f;
            float normalizedOffset = (launchPosition - center) / (GameConfig.LaunchPositionCount / 2f); // -1(左端)..+1(右端)
            return -normalizedOffset * GameConfig.CpuCenterAimBiasRadians;
        }
    }
}
