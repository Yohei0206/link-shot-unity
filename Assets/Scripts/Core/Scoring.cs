using System.Collections.Generic;

namespace LinkShot.Core
{
    /// <summary>得点計算（GAME_RULES.md 5章）。</summary>
    public static class Scoring
    {
        /// <summary>
        /// カード効果適用前の基礎点を求める。的は貫通式で1ショットに複数命中しうるため、
        /// 着弾までに通過した全ての的の得点を合算する（壁命中・枠外・時間切れそのものは加点しない。
        /// ただし途中で通過した的の得点は失われない）。
        /// </summary>
        public static int BaseScore(IReadOnlyList<TargetZoneId> hitZones)
        {
            int total = 0;
            foreach (TargetZoneId zone in hitZones)
            {
                total += ZoneScore(zone);
            }

            return total;
        }

        private static int ZoneScore(TargetZoneId zone)
        {
            return zone switch
            {
                TargetZoneId.Score500 => GameConfig.Score500Value,
                TargetZoneId.Score300 => GameConfig.Score300Value,
                TargetZoneId.Score100 => GameConfig.Score100Value,
                _ => 0,
            };
        }
    }
}
