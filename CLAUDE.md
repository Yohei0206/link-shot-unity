# CLAUDE.md — Link-Shot（仮）開発ガイド

このリポジトリは、サッカーのフリーキックをモチーフにした1対1の交代制対戦ブラウザゲーム『Link-Shot（仮）』の開発プロジェクトである。

## ドキュメント構成

実装前に必ず以下を読むこと。

| ファイル | 内容 |
| :---- | :---- |
| `docs/GAME_RULES.md` | ゲームルールの完全仕様。フェーズ進行・メダル・壁・得点の全ルールと暫定数値 |
| `docs/ARCHITECTURE.md` | 技術スタック・ディレクトリ構成・モジュール分割・データモデル |
| `docs/ROADMAP.md` | 実装フェーズの分割と各フェーズの完了条件 |

## プロジェクトの核となる設計思想

**「戦略パートが状況を作り、アクションパートが決める」**

- 戦略パート＝メダルの読み合い・壁のリソース配分（ターン制のルールロジック）
- アクションパート＝スリングショットの操作精度（物理演算）
- この2つは疎結合に実装する。ルールロジックは物理エンジンに依存させないこと（テスト容易性とサーバー移植性のため）

## 技術スタック（概要）

- **Unity（LTS版・2Dテンプレート）+ C#**、WebGLビルドで **unityroom** に公開する
- **物理演算**: Unity 2D Physics（Rigidbody2D / Collider2D）
- **UI**: メダル選択・壁配置などはuGUI（Canvas）
- **状態管理**: ルールロジックは純粋C#のステートマシンとして実装（UnityEngine非依存）
- **バックエンド（Phase 3以降）**: Supabase REST API（UnityWebRequest経由・非同期対戦）

詳細は `docs/ARCHITECTURE.md` を参照。

## 実装の優先順位

1. **Phase 1（MVP）**: ルールエンジン＋1台ローカル対戦（人間 vs 人間、同一デバイス交互操作）
2. **Phase 2**: CPU対戦（AIの実装）
3. **Phase 3**: オンライン非同期対戦（Supabase連携）

現在のフェーズは `docs/ROADMAP.md` を参照。**Phase 1完了前にPhase 2以降のコードを書かないこと。**

## コーディング規約

- ルールロジック（`Assets/Scripts/Core/`）は `using UnityEngine;` を書かない（純粋C#として実装）
- ゲームルールの数値（得点・枚数・円のサイズ等）はすべて `Core/GameConfig.cs` に定数として集約する。マジックナンバーをコードに散らばらせない
- `GAME_RULES.md` に「暫定」と記された数値はプレイテストで変更される前提。GameConfig経由で一元管理すること
- ルールエンジンには必ずユニットテストを書く（Unity Test Framework / EditMode）。物理演算部分はテスト対象外でよい
- 開発は **Unity MCP（CoplayDev/unity-mcp）** を接続した状態で行う。シーン・GameObject操作、コンパイルエラー確認（read_console）、テスト実行はMCPツール経由で自律的に行うこと（セットアップと運用ルールは `docs/ARCHITECTURE.md` 3章）
- MCPでシーンを組める場合でも、フィールド要素（壁・的・発射円）は実行時生成を基本とし、数値はGameConfigに集約する（シーンへのデータ埋め込み禁止）

## 用語集（コード内の命名に使用）

| 日本語 | 英語（コード内） |
| :---- | :---- |
| メダル | `Medal` |
| 属性 | `Element`（3種: 仮に `ALPHA` / `BETA` / `GAMMA`。名称確定後にリネーム） |
| 壁カード | `WallCard` |
| 常設壁 | `defaultWall` |
| 発射ポジション | `LaunchPosition`（1〜6） |
| 的（得点ゾーン） | `TargetZone` |
| ラウンド | `Round`（1〜5） |
| ショット | `Shot`（各ラウンドに先攻・後攻の2回） |
| 先攻／後攻 | `first` / `second` |
| 攻撃側／防御側 | `attacker` / `defender` |
