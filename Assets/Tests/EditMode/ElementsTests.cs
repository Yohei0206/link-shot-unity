using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class ElementsTests
    {
        [Test]
        public void Beats_TriangleIsCorrect()
        {
            Assert.IsTrue(Elements.Beats(Element.ALPHA, Element.BETA));
            Assert.IsTrue(Elements.Beats(Element.BETA, Element.GAMMA));
            Assert.IsTrue(Elements.Beats(Element.GAMMA, Element.ALPHA));

            Assert.IsFalse(Elements.Beats(Element.BETA, Element.ALPHA));
            Assert.IsFalse(Elements.Beats(Element.GAMMA, Element.BETA));
            Assert.IsFalse(Elements.Beats(Element.ALPHA, Element.GAMMA));
        }

        [Test]
        public void AttackerEffectActivates_WhenAttackerWins()
        {
            Assert.IsTrue(Elements.AttackerEffectActivates(Element.ALPHA, Element.BETA));
        }

        [Test]
        public void AttackerEffectActivates_WhenSameElement()
        {
            Assert.IsTrue(Elements.AttackerEffectActivates(Element.GAMMA, Element.GAMMA));
        }

        [Test]
        public void AttackerEffectActivates_False_WhenAttackerLoses()
        {
            Assert.IsFalse(Elements.AttackerEffectActivates(Element.BETA, Element.ALPHA));
        }
    }
}
