namespace LinkShot.Core.Effects
{
    /// <summary>
    /// ★★★レジェンド（GAMMA固定）。このラウンドで防御側が使い捨て壁カードを1枚以上使っていた場合、
    /// 得点解決フェーズ後にその1枚分を攻撃側の手持ちに加える（CARDS.md 3章）。
    /// カードは同一種類で識別されないため「防御側が使った」事実のみを見て攻撃側の残数を1加算する。
    /// 防御側が1枚も使っていなければ不発（CARDS.md 3章補足）。
    /// </summary>
    public sealed class WallReturnEffect : CardEffectBase
    {
        public override EffectId Id => EffectId.WallReturn;

        public override void OnAfterScore(GameState state, int attacker, int defender)
        {
            if (state.Players[defender].DisposableWallCardsUsedThisRound > 0)
            {
                state.Players[attacker].DisposableWallCardsRemaining += 1;
            }
        }
    }
}
