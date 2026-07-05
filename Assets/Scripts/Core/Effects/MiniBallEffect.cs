namespace LinkShot.Core.Effects
{
    /// <summary>ボール縮小。ボールのサイズが縮小し、壁の隙間を通しやすくなる（倍率はGameConfig参照）（MEDALS.md 4章#12）。</summary>
    public sealed class MiniBallEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.MiniBall;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.BallSizeMultiplier *= GameConfig.MiniBallSizeMultiplier;
            return baseModifier;
        }
    }
}
