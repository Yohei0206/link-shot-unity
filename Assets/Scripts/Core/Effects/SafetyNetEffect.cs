namespace LinkShot.Core.Effects
{
    /// <summary>
    /// 保険。このショットが0点だった場合（壁命中・枠外・時間切れ）、代わりに保険点を獲得する（CARDS.md 4章#11）。
    /// 属性判定で打ち消された場合はそもそも呼ばれないため、無条件の保険にはならない（CARDS.md 4章補足）。
    /// </summary>
    public sealed class SafetyNetEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.SafetyNet;

        public override int ModifyScore(GameState state, ShotOutcomeKind outcome, int baseScore)
        {
            return baseScore == 0 ? GameConfig.SafetyNetScore : baseScore;
        }
    }
}
