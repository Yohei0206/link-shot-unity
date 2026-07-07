using System;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class ScoringTests
    {
        [TestCase(TargetZoneId.Score500, GameConfig.Score500Value)]
        [TestCase(TargetZoneId.Score300, GameConfig.Score300Value)]
        [TestCase(TargetZoneId.Score100, GameConfig.Score100Value)]
        public void BaseScore_SingleZone_ReturnsZoneScore(TargetZoneId zone, int expected)
        {
            Assert.AreEqual(expected, Scoring.BaseScore(new[] { zone }));
        }

        [Test]
        public void BaseScore_MultipleZones_SumsAllHits()
        {
            // 的は貫通式なので1ショットで複数命中しうる（GAME_RULES.md 5.1章）。合算されることを確認する。
            var zones = new[] { TargetZoneId.Score500, TargetZoneId.Score300, TargetZoneId.Score100 };
            int expected = GameConfig.Score500Value + GameConfig.Score300Value + GameConfig.Score100Value;

            Assert.AreEqual(expected, Scoring.BaseScore(zones));
        }

        [Test]
        public void BaseScore_NoHits_IsZero()
        {
            Assert.AreEqual(0, Scoring.BaseScore(Array.Empty<TargetZoneId>()));
        }
    }
}
