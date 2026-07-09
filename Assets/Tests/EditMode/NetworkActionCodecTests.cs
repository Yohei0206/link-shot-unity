using System.Linq;
using LinkShot.Core;
using LinkShot.Network;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class NetworkActionCodecTests
    {
        [Test]
        public void RoundTrip_SetCardAction()
        {
            var original = new SetCardAction(1, "WALL_SHIFT_ALPHA");
            var decoded = (SetCardAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));

            Assert.AreEqual(original.Player, decoded.Player);
            Assert.AreEqual(original.CardId, decoded.CardId);
        }

        [Test]
        public void RoundTrip_PlaceWallsAction()
        {
            var original = new PlaceWallsAction(3, new System.Collections.Generic.List<int> { 1, 7, 9 });
            var decoded = (PlaceWallsAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));

            Assert.AreEqual(original.DefaultWallCell, decoded.DefaultWallCell);
            CollectionAssert.AreEqual(original.DisposableWallCells, decoded.DisposableWallCells);
        }

        [Test]
        public void RoundTrip_RollPositionAction()
        {
            var decoded = NetworkActionCodec.Decode(NetworkActionCodec.Encode(new RollPositionAction()));
            Assert.IsInstanceOf<RollPositionAction>(decoded);
        }

        [Test]
        public void RoundTrip_ChoosePositionAction()
        {
            var original = new ChoosePositionAction(4);
            var decoded = (ChoosePositionAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));
            Assert.AreEqual(original.Position, decoded.Position);
        }

        [Test]
        public void RoundTrip_RerollAndConfirmActions()
        {
            Assert.IsInstanceOf<RerollAction>(NetworkActionCodec.Decode(NetworkActionCodec.Encode(new RerollAction())));
            Assert.IsInstanceOf<ConfirmPositionAction>(NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ConfirmPositionAction())));
        }

        [Test]
        public void RoundTrip_ProceedAction()
        {
            Assert.IsInstanceOf<ProceedAction>(NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ProceedAction())));
        }

        [Test]
        public void RoundTrip_ResolveEffectAction_EmptyChoice()
        {
            var original = new ResolveEffectAction(default);
            var decoded = (ResolveEffectAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));

            Assert.IsNull(decoded.Choice.WallTargetCellIndex);
            Assert.IsNull(decoded.Choice.WallDestinationCellIndex);
            Assert.IsNull(decoded.Choice.BouncePosition);
            Assert.IsNull(decoded.Choice.WideGateZone);
            Assert.IsNull(decoded.Choice.StolenWallCardOwnerPlayer);
        }

        [Test]
        public void RoundTrip_ResolveEffectAction_WallShiftChoice()
        {
            var choice = new EffectChoice { WallTargetCellIndex = 5, WallDestinationCellIndex = 9 };
            var decoded = (ResolveEffectAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ResolveEffectAction(choice)));

            Assert.AreEqual(5, decoded.Choice.WallTargetCellIndex);
            Assert.AreEqual(9, decoded.Choice.WallDestinationCellIndex);
        }

        [Test]
        public void RoundTrip_ResolveEffectAction_BounceBoardChoice()
        {
            var choice = new EffectChoice { BouncePosition = new Vec2(0.4f, 0.6f) };
            var decoded = (ResolveEffectAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ResolveEffectAction(choice)));

            Assert.IsNotNull(decoded.Choice.BouncePosition);
            Assert.AreEqual(0.4f, decoded.Choice.BouncePosition.Value.X, 0.0001f);
            Assert.AreEqual(0.6f, decoded.Choice.BouncePosition.Value.Y, 0.0001f);
        }

        [Test]
        public void RoundTrip_ResolveEffectAction_WideGateChoice()
        {
            var choice = new EffectChoice { WideGateZone = TargetZoneId.Score500 };
            var decoded = (ResolveEffectAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ResolveEffectAction(choice)));

            Assert.AreEqual(TargetZoneId.Score500, decoded.Choice.WideGateZone);
        }

        [Test]
        public void RoundTrip_ResolveEffectAction_StolenWallOwnerChoice()
        {
            var choice = new EffectChoice { StolenWallCardOwnerPlayer = 1 };
            var decoded = (ResolveEffectAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(new ResolveEffectAction(choice)));

            Assert.AreEqual(1, decoded.Choice.StolenWallCardOwnerPlayer);
        }

        [Test]
        public void RoundTrip_SubmitShotResultAction_WithHitZones()
        {
            var original = new SubmitShotResultAction(ShotOutcomeKind.WallHit, new[] { TargetZoneId.Score500, TargetZoneId.Score100 });
            var decoded = (SubmitShotResultAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));

            Assert.AreEqual(ShotOutcomeKind.WallHit, decoded.Outcome);
            CollectionAssert.AreEqual(original.HitZones, decoded.HitZones);
        }

        [Test]
        public void RoundTrip_SubmitShotResultAction_NoHitZones()
        {
            var original = new SubmitShotResultAction(ShotOutcomeKind.OutOfField);
            var decoded = (SubmitShotResultAction)NetworkActionCodec.Decode(NetworkActionCodec.Encode(original));

            Assert.AreEqual(ShotOutcomeKind.OutOfField, decoded.Outcome);
            Assert.IsEmpty(decoded.HitZones);
        }
    }
}
