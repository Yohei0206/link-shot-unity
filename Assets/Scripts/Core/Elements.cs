namespace LinkShot.Core
{
    /// <summary>属性三すくみの判定ロジック（GAME_RULES.md 4.1, 4.2章）。ALPHA→BETA→GAMMA→ALPHAの順に勝つ。</summary>
    public static class Elements
    {
        /// <summary>attacker が defender に勝つ関係かどうか。</summary>
        public static bool Beats(Element attacker, Element defender)
        {
            return (attacker == Element.ALPHA && defender == Element.BETA)
                || (attacker == Element.BETA && defender == Element.GAMMA)
                || (attacker == Element.GAMMA && defender == Element.ALPHA);
        }

        /// <summary>
        /// 攻撃側のカード効果が発動するか（GAME_RULES.md 4.2章）。
        /// 攻撃側が勝つ、または同属性（引き分け）なら発動する。攻撃側が負ける場合のみ打ち消される。
        /// </summary>
        public static bool AttackerEffectActivates(Element attacker, Element defender)
        {
            return attacker == defender || Beats(attacker, defender);
        }
    }
}
