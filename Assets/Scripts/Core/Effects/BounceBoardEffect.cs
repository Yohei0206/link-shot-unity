namespace LinkShot.Core.Effects
{
    /// <summary>バウンド板。攻撃側が有利な位置に反射板を1枚設置する（設置位置は攻撃側が選ぶ）（MEDALS.md 4章#2）。</summary>
    public sealed class BounceBoardEffect : MedalEffectBase
    {
        public override EffectId Id => EffectId.BounceBoard;

        public override void OnResolve(GameState state, EffectChoice choice)
        {
            if (choice.BouncePosition == null)
            {
                return;
            }

            state.Field.BounceBoards.Add(new BouncePlacement(choice.BouncePosition.Value));
        }
    }
}
