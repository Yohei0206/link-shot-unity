using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;

namespace LinkShot.AI
{
    /// <summary>CPUのデッキ構築（GameConfig.DeckSize枚、レアリティ上限を守ってランダムに選ぶ）。</summary>
    public static class CpuDeckBuilder
    {
        public static List<string> BuildDeck(Rng rng)
        {
            var pool = CardCatalog.All.Select(c => c.Id).ToList();

            while (true)
            {
                var shuffled = Shuffle(pool, rng);
                var candidate = shuffled.Take(GameConfig.DeckSize).ToList();

                if (CardCatalog.IsValidDeck(candidate, out _))
                {
                    return candidate;
                }
            }
        }

        private static List<string> Shuffle(List<string> source, Rng rng)
        {
            var result = new List<string>(source);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}
