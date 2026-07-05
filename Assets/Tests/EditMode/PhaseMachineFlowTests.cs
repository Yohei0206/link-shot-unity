using System;
using System.Collections.Generic;
using LinkShot.Core;
using NUnit.Framework;

namespace LinkShot.Core.Tests
{
    public class PhaseMachineFlowTests
    {
        // 効果解決に選択対象を必要としない5種のみで組んだデッキ（フェーズ遷移の純粋な検証用）。
        private static readonly List<string> NoChoiceDeck = new List<string>
        {
            "RANGE_BOOST_GAMMA", "POWER_SHOT_GAMMA", "MINI_BALL_GAMMA", "GHOST_BALL_BETA", "CURVE_SHOT_BETA",
        };

        [Test]
        public void FullMatch_TenShots_ReachesMatchEndWithHistory()
        {
            var state = new GameState(new List<string>(NoChoiceDeck), new List<string>(NoChoiceDeck), 0, new Rng(42));

            for (int round = 1; round <= GameConfig.RoundCount; round++)
            {
                Assert.AreEqual(Phase.MedalSet, state.Phase);
                PhaseMachine.Dispatch(state, new SetMedalAction(0, state.Players[0].Hand[0]));
                PhaseMachine.Dispatch(state, new SetMedalAction(1, state.Players[1].Hand[0]));

                for (int shot = 0; shot < GameConfig.ShotsPerRound; shot++)
                {
                    Assert.AreEqual(Phase.WallPlacement, state.Phase);
                    PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int>()));

                    Assert.AreEqual(Phase.PositionRoll, state.Phase);
                    PhaseMachine.Dispatch(state, new RollPositionAction());

                    Assert.AreEqual(Phase.EffectResolve, state.Phase);
                    PhaseMachine.Dispatch(state, new ResolveEffectAction(default));

                    Assert.AreEqual(Phase.Shot, state.Phase);
                    PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center));

                    Assert.AreEqual(Phase.ScoreResolve, state.Phase);
                    PhaseMachine.Dispatch(state, new ProceedAction());
                }
            }

            Assert.AreEqual(Phase.MatchEnd, state.Phase);
            Assert.AreEqual(GameConfig.TotalShots, state.History.Count);
            Assert.AreEqual(0, state.Players[0].Hand.Count);
            Assert.AreEqual(0, state.Players[1].Hand.Count);
            // 各プレイヤーは1ラウンドにつき1回だけ攻撃する（先攻/後攻いずれか）ため、獲得点はRoundCount回分。
            Assert.AreEqual(GameConfig.CenterZoneScore * GameConfig.RoundCount, state.Players[0].Score);
            Assert.AreEqual(GameConfig.CenterZoneScore * GameConfig.RoundCount, state.Players[1].Score);
        }

        [Test]
        public void SetMedalAction_Throws_WhenNotInMedalSetPhase()
        {
            var state = TestHelpers.NewState("RANGE_BOOST_GAMMA", "MINI_BALL_GAMMA");
            Assert.Throws<InvalidOperationException>(() =>
                PhaseMachine.Dispatch(state, new SetMedalAction(0, "RANGE_BOOST_GAMMA")));
        }

        [Test]
        public void PlaceWallsAction_Throws_WhenExceedingRemainingDisposableCards()
        {
            var state = TestHelpers.NewState("RANGE_BOOST_GAMMA", "MINI_BALL_GAMMA");
            var tooMany = new List<int>();
            for (int i = 1; i <= GameConfig.DisposableWallCardCount + 1; i++)
            {
                tooMany.Add(i);
            }

            Assert.Throws<InvalidOperationException>(() =>
                PhaseMachine.Dispatch(state, new PlaceWallsAction(0, tooMany)));
        }

        [Test]
        public void PlaceWallsAction_Throws_OnDuplicateCell()
        {
            var state = TestHelpers.NewState("RANGE_BOOST_GAMMA", "MINI_BALL_GAMMA");
            Assert.Throws<InvalidOperationException>(() =>
                PhaseMachine.Dispatch(state, new PlaceWallsAction(0, new List<int> { 0 })));
        }

        [Test]
        public void SubmitShotResultAction_Throws_WhenNotInShotPhase()
        {
            var state = TestHelpers.NewState("RANGE_BOOST_GAMMA", "MINI_BALL_GAMMA");
            Assert.Throws<InvalidOperationException>(() =>
                PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center)));
        }

        [Test]
        public void Reroll_Then_Finalizes_NewRoll_And_SecondRerollThrows()
        {
            // REROLL(ALPHA) が発動するよう、防御側をBETAにする（ALPHAがBETAに勝つ）。
            var state = TestHelpers.NewState("REROLL_ALPHA", "BOUNCE_BOARD_BETA");
            TestHelpers.PlaceDefaultWallOnly(state);

            PhaseMachine.Dispatch(state, new RollPositionAction());
            Assert.IsTrue(state.RerollAvailable);
            Assert.IsNotNull(state.PendingDiceRoll);
            Assert.AreEqual(Phase.PositionRoll, state.Phase);

            PhaseMachine.Dispatch(state, new RerollAction());
            Assert.AreEqual(Phase.EffectResolve, state.Phase);
            Assert.IsFalse(state.RerollAvailable);
            Assert.IsTrue(state.Field.LaunchPosition >= 1 && state.Field.LaunchPosition <= GameConfig.LaunchPositionCount);
        }

        [Test]
        public void ConfirmPosition_FinalizesPendingRoll_WithoutReroll()
        {
            var state = TestHelpers.NewState("REROLL_ALPHA", "BOUNCE_BOARD_BETA");
            TestHelpers.PlaceDefaultWallOnly(state);

            PhaseMachine.Dispatch(state, new RollPositionAction());
            int pending = state.PendingDiceRoll.Value;

            PhaseMachine.Dispatch(state, new ConfirmPositionAction());
            Assert.AreEqual(Phase.EffectResolve, state.Phase);
            Assert.AreEqual(pending, state.Field.LaunchPosition);
        }

        [Test]
        public void Reroll_NotAvailable_WhenEffectNotActivated()
        {
            // REROLL(ALPHA) は defender=GAMMA だと負けるので発動しない（GAMMAがALPHAに勝つ）。
            var state = TestHelpers.NewState("REROLL_ALPHA", "RANGE_BOOST_GAMMA");
            TestHelpers.PlaceDefaultWallOnly(state);

            PhaseMachine.Dispatch(state, new RollPositionAction());
            Assert.IsFalse(state.RerollAvailable);
            Assert.AreEqual(Phase.EffectResolve, state.Phase);
        }

        [Test]
        public void RollPosition_Throws_WhenPositionChoiceActive()
        {
            // POSITION_CHOICE(BETA)。defender=GAMMAならBETAがGAMMAに勝つので発動する。
            var state = TestHelpers.NewState("POSITION_CHOICE", "RANGE_BOOST_GAMMA");
            TestHelpers.PlaceDefaultWallOnly(state);

            Assert.Throws<InvalidOperationException>(() => PhaseMachine.Dispatch(state, new RollPositionAction()));
        }

        [Test]
        public void ChoosePosition_SetsPositionDirectly_WhenActivated()
        {
            var state = TestHelpers.NewState("POSITION_CHOICE", "RANGE_BOOST_GAMMA");
            TestHelpers.PlaceDefaultWallOnly(state);

            PhaseMachine.Dispatch(state, new ChoosePositionAction(4));
            Assert.AreEqual(Phase.EffectResolve, state.Phase);
            Assert.AreEqual(4, state.Field.LaunchPosition);
        }

        [Test]
        public void ChoosePosition_Throws_WhenNotActivated()
        {
            // POSITION_CHOICE(BETA) は defender=ALPHAだと負ける（ALPHAがBETAに勝つ）ので発動しない。
            var state = TestHelpers.NewState("POSITION_CHOICE", "WALL_REMOVE_ALPHA");
            TestHelpers.PlaceDefaultWallOnly(state);

            Assert.Throws<InvalidOperationException>(() => PhaseMachine.Dispatch(state, new ChoosePositionAction(3)));
        }

        [Test]
        public void DoubleShot_RequiresTwoAttempts_AndTakesHigherScore()
        {
            // DOUBLE_SHOT(ALPHA)。defender=BETAならALPHAが勝つので発動する。
            var state = TestHelpers.NewState("DOUBLE_SHOT", "BOUNCE_BOARD_BETA");
            TestHelpers.PlaceDefaultWallOnly(state);
            PhaseMachine.Dispatch(state, new RollPositionAction());
            PhaseMachine.Dispatch(state, new ResolveEffectAction(default));

            Assert.AreEqual(2, state.ShotAttemptsRemaining);

            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.Center));
            Assert.AreEqual(Phase.Shot, state.Phase); // まだ2投目待ち
            Assert.AreEqual(1, state.ShotAttemptsRemaining);

            PhaseMachine.Dispatch(state, new SubmitShotResultAction(ShotOutcomeKind.TargetHit, TargetZoneId.TopLeftCorner));
            Assert.AreEqual(Phase.ScoreResolve, state.Phase);
            Assert.AreEqual(1, state.History.Count);
            Assert.AreEqual(GameConfig.CornerZoneScore, state.History[0].Score);
            Assert.AreEqual(GameConfig.CornerZoneScore, state.Players[0].Score);
        }
    }
}
