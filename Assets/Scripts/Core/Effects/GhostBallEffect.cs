namespace LinkShot.Core.Effects
{
    /// <summary>すり抜け。ボールが最初に触れた壁1枚のみを貫通する（2枚目からは通常判定）（CARDS.md 4章#5）。</summary>
    public sealed class GhostBallEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.GhostBall;

        public override ShotModifier ModifyShot(ShotModifier baseModifier)
        {
            baseModifier.PassThroughFirstWall = true;
            return baseModifier;
        }
    }
}
