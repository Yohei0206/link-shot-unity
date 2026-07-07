namespace LinkShot.Core.Effects
{
    /// <summary>得点2倍。このショットで獲得する得点が2倍になる（CARDS.md 4章#10）。</summary>
    public sealed class ScoreDoubleEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.ScoreDouble;

        public override int ModifyScore(GameState state, ShotOutcomeKind outcome, int baseScore)
        {
            return baseScore * 2;
        }
    }
}
