namespace LinkShot.Core.Effects
{
    /// <summary>範囲拡張。発射ポジションの円の半径が拡大する（GAME_RULES.md 6章、倍率はGameConfig参照）。</summary>
    public sealed class RangeBoostEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.RangeBoost;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.LaunchRadiusMultiplier *= GameConfig.RangeBoostRadiusMultiplier;
            return baseModifier;
        }
    }
}
