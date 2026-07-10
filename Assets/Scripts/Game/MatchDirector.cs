using System;
using System.Collections;
using System.Collections.Generic;
using LinkShot.AI;
using LinkShot.Core;
using LinkShot.Core.Effects;
using LinkShot.Network;
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
        private OnlineRoomPanel _onlineRoomPanel;
        private DeckSelectPanel _deckSelectPanel;
        private CardSelectPanel _cardSelectPanel;
        private WallPlacementPanel _wallPlacementPanel;
        private PositionRollPanel _positionRollPanel;
        private EffectChoicePanel _effectChoicePanel;
        private HudPanel _hudPanel;
        private ResultPanel _resultPanel;

        // --- Phase 2: CPU対戦 ---
        private int? _cpuPlayerIndex; // null = CPUなし。0/1ならそのプレイヤー枠がCPU。
        private CpuDifficulty _cpuDifficulty;
        private readonly Rng _cpuRng = new Rng();

        // 的の配置(FieldView.RebuildTargets)専用のシード。オンライン対戦では両クライアントで
        // 一致させ、同じshotで同じ的レイアウトになるようにする(Core.Rngとは別物。詳細はRebuildTargets参照)。
        private int _fieldRngSeed;

        private bool IsCpu(int player) => _cpuPlayerIndex == player;

        // --- Phase 3: オンライン同期対戦 ---
        private OnlineMatchService _onlineService;
        private bool _isOnlineMode;
        private int? _remotePlayerIndex; // null = オンラインでない。0/1ならそのプレイヤー枠が相手(リモート)。

        private bool IsRemote(int player) => _remotePlayerIndex == player;

        /// <summary>確定したactionを、オンライン対戦中だけmatch_actionsへ非同期でpushする(結果を待たない)。</summary>
        private void SyncIfOnline(GameAction action)
        {
            if (_isOnlineMode)
            {
                StartCoroutine(PushActionFireAndForget(action));
            }
        }

        private IEnumerator PushActionFireAndForget(GameAction action)
        {
            yield return _onlineService.PushAction(action, (ok, err) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[Online] アクション送信失敗: {err}");
                }
            });
        }

        /// <summary>相手(リモート)からの次のアクションが届くまでポーリングし続け、届いたらonReceivedへ渡す。</summary>
        private IEnumerator WaitForRemoteAction(Action<GameAction> onReceived)
        {
            while (true)
            {
                MatchActionRow row = null;
                bool ok = false;

                yield return _onlineService.FetchNextAction((success, result, err) =>
                {
                    ok = success;
                    row = result;
                    if (!success)
                    {
                        Debug.LogError($"[Online] ポーリング失敗: {err}");
                    }
                });

                if (ok && row != null)
                {
                    onReceived(NetworkActionCodec.DecodeFromPayload(row.payload));
                    yield break;
                }

                yield return new WaitForSeconds(1.2f);
            }
        }

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

        /// <summary>対戦モード選択(ROADMAP.md Phase 2/3)。</summary>
        private IEnumerator RunModeSelect()
        {
            _cpuPlayerIndex = null;
            _isOnlineMode = false;
            _remotePlayerIndex = null;
            _onlineService = null;

            bool chosen = false;
            bool onlineChosen = false;

            _modeSelectPanel.Show(
                (cpuPlayerIndex, difficulty) =>
                {
                    _cpuPlayerIndex = cpuPlayerIndex;
                    _cpuDifficulty = difficulty;
                    chosen = true;
                },
                () =>
                {
                    onlineChosen = true;
                    chosen = true;
                });

            yield return new WaitUntil(() => chosen);

            if (onlineChosen)
            {
                yield return RunOnlineRoomSetup();
            }
        }

        /// <summary>オンライン対戦: サインイン→ルーム作成/参加→相手接続待ちまでを行う(ROADMAP.md Phase 3)。</summary>
        private IEnumerator RunOnlineRoomSetup()
        {
            _isOnlineMode = true;

            var config = Resources.Load<SupabaseConfig>("Network/SupabaseConfig");
            if (config == null)
            {
                _onlineRoomPanel.ShowFatalError("設定が見つかりません(Resources/Network/SupabaseConfig)");
                yield break;
            }

            _onlineService = new OnlineMatchService(config);

            bool signInOk = false;
            string signInError = null;
            yield return _onlineService.SignIn((ok, err) =>
            {
                signInOk = ok;
                signInError = err;
            });

            if (!signInOk)
            {
                _onlineRoomPanel.ShowFatalError($"サインインに失敗しました: {signInError}");
                yield break;
            }

            bool ready = false;
            _onlineRoomPanel.Show(
                () => StartCoroutine(HandleCreateRoomClicked(() => ready = true)),
                code => StartCoroutine(HandleJoinRoomClicked(code, () => ready = true)));

            yield return new WaitUntil(() => ready);

            _remotePlayerIndex = 1 - _onlineService.LocalPlayerIndex;
            _onlineRoomPanel.Hide();
        }

        private IEnumerator HandleCreateRoomClicked(Action onReady)
        {
            bool created = false;
            string roomCode = null;

            yield return _onlineService.CreateRoom((ok, code, err) =>
            {
                created = ok;
                roomCode = code;
                if (!ok)
                {
                    _onlineRoomPanel.ShowRetryableError($"部屋の作成に失敗しました: {err}");
                }
            });

            if (!created)
            {
                yield break;
            }

            _onlineRoomPanel.ShowWaitingForOpponent(roomCode);

            bool opponentOk = false;
            string opponentError = null;
            yield return _onlineService.WaitForOpponent((ok, err) =>
            {
                opponentOk = ok;
                opponentError = err;
            });

            if (!opponentOk)
            {
                _onlineRoomPanel.ShowRetryableError($"エラー: {opponentError}");
                yield break;
            }

            onReady?.Invoke();
        }

        private IEnumerator HandleJoinRoomClicked(string roomCode, Action onReady)
        {
            bool joined = false;

            yield return _onlineService.JoinRoom(roomCode, (ok, err) =>
            {
                joined = ok;
                if (!ok)
                {
                    _onlineRoomPanel.ShowRetryableError($"参加に失敗しました: {err}");
                }
            });

            if (joined)
            {
                onReady?.Invoke();
            }
        }

        /// <summary>対戦開始前に、両プレイヤーがデッキ（15種から{GameConfig.DeckSize}枚）を選ぶ。</summary>
        private IEnumerator RunPreMatch()
        {
            var decks = new List<string>[2];

            // カード選択と同じ理由(相手の手を見ずに決める)で、デッキ選択もオンラインでは
            // 両プレイヤー同時でよい。ローカル対戦は1台を交代して使うため順番のまま。
            if (_isOnlineMode)
            {
                bool[] done = { false, false };
                for (int player = 0; player < 2; player++)
                {
                    int currentPlayer = player;
                    StartCoroutine(RunDeckSelectForPlayer(currentPlayer, decks, () => done[currentPlayer] = true));
                }

                yield return new WaitUntil(() => done[0] && done[1]);
            }
            else
            {
                for (int player = 0; player < 2; player++)
                {
                    yield return RunDeckSelectForPlayer(player, decks, null);
                }
            }

            if (!_isOnlineMode)
            {
                bool concealed = false;
                _handoverScreen.Show("デッキが揃いました\nタップして対戦開始", () => concealed = true);
                yield return new WaitUntil(() => concealed);
            }

            _state = new GameState(decks[0], decks[1]);
            _fieldRngSeed = _isOnlineMode ? _onlineService.RngSeed : new System.Random().Next();
            _hudPanel.Show();
        }

        /// <summary>1プレイヤー分のデッキ選択。onDoneはオンライン並列実行時のみ使う(nullなら呼ばない)。</summary>
        private IEnumerator RunDeckSelectForPlayer(int currentPlayer, List<string>[] decks, Action onDone)
        {
            if (IsCpu(currentPlayer))
            {
                yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                decks[currentPlayer] = CpuDeckBuilder.BuildDeck(_cpuRng);
                onDone?.Invoke();
                yield break;
            }

            if (IsRemote(currentPlayer))
            {
                List<string> remoteDeck = null;
                yield return WaitForRemoteDeckSelection(deck => remoteDeck = deck);
                decks[currentPlayer] = remoteDeck;
                onDone?.Invoke();
                yield break;
            }

            if (!_isOnlineMode)
            {
                bool handoverDone = false;
                _handoverScreen.Show($"プレイヤー{currentPlayer + 1}に交代してください\nタップしてデッキを選んでください", () => handoverDone = true);
                yield return new WaitUntil(() => handoverDone);
            }

            List<string> chosenDeck = null;
            _deckSelectPanel.Show(currentPlayer, deck => chosenDeck = new List<string>(deck));
            yield return new WaitUntil(() => chosenDeck != null);

            decks[currentPlayer] = chosenDeck;

            if (_isOnlineMode)
            {
                yield return _onlineService.PushDeckSelection(chosenDeck, (ok, err) =>
                {
                    if (!ok)
                    {
                        Debug.LogError($"[Online] デッキ送信失敗: {err}");
                    }
                });
            }

            onDone?.Invoke();
        }

        private IEnumerator WaitForRemoteDeckSelection(Action<List<string>> onReceived)
        {
            while (true)
            {
                MatchActionRow row = null;
                bool ok = false;

                yield return _onlineService.FetchNextAction((success, result, err) =>
                {
                    ok = success;
                    row = result;
                    if (!success)
                    {
                        Debug.LogError($"[Online] ポーリング失敗: {err}");
                    }
                });

                if (ok && row != null)
                {
                    onReceived(new List<string>(row.payload.deckCardIds));
                    yield break;
                }

                yield return new WaitForSeconds(1.2f);
            }
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
            _onlineRoomPanel = CreateFullScreenPanel<OnlineRoomPanel>(canvas.transform, "OnlineRoomPanel");
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
            // オンライン対戦では、カード選択は相手の手を見ずに決める同時公開制なので、
            // 片方が終わるまで待たず両プレイヤーが同時に選べてよい(ローカル対戦は1台を
            // 交代して使うため、これまで通り順番に行う)。
            if (_isOnlineMode)
            {
                bool[] done = { false, false };
                for (int player = 0; player < 2; player++)
                {
                    int currentPlayer = player;
                    StartCoroutine(RunCardSetForPlayer(currentPlayer, () => done[currentPlayer] = true));
                }

                yield return new WaitUntil(() => done[0] && done[1]);
            }
            else
            {
                for (int player = 0; player < 2; player++)
                {
                    yield return RunCardSetForPlayer(player, null);
                }
            }

            if (!_isOnlineMode)
            {
                bool concealed = false;
                _handoverScreen.Show("カードが揃いました\nタップして壁配置に進みます", () => concealed = true);
                yield return new WaitUntil(() => concealed);
            }
        }

        /// <summary>1プレイヤー分のカード選択。onDoneはオンライン並列実行時のみ使う(nullなら呼ばない)。</summary>
        private IEnumerator RunCardSetForPlayer(int currentPlayer, Action onDone)
        {
            if (IsCpu(currentPlayer))
            {
                yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                int opponent = 1 - currentPlayer;
                string cardId = CpuCardSelector.ChooseCard(_state.Players[currentPlayer], _state.Players[opponent], _cpuDifficulty, _cpuRng);
                PhaseMachine.Dispatch(_state, new SetCardAction(currentPlayer, cardId));
                onDone?.Invoke();
                yield break;
            }

            if (IsRemote(currentPlayer))
            {
                yield return WaitForRemoteAction(action => PhaseMachine.Dispatch(_state, action));
                onDone?.Invoke();
                yield break;
            }

            if (!_isOnlineMode)
            {
                bool handoverDone = false;
                _handoverScreen.Show($"プレイヤー{currentPlayer + 1}に交代してください\nタップしてカードを選んでください", () => handoverDone = true);
                yield return new WaitUntil(() => handoverDone);
            }

            bool selected = false;
            _cardSelectPanel.Show(currentPlayer, _state.Players[currentPlayer].Hand, cardId =>
            {
                var action = new SetCardAction(currentPlayer, cardId);
                PhaseMachine.Dispatch(_state, action);
                SyncIfOnline(action);
                selected = true;
            });
            yield return new WaitUntil(() => selected);
            _cardSelectPanel.Hide();
            onDone?.Invoke();
        }

        private IEnumerator RunWallPlacementPhase()
        {
            int defender = _state.CurrentDefender;

            // 的は貫通式でショットごとにランダム配置し直す（先攻/後攻それぞれ、GAME_RULES.md 5.1章）。
            // シードはRound/ShotIndexと組み合わせる: オンラインでは両クライアントが同じ値を計算するため、
            // 的のレイアウトが両者で一致する。
            int shotSeed = _fieldRngSeed ^ (_state.Round * 397 + _state.ShotIndex);
            Vector2? starWorldPosition = _fieldView.RebuildTargets(shotSeed);
            _state.Field.StarWallColumn = starWorldPosition.HasValue ? FieldView.GetWallColumnForWorldX(starWorldPosition.Value.x) : (int?)null;
            _state.Field.StarNearestLaunchPosition = starWorldPosition.HasValue ? FieldView.GetNearestLaunchPositionForWorldX(starWorldPosition.Value.x) : (int?)null;
            _fieldView.HighlightLaunchPosition(null);

            if (IsCpu(defender))
            {
                yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                (int defaultCell, List<int> disposableCells) = CpuWallPlanner.PlanWalls(_state, defender, _cpuDifficulty, _cpuRng);
                PhaseMachine.Dispatch(_state, new PlaceWallsAction(defaultCell, disposableCells));
                _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                yield break;
            }

            if (IsRemote(defender))
            {
                yield return WaitForRemoteAction(action =>
                {
                    PhaseMachine.Dispatch(_state, action);
                    _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                });
                yield break;
            }

            bool confirmed = false;
            _wallPlacementPanel.Show(defender, _state.Players[defender].DisposableWallCardsRemaining, (defaultCell, disposableCells) =>
            {
                var action = new PlaceWallsAction(defaultCell, disposableCells);
                PhaseMachine.Dispatch(_state, action);
                _fieldView.ApplyWalls(_state.Field.DefenderWalls);
                SyncIfOnline(action);
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
            int attacker = _state.CurrentAttacker;

            if (IsRemote(attacker))
            {
                // 発射ポジション決定(サイコロ・振り直し等)は全て攻撃側(相手)のRngで行われるため、
                // こちらでは再現しようとせず、確定した最終ポジションだけを受け取って反映する。
                yield return WaitForRemotePositionFinalized();
                _fieldView.HighlightLaunchPosition(_state.Field.LaunchPosition);
                Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 発射ポジション: {_state.Field.LaunchPosition}");
                yield break;
            }

            ICardEffect effect = _state.CurrentShotEffectActivated ? EffectRegistry.Get(_state.AttackerCard.Effect) : null;
            bool attackerIsCpu = IsCpu(attacker);

            if (effect != null && effect.ReplacesPositionRoll)
            {
                if (attackerIsCpu)
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    int position = CpuPositionChooser.ChoosePosition(_state, _cpuDifficulty, _cpuRng);
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
                        bool wantsReroll = CpuPositionChooser.ChooseReroll(_state, rolled, _cpuDifficulty, _cpuRng);
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

            // オンライン対戦では、サイコロ/振り直しの過程そのものは同期せず、
            // 攻撃側で最終確定したLaunchPositionの値だけを相手へ送る(乱数のロックステップを避けるため)。
            if (_isOnlineMode)
            {
                var payload = new NetworkActionCodec.Payload { actionType = "PositionFinalized", position = _state.Field.LaunchPosition };
                StartCoroutine(PushRawFireAndForget("PositionFinalized", JsonUtility.ToJson(payload)));
            }

            _fieldView.HighlightLaunchPosition(_state.Field.LaunchPosition);
            Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 発射ポジション: {_state.Field.LaunchPosition}");
        }

        private IEnumerator PushRawFireAndForget(string actionType, string payloadJson)
        {
            yield return _onlineService.PushRaw(actionType, payloadJson, (ok, err) =>
            {
                if (!ok)
                {
                    Debug.LogError($"[Online] {actionType}送信失敗: {err}");
                }
            });
        }

        /// <summary>相手(攻撃側)が確定した発射ポジションを受け取り、Coreの状態へ直接反映する(乱数は再現しない)。</summary>
        private IEnumerator WaitForRemotePositionFinalized()
        {
            while (true)
            {
                MatchActionRow row = null;
                bool ok = false;

                yield return _onlineService.FetchNextAction((success, result, err) =>
                {
                    ok = success;
                    row = result;
                    if (!success)
                    {
                        Debug.LogError($"[Online] ポーリング失敗: {err}");
                    }
                });

                if (ok && row != null)
                {
                    int position = row.payload.position;
                    _state.Field.LaunchPosition = position;
                    _state.PendingDiceRoll = null;
                    _state.RerollAvailable = false;
                    _state.Phase = Phase.EffectResolve;
                    yield break;
                }

                yield return new WaitForSeconds(1.2f);
            }
        }

        /// <summary>
        /// (4)(9) カード効果解決。対象選択が必要な効果（WALL_REMOVE/WALL_SHIFT/BOUNCE_BOARD/WIDE_GATE）は
        /// 攻撃側にUIで選ばせてからResolveEffectActionをdispatchする。それ以外はChoice無しで即座に解決する。
        /// </summary>
        private IEnumerator RunEffectResolvePhase()
        {
            EffectChoice choice = default;
            int attacker = _state.CurrentAttacker;
            bool receivedFromRemote = false;

            if (_state.CurrentShotEffectActivated)
            {
                EffectId effectId = _state.AttackerCard.Effect;
                Debug.Log($"[Round {_state.Round} Shot {_state.ShotIndex}] 攻撃効果発動: {effectId}");

                if (IsRemote(attacker) && NeedsEffectTarget(effectId))
                {
                    yield return WaitForRemoteAction(action => choice = ((ResolveEffectAction)action).Choice);
                    receivedFromRemote = true;
                }
                else if (IsCpu(attacker) && NeedsEffectTarget(effectId))
                {
                    yield return new WaitForSeconds(GameConfig.CpuThinkDelaySeconds);
                    choice = CpuEffectChoiceSelector.Choose(_state, effectId, _cpuDifficulty, _cpuRng);
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

            var resolveAction = new ResolveEffectAction(choice);
            PhaseMachine.Dispatch(_state, resolveAction);
            if (!receivedFromRemote)
            {
                SyncIfOnline(resolveAction);
            }

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
                if (IsRemote(_state.CurrentAttacker))
                {
                    yield return RunRemoteAttempt();
                }
                else
                {
                    yield return RunSingleAttempt(origin);
                }
            }
        }

        /// <summary>
        /// 相手(攻撃側)のショットを待つ。物理演算のリプレイ再生は未実装で、
        /// 結果(SubmitShotResultAction)が届くまで待機表示するだけ(ROADMAP.md Phase 3の未完了項目)。
        /// </summary>
        private IEnumerator RunRemoteAttempt()
        {
            _hudPanel.UpdateStatus("相手がショットを行っています…");
            yield return WaitForRemoteAction(action => PhaseMachine.Dispatch(_state, action));
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
                (float angleOffset, float power) = CpuShotAimPlanner.GetAim(_state, _cpuDifficulty, _cpuRng);
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
            var resultAction = new SubmitShotResultAction(outcome, hitZones);
            PhaseMachine.Dispatch(_state, resultAction);
            SyncIfOnline(resultAction);
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
