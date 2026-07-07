namespace LinkShot.Core.Effects
{
    /// <summary>★★★レジェンド（BETA固定）。サイコロを振らず、1〜6の発射ポジションから自由に1つ選ぶ（CARDS.md 3章）。</summary>
    public sealed class PositionChoiceEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.PositionChoice;
        public override bool ReplacesPositionRoll => true;
    }
}
