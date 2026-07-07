using System;
using System.Collections.Generic;
using System.Linq;
using LinkShot.Core.Effects;

namespace LinkShot.Core
{
    /// <summary>
    /// GAME_RULES.md 3章の全フェーズ遷移を実装するステートマシン（ARCHITECTURE.md 2.3章）。
    /// Dispatch(state, action) の形で状態を更新する。UnityEngineに依存しない。
    /// </summary>
    public static class PhaseMachine
    {
        public static void Dispatch(GameState state, GameAction action)
        {
            switch (action)
            {
                case SetCardAction a:
                    HandleSetCard(state, a);
                    break;
                case PlaceWallsAction a:
                    HandlePlaceWalls(state, a);
                    break;
                case RollPositionAction _:
                    HandleRollPosition(state);
                    break;
                case ChoosePositionAction a:
                    HandleChoosePosition(state, a);
                    break;
                case RerollAction _:
                    HandleReroll(state);
                    break;
                case ConfirmPositionAction _:
                    HandleConfirmPosition(state);
                    break;
                case ResolveEffectAction a:
                    HandleResolveEffect(state, a);
                    break;
                case SubmitShotResultAction a:
                    HandleSubmitShotResult(state, a);
                    break;
                case ProceedAction _:
                    HandleProceed(state);
                    break;
                default:
                    throw new ArgumentException($"未知のアクション: {action.GetType()}");
            }
        }

        private static void RequirePhase(GameState state, Phase expected)
        {
            if (state.Phase != expected)
            {
                throw new InvalidOperationException($"フェーズ {expected} でのみ許可される操作です（現在: {state.Phase}）");
            }
        }

        private static void HandleSetCard(GameState state, SetCardAction action)
        {
            RequirePhase(state, Phase.CardSet);

            PlayerState player = state.Players[action.Player];
            if (player.SetCardId != null)
            {
                throw new InvalidOperationException("このラウンドは既にカードをセット済みです");
            }

            if (!player.Hand.Contains(action.CardId))
            {
                throw new InvalidOperationException($"手札にないカードです: {action.CardId}");
            }

            player.Hand.Remove(action.CardId);
            player.UsedCardIds.Add(action.CardId);
            player.SetCardId = action.CardId;

            if (state.BothCardsSet)
            {
                state.Phase = Phase.WallPlacement;
            }
        }

        private static void HandlePlaceWalls(GameState state, PlaceWallsAction action)
        {
            RequirePhase(state, Phase.WallPlacement);

            PlayerState defender = state.Players[state.CurrentDefender];
            IReadOnlyList<int> disposableCells = action.DisposableWallCells;

            if (disposableCells.Count > defender.DisposableWallCardsRemaining)
            {
                throw new InvalidOperationException("残りの使い捨て壁カードを超える枚数は配置できません");
            }

            var allCells = new List<int> { action.DefaultWallCell };
            allCells.AddRange(disposableCells);

            if (allCells.Distinct().Count() != allCells.Count)
            {
                throw new InvalidOperationException("同じマスに複数の壁は配置できません");
            }

            if (allCells.Any(c => c < 0 || c >= GameConfig.WallGridCellCount))
            {
                throw new InvalidOperationException("壁グリッドの範囲外です");
            }

            state.Field.Reset();
            state.Field.DefenderWalls.Add(new WallPlacement(action.DefaultWallCell, isDefaultWall: true));
            foreach (int cell in disposableCells)
            {
                state.Field.DefenderWalls.Add(new WallPlacement(cell, isDefaultWall: false));
            }

            defender.DisposableWallCardsRemaining -= disposableCells.Count;
            defender.DisposableWallCardsUsedThisRound += disposableCells.Count;

            state.Phase = Phase.PositionRoll;
        }

        private static void HandleRollPosition(GameState state)
        {
            RequirePhase(state, Phase.PositionRoll);

            if (AttackerEffectFlag(state, e => e.ReplacesPositionRoll))
            {
                throw new InvalidOperationException("POSITION_CHOICE発動中はサイコロではなく位置を選択してください");
            }

            int roll = state.Rng.RollPosition();

            if (AttackerEffectFlag(state, e => e.AllowsReroll))
            {
                state.PendingDiceRoll = roll;
                state.RerollAvailable = true;
            }
            else
            {
                FinalizePosition(state, roll);
            }
        }

        private static void HandleChoosePosition(GameState state, ChoosePositionAction action)
        {
            RequirePhase(state, Phase.PositionRoll);

            if (!AttackerEffectFlag(state, e => e.ReplacesPositionRoll))
            {
                throw new InvalidOperationException("POSITION_CHOICEが発動していないため位置を自由選択できません");
            }

            if (action.Position < 1 || action.Position > GameConfig.LaunchPositionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(action.Position));
            }

            FinalizePosition(state, action.Position);
        }

        private static void HandleReroll(GameState state)
        {
            RequirePhase(state, Phase.PositionRoll);

            if (!state.RerollAvailable || state.RerollUsed)
            {
                throw new InvalidOperationException("振り直しは行えません");
            }

            state.RerollUsed = true;
            int roll = state.Rng.RollPosition();
            FinalizePosition(state, roll);
        }

        private static void HandleConfirmPosition(GameState state)
        {
            RequirePhase(state, Phase.PositionRoll);

            if (!state.RerollAvailable || state.PendingDiceRoll == null)
            {
                throw new InvalidOperationException("確定できる出目がありません");
            }

            FinalizePosition(state, state.PendingDiceRoll.Value);
        }

        private static void FinalizePosition(GameState state, int position)
        {
            state.Field.LaunchPosition = position;
            state.PendingDiceRoll = null;
            state.RerollAvailable = false;
            state.Phase = Phase.EffectResolve;
        }

        private static void HandleResolveEffect(GameState state, ResolveEffectAction action)
        {
            RequirePhase(state, Phase.EffectResolve);

            if (state.CurrentShotEffectActivated)
            {
                ICardEffect effect = EffectRegistry.Get(state.AttackerCard.Effect);
                effect.OnResolve(state, action.Choice);
                state.CurrentShotModifier = effect.ModifyShot(ShotModifier.Default);
            }
            else
            {
                state.CurrentShotModifier = ShotModifier.Default;
            }

            state.ShotAttemptsRemaining = state.CurrentShotModifier.ShotAttempts;
            state.ShotAttemptScores.Clear();
            state.ShotAttemptOutcomes.Clear();
            state.Phase = Phase.Shot;
        }

        private static void HandleSubmitShotResult(GameState state, SubmitShotResultAction action)
        {
            RequirePhase(state, Phase.Shot);

            if (state.ShotAttemptsRemaining <= 0)
            {
                throw new InvalidOperationException("このショットの試行はすべて終了しています");
            }

            int baseScore = Scoring.BaseScore(action.HitZones);
            state.ShotAttemptScores.Add(baseScore);
            state.ShotAttemptOutcomes.Add(action.Outcome);
            state.ShotAttemptsRemaining -= 1;

            if (state.ShotAttemptsRemaining > 0)
            {
                return;
            }

            int bestIndex = 0;
            for (int i = 1; i < state.ShotAttemptScores.Count; i++)
            {
                if (state.ShotAttemptScores[i] > state.ShotAttemptScores[bestIndex])
                {
                    bestIndex = i;
                }
            }

            int baseFinal = state.ShotAttemptScores[bestIndex];
            ShotOutcomeKind outcomeFinal = state.ShotAttemptOutcomes[bestIndex];

            int attacker = state.CurrentAttacker;
            int defender = state.CurrentDefender;
            bool activated = state.CurrentShotEffectActivated;

            int finalScore = baseFinal;
            if (activated)
            {
                finalScore = EffectRegistry.Get(state.AttackerCard.Effect).ModifyScore(state, outcomeFinal, baseFinal);
            }

            state.Players[attacker].Score += finalScore;

            state.History.Add(new ShotRecord
            {
                Round = state.Round,
                ShotIndex = state.ShotIndex,
                Attacker = attacker,
                Defender = defender,
                AttackerCardId = state.Players[attacker].SetCardId,
                DefenderCardId = state.Players[defender].SetCardId,
                EffectActivated = activated,
                Outcome = outcomeFinal,
                Score = finalScore,
            });

            if (activated)
            {
                EffectRegistry.Get(state.AttackerCard.Effect).OnAfterScore(state, attacker, defender);
            }

            state.Phase = Phase.ScoreResolve;
        }

        private static void HandleProceed(GameState state)
        {
            RequirePhase(state, Phase.ScoreResolve);

            if (state.ShotIndex == 0)
            {
                state.ShotIndex = 1;
                ResetPerShotState(state);
                state.Phase = Phase.WallPlacement;
                return;
            }

            if (state.Round < GameConfig.RoundCount)
            {
                state.Round += 1;
                state.ShotIndex = 0;
                ResetPerShotState(state);
                state.Players[0].SetCardId = null;
                state.Players[1].SetCardId = null;
                state.Players[0].DisposableWallCardsUsedThisRound = 0;
                state.Players[1].DisposableWallCardsUsedThisRound = 0;
                state.Phase = Phase.CardSet;
            }
            else
            {
                state.Phase = Phase.MatchEnd;
            }
        }

        private static void ResetPerShotState(GameState state)
        {
            state.Field.Reset();
            state.PendingDiceRoll = null;
            state.RerollAvailable = false;
            state.RerollUsed = false;
            state.CurrentShotModifier = ShotModifier.Default;
            state.ShotAttemptsRemaining = 0;
            state.ShotAttemptScores.Clear();
            state.ShotAttemptOutcomes.Clear();
        }

        private static bool AttackerEffectFlag(GameState state, Func<ICardEffect, bool> flag)
        {
            return state.CurrentShotEffectActivated && flag(EffectRegistry.Get(state.AttackerCard.Effect));
        }
    }
}
