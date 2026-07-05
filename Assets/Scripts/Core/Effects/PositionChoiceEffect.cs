namespace LinkShot.Core.Effects
{
    /// <summary>★★★レジェンド（BETA固定）。サイコロを振らず、1〜6の発射ポジションから自由に1つ選ぶ（MEDALS.md 3章）。</summary>
    public sealed class PositionChoiceEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.PositionChoice;
        public override bool ReplacesPositionRoll => true;
    }
}
