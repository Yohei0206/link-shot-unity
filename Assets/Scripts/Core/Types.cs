namespace LinkShot.Core
{
    /// <summary>三すくみの属性。名称は仮（CLAUDE.md 用語集参照）。ALPHA→BETA→GAMMA→ALPHAの順に勝つ。</summary>
    public enum Element
    {
        ALPHA,
        BETA,
        GAMMA,
    }

    /// <summary>メダルのレアリティ（MEDALS.md 2章）。</summary>
    public enum Rarity
    {
        Common,
        Rare,
        Legendary,
    }

    /// <summary>メダル効果の識別子（GAME_RULES.md 4.3章 / MEDALS.md 3-4章、計15種）。</summary>
    public enum EffectId
    {
        // ★★★ レジェンド（属性固定）
        DoubleShot,
        PositionChoice,
        WallReturn,

        // ★★ / ★ 普通効果（属性自由）
        WallRemove,
        BounceBoard,
        RangeBoost,
        WallShift,
        GhostBall,
        WideGate,
        Reroll,
        CurveShot,
        PowerShot,
        ScoreDouble,
        SafetyNet,
        MiniBall,
    }

    /// <summary>1ラウンド内のフェーズ（GAME_RULES.md 3章）。</summary>
    public enum Phase
    {
        MedalSet,
        WallPlacement,
        PositionRoll,
        EffectResolve,
        Shot,
        ScoreResolve,
        MatchEnd,
    }

    /// <summary>的（ターゲットゾーン）の識別子（GAME_RULES.md 5.1章）。BonusはGAME_RULES.md未記載の追加的（得点は控えめ）。</summary>
    public enum TargetZoneId
    {
        TopLeftCorner,
        TopRightCorner,
        Center,
        Bonus,
    }

    /// <summary>1ショットの最終的な着弾結果の種別（GAME_RULES.md 5.2章）。</summary>
    public enum ShotOutcomeKind
    {
        TargetHit,
        WallHit,
        OutOfField,
        Timeout,
    }

    /// <summary>UnityEngine非依存の軽量2次元ベクトル（正規化フィールド座標系で使用）。</summary>
    public readonly struct Vec2
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>壁グリッド上の1配置（GAME_RULES.md 7章: 横5列×縦2行）。</summary>
    public readonly struct WallPlacement
    {
        public readonly int CellIndex; // 0..GameConfig.WallGridCellCount-1
        public readonly bool IsDefaultWall;

        public WallPlacement(int cellIndex, bool isDefaultWall)
        {
            CellIndex = cellIndex;
            IsDefaultWall = isDefaultWall;
        }
    }

    /// <summary>バウンド板の設置（自由配置。GAME_RULES.md 7章）。</summary>
    public readonly struct BouncePlacement
    {
        public readonly Vec2 Position;

        public BouncePlacement(Vec2 position)
        {
            Position = position;
        }
    }

    /// <summary>Game/層のショット物理・入力パラメータへ渡す変更差分（乗算合成が前提）。</summary>
    public struct ShotModifier
    {
        public float LaunchRadiusMultiplier;
        public float VelocityMultiplier;
        public float BallSizeMultiplier;
        public bool PassThroughFirstWall; // GHOST_BALL
        public bool AllowMidFlightCurve; // CURVE_SHOT
        public int ShotAttempts; // DOUBLE_SHOT時は2、それ以外は1

        public static ShotModifier Default => new ShotModifier
        {
            LaunchRadiusMultiplier = 1f,
            VelocityMultiplier = 1f,
            BallSizeMultiplier = 1f,
            PassThroughFirstWall = false,
            AllowMidFlightCurve = false,
            ShotAttempts = 1,
        };
    }

    /// <summary>効果解決フェーズで必要になる選択パラメータ（対象が要らない効果はデフォルトのまま無視される）。</summary>
    public struct EffectChoice
    {
        public int? WallTargetCellIndex; // WALL_REMOVE, WALL_SHIFT(移動元)
        public int? WallDestinationCellIndex; // WALL_SHIFT(移動先)
        public Vec2? BouncePosition; // BOUNCE_BOARD
        public TargetZoneId? WideGateZone; // WIDE_GATE
        public int? StolenWallCardOwnerPlayer; // WALL_RETURN: どのプレイヤーの使用済みカードを奪うか
    }

    /// <summary>1ショットの記録（履歴ログ・UI表示用）。</summary>
    public class ShotRecord
    {
        public int Round;
        public int ShotIndex; // 0=先攻の攻撃, 1=後攻の攻撃
        public int Attacker;
        public int Defender;
        public string AttackerMedalId;
        public string DefenderMedalId;
        public bool EffectActivated;
        public ShotOutcomeKind Outcome;
        public int Score;
    }
}
