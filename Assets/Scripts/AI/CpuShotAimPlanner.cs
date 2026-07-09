using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUのショット精度(GAME_RULES.md 9章)。難易度別の標準偏差で、狙い角度とパワーに正規分布ノイズを加える。
    /// 基準の狙い方向(まっすぐ的帯へ)はGame層が持つため、ここでは角度オフセットとパワーだけを返す。
    /// Strongは`state.Field.StarNearestLaunchPosition`(最高得点の的「星」に最も近い発射ポジション。
    /// Game層がショットごとに算出しCoreへセットする)へ向けて軌道を補正する。
    /// </summary>
    public static class CpuShotAimPlanner
    {
        public static (float AngleOffsetRadians, float Power) GetAim(GameState state, CpuDifficulty difficulty, Rng rng)
        {
            float sigma = difficulty == CpuDifficulty.Strong
                ? GameConfig.CpuShotAimSigmaStrongRadians
                : GameConfig.CpuShotAimSigmaWeakRadians;

            float noise = (float)rng.NextGaussian(0.0, sigma);
            float bias = difficulty == CpuDifficulty.Strong ? StarAimBias(state) : 0f;

            float power = (float)rng.NextGaussian(GameConfig.CpuShotPowerMean, GameConfig.CpuShotPowerSigma);
            power = System.Math.Clamp(power, 0.2f, 1.0f);

            return (noise + bias, power);
        }

        /// <summary>
        /// 発射ポジションが星の位置から離れているほど、星へ向けて軌道を傾ける補正角(ラジアン)を返す。
        /// 星の位置が不明な場合はフィールド中央を代わりに使う。
        /// </summary>
        private static float StarAimBias(GameState state)
        {
            float target = state.Field.StarNearestLaunchPosition ?? (GameConfig.LaunchPositionCount + 1) / 2f;
            float normalizedOffset = (state.Field.LaunchPosition - target) / (GameConfig.LaunchPositionCount / 2f); // -1(星より左)..+1(星より右)
            return -normalizedOffset * GameConfig.CpuCenterAimBiasRadians;
        }
    }
}
