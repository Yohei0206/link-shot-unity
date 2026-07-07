using LinkShot.Core;
using UnityEngine;

namespace LinkShot.UI
{
    /// <summary>
    /// カード1枚の見た目に関する共通ヘルパー（CardSelectPanel/DeckSelectPanelで共用）。
    /// 属性は色分け＋アイコン（火・水・草）、レアリティは★の数で表現する（文字ラベルは使わない）。
    /// </summary>
    public static class CardVisuals
    {
        private const string ElementIconPath = "UI/Elements/icon_element_";

        /// <summary>属性ごとの色分け（三すくみを見分けやすくする。CLAUDE.md用語集: ALPHA/BETA/GAMMA）。</summary>
        public static Color ElementColor(Element element)
        {
            return element switch
            {
                Element.ALPHA => new Color(0.85f, 0.3f, 0.3f),
                Element.BETA => new Color(0.3f, 0.5f, 0.9f),
                Element.GAMMA => new Color(0.35f, 0.75f, 0.4f),
                _ => Color.white,
            };
        }

        /// <summary>属性を表す自作アイコン（火=ALPHA/水=BETA/草=GAMMA。ElementColorの配色と対応させている）。</summary>
        public static Sprite ElementIcon(Element element)
        {
            return element switch
            {
                Element.ALPHA => Resources.Load<Sprite>(ElementIconPath + "fire"),
                Element.BETA => Resources.Load<Sprite>(ElementIconPath + "water"),
                Element.GAMMA => Resources.Load<Sprite>(ElementIconPath + "leaf"),
                _ => null,
            };
        }

        /// <summary>レアリティを星の数で表現する（文字ラベルではなく★の個数で見分ける）。</summary>
        public static string RarityStars(Rarity rarity)
        {
            int count = rarity switch
            {
                Rarity.Legendary => 3,
                Rarity.Rare => 2,
                _ => 1,
            };
            return new string('★', count);
        }

        /// <summary>効果名の日本語表記（CARDS.md 3-4章の効果名に準拠）。</summary>
        public static string EffectJapanese(EffectId effect)
        {
            return effect switch
            {
                EffectId.WallRemove => "壁除去",
                EffectId.BounceBoard => "バウンド板",
                EffectId.RangeBoost => "範囲拡張",
                EffectId.WallShift => "壁移動",
                EffectId.GhostBall => "すり抜け",
                EffectId.WideGate => "的の拡大",
                EffectId.Reroll => "振り直し",
                EffectId.CurveShot => "軌道操作",
                EffectId.PowerShot => "初速アップ",
                EffectId.ScoreDouble => "得点2倍",
                EffectId.SafetyNet => "保険",
                EffectId.MiniBall => "ボール縮小",
                EffectId.DoubleShot => "2連射",
                EffectId.PositionChoice => "出目選択",
                EffectId.WallReturn => "壁カード強奪",
                _ => effect.ToString(),
            };
        }
    }
}
