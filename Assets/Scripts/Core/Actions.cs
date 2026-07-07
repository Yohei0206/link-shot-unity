namespace LinkShot.Core
{
    /// <summary>PhaseMachine.Dispatch に渡すアクションの基底クラス（ARCHITECTURE.md 2.3章: Dispatch(action)形式）。</summary>
    public abstract class GameAction
    {
    }

    /// <summary>(1) 準備フェーズ: プレイヤーがカードを1枚伏せてセットする。</summary>
    public sealed class SetCardAction : GameAction
    {
        public readonly int Player;
        public readonly string CardId;

        public SetCardAction(int player, string cardId)
        {
            Player = player;
            CardId = cardId;
        }
    }

    /// <summary>(2)(7) 壁選択フェーズ: 防御側が常設壁1個＋使い捨て壁カード0枚以上を配置する。</summary>
    public sealed class PlaceWallsAction : GameAction
    {
        public readonly int DefaultWallCell;
        public readonly System.Collections.Generic.IReadOnlyList<int> DisposableWallCells;

        public PlaceWallsAction(int defaultWallCell, System.Collections.Generic.IReadOnlyList<int> disposableWallCells)
        {
            DefaultWallCell = defaultWallCell;
            DisposableWallCells = disposableWallCells;
        }
    }

    /// <summary>(3)(8) 発射ポジション決定: サイコロを振る。</summary>
    public sealed class RollPositionAction : GameAction
    {
    }

    /// <summary>(3)(8) 発射ポジション決定: POSITION_CHOICE発動時、サイコロの代わりに自由選択する。</summary>
    public sealed class ChoosePositionAction : GameAction
    {
        public readonly int Position;

        public ChoosePositionAction(int position)
        {
            Position = position;
        }
    }

    /// <summary>(3)(8) 発射ポジション決定: REROLL発動時、出目を見た後に1回だけ振り直す。</summary>
    public sealed class RerollAction : GameAction
    {
    }

    /// <summary>(3)(8) 発射ポジション決定: REROLL発動時、振り直さずに出目を確定する。</summary>
    public sealed class ConfirmPositionAction : GameAction
    {
    }

    /// <summary>(4)(9) カード効果解決: 属性判定の上、対象選択が必要な効果はchoiceで指定する。</summary>
    public sealed class ResolveEffectAction : GameAction
    {
        public readonly EffectChoice Choice;

        public ResolveEffectAction(EffectChoice choice)
        {
            Choice = choice;
        }
    }

    /// <summary>
    /// (5)(10) ショットフェーズ: Game/層から物理判定結果を1回分report する。
    /// 的は貫通するため、着弾までにボールが通過した的をHitZonesに複数含められる（0個も可）。
    /// </summary>
    public sealed class SubmitShotResultAction : GameAction
    {
        public readonly ShotOutcomeKind Outcome;
        public readonly System.Collections.Generic.IReadOnlyList<TargetZoneId> HitZones;

        public SubmitShotResultAction(ShotOutcomeKind outcome, System.Collections.Generic.IReadOnlyList<TargetZoneId> hitZones = null)
        {
            Outcome = outcome;
            HitZones = hitZones ?? System.Array.Empty<TargetZoneId>();
        }
    }

    /// <summary>得点解決フェーズの表示が終わったら次へ進める（次ショット／次ラウンド／試合終了）。</summary>
    public sealed class ProceedAction : GameAction
    {
    }
}
