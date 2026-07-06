using System;
using System.Collections;
using System.Collections.Generic;
using LinkShot.Core;
using LinkShot.UI;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// Match.unityに1つ置くHumble Object（ARCHITECTURE.md 2.3章）。Core.PhaseMachineを保持して駆動し、
    /// FieldView/BallController/SlingshotInput/UI/層のパネルに反映する。
    /// メダル選択・壁配置は実際のUI操作（人間の入力）で決定する。発射ポジション決定とメダル効果解決は、
    /// 対象選択が不要な暫定デッキで運用しているため自動で進める（DeckSelect画面実装後に見直す）。
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        private const float BallBaseDiameter = 0.55f;
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

        private HandoverScreen _handoverScreen;
        private MedalSelectPanel _medalSelectPanel;
        private WallPlacementPanel _wallPlacementPanel;
        private HudPanel _hudPanel;

        private void Start()
        {
            SetUpCamera();

            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(transform);
            _fieldView = fieldGo.AddComponent<FieldView>();
            _fieldView.BuildStaticField();

            CreateBall();
            CreateUI();

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

            // Game Viewの縦横比がフィールドと異なる場合でも全体が収まるようにレターボックス的に合わせる。
            // 壁帯・的帯はFieldWidthより横幅が広い(FieldView.WideBandWidth)ため、そちらが画面外に
            // 切れないよう、幅の基準は両者のうち広い方を使う。
            float contentWidth = Mathf.Max(FieldView.FieldWidth, FieldView.WideBandWidth);
            float targetAspect = contentWidth / FieldView.FieldHeight;
            float screenAspect = (float)Screen.width / Screen.height;
            camera.orthographicSize = screenAspect >= targetAspect
                ? FieldView.FieldHeight / 2f
                : (FieldView.FieldHeight / 2f) * (targetAspect / screenAspect);

            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void CreateBall()
        {
            _ballObject = new GameObject("Ball");
            _ballObject.transform.SetParent(transform);

            var renderer = _ballObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Field/Kenney/Sports/ball_soccer1");
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

        private void CreateUI()
        {
            GameObject eventSystemGo = UITheme.CreateEventSystem();
            eventSystemGo.transform.SetParent(transform);

            Canvas canvas = UITheme.CreateCanvas("UICanvas", transform, 10);

            _handoverScreen = CreateFullScreenPanel<HandoverScreen>(canvas.transform, "HandoverScreen");
            _medalSelectPanel = CreateFullScreenPanel<MedalSelectPanel>(canvas.transform, "MedalSelectPanel");
            _wallPlacementPanel = CreateFullScreenPanel<WallPlacementPanel>(canvas.transform, "WallPlacementPanel");
            _wallPlacementPanel.Configure(_fieldView, Camera.main);
            _hudPanel = CreateFullScreenPanel<HudPanel>(canvas.transform, "HudPanel");
        }

        private static T CreateFullScreenPanel<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            UITheme.Stretch((RectTransform)go.transform);
            return go.AddComponent<T>();
        }

        private IEnumerator RunMatch()
        {
            while (_state.Phase != Phase.MatchEnd)
            {
                UpdateHudStatus();

                switch (_state.Phase)
                {
                    case Phase.MedalSet:
                        yield return RunMedalSetPhase();
                        break;
                    case Phase.WallPlacement:
                        yield return RunWallPlacementPhase();
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
                        yield return RunScoreResolvePhase();
                        break;
                }

                yield return null;
            }

            UpdateHudStatus();
            LogMatchEnd();
        }

        private IEnumerator RunMedalSetPhase()
        {
            for (int player = 0; player < 2; player++)
            {
                int currentPlayer = player;

                bool handoverDone = false;
                _handoverScreen.Show($"プレイヤー{currentPlayer + 1}に交代してください\nタップしてメダルを選んでください", () => handoverDone = true);
                yield return new WaitUntil(() => handoverDone);

                bool selected = false;
                _medalSelectPanel.Show(currentPlayer, _state.Players[currentPlayer].Hand, medalId =>
                {
                    PhaseMachine.Dispatch(_state, new SetMedalAction(currentPlayer, medalId));
                    selected = true;
                });
                yield return new WaitUntil(() => selected);
                _medalSelectPanel.Hide();
            }

            bool concealed = false;
            _handoverScreen.Show("メダルが揃いました\nタップして壁配置に進みます", () => concealed = true);
            yield return new WaitUntil(() => concealed);
        }

        private IEnumerator RunWallPlacementPhase()
        {
            int defender = _state.CurrentDefender;
            bool confirmed = false;

            _fieldView.HighlightLaunchPosition(null);

            _wallPlacementPanel.Show(defender, _state.Players[defender].DisposableWallCardsRemaining, (defaultCell, disposableCells) =>
            {
                PhaseMachine.Dispatch(_state, new PlaceWallsAction(defaultCell, disposableCells));
                _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                confirmed = true;
            });

            yield return new WaitUntil(() => confirmed);
        }

        private void AutoRollPosition()
        {
            PhaseMachine.Dispatch(_state, new RollPositionAction());
            _fieldView.HighlightLaunchPosition(_state.Field.LaunchPosition);
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

        private IEnumerator RunScoreResolvePhase()
        {
            ShotRecord record = _state.History[_state.History.Count - 1];
            string effectNote = record.EffectActivated ? "\n(攻撃効果 発動)" : string.Empty;
            string message = $"ラウンド{record.Round} / ショット{record.ShotIndex + 1}\n"
                + $"攻撃側: プレイヤー{record.Attacker + 1}\n"
                + $"結果: {record.Outcome}\n"
                + $"獲得得点: {record.Score}"
                + effectNote;

            bool proceed = false;
            _hudPanel.ShowResult(message, () => proceed = true);
            yield return new WaitUntil(() => proceed);

            PhaseMachine.Dispatch(_state, new ProceedAction());

            // このショットの壁・バウンド板は役目を終えたので撤去する（次の壁配置フェーズで新たに配置される）。
            _fieldView.ApplyWalls(new List<WallPlacement>());
            _fieldView.ApplyBounceBoards(new List<BouncePlacement>());
        }

        private void UpdateHudStatus()
        {
            string phaseLabel = _state.Phase switch
            {
                Phase.MedalSet => "メダル選択",
                Phase.WallPlacement => "壁配置",
                Phase.PositionRoll => "発射ポジション決定",
                Phase.EffectResolve => "メダル効果解決",
                Phase.Shot => $"ここから発射！（発射ポジション{_state.Field.LaunchPosition}）",
                Phase.ScoreResolve => "得点解決",
                Phase.MatchEnd => "試合終了",
                _ => string.Empty,
            };

            _hudPanel.UpdateStatus(_state.Round, GameConfig.RoundCount, _state.Players[0].Score, _state.Players[1].Score, phaseLabel);
        }

        private void LogMatchEnd()
        {
            int p0 = _state.Players[0].Score;
            int p1 = _state.Players[1].Score;
            string result = _state.Winner == null ? "引き分け" : $"プレイヤー{_state.Winner + 1}の勝利";
            string message = $"試合終了\nP1: {p0}  -  P2: {p1}\n{result}";
            Debug.Log($"[試合終了] P0={p0} / P1={p1} → {result}");
            _hudPanel.ShowResult(message, () => { });
        }
    }
}
