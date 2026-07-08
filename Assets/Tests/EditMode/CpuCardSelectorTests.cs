using System.Linq;
using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuCardSelectorTests
    {
        private static PlayerState MakePlayer(int index, params string[] hand)
        {
            return new PlayerState(index, hand);
        }

        [Test]
        public void ChooseCard_AlwaysReturnsACardFromHand()
        {
            var self = MakePlayer(0, "WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA", "POWER_SHOT_GAMMA");
            var opponent = MakePlayer(1, "MINI_BALL_GAMMA");

            for (int seed = 0; seed < 30; seed++)
            {
                string chosen = CpuCardSelector.ChooseCard(self, opponent, CpuDifficulty.Weak, new Rng(seed));
                CollectionAssert.Contains(self.Hand, chosen);

                chosen = CpuCardSelector.ChooseCard(self, opponent, CpuDifficulty.Strong, new Rng(seed));
                CollectionAssert.Contains(self.Hand, chosen);
            }
        }

        [Test]
        public void ChooseCard_Strong_BiasesTowardsCounteringOpponentsMostUsedElement()
        {
            // 相手はALPHA属性のカードばかり使っている(3回)ので、ALPHAに勝つGAMMAへ寄るはず。
            var opponent = MakePlayer(1);
            opponent.UsedCardIds.Add("WALL_REMOVE_ALPHA");
            opponent.UsedCardIds.Add("SCORE_DOUBLE_ALPHA");
            opponent.UsedCardIds.Add("REROLL_ALPHA");

            var self = MakePlayer(0, "WALL_SHIFT_ALPHA", "WIDE_GATE_GAMMA");

            int gammaCount = 0;
            const int trials = 200;
            for (int seed = 0; seed < trials; seed++)
            {
                string chosen = CpuCardSelector.ChooseCard(self, opponent, CpuDifficulty.Strong, new Rng(seed));
                if (CardCatalog.Get(chosen).Element == Element.GAMMA)
                {
                    gammaCount++;
                }
            }

            // バイアス重み(GameConfig.CpuStrongCardBiasWeight)ぶんは50%を明確に上回るはず。
            Assert.Greater(gammaCount, trials / 2);
        }
    }
}
