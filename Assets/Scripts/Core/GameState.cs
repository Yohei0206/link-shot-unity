using System.Collections.Generic;
using System.Linq;

namespace LinkShot.Core
{
    /// <summary>1プレイヤーの手持ちリソースと状態（GAME_RULES.md 2章）。</summary>
    public sealed class PlayerState
    {
        public readonly int PlayerIndex;
        public readonly List<string> Hand = new List<string>();
        public readonly HashSet<string> UsedCardIds = new HashSet<string>();
        public int DisposableWallCardsRemaining = GameConfig.DisposableWallCardCount;

        /// <summary>このラウンドで防御側として使い捨て壁カードを何枚使ったか（WALL_RETURN判定用、ラウンド開始時に0リセット）。</summary>
        public int DisposableWallCardsUsedThisRound;

        /// <summary>このラウンドで伏せてセットしたカードID（ラウンド中は不変）。</summary>
        public string SetCardId;

        public int Score;

        public PlayerState(int playerIndex, IEnumerable<string> deckCardIds)
        {
            PlayerIndex = playerIndex;
            Hand.AddRange(deckCardIds);
        }
    }

    /// <summary>フィールドの可変状態。壁・バウンド板・発射ポジションはショットごとにリセットされる。</summary>
    public sealed class FieldState
    {
        public readonly List<WallPlacement> DefenderWalls = new List<WallPlacement>();
        public readonly List<BouncePlacement> BounceBoards = new List<BouncePlacement>();
        public int LaunchPosition; // 1..6。0は未決定
        public TargetZoneId? WideGateZone;

        /// <summary>
        /// 最高得点の的(Score500、通称「星」)が置かれている壁グリッドの列(0..WallGridColumns-1)。
        /// Game層(FieldView)がショットごとの的配置から算出してセットする。Core自体は座標を持たない。
        /// 壁配置フェーズの開始時(RebuildTargets直後)にセットされ、そのショットの間は保持される
        /// (Reset()では消さない。壁配置とショットのAI判断の両方が同じ値を参照するため)。
        /// </summary>
        public int? StarWallColumn;

        /// <summary>星に最も近い発射ポジション(1..LaunchPositionCount)。StarWallColumnと同様、Game層が算出する。</summary>
        public int? StarNearestLaunchPosition;

        public void Reset()
        {
            DefenderWalls.Clear();
            BounceBoards.Clear();
            LaunchPosition = 0;
            WideGateZone = null;
        }
    }

    /// <summary>試合全体の状態（ARCHITECTURE.md 2.3章）。MonoBehaviourを持ち込まない純粋C#。</summary>
    public sealed class GameState
    {
        public int Round = 1; // 1..RoundCount
        public int ShotIndex; // 0=先攻の攻撃, 1=後攻の攻撃
        public Phase Phase = Phase.CardSet;
        public readonly PlayerState[] Players = new PlayerState[2];
        public readonly int FirstAttackerPlayer; // 試合を通して固定される先攻P
        public readonly FieldState Field = new FieldState();
        public readonly List<ShotRecord> History = new List<ShotRecord>();

        // --- 発射ポジション決定フェーズの一時状態 ---
        public int? PendingDiceRoll;
        public bool RerollAvailable;
        public bool RerollUsed;

        // --- ショットフェーズの一時状態 ---
        public ShotModifier CurrentShotModifier = ShotModifier.Default;
        public int ShotAttemptsRemaining;
        public readonly List<int> ShotAttemptScores = new List<int>();
        public readonly List<ShotOutcomeKind> ShotAttemptOutcomes = new List<ShotOutcomeKind>();

        public readonly Rng Rng;

        public GameState(IReadOnlyList<string> deckPlayer0, IReadOnlyList<string> deckPlayer1, int firstAttackerPlayer = 0, Rng rng = null)
        {
            Players[0] = new PlayerState(0, deckPlayer0);
            Players[1] = new PlayerState(1, deckPlayer1);
            FirstAttackerPlayer = firstAttackerPlayer;
            Rng = rng ?? new Rng();
        }

        public int CurrentAttacker => ShotIndex == 0 ? FirstAttackerPlayer : 1 - FirstAttackerPlayer;
        public int CurrentDefender => 1 - CurrentAttacker;

        public Card AttackerCard => CardCatalog.Get(Players[CurrentAttacker].SetCardId);
        public Card DefenderCard => CardCatalog.Get(Players[CurrentDefender].SetCardId);

        /// <summary>
        /// 攻撃側のカード効果が発動するか（GAME_RULES.md 4.2章）。
        /// ルールエンジンは非公開情報の制約を受けないため、両者のカードが確定した時点でいつでも判定できる。
        /// </summary>
        public bool CurrentShotEffectActivated => Elements.AttackerEffectActivates(AttackerCard.Element, DefenderCard.Element);

        public bool BothCardsSet => Players[0].SetCardId != null && Players[1].SetCardId != null;

        /// <summary>試合の勝者（0/1）。同点は引き分けでnull（GAME_RULES.md 1章）。Phase.MatchEnd到達後に参照する想定。</summary>
        public int? Winner
        {
            get
            {
                if (Players[0].Score == Players[1].Score)
                {
                    return null;
                }

                return Players[0].Score > Players[1].Score ? 0 : 1;
            }
        }
    }
}
