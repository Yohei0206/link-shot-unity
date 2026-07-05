namespace LinkShot.Core.Effects
{
    /// <summary>振り直し。サイコロの出目を見た後、1回だけ振り直せる（振り直し後は強制確定）（MEDALS.md 4章#7）。</summary>
    public sealed class RerollEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.Reroll;
        public override bool AllowsReroll => true;
    }
}
