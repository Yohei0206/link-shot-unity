namespace LinkShot.Core.Effects
{
    /// <summary>的の拡大。的1つを選び、当たり判定を拡大する（倍率はGameConfig参照）（MEDALS.md 4章#6）。</summary>
    public sealed class WideGateEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.WideGate;

        public override void OnResolve(GameState state, EffectChoice choice)
        {
            if (choice.WideGateZone == null)
            {
                return;
            }

            state.Field.WideGateZone = choice.WideGateZone.Value;
        }
    }
}
