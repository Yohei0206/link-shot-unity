using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>
    /// CPUのカード選択（GAME_RULES.md 9章）。
    /// Weak: 手札からランダム。
    /// Strong: 相手の使用済みカードの属性傾向から、有利属性（三すくみ）のカードを選ぶ確率を上げる。
    /// 相手の手札そのものは覗かず、公開情報（使用済みカード履歴）だけを見る。
    /// </summary>
    public static class CpuCardSelector
    {
        public static string ChooseCard(PlayerState self, PlayerState opponent, CpuDifficulty difficulty, Rng rng)
        {
            if (difficulty == CpuDifficulty.Weak || opponent.UsedCardIds.Count == 0)
            {
                return PickRandom(self.Hand, rng);
            }

            Element guessedElement = GuessOpponentElement(opponent);
            var favorable = self.Hand.Where(id => Elements.Beats(CardCatalog.Get(id).Element, guessedElement)).ToList();

            if (favorable.Count > 0 && rng.NextFloat01() < GameConfig.CpuStrongCardBiasWeight)
            {
                return PickRandom(favorable, rng);
            }

            return PickRandom(self.Hand, rng);
        }

        private static Element GuessOpponentElement(PlayerState opponent)
        {
            return opponent.UsedCardIds
                .Select(id => CardCatalog.Get(id).Element)
                .GroupBy(e => e)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }

        private static string PickRandom(IReadOnlyList<string> cardIds, Rng rng)
        {
            return cardIds[rng.NextInt(0, cardIds.Count)];
        }
    }
}
