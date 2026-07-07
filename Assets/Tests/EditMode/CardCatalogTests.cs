using System.Collections.Generic;
using System.Linq;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class CardCatalogTests
    {
        [Test]
        public void All_HasFifteenCards()
        {
            Assert.AreEqual(15, CardCatalog.All.Count);
        }

        [Test]
        public void All_CardIdsAreUnique()
        {
            Assert.AreEqual(CardCatalog.All.Count, CardCatalog.All.Select(m => m.Id).Distinct().Count());
        }

        [Test]
        public void Legendary_HasThreeWithDistinctFixedElements()
        {
            var legendary = CardCatalog.All.Where(m => m.Rarity == Rarity.Legendary).ToList();
            Assert.AreEqual(3, legendary.Count);
            Assert.AreEqual(3, legendary.Select(m => m.Element).Distinct().Count());
        }

        [Test]
        public void Rare_HasFourMatchingSpec()
        {
            var rareEffects = CardCatalog.All.Where(m => m.Rarity == Rarity.Rare).Select(m => m.Effect).ToHashSet();
            var expected = new HashSet<EffectId>
            {
                EffectId.WallRemove, EffectId.ScoreDouble, EffectId.GhostBall, EffectId.Reroll,
            };
            CollectionAssert.AreEquivalent(expected, rareEffects);
        }

        [Test]
        public void Common_HasEightMatchingSpec()
        {
            var commonEffects = CardCatalog.All.Where(m => m.Rarity == Rarity.Common).Select(m => m.Effect).ToHashSet();
            var expected = new HashSet<EffectId>
            {
                EffectId.BounceBoard, EffectId.RangeBoost, EffectId.WallShift, EffectId.WideGate,
                EffectId.CurveShot, EffectId.PowerShot, EffectId.SafetyNet, EffectId.MiniBall,
            };
            CollectionAssert.AreEquivalent(expected, commonEffects);
        }

        [Test]
        public void IsValidDeck_True_ForValidFiveCardDeck()
        {
            var deck = new List<string>
            {
                "DOUBLE_SHOT", "WALL_REMOVE_ALPHA", "SCORE_DOUBLE_ALPHA", "BOUNCE_BOARD_BETA", "RANGE_BOOST_GAMMA",
            };
            Assert.IsTrue(CardCatalog.IsValidDeck(deck, out string error), error);
        }

        [Test]
        public void IsValidDeck_False_ForDuplicateCard()
        {
            var deck = new List<string>
            {
                "WALL_REMOVE_ALPHA", "WALL_REMOVE_ALPHA", "SCORE_DOUBLE_ALPHA", "BOUNCE_BOARD_BETA", "RANGE_BOOST_GAMMA",
            };
            Assert.IsFalse(CardCatalog.IsValidDeck(deck, out _));
        }

        [Test]
        public void IsValidDeck_False_WhenTwoLegendary()
        {
            var deck = new List<string>
            {
                "DOUBLE_SHOT", "POSITION_CHOICE", "SCORE_DOUBLE_ALPHA", "BOUNCE_BOARD_BETA", "RANGE_BOOST_GAMMA",
            };
            Assert.IsFalse(CardCatalog.IsValidDeck(deck, out _));
        }

        [Test]
        public void IsValidDeck_False_WhenThreeRare()
        {
            var deck = new List<string>
            {
                "WALL_REMOVE_ALPHA", "SCORE_DOUBLE_ALPHA", "REROLL_ALPHA", "BOUNCE_BOARD_BETA", "RANGE_BOOST_GAMMA",
            };
            Assert.IsFalse(CardCatalog.IsValidDeck(deck, out _));
        }

        [Test]
        public void IsValidDeck_False_WhenNotFiveCards()
        {
            var deck = new List<string> { "WALL_REMOVE_ALPHA", "SCORE_DOUBLE_ALPHA" };
            Assert.IsFalse(CardCatalog.IsValidDeck(deck, out _));
        }
    }
}
