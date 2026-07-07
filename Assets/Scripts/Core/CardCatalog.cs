using System.Collections.Generic;
using System.Linq;

namespace LinkShot.Core
{
    /// <summary>1枚のカード定義（属性＋効果＋レアリティ）。CARDS.md 1章。</summary>
    public sealed class Card
    {
        public readonly string Id;
        public readonly Element Element;
        public readonly EffectId Effect;
        public readonly Rarity Rarity;

        public Card(string id, Element element, EffectId effect, Rarity rarity)
        {
            Id = id;
            Element = element;
            Effect = effect;
            Rarity = rarity;
        }
    }

    /// <summary>
    /// MVP用カード固定プール（CARDS.md 5.1章準拠、15枚: レジェンド3種+普通12種）。
    /// 属性割り当て・レアリティ区分はすべて【暫定】であり、この一元管理場所を変更するだけで差し替えられる。
    /// </summary>
    public static class CardCatalog
    {
        public static readonly IReadOnlyList<Card> All = new List<Card>
        {
            // ★★★ レジェンド（属性固定。CARDS.md 3章）
            new Card("DOUBLE_SHOT", Element.ALPHA, EffectId.DoubleShot, Rarity.Legendary),
            new Card("POSITION_CHOICE", Element.BETA, EffectId.PositionChoice, Rarity.Legendary),
            new Card("WALL_RETURN", Element.GAMMA, EffectId.WallReturn, Rarity.Legendary),

            // ★★ レア（CARDS.md 2章・4章）
            new Card("WALL_REMOVE_ALPHA", Element.ALPHA, EffectId.WallRemove, Rarity.Rare),
            new Card("SCORE_DOUBLE_ALPHA", Element.ALPHA, EffectId.ScoreDouble, Rarity.Rare),
            new Card("REROLL_ALPHA", Element.ALPHA, EffectId.Reroll, Rarity.Rare),
            new Card("GHOST_BALL_BETA", Element.BETA, EffectId.GhostBall, Rarity.Rare),

            // ★ コモン（CARDS.md 2章・4章）
            new Card("WALL_SHIFT_ALPHA", Element.ALPHA, EffectId.WallShift, Rarity.Common),
            new Card("BOUNCE_BOARD_BETA", Element.BETA, EffectId.BounceBoard, Rarity.Common),
            new Card("CURVE_SHOT_BETA", Element.BETA, EffectId.CurveShot, Rarity.Common),
            new Card("SAFETY_NET_BETA", Element.BETA, EffectId.SafetyNet, Rarity.Common),
            new Card("RANGE_BOOST_GAMMA", Element.GAMMA, EffectId.RangeBoost, Rarity.Common),
            new Card("WIDE_GATE_GAMMA", Element.GAMMA, EffectId.WideGate, Rarity.Common),
            new Card("POWER_SHOT_GAMMA", Element.GAMMA, EffectId.PowerShot, Rarity.Common),
            new Card("MINI_BALL_GAMMA", Element.GAMMA, EffectId.MiniBall, Rarity.Common),
        };

        private static readonly Dictionary<string, Card> ById = All.ToDictionary(m => m.Id, m => m);

        public static Card Get(string id) => ById[id];

        public static bool TryGetById(string id, out Card card) => ById.TryGetValue(id, out card);

        /// <summary>デッキ採用ルールの検証（CARDS.md 2章・5.2章: 重複不可・レアリティ別上限）。</summary>
        public static bool IsValidDeck(IReadOnlyList<string> cardIds, out string error)
        {
            error = null;

            if (cardIds.Count != GameConfig.DeckSize)
            {
                error = $"デッキは{GameConfig.DeckSize}枚である必要があります（{cardIds.Count}枚）";
                return false;
            }

            if (cardIds.Distinct().Count() != cardIds.Count)
            {
                error = "同一カードの重複は許可されていません";
                return false;
            }

            var cards = new List<Card>();
            foreach (var id in cardIds)
            {
                if (!TryGetById(id, out var card))
                {
                    error = $"未知のカードID: {id}";
                    return false;
                }

                cards.Add(card);
            }

            int legendaryCount = cards.Count(m => m.Rarity == Rarity.Legendary);
            int rareCount = cards.Count(m => m.Rarity == Rarity.Rare);

            if (legendaryCount > GameConfig.LegendaryDeckLimit)
            {
                error = $"レジェンドは{GameConfig.LegendaryDeckLimit}枚までです（{legendaryCount}枚）";
                return false;
            }

            if (rareCount > GameConfig.RareDeckLimit)
            {
                error = $"レアは{GameConfig.RareDeckLimit}枚までです（{rareCount}枚）";
                return false;
            }

            return true;
        }
    }
}
