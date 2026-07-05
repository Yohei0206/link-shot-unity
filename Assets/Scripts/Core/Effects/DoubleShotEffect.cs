namespace LinkShot.Core.Effects
{
    /// <summary>★★★レジェンド（ALPHA固定）。2回ショットし、得点の高い方のみを採用する（MEDALS.md 3章）。</summary>
    public sealed class DoubleShotEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.DoubleShot;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.ShotAttempts = GameConfig.DoubleShotAttempts;
            return baseModifier;
        }
    }
}
