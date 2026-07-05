using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class ScoringTests
    {
        [TestCase(TargetZoneId.TopLeftCorner, GameConfig.CornerZoneScore)]
        [TestCase(TargetZoneId.TopRightCorner, GameConfig.CornerZoneScore)]
        [TestCase(TargetZoneId.Center, GameConfig.CenterZoneScore)]
        public void BaseScore_TargetHit_ReturnsZoneScore(TargetZoneId zone, int expected)
        {
            Assert.AreEqual(expected, Scoring.BaseScore(ShotOutcomeKind.TargetHit, zone));
        }

        [Test]
        public void BaseScore_WallHit_IsZero()
        {
            Assert.AreEqual(0, Scoring.BaseScore(ShotOutcomeKind.WallHit, null));
        }

        [Test]
        public void BaseScore_OutOfField_IsZero()
        {
            Assert.AreEqual(0, Scoring.BaseScore(ShotOutcomeKind.OutOfField, null));
        }

        [Test]
        public void BaseScore_Timeout_IsZero()
        {
            Assert.AreEqual(0, Scoring.BaseScore(ShotOutcomeKind.Timeout, null));
        }
    }
}
