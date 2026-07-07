namespace LinkShot.Core.Effects
{
    /// <summary>初速アップ。スリングショットの最大初速が上がる（倍率はGameConfig参照）（CARDS.md 4章#9）。</summary>
    public sealed class PowerShotEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.PowerShot;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.VelocityMultiplier *= GameConfig.PowerShotVelocityMultiplier;
            return baseModifier;
        }
    }
}
