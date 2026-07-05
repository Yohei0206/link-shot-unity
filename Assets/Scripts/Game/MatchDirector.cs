using System;
using System.Collections;
using System.Collections.Generic;
using LinkShot.Core;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// Match.unityに1つ置くHumble Object（ARCHITECTURE.md 2.3章）。Core.PhaseMachineを保持して駆動し、
    /// FieldView/BallController/SlingshotInputに反映する。
    /// メダル選択・壁配置UI（UI/層）は未実装のため、暫定的に自動選択で埋めて物理・ルールエンジンの
    /// 結合動作を検証できるようにする。ショットフェーズのみ実際のスリングショット操作を受け付ける。
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        private const float BallBaseDiameter = 0.3f;
        private const float BaseLaunchSpeed = 8f;

        // UI/DeckSelect未実装のための暫定デッキ（対象選択が不要な5種で統一）。
        private static readonly List<string> PlaceholderDeck = new List<string>
        {
            "RANGE_BOOST_GAMMA", "POWER_SHOT_GAMMA", "MINI_BALL_GAMMA", "GHOST_BALL_BETA", "CURVE_SHOT_BETA",
        };

        private GameState _state;
        private FieldView _fieldView;
        private GameObject _ballObject;
        private BallController _ballController;
        private SlingshotInput _slingshotInput;

        private void Start()
        {
            SetUpCamera();

            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(transform);
            _fieldView = fieldGo.AddComponent<FieldView>();
            _fieldView.BuildStaticField();

            CreateBall();

            if (!MedalCatalog.IsValidDeck(PlaceholderDeck, out string error))
            {
                Debug.LogError($"[MatchDirector] 暫定デッキが不正です: {error}");
                return;
            }

            _state = new GameState(new List<string>(PlaceholderDeck), new List<string>(PlaceholderDeck));
            StartCoroutine(RunMatch());
        }

        private static void SetUpCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = FieldView.FieldHeight / 2f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void CreateBall()
        {
            _ballObject = new GameObject("Ball");
            _ballObject.transform.SetParent(transform);

            var renderer = _ballObject.AddComponent<SpriteRenderer>();
            renderer.sprite = FieldView.GetSharedSquareSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 10;

            var rigidbody = _ballObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.linearDamping = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = _ballObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.sharedMaterial = PhysicsMaterials.Bouncy;

            _ballController = _ballObject.AddComponent<BallController>();
            _slingshotInput = _ballObject.AddComponent<SlingshotInput>();

            _ballObject.transform.localScale = Vector3.one * BallBaseDiameter;
            _ballObject.SetActive(false);
        }

        private IEnumerator RunMatch()
        {
            while (_state.Phase != Phase.MatchEnd)
            {
                switch (_state.Phase)
                {
                    case Phase.MedalSet:
                        AutoSetMedals();
                        break;
                    case Phase.WallPlacement:
                        AutoPlaceWalls();
                        break;
                    case Phase.PositionRoll:
                        AutoRollPosition();
                        break;
                    case Phase.EffectResolve:
                        AutoResolveEffect();
                        break;
                    case Phase.Shot:
                        yield return RunShotPhase();
                        break;
                    case Phase.ScoreResolve:
                        LogScoreResolve();
                        PhaseMachine.Dispatch(_state, new ProceedAction());
                        break;
                }

                yield return null;
            }

            LogMatchEnd();
        }

        private void AutoSetMedals()
        {
            PhaseMachine.Dispatch(_state, new SetMedalAction(0, _state.Players[0].Hand[0]));
            PhaseMachine.Dispatch(_state, new SetMedalAction(1, _state.Players[1].Hand[0]));
            Debug.Log($"[Round {_state.Round}] メダルセット完了: P0={_state.Players[0].SetMedalId} / P1={_state.Players[1].SetMedalId}");
        }

        private void AutoPlaceWalls()
        {
            int cell = UnityEngine.Random.Range(0, GameConfig.WallGridCellCount);
            PhaseMachine.Dispatch(_state, new PlaceWallsAction(cell, new List<int>()));
            _fieldView.ApplyWalls(_state.Field.DefenderWalls);
        }

        private void AutoRollPosition()
        {
            PhaseMachine.Dispatch(_state, new RollPositionAction());
            Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 発射ポジション: {_state.Field.LaunchPosition}");
        }

        private void AutoResolveEffect()
        {
            PhaseMachine.Dispatch(_state, new ResolveEffectAction(default));
            _fieldView.ApplyBounceBoards(_state.Field.BounceBoards);

            if (_state.CurrentShotEffectActivated)
            {
                Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 攻撃効果発動: {_state.AttackerMedal.Effect}");
            }
        }

        private IEnumerator RunShotPhase()
        {
            Vector2 origin = _fieldView.GetLaunchPositionWorld(_state.Field.LaunchPosition);
            ApplyShotModifierToBall();

            while (_state.Phase == Phase.Shot)
            {
                yield return RunSingleAttempt(origin);
            }
        }

        private void ApplyShotModifierToBall()
        {
            ShotModifier modifier = _state.CurrentShotModifier;
            _ballController.PassThroughFirstWall = modifier.PassThroughFirstWall;
            _slingshotInput.MaxLaunchSpeed = BaseLaunchSpeed * modifier.VelocityMultiplier;
            _ballObject.transform.localScale = Vector3.one * BallBaseDiameter * modifier.BallSizeMultiplier;
        }

        private IEnumerator RunSingleAttempt(Vector2 origin)
        {
            _ballObject.SetActive(true);
            _slingshotInput.BeginAt(origin);

            bool resolved = false;
            ShotOutcomeKind outcome = default;
            TargetZoneId? zone = null;
            Action<ShotOutcomeKind, TargetZoneId?> onResolved = (o, z) =>
            {
                outcome = o;
                zone = z;
                resolved = true;
            };

            bool launched = false;
            Action onLaunched = () => launched = true;

            _ballController.ShotResolved += onResolved;
            _slingshotInput.Launched += onLaunched;

            float preLaunchTimer = 0f;
            float flightTimer = 0f;

            while (!resolved)
            {
                if (!launched)
                {
                    preLaunchTimer += Time.deltaTime;
                    if (preLaunchTimer >= GameConfig.ShotTimeLimitSeconds)
                    {
                        _ballController.ResolveTimeout();
                    }
                }
                else
                {
                    flightTimer += Time.deltaTime;
                    if (flightTimer >= GameConfig.BallFlightTimeoutSeconds)
                    {
                        _ballController.ResolveTimeout();
                    }
                }

                yield return null;
            }

            _ballController.ShotResolved -= onResolved;
            _slingshotInput.Launched -= onLaunched;
            _ballObject.SetActive(false);

            Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 着弾結果: {outcome} zone={zone}");
            PhaseMachine.Dispatch(_state, new SubmitShotResultAction(outcome, zone));
        }

        private void LogScoreResolve()
        {
            ShotRecord record = _state.History[_state.History.Count - 1];
            Debug.Log($"[Round {record.Round} Shot {record.ShotIndex}] 得点: {record.Score} (攻撃側P{record.Attacker}, 効果発動={record.EffectActivated})");
        }

        private void LogMatchEnd()
        {
            int p0 = _state.Players[0].Score;
            int p1 = _state.Players[1].Score;
            string result = _state.Winner == null ? "引き分け" : $"P{_state.Winner}の勝利";
            Debug.Log($"[試合終了] P0={p0} / P1={p1} → {result}");
        }
    }
}
