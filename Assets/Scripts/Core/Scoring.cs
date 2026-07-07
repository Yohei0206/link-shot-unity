namespace LinkShot.Core
{
    /// <summary>得点計算（GAME_RULES.md 5章）。</summary>
    public static class Scoring
    {
        /// <summary>
        /// 着弾結果からカード効果適用前の基礎点を求める。
        /// 壁命中・枠外・時間切れは常に0点（GAME_RULES.md 5.2, 5.3章）。
        /// </summary>
        public static int BaseScore(ShotOutcomeKind outcome, TargetZoneId? zone)
        {
            if (outcome != ShotOutcomeKind.TargetHit)
            {
                return 0;
            }

            return zone switch
            {
                TargetZoneId.TopLeftCorner => GameConfig.CornerZoneScore,
                TargetZoneId.TopRightCorner => GameConfig.CornerZoneScore,
                TargetZoneId.Center => GameConfig.CenterZoneScore,
                TargetZoneId.Bonus => GameConfig.BonusZoneScore,
                _ => 0,
            };
        }
    }
}
