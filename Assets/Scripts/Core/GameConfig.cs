namespace LinkShot.Core
{
    /// <summary>
    /// GAME_RULES.md / CARDS.md に記載された【暫定】数値を含む、全ゲーム定数の一元管理場所。
    /// マジックナンバーはここにのみ置き、他のCore/コードから参照する。
    /// </summary>
    public static class GameConfig
    {
        // --- 試合構成 (GAME_RULES.md 1章) ---
        public const int RoundCount = 5;
        public const int ShotsPerRound = 2;
        public const int TotalShots = RoundCount * ShotsPerRound;

        // --- 手持ちリソース (GAME_RULES.md 2章) ---
        public const int CardHandSize = 5;
        public const int DisposableWallCardCount = 5;
        public const int DefaultWallCountPerShot = 1;

        // --- カードプール・デッキ構築 (CARDS.md 2章, 5.2章) ---
        public const int CardPoolSize = 15;
        public const int DeckSize = 5;
        public const int LegendaryDeckLimit = 1;
        public const int RareDeckLimit = 2;
        public const int CommonDeckLimit = int.MaxValue; // 制限なし

        // --- 発射ポジション (GAME_RULES.md 6章) ---
        public const int LaunchPositionCount = 6;
        public const float LaunchCircleRadiusRatio = 0.06f; // フィールド幅比
        public const float RangeBoostRadiusMultiplier = 1.5f; // 【暫定】

        // --- 得点システム (GAME_RULES.md 5章) ---
        public const int CornerZoneScore = 500;
        public const int CenterZoneScore = 200;
        public const int BonusZoneScore = 100; // 追加した10個の的（見た目のにぎやかしを兼ねる）
        public const float CornerZoneRadiusRatio = 0.05f; // フィールド幅比
        public const float CenterZoneRadiusRatio = 0.08f; // フィールド幅比
        public const float BonusZoneRadiusRatio = 0.03f; // フィールド幅比
        public const int BonusZoneCount = 10;

        // --- フィールドレイアウト (GAME_RULES.md 7章) ---
        public const int WallGridColumns = 12; // 壁配置エリアの拡張(FieldView.WallBandWidth)に合わせ、置ける位置を増やす【暫定】
        public const int WallGridRows = 2;
        public const int WallGridCellCount = WallGridColumns * WallGridRows;

        // --- 制限時間 (GAME_RULES.md 3章, 5.2章) ---
        public const float ShotTimeLimitSeconds = 30f; // 【暫定】
        public const float BallFlightTimeoutSeconds = 5f; // 【暫定】

        // --- 物理 (ARCHITECTURE.md 2.2章) ---
        public const float WallRestitution = 1.0f; // 【暫定】完全弾性
        public const float BounceBoardSizeRatio = 0.16f; // フィールド幅比（WallGridColumnsからは独立した見た目サイズ）

        // --- カード効果パラメータ (CARDS.md 3章, 4章) ---
        public const float WideGateHitboxMultiplier = 1.3f; // 【暫定】
        public const float PowerShotVelocityMultiplier = 1.5f; // 【暫定】
        public const float MiniBallSizeMultiplier = 0.7f; // 【暫定】
        public const int SafetyNetScore = 100; // 【暫定】
        public const int WallRemoveCount = 1;
        public const int WallShiftDistance = 1; // 隣接マスへの移動
        public const int DoubleShotAttempts = 2;
    }
}
