using LinkShot.AI;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CpuDeckBuilderTests
    {
        [Test]
        public void BuildDeck_AlwaysReturnsAValidDeck()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var deck = CpuDeckBuilder.BuildDeck(new Rng(seed));
                Assert.IsTrue(CardCatalog.IsValidDeck(deck, out string error), $"seed={seed}: {error}");
            }
        }

        [Test]
        public void BuildDeck_SameSeed_IsDeterministic()
        {
            var deckA = CpuDeckBuilder.BuildDeck(new Rng(42));
            var deckB = CpuDeckBuilder.BuildDeck(new Rng(42));

            CollectionAssert.AreEqual(deckA, deckB);
        }
    }
}
