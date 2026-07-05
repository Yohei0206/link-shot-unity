namespace LinkShot.Core.Effects
{
    /// <summary>
    /// メダル効果のstrategyインターフェース（ARCHITECTURE.md 2.4章 / MEDALS.md 6章）。
    /// PhaseMachineは、GameState.CurrentShotEffectActivated が true の場合のみ各フックを呼び出す。
    /// 発動しない（打ち消された）場合は一切のフックが呼ばれない。
    /// </summary>
    public interface IMedalEffect
    {
        EffectId Id { get; }

        /// <summary>true の場合、発射ポジション決定フェーズでサイコロの代わりに攻撃側の自由選択を要求する（POSITION_CHOICE専用）。</summary>
        bool ReplacesPositionRoll { get; }

        /// <summary>true の場合、発射ポジション決定フェーズでサイコロの目を見た後に1回だけ振り直しを許可する（REROLL専用）。</summary>
        bool AllowsReroll { get; }

        /// <summary>効果解決フェーズ（(4)(9)）。壁除去・バウンド板設置・壁移動・的拡大等、フィールド状態を変更する効果はここで処理する。</summary>
        void OnResolve(GameState state, EffectChoice choice);

        /// <summary>ショットフェーズの物理/入力パラメータを変更する（乗算合成の起点となるbaseを受け取り、変更後を返す）。</summary>
        ShotModifier ModifyShot(ShotModifier baseModifier);

        /// <summary>得点解決フェーズ。得点計算後の値を受け取り、最終値を返す（SCORE_DOUBLE・SAFETY_NET等）。</summary>
        int ModifyScore(GameState state, ShotOutcomeKind outcome, int baseScore);

        /// <summary>得点解決フェーズの後に呼ばれる（WALL_RETURN専用）。</summary>
        void OnAfterScore(GameState state, int attacker, int defender);
    }

    /// <summary>各フックのデフォルト（何もしない）実装を提供する基底クラス。効果クラスは必要なフックだけをoverrideする。</summary>
    public abstract class MedalEffectBase : IMedalEffect
    {
        public abstract EffectId Id { get; }
        public virtual bool ReplacesPositionRoll => false;
        public virtual bool AllowsReroll => false;
        public virtual void OnResolve(GameState state, EffectChoice choice) { }
        public virtual ShotModifier ModifyShot(ShotModifier baseModifier) => baseModifier;
        public virtual int ModifyScore(GameState state, ShotOutcomeKind outcome, int baseScore) => baseScore;
        public virtual void OnAfterScore(GameState state, int attacker, int defender) { }
    }
}
