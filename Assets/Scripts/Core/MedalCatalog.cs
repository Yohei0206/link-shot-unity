using System.Collections.Generic;
using System.Linq;

namespace LinkShot.Core
{
    /// <summary>1枚のメダル定義（属性＋効果＋レアリティ）。MEDALS.md 1章。</summary>
    public sealed class Medal
    {
        public readonly string Id;
        public readonly Element Element;
        public readonly EffectId Effect;
        public readonly Rarity Rarity;

        public Medal(string id, Element element, EffectId effect, Rarity rarity)
        {
            Id = id;
            Element = element;
            Effect = effect;
            Rarity = rarity;
        }
    }

    /// <summary>
    /// MVP用メダル固定プール（MEDALS.md 5.1章準拠、15枚: レジェンド3種+普通12種）。
    /// 属性割り当て・レアリティ区分はすべて【暫定】であり、この一元管理場所を変更するだけで差し替えられる。
    /// </summary>
    public static class MedalCatalog
    {
        public static readonly IReadOnlyList<Medal> All = new List<Medal>
        {
            // ★★★ レジェンド（属性固定。MEDALS.md 3章）
            new Medal("DOUBLE_SHOT", Element.ALPHA, EffectId.DoubleShot, Rarity.Legendary),
            new Medal("POSITION_CHOICE", Element.BETA, EffectId.PositionChoice, Rarity.Legendary),
            new Medal("WALL_RETURN", Element.GAMMA, EffectId.WallReturn, Rarity.Legendary),

            // ★★ レア（MEDALS.md 2章・4章）
            new Medal("WALL_REMOVE_ALPHA", Element.ALPHA, EffectId.WallRemove, Rarity.Rare),
            new Medal("SCORE_DOUBLE_ALPHA", Element.ALPHA, EffectId.ScoreDouble, Rarity.Rare),
            new Medal("REROLL_ALPHA", Element.ALPHA, EffectId.Reroll, Rarity.Rare),
            new Medal("GHOST_BALL_BETA", Element.BETA, EffectId.GhostBall, Rarity.Rare),

            // ★ コモン（MEDALS.md 2章・4章）
            new Medal("WALL_SHIFT_ALPHA", Element.ALPHA, EffectId.WallShift, Rarity.Common),
            new Medal("BOUNCE_BOARD_BETA", Element.BETA, EffectId.BounceBoard, Rarity.Common),
            new Medal("CURVE_SHOT_BETA", Element.BETA, EffectId.CurveShot, Rarity.Common),
            new Medal("SAFETY_NET_BETA", Element.BETA, EffectId.SafetyNet, Rarity.Common),
            new Medal("RANGE_BOOST_GAMMA", Element.GAMMA, EffectId.RangeBoost, Rarity.Common),
            new Medal("WIDE_GATE_GAMMA", Element.GAMMA, EffectId.WideGate, Rarity.Common),
            new Medal("POWER_SHOT_GAMMA", Element.GAMMA, EffectId.PowerShot, Rarity.Common),
            new Medal("MINI_BALL_GAMMA", Element.GAMMA, EffectId.MiniBall, Rarity.Common),
        };

        private static readonly Dictionary<string, Medal> ById = All.ToDictionary(m => m.Id, m => m);

        public static Medal Get(string id) => ById[id];

        public static bool TryGetById(string id, out Medal medal) => ById.TryGetValue(id, out medal);

        /// <summary>デッキ採用ルールの検証（MEDALS.md 2章・5.2章: 重複不可・レアリティ別上限）。</summary>
        public static bool IsValidDeck(IReadOnlyList<string> medalIds, out string error)
        {
            error = null;

            if (medalIds.Count != GameConfig.DeckSize)
            {
                error = $"デッキは{GameConfig.DeckSize}枚である必要があります（{medalIds.Count}枚）";
                return false;
            }

            if (medalIds.Distinct().Count() != medalIds.Count)
            {
                error = "同一メダルの重複は許可されていません";
                return false;
            }

            var medals = new List<Medal>();
            foreach (var id in medalIds)
            {
                if (!TryGetById(id, out var medal))
                {
                    error = $"未知のメダルID: {id}";
                    return false;
                }

                medals.Add(medal);
            }

            int legendaryCount = medals.Count(m => m.Rarity == Rarity.Legendary);
            int rareCount = medals.Count(m => m.Rarity == Rarity.Rare);

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
