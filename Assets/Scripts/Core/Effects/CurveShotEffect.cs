namespace LinkShot.Core.Effects
{
    /// <summary>軌道操作。発射後、飛翔中に1回だけ画面タップで弾道を曲げられる（MEDALS.md 4章#8）。</summary>
    public sealed class CurveShotEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.CurveShot;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.AllowMidFlightCurve = true;
            return baseModifier;
        }
    }
}
