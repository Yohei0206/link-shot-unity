# ARCHITECTURE.md — 技術アーキテクチャ設計

## 1. 技術スタック

| 領域 | 採用技術 | 備考 |
| :---- | :---- | :---- |
| エンジン | **Unity（LTS版・2D テンプレート）** | WebGLビルドで unityroom に公開する |
| 言語 | C# | |
| 物理演算 | Unity 2D Physics（Box2Dベース: Rigidbody2D / Collider2D） | ボールの飛翔・壁/バウンド板との反射のみに使用 |
| UI | uGUI（Canvas） | カード選択・壁配置UI・スコア表示・ログ |
| テスト | Unity Test Framework（NUnit） | `Core/` のルールエンジンを対象（EditModeテスト） |
| 公開先 | **unityroom**（Unity WebGL） | |
| バックエンド | Supabase REST API（Phase 3のみ・UnityWebRequest経由） | 非同期対戦のデータ保存 |

### unityroom公開に関する制約・注意

- ビルドターゲットは WebGL。**圧縮形式は unityroom の推奨設定（Gzip）に合わせる**こと
- WebGLではマルチスレッドや一部.NET APIが制限される。`System.Threading` に依存しない実装とする
- モバイルブラウザからのプレイも想定し、タッチ入力（`Input.GetTouch` / 新InputSystemのTouch）とマウス入力の両対応にする
- 初回ロード軽量化のため、不要なパッケージ・アセットは削除し、テクスチャ圧縮を適切に設定する（unityroomプレイヤーはある程度のロードを許容するが、軽いに越したことはない）

## 2. 設計原則

### 2.1 ルールエンジンと描画の分離（最重要）

```
Assets/
├── Scripts/
│   ├── Core/           ← 純粋C#。UnityEngineに依存しない（usingしない）
│   │   ├── GameConfig.cs      ← 全ゲーム数値の定数（得点、枚数、半径、暫定値すべて）
│   │   ├── Types.cs           ← Card, Element, WallCard, TargetZone等の型定義
│   │   ├── GameState.cs       ← 試合全体の状態
│   │   ├── PhaseMachine.cs    ← フェーズ進行のステートマシン
│   │   ├── Elements.cs        ← 属性三すくみの判定ロジック
│   │   ├── Effects/           ← カード効果（strategyパターンで1効果1ファイル）
│   │   │   ├── ICardEffect.cs
│   │   │   ├── WallRemoveEffect.cs
│   │   │   ├── ...（CARDS.md記載の15種）
│   │   ├── CardCatalog.cs    ← 15枚のカードプール定義（データ駆動）
│   │   ├── Scoring.cs         ← 得点計算
│   │   └── Rng.cs             ← 乱数（サイコロ）。seed注入可能にしてテスト可能に
│   │
│   ├── Game/           ← Unity層。Core/ の状態を購読して描画する
│   │   ├── FieldView.cs       ← フィールド・壁・的・発射円の描画
│   │   ├── BallController.cs  ← ボールの物理（Rigidbody2D）と接触検出
│   │   ├── SlingshotInput.cs  ← スリングショット入力（ドラッグ→初速ベクトル）
│   │   └── EffectVisuals.cs   ← カード効果の演出反映（壁除去・バウンド板生成等）
│   │
│   ├── UI/             ← uGUI層
│   │   ├── CardSelectPanel.cs
│   │   ├── WallPlacementPanel.cs
│   │   ├── ScoreBoard.cs
│   │   ├── HistoryLog.cs
│   │   └── HandoverScreen.cs  ← ローカル対戦のデバイス受け渡し画面
│   │
│   ├── AI/             ← Phase 2。CPU思考ルーチン（Core/のみに依存）
│   │
│   └── Net/            ← Phase 3。Supabase REST クライアント（UnityWebRequest）
│
├── Scenes/
│   ├── Title.unity
│   ├── DeckSelect.unity
│   ├── Match.unity
│   └── Result.unity
├── Prefabs/            ← 壁・ボール・的・バウンド板・カードカードUI
└── Tests/
    └── EditMode/       ← Core/ のユニットテスト（NUnit）
```

**依存方向は一方向のみ**: `Game/`, `UI/`, `AI/`, `Net/` → `Core/`。逆は禁止。
`Core/` は `using UnityEngine;` を書かない（Vector2が必要な場合は自前の軽量構造体 or System.Numericsを使用）。

この分離の理由:
1. ルールエンジンをEditModeテストで高速にユニットテストできる（WebGLビルド不要）
2. CPU戦（AI/）はCore/だけ相手にすれば書ける
3. Phase 3でサーバー側検証が必要になった場合、Core/を.NETでそのまま動かせる

### 2.2 物理演算の扱い

- 物理演算の結果（ボールが最初に触れた対象: 的/壁/バウンド板/場外）は `Game/BallController.cs` が `OnCollisionEnter2D` / `OnTriggerEnter2D` で検出し、**イベントとしてCore/に通知する**。Core/は「最初の接触対象」だけを受け取って得点計算する
- 壁は `static` なCollider2D（Rigidbody2Dなし）。壊れない仕様（GAME_RULES.md 5.3）とそのまま対応する
- 的はTrigger Collider。ボールが触れた瞬間に得点確定＆ボール停止
- 反発係数はPhysicsMaterial2Dで管理し、数値はGameConfigの値をロード時に反映する
- Unityの物理は決定論的でないが、クライアント判定＋結果送信方式のため問題ない。リプレイは初速ベクトル＋乱数seedからの**近似再現**でよい【暫定】

### 2.3 ステートマシン

`Core/PhaseMachine.cs` は GAME_RULES.md 3章のフェーズ図をそのまま実装する。

```csharp
public enum Phase {
    CardSet,        // 準備フェーズ
    WallPlacement,   // 壁選択フェーズ（shotIndexで1回目/2回目を区別）
    PositionRoll,    // 発射ポジション決定
    EffectResolve,   // カード効果解決
    Shot,            // ショットフェーズ
    ScoreResolve,    // 得点解決
    MatchEnd
}

public class GameState {
    public int Round;              // 1-5
    public int ShotIndex;          // 0=先攻の攻撃, 1=後攻の攻撃
    public Phase Phase;
    public PlayerState[] Players;  // [2]
    public int CurrentAttacker;    // 0 or 1
    public FieldState Field;       // 配置済みの壁・バウンド板・決定済み発射円
    public List<ShotRecord> History;
}
```

- 状態遷移は `Dispatch(action)` 形式で実装し、`(state, action) => state` を純粋に保ってテスト可能にする
- MonoBehaviourはCore/に持ち込まない。Match.unityシーンに1つの `MatchDirector : MonoBehaviour` を置き、Core/のステートマシンを駆動して各Viewに反映する（Humble Objectパターン）

### 2.4 カード効果の抽象化

```csharp
public interface ICardEffect {
    EffectId Id { get; }
    // 発動タイミングごとのフック（該当しないものは何もしない）
    void OnResolve(GameState state);      // 効果解決フェーズ（壁除去・バウンド板等）
    ShotModifier OnShotSetup();           // ショット中の物理/入力パラメータ変更
    int OnScoring(int baseScore);         // 得点解決フェーズ（2倍・保険等）
}
```

新しい効果の追加は `Core/Effects/` にクラスを1つ足して `CardCatalog` に登録するだけで済む構造にする。

## 3. Claude Code + Unity MCP での開発

本プロジェクトは **Unity MCP（CoplayDev/unity-mcp）** を使い、Claude CodeからUnityエディタを直接操作して開発する。

### 3.1 セットアップ（人間側の初回作業）

1. Unityプロジェクト作成（LTS版・2Dテンプレート）
2. Package Manager → Add package from git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`（バージョンは安定版タグにピン留めする）
3. 前提ランタイム: Python 3.10+ / uv
4. `Window > MCP for Unity` → Start Server → Client Configuration で「Claude Code」を選択して Configure（Claude Desktopと間違えないこと）
5. Unityプロジェクトのルートで `claude` を起動し、`/mcp` で unity-mcp の接続を確認

**毎回の起動順序**: Unity Editor起動 → MCPサーバー起動 → Claude Code起動。順序が逆だと接続に失敗する。

### 3.2 Claude CodeがMCP経由で行うこと

- シーン・GameObject・プレハブの作成と配置（Title / DeckSelect / Match / Result）
- C#スクリプトの作成・編集と、コンパイルエラーの確認・自己修正（`read_console`）
- コンポーネントのアタッチとプロパティ設定（Rigidbody2D、Collider2D、PhysicsMaterial2D等）
- EditModeユニットテストの実行（`Core/` のルールエンジン検証）
- Play Modeでの動作確認

### 3.3 人間がエディタ/ブラウザで行うこと

- Unity MCPサーバーの起動と接続承認
- WebGLビルドの実行と確認（ビルドはMCPから可能な場合もあるが、最終確認は人間が行う）
- unityroomへのアップロードと公開設定
- プレイテストと数値フィードバック

### 3.4 設計上の指針（MCPがあっても維持すること）

- **シーンへの手置きは最小限にし、フィールド要素（壁・的・発射円・バウンド板）は実行時生成を基本とする。** MCPでシーンを組めるからといってシーンにデータを埋め込むと、GameConfigとの二重管理になり数値調整が壊れるため
- シーンに置くのはブートストラップ（`MatchDirector`）、Canvas、カメラ程度に留める
- MCP操作で作成したアセット・シーンの状態は、作業後にClaude Code自身がgitコミットして差分を追跡可能にする

## 4. データモデル（Phase 3: Supabase）

方針のみ（Phase 3着手時に詳細化）:

- WebGLからはWebSocketの安定性に注意が必要なため、**Realtime購読ではなくポーリング方式を第一候補**とする【暫定】
- UnityWebRequestでSupabase REST API（PostgREST）を叩く。公式SDKには依存しない
- テーブル案: `matches` / `match_actions`（カードセット・壁配置・ショット結果の逐次ログ）
- 認証は匿名キー＋行レベルセキュリティから始める

## 5. 画面構成

| シーン | 内容 |
| :---- | :---- |
| Title | モード選択（ローカル対戦 / CPU戦 / オンライン） |
| DeckSelect | 15枚から5枚を選ぶ（ローカル対戦では交互に選択） |
| Match | フィールド＋uGUIオーバーレイ（スコア/ラウンド/カード履歴/フェーズ案内） |
| Result | 最終スコアと勝敗、ショット履歴の振り返り |

- 横持ち16:9基準のレイアウト（unityroomはPCブラウザ中心のため。Canvas Scalerでスマホ等の他アスペクト比にも対応する）
- ローカル対戦の「伏せてセット」はデバイス受け渡し画面（HandoverScreen）で秘匿する

## 6. パフォーマンス・ビルド要件

- WebGLビルドサイズ: 可能な限り削減する（Strip Engine Code有効、未使用パッケージ削除、テクスチャはスプライトアトラス化）
- 物理演算はショット中のみ必要。待機中は `Physics2D.simulationMode` を Script に切り替えるか、ボールを非アクティブにして負荷ゼロを維持する
- unityroomのアップロード上限サイズに収まることをPhase 1完了条件に含める

### 6.1 WebGL Player Settings（確定値）

unityroomのヘルプ（https://help.unityroom.com/unityroom-351dc3ed5de980eebd79eef3b153be31 ）に記載の推奨設定に合わせて確定。今後のビルドもこの設定を維持する。

| 設定項目 | 値 | 備考 |
| :---- | :---- | :---- |
| Development Build | オフ | |
| Compression Format | **Gzip** | 既定のBrotliではなくunityroom推奨のGzipを使用（`PlayerSettings.WebGL.compressionFormat`） |
| Decompression Fallback | オフ | |
| Scenes In Build | `Assets/Scenes/Match.unity` のみ | 2DテンプレートのデフォルトSampleSceneは含めない |

ビルドサイズの実績: 約16MB（Gzip、フォント埋め込み込み）。

### 6.2 日本語フォント

- uGUIの`Text`は動的OSフォント（`Arial.ttf`/`LegacyRuntime.ttf`）ではWebGLで描画されない（OSフォントレンダリングに依存するため）上、日本語グリフも持たない
- `Assets/Resources/Fonts/NotoSansJP-Subset.ttf` に、Noto Sans JP（SIL Open Font License）からゲーム内で実際に使用する文字だけを抽出したサブセットを同梱し、`UITheme.DefaultFont` から読み込む
- 新しい日本語テキストをコードに追加した場合、そのグリフがサブセットに含まれていないと文字化け（tofu）するため、フォントの再生成が必要になる場合がある
