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
        // 的は貫通式（複数枚を1ショットで取得できる）。ショットごとにランダム配置する【暫定】。
        public const int Score500Value = 500;
        public const int Score300Value = 300;
        public const int Score100Value = 100;
        public const int Score500Count = 1;
        public const int Score300Count = 3;
        public const int Score100Count = 12;
        public const float Score500RadiusRatio = 0.07f; // フィールド幅比。最高得点は1個だけの目玉として最大サイズにする
        public const float Score300RadiusRatio = 0.05f; // フィールド幅比
        public const float Score100RadiusRatio = 0.03f; // フィールド幅比。最多数のぶん最小サイズにする

        // --- フィールドレイアウト (GAME_RULES.md 7章) ---
        // 【暫定】WallBandWidthの両端2列ずつ(計4列)はボールの実射程外で実質意味がないため、
        // プレイテストで削除(12→8列)。
        public const int WallGridColumns = 8;
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

        // --- CPU対戦 (GAME_RULES.md 9章, Phase 2) ---
        public const float CpuThinkDelaySeconds = 0.6f; // 【暫定】人間から見て「考えている」体感を出すための最小待機
        public const float CpuShotAimSigmaWeakRadians = 0.35f; // 【暫定】弱CPUの狙い角度ノイズ(標準偏差)
        public const float CpuShotAimSigmaStrongRadians = 0.12f; // 【暫定】強CPUの狙い角度ノイズ(標準偏差)
        public const float CpuShotPowerMean = 0.85f; // 【暫定】CPUの狙いパワーの平均値(0..1)
        public const float CpuShotPowerSigma = 0.08f; // 【暫定】CPUの狙いパワーのノイズ(標準偏差)
        public const float CpuStrongCardBiasWeight = 0.6f; // 【暫定】強CPUが有利属性カードを選ぶ確率の重み付け
        public const float CpuWallSpendBehindThreshold = 0.5f; // 【暫定】この割合以上ラウンドが残っていれば負けていても温存
        public const float CpuCenterAimBiasRadians = 0.3f; // 【暫定】強CPUが端の発射ポジションから撃つときの中央寄せ補正角(最大値)
    }
}
