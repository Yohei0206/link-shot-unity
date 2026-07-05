using System.Collections.Generic;
using LinkShot.Core;
using LinkShot.Core.Effects;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    /// <summary>
    /// MEDALS.md記載の15効果をカバーするテスト。
    /// DOUBLE_SHOT / POSITION_CHOICE / REROLL は発射ポジション決定・ショット回数に関わるためPhaseMachineFlowTests側で検証済み。
    /// </summary>
    public class MedalEffectsTests
    {
        [Test]
        public void WallRemove_RemovesTargetedWall_WhenActivated()
        {
            // WALL_REMOVE(ALPHA) vs defender BETA => ALPHAが勝つので発動する。
            var state = TestHelpers.NewState("WALL_REMOVE_ALPHA", "BOUNCE_BOARD_BETA");
            PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int> { 3 }));
            PhaseMachine.Dispatch(state, new RollPositionAction());

            Assert.AreEqual(2, state.Field.DefenderWalls.Count);

            var choice = new EffectChoice { WallTargetCellIndex = 3 };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.AreEqual(1, state.Field.DefenderWalls.Count);
            Assert.AreEqual(0, state.Field.DefenderWalls[0].CellIndex);
        }

        [Test]
        public void WallRemove_CanRemoveDefaultWall()
        {
            var state = TestHelpers.NewState("WALL_REMOVE_ALPHA", "BOUNCE_BOARD_BETA");
            PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int>()));
            PhaseMachine.Dispatch(state, new RollPositionAction());

            var choice = new EffectChoice { WallTargetCellIndex = 0 };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.AreEqual(0, state.Field.DefenderWalls.Count);
        }

        [Test]
        public void BounceBoard_AddsPlacement_WhenActivated()
        {
            // BOUNCE_BOARD(BETA) vs defender GAMMA => BETAが勝つので発動する。
            var state = TestHelpers.NewState("BOUNCE_BOARD_BETA", "RANGE_BOOST_GAMMA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());

            var choice = new EffectChoice { BouncePosition = new Vec2(0.4f, 0.6f) };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.AreEqual(1, state.Field.BounceBoards.Count);
            Assert.AreEqual(0.4f, state.Field.BounceBoards[0].Position.X);
            Assert.AreEqual(0.6f, state.Field.BounceBoards[0].Position.Y);
        }

        [Test]
        public void RangeBoost_MultipliesLaunchRadius()
        {
            var modifier = EffectRegistry.Get(EffectId.RangeBoost).ModifyShot(ShotModifier.Default);
            Assert.AreEqual(GameConfig.RangeBoostRadiusMultiplier, modifier.LaunchRadiusMultiplier);
        }

        [Test]
        public void RangeBoost_AppliesThroughResolveEffect_WhenActivated()
        {
            // RANGE_BOOST(GAMMA) vs defender ALPHA => GAMMAが勝つので発動する。
            var state = TestHelpers.NewState("RANGE_BOOST_GAMMA", "WALL_REMOVE_ALPHA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));

            Assert.AreEqual(GameConfig.RangeBoostRadiusMultiplier, state.CurrentShotModifier.LaunchRadiusMultiplier);
        }

        [Test]
        public void WallShift_MovesWall_ToEmptyCell()
        {
            // WALL_SHIFT(ALPHA) vs defender BETA => ALPHAが勝つので発動する。
            var state = TestHelpers.NewState("WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA");
            PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int> { 2 }));
            PhaseMachine.Dispatch(state, new RollPositionAction());

            var choice = new EffectChoice { WallTargetCellIndex = 2, WallDestinationCellIndex = 7 };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.IsFalse(state.Field.DefenderWalls.Exists(w => w.CellIndex == 2));
            Assert.IsTrue(state.Field.DefenderWalls.Exists(w => w.CellIndex == 7 && !w.IsDefaultWall));
        }

        [Test]
        public void WallShift_DoesNothing_WhenDestinationOccupied()
        {
            var state = TestHelpers.NewState("WALL_SHIFT_ALPHA", "BOUNCE_BOARD_BETA");
            PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int> { 2 }));
            PhaseMachine.Dispatch(state, new RollPositionAction());

            // 移動先(0)は常設壁が既に占有している。
            var choice = new EffectChoice { WallTargetCellIndex = 2, WallDestinationCellIndex = 0 };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.IsTrue(state.Field.DefenderWalls.Exists(w => w.CellIndex == 2));
        }

        [Test]
        public void GhostBall_SetsPassThroughFirstWall()
        {
            var modifier = EffectRegistry.Get(EffectId.GhostBall).ModifyShot(ShotModifier.Default);
            Assert.IsTrue(modifier.PassThroughFirstWall);
        }

        [Test]
        public void WideGate_SetsWideGateZone_WhenActivated()
        {
            // WIDE_GATE(GAMMA) vs defender ALPHA => GAMMAが勝つので発動する。
            var state = TestHelpers.NewState("WIDE_GATE_GAMMA", "WALL_REMOVE_ALPHA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());

            var choice = new EffectChoice { WideGateZone = TargetZoneId.Center };
            PhaseMachine.Dispatch(state, new ResolveEffectAction(choice));

            Assert.AreEqual(TargetZoneId.Center, state.Field.WideGateZone);
        }

        [Test]
        public void CurveShot_AllowsMidFlightCurve()
        {
            var modifier = EffectRegistry.Get(EffectId.CurveShot).ModifyShot(ShotModifier.Default);
            Assert.IsTrue(modifier.AllowMidFlightCurve);
        }

        [Test]
        public void PowerShot_MultipliesVelocity()
        {
            var modifier = EffectRegistry.Get(EffectId.PowerShot).ModifyShot(ShotModifier.Default);
            Assert.AreEqual(GameConfig.PowerShotVelocityMultiplier, modifier.VelocityMultiplier);
        }

        [Test]
        public void ScoreDouble_DoublesScore_WhenActivated()
        {
            // SCORE_DOUBLE(ALPHA) vs defender BETA => ALPHAが勝つので発動する。
            var state = TestHelpers.NewState("SCORE_DOUBLE_ALPHA", "BOUNCE_BOARD_BETA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));
            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center));

            Assert.AreEqual(GameConfig.CenterZoneScore * 2, state.History[0].Score);
        }

        [Test]
        public void SafetyNet_GrantsFloorScore_OnZeroScore_WhenActivated()
        {
            // SAFETY_NET(BETA) vs defender GAMMA => BETAが勝つので発動する。
            var state = TestHelpers.NewState("SAFETY_NET_BETA", "RANGE_BOOST_GAMMA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));
            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.WallHit));

            Assert.AreEqual(GameConfig.SafetyNetScore, state.History[0].Score);
        }

        [Test]
        public void SafetyNet_DoesNotApply_WhenNotActivated()
        {
            // SAFETY_NET(BETA) vs defender ALPHA => ALPHAが勝つのでBETAは発動しない。
            var state = TestHelpers.NewState("SAFETY_NET_BETA", "WALL_REMOVE_ALPHA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));
            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.WallHit));

            Assert.AreEqual(0, state.History[0].Score);
            Assert.IsFalse(state.History[0].EffectActivated);
        }

        [Test]
        public void MiniBall_MultipliesBallSize()
        {
            var modifier = EffectRegistry.Get(EffectId.MiniBall).ModifyShot(ShotModifier.Default);
            Assert.AreEqual(GameConfig.MiniBallSizeMultiplier, modifier.BallSizeMultiplier);
        }

        [Test]
        public void WallReturn_GrantsCardToAttacker_WhenDefenderUsedDisposableCard()
        {
            // WALL_RETURN(GAMMA) vs defender ALPHA(WALL_REMOVE_ALPHA) => GAMMAが勝つので発動する。
            var state = TestHelpers.NewState("WALL_RETURN", "WALL_REMOVE_ALPHA");
            int attackerBefore = state.Players[0].DisposableWallCardsRemaining;

            PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int> { 5 })); // 防御側(player1)が使い捨てを1枚使用
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));
            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center));

            Assert.AreEqual(attackerBefore + 1, state.Players[0].DisposableWallCardsRemaining);
        }

        [Test]
        public void WallReturn_NoOp_WhenDefenderUsedNoDisposableCard()
        {
            var state = TestHelpers.NewState("WALL_RETURN", "WALL_REMOVE_ALPHA");
            int attackerBefore = state.Players[0].DisposableWallCardsRemaining;

            TestHelpers.PlaceDefaultWallOnly(state); // 防御側は使い捨てカードを使わない
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));
            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center));

            Assert.AreEqual(attackerBefore, state.Players[0].DisposableWallCardsRemaining);
        }
    }
}
