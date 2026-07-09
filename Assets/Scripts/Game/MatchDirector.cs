using System;
using System.Collections;
using System.Collections.Generic;
using LinkShot.AI;
using LinkShot.Core;
using LinkShot.Core.Effects;
using LinkShot.UI;
using UnityEngine;

namespace LinkShot.Game
{
    /// <summary>
    /// Match.unityに1つ置くHumble Object（ARCHITECTURE.md 2.3章）。Core.PhaseMachineを保持して駆動し、
    /// FieldView/BallController/SlingshotInput/UI/層のパネルに反映する。
    /// カード選択・壁配置・対象選択が必要なカード効果（WALL_REMOVE/BOUNCE_BOARD/WALL_SHIFT/WIDE_GATE）・
    /// 発射ポジション決定（POSITION_CHOICE/REROLL）はすべて実際のUI操作（人間の入力）で決定する。
    /// 対象選択が不要な効果は即座に解決する。
    /// </summary>
    public class MatchDirector : MonoBehaviour
    {
        private const float BallBaseDiameter = 0.55f;
        private const float BaseLaunchSpeed = 8f;

        private GameState _state;
        private FieldView _fieldView;
        private GameObject _ballObject;
        private GameObject _ballVisualObject;
        private BallController _ballController;
        private SlingshotInput _slingshotInput;

        private HandoverScreen _handoverScreen;
        private TitlePanel _titlePanel;
        private ModeSelectPanel _modeSelectPanel;
        private DeckSelectPanel _deckSelectPanel;
        private CardSelectPanel _cardSelectPanel;
        private WallPlacementPanel _wallPlacementPanel;
        private PositionRollPanel _positionRollPanel;
        private EffectChoicePanel _effectChoicePanel;
        private HudPanel _hudPanel;
        private ResultPanel _resultPanel;

        // --- Phase 2: CPU対戦 ---
        private int? _cpuPlayerIndex; // null = 2人対戦。0/1ならそのプレイヤー枠がCPU。
        private CpuDifficulty _cpuDifficulty;
        private readonly Rng _cpuRng = new Rng();

        private bool IsCpu(int player) => _cpuPlayerIndex == player;

        private void Start()
        {
            SetUpCamera();

            var fieldGo = new GameObject("Field");
            fieldGo.transform.SetParent(transform);
            _fieldView = fieldGo.AddComponent<FieldView>();
            _fieldView.BuildStaticField();

            CreateBall();
            CreateUI();

            StartCoroutine(RunGameLoop());
        }

        /// <summary>
        /// タイトル→モード選択→デッキ選択→対戦→リザルトを1試合ぶん行い、
        /// リザルトの「もう一度遊ぶ」でタイトルから繰り返す。
        /// </summary>
        private IEnumerator RunGameLoop()
        {
            while (true)
            {
                yield return RunTitleScreen();
                yield return RunModeSelect();
                yield return RunPreMatch();
                yield return RunMatch();
                yield return RunResultPhase();
            }
        }

        /// <summary>タイトル画面。タップでモード選択へ進む。試合開始前は情報パネルも隠す。</summary>
        private IEnumerator RunTitleScreen()
        {
            _hudPanel.Hide();

            bool started = false;
            _titlePanel.Show(() => started = true);
            yield return new WaitUntil(() => started);
        }

        /// <summary>対戦モード選択(ROADMAP.md Phase 2)。</summary>
        private IEnumerator RunModeSelect()
        {
            bool chosen = false;
            _modeSelectPanel.Show((cpuPlayerIndex, difficulty) =>
            {
                _cpuPlayerIndex = cpuPlayerIndex;
                _cpuDifficulty = difficulty;
                chosen = true;
            });
            yield return new WaitUntil(() => chosen);
        }

        /// <summary>対戦開始前に、両プレイヤーがデッキ（15種から{GameConfig.DeckSize}枚）を選ぶ。</summary>
        private IEnumerator RunPreMatch()
        {
            var decks = new List<string>[2];

            for (int player = 0; player < 2; player++)
            {
                int currentPlayer = player;

                if (IsCpu(currentPlayer))
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    decks[currentPlayer] = CpuDeckBuilder.BuildDeck(_cpuRng);
                    continue;
                }

                bool handoverDone = false;
                _handoverScreen.Show($"プレイヤー{currentPlayer + 1}に交代してください\nタップしてデッキを選んでください", () => handoverDone = true);
                yield return new WaitUntil(() => handoverDone);

                List<string> chosenDeck = null;
                _deckSelectPanel.Show(currentPlayer, deck => chosenDeck = new List<string>(deck));
                yield return new WaitUntil(() => chosenDeck != null);

                decks[currentPlayer] = chosenDeck;
            }

            bool concealed = false;
            _handoverScreen.Show("デッキが揃いました\nタップして対戦開始", () => concealed = true);
            yield return new WaitUntil(() => concealed);

            _state = new GameState(decks[0], decks[1]);
            _hudPanel.Show();
        }

        /// <summary>(12) リザルト画面: 最終スコア・勝敗・全ショット履歴を表示し、「もう一度遊ぶ」を待つ。</summary>
        private IEnumerator RunResultPhase()
        {
            bool restart = false;
            _resultPanel.Show(_state, () => restart = true);
            yield return new WaitUntil(() => restart);
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
            // 壁帯・的帯・発射帯で横幅が異なる(FieldView.WallBandWidth > WideBandWidth > FieldWidth)ため、
            // そちらが画面外に切れないよう、幅の基準は3つのうち最も広いものを使う。
            float contentWidth = Mathf.Max(FieldView.FieldWidth, Mathf.Max(FieldView.WideBandWidth, FieldView.WallBandWidth));
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

            var rigidbody = _ballObject.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.linearDamping = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = _ballObject.AddComponent<CircleCollider2D>();
            collider.sharedMaterial = PhysicsMaterials.Bouncy;

            _ballController = _ballObject.AddComponent<BallController>();
            _slingshotInput = _ballObject.AddComponent<SlingshotInput>();

            // 見た目のスケールは子オブジェクトに閉じ込める。ball_soccer1はネイティブサイズが1x1ワールド単位
            // ではないため、この親（_ballObject）自身をスケールすると、CircleCollider2Dの当たり判定の大きさ
            // まで意図せず一緒にスケールされてしまう。
            _ballVisualObject = new GameObject("Visual");
            _ballVisualObject.transform.SetParent(_ballObject.transform, false);
            var renderer = _ballVisualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Field/Kenney/Sports/ball_soccer1");
            renderer.color = Color.white;
            renderer.sortingOrder = 10;

            ApplyBallDiameter(BallBaseDiameter);
            _ballObject.SetActive(false);
        }

        private void ApplyBallDiameter(float diameter)
        {
            _ballObject.GetComponent<CircleCollider2D>().radius = diameter / 2f;

            var renderer = _ballVisualObject.GetComponent<SpriteRenderer>();
            Vector2 nativeSize = renderer.sprite.bounds.size;
            _ballVisualObject.transform.localScale = new Vector3(diameter / nativeSize.x, diameter / nativeSize.y, 1f);
        }

        private void CreateUI()
        {
            GameObject eventSystemGo = UITheme.CreateEventSystem();
            eventSystemGo.transform.SetParent(transform);

            Canvas canvas = UITheme.CreateCanvas("UICanvas", transform, 10);

            // HudPanelは常時表示の背面レイヤーなので、他の全画面ダイアログより先に生成し、
            // 兄弟順で一番手前に来ないようにする(そうしないとHudPanelのカードボードが他のダイアログの上に透けて見えてしまう)。
            _hudPanel = CreateFullScreenPanel<HudPanel>(canvas.transform, "HudPanel");

            _handoverScreen = CreateFullScreenPanel<HandoverScreen>(canvas.transform, "HandoverScreen");
            _modeSelectPanel = CreateFullScreenPanel<ModeSelectPanel>(canvas.transform, "ModeSelectPanel");
            _deckSelectPanel = CreateFullScreenPanel<DeckSelectPanel>(canvas.transform, "DeckSelectPanel");
            _cardSelectPanel = CreateFullScreenPanel<CardSelectPanel>(canvas.transform, "CardSelectPanel");
            _wallPlacementPanel = CreateFullScreenPanel<WallPlacementPanel>(canvas.transform, "WallPlacementPanel");
            _wallPlacementPanel.Configure(_fieldView, Camera.main);
            _positionRollPanel = CreateFullScreenPanel<PositionRollPanel>(canvas.transform, "PositionRollPanel");
            _effectChoicePanel = CreateFullScreenPanel<EffectChoicePanel>(canvas.transform, "EffectChoicePanel");
            _effectChoicePanel.Configure(_fieldView, Camera.main);
            _resultPanel = CreateFullScreenPanel<ResultPanel>(canvas.transform, "ResultPanel");
            _titlePanel = CreateFullScreenPanel<TitlePanel>(canvas.transform, "TitlePanel");
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
                    case Phase.CardSet:
                        yield return RunCardSetPhase();
                        break;
                    case Phase.WallPlacement:
                        yield return RunWallPlacementPhase();
                        break;
                    case Phase.PositionRoll:
                        yield return RunPositionRollPhase();
                        break;
                    case Phase.EffectResolve:
                        yield return RunEffectResolvePhase();
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
        }

        private IEnumerator RunCardSetPhase()
        {
            for (int player = 0; player < 2; player++)
            {
                int currentPlayer = player;

                if (IsCpu(currentPlayer))
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    int opponent = 1 - currentPlayer;
                    string cardId = CpuCardSelector.ChooseCard(_state.Players[currentPlayer], _state.Players[opponent], _cpuDifficulty, _cpuRng);
                    PhaseMachine.Dispatch(_state, new SetCardAction(currentPlayer, cardId));
                    continue;
                }

                bool handoverDone = false;
                _handoverScreen.Show($"プレイヤー{currentPlayer + 1}に交代してください\nタップしてカードを選んでください", () => handoverDone = true);
                yield return new WaitUntil(() => handoverDone);

                bool selected = false;
                _cardSelectPanel.Show(currentPlayer, _state.Players[currentPlayer].Hand, cardId =>
                {
                    PhaseMachine.Dispatch(_state, new SetCardAction(currentPlayer, cardId));
                    selected = true;
                });
                yield return new WaitUntil(() => selected);
                _cardSelectPanel.Hide();
            }

            bool concealed = false;
            _handoverScreen.Show("カードが揃いました\nタップして壁配置に進みます", () => concealed = true);
            yield return new WaitUntil(() => concealed);
        }

        private IEnumerator RunWallPlacementPhase()
        {
            int defender = _state.CurrentDefender;

            // 的は貫通式でショットごとにランダム配置し直す（先攻/後攻それぞれ、GAME_RULES.md 5.1章）。
            _fieldView.RebuildTargets();
            _fieldView.HighlightLaunchPosition(null);

            if (IsCpu(defender))
            {
                yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                (int defaultCell, List<int> disposableCells) = CpuWallPlanner.PlanWalls(_state, defender, _cpuDifficulty, _cpuRng);
                PhaseMachine.Dispatch(_state, new PlaceWallsAction(defaultCell, disposableCells));
                _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                yield break;
            }

            bool confirmed = false;
            _wallPlacementPanel.Show(defender, _state.Players[defender].DisposableWallCardsRemaining, (defaultCell, disposableCells) =>
            {
                PhaseMachine.Dispatch(_state, new PlaceWallsAction(defaultCell, disposableCells));
                _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                confirmed = true;
            });

            yield return new WaitUntil(() => confirmed);
        }

        /// <summary>
        /// (3)(8) 発射ポジション決定。POSITION_CHOICE発動中はサイコロを振らず自由選択UIを出す。
        /// REROLL発動中は出目を見せてから振り直すか確定するかのUIを出す。どちらでもなければ即座に確定する。
        /// </summary>
        private IEnumerator RunPositionRollPhase()
        {
            ICardEffect effect = _state.CurrentShotEffectActivated ? EffectRegistry.Get(_state.AttackerCard.Effect) : null;
            bool attackerIsCpu = IsCpu(_state.CurrentAttacker);

            if (effect != null && effect.ReplacesPositionRoll)
            {
                if (attackerIsCpu)
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    int position = CpuPositionChooser.ChoosePosition(_cpuDifficulty, _cpuRng);
                    PhaseMachine.Dispatch(_state, new ChoosePositionAction(position));
                }
                else
                {
                    bool chosen = false;
                    _positionRollPanel.ShowPositionChoice(position =>
                    {
                        PhaseMachine.Dispatch(_state, new ChoosePositionAction(position));
                        chosen = true;
                    });
                    yield return new WaitUntil(() => chosen);
                }
            }
            else
            {
                PhaseMachine.Dispatch(_state, new RollPositionAction());

                if (_state.RerollAvailable)
                {
                    int rolled = _state.PendingDiceRoll.Value;

                    if (attackerIsCpu)
                    {
                        yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                        bool wantsReroll = CpuPositionChooser.ChooseReroll(rolled, _cpuDifficulty, _cpuRng);
                        GameAction action = wantsReroll ? new RerollAction() : (GameAction)new ConfirmPositionAction();
                        PhaseMachine.Dispatch(_state, action);
                    }
                    else
                    {
                        bool decided = false;
                        _positionRollPanel.ShowReroll(rolled, wantsReroll =>
                        {
                            GameAction action = wantsReroll ? new RerollAction() : (GameAction)new ConfirmPositionAction();
                            PhaseMachine.Dispatch(_state, action);
                            decided = true;
                        });
                        yield return new WaitUntil(() => decided);
                    }
                }
            }

            _fieldView.HighlightLaunchPosition(_state.Field.LaunchPosition);
            Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 発射ポジション: {_state.Field.LaunchPosition}");
        }

        /// <summary>
        /// (4)(9) カード効果解決。対象選択が必要な効果（WALL_REMOVE/WALL_SHIFT/BOUNCE_BOARD/WIDE_GATE）は
        /// 攻撃側にUIで選ばせてからResolveEffectActionをdispatchする。それ以外はChoice無しで即座に解決する。
        /// </summary>
        private IEnumerator RunEffectResolvePhase()
        {
            EffectChoice choice = default;

            if (_state.CurrentShotEffectActivated)
            {
                EffectId effectId = _state.AttackerCard.Effect;
                Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 攻撃効果発動: {effectId}");

                if (IsCpu(_state.CurrentAttacker) && NeedsEffectTarget(effectId))
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    choice = CpuEffectChoiceSelector.Choose(_state, effectId, _cpuRng);
                }
                else
                {
                    bool resolved = false;

                    switch (effectId)
                    {
                        case EffectId.WallRemove when _state.Field.DefenderWalls.Count > 0:
                            _effectChoicePanel.ShowWallRemove(_state.Field.DefenderWalls, cell =>
                            {
                                choice = new EffectChoice { WallTargetCellIndex = cell };
                                resolved = true;
                            });
                            yield return new WaitUntil(() => resolved);
                            break;

                        case EffectId.WallShift when _state.Field.DefenderWalls.Count > 0:
                            _effectChoicePanel.ShowWallShift(_state.Field.DefenderWalls, (fromCell, toCell) =>
                            {
                                choice = new EffectChoice { WallTargetCellIndex = fromCell, WallDestinationCellIndex = toCell };
                                resolved = true;
                            });
                            yield return new WaitUntil(() => resolved);
                            break;

                        case EffectId.BounceBoard:
                            _effectChoicePanel.ShowBounceBoard(position =>
                            {
                                choice = new EffectChoice { BouncePosition = position };
                                resolved = true;
                            });
                            yield return new WaitUntil(() => resolved);
                            break;

                        case EffectId.WideGate:
                            _effectChoicePanel.ShowWideGate(zone =>
                            {
                                choice = new EffectChoice { WideGateZone = zone };
                                resolved = true;
                            });
                            yield return new WaitUntil(() => resolved);
                            break;
                    }
                }
            }

            PhaseMachine.Dispatch(_state, new ResolveEffectAction(choice));
            _fieldView.ApplyWalls(_state.Field.DefenderWalls);
            _fieldView.ApplyBounceBoards(_state.Field.BounceBoards);
            _fieldView.ApplyWideGate(_state.Field.WideGateZone);
        }

        /// <summary>対象選択UI(EffectChoicePanel)が実際に出る効果かどうか。CPU分岐の条件を人間側と揃えるために使う。</summary>
        private bool NeedsEffectTarget(EffectId effectId)
        {
            switch (effectId)
            {
                case EffectId.WallRemove:
                case EffectId.WallShift:
                    return _state.Field.DefenderWalls.Count > 0;
                case EffectId.BounceBoard:
                case EffectId.WideGate:
                    return true;
                default:
                    return false;
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
            ApplyBallDiameter(BallBaseDiameter * modifier.BallSizeMultiplier);
        }

        private IEnumerator RunSingleAttempt(Vector2 origin)
        {
            _ballObject.SetActive(true);
            _ballController.Rearm();
            _slingshotInput.BeginAt(origin);

            bool attackerIsCpu = IsCpu(_state.CurrentAttacker);
            _slingshotInput.InputEnabled = !attackerIsCpu;

            if (attackerIsCpu)
            {
                yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                (float angleOffset, float power) = CpuShotAimPlanner.GetAim(_cpuDifficulty, _state.Field.LaunchPosition, _cpuRng);
                Vector2 direction = Quaternion.Euler(0f, 0f, angleOffset * Mathf.Rad2Deg) * Vector2.up;
                _slingshotInput.LaunchCpuShot(direction, power);
            }

            bool resolved = false;
            ShotOutcomeKind outcome = default;
            IReadOnlyList<TargetZoneId> hitZones = Array.Empty<TargetZoneId>();
            Action<ShotOutcomeKind, IReadOnlyList<TargetZoneId>> onResolved = (o, zones) =>
            {
                outcome = o;
                hitZones = zones;
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

            Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 着弾結果: {outcome} 命中した的={hitZones.Count}個");
            PhaseMachine.Dispatch(_state, new SubmitShotResultAction(outcome, hitZones));
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
                Phase.CardSet => "カード選択",
                Phase.WallPlacement => "壁配置",
                Phase.PositionRoll => "発射ポジション決定",
                Phase.EffectResolve => "カード効果解決",
                Phase.Shot => $"ここから発射！（発射ポジション{_state.Field.LaunchPosition}）",
                Phase.ScoreResolve => "得点解決",
                Phase.MatchEnd => "試合終了",
                _ => string.Empty,
            };

            _hudPanel.UpdateStatus(phaseLabel);

            bool bothCardsSet = _state.BothCardsSet;
            int attacker = bothCardsSet ? _state.CurrentAttacker : -1;
            bool activated = bothCardsSet && _state.CurrentShotEffectActivated;

            for (int player = 0; player < 2; player++)
            {
                string cardId = _state.Players[player].SetCardId;
                Card card = cardId != null ? CardCatalog.Get(cardId) : null;
                _hudPanel.UpdatePlayerInfo(player, _state.Players[player].Score, card, player == attacker, activated);
            }
        }

    }
}
