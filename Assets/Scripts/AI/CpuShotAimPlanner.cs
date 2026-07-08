using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUのショット精度(GAME_RULES.md 9章)。難易度別の標準偏差で、狙い角度とパワーに正規分布ノイズを加える。
    /// 基準の狙い方向(まっすぐ的帯へ)はGame層が持つため、ここでは角度オフセットとパワーだけを返す。
    /// </summary>
    public static class CpuShotAimPlanner
    {
        public static (float AngleOffsetRadians, float Power) GetAim(CpuDifficulty difficulty, Rng rng)
        {
            float sigma = difficulty == CpuDifficulty.Strong
                ? GameConfig.CpuShotAimSigmaStrongRadians
                : GameConfig.CpuShotAimSigmaWeakRadians;

            float angleOffset = (float)rng.NextGaussian(0.0, sigma);
            float power = (float)rng.NextGaussian(GameConfig.CpuShotPowerMean, GameConfig.CpuShotPowerSigma);
            power = System.Math.Clamp(power, 0.2f, 1.0f);

            return (angleOffset, power);
        }
    }
}
