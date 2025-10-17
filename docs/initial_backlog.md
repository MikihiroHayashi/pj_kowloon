# Kowloon Break 初期バックログ（MVP向け）

MVP目標: SURVIVALフェーズの最小プレイループ（探索→資源取得→拠点利用→保存）。

## 0. プロジェクト基盤
- Unity 2022.3 LTS/URP プロジェクト作成、テンプレート設定
- アセンブリ定義/名前空間規約/ディレクトリ構造の策定
- 共通ユーティリティ（時間、乱数、イベント、ログ）

## 1. 進行管理（FSM）
- GameState: Boot → MainMenu → InGame(SURVIVAL) → Pause → Save/Load
- フェーズ間IF: SURVIVAL→DEFENSE を見据えた最小の遷移口

## 2. データ基盤
- 設定ScriptableObject群（定数、初期値、難易度）
- セーブ/ロード（JSON、バージョン識別、最低限のマイグレーション）
- アドレス指定アセットの方針（Addressables採用可否の決定）

## 3. コアシステム（MVP）
- ResourceManager（所持/消費/上限/経過消費）
- ExplorationSystem（シンプルなマップ/ノード移動/拾得）
- Inventory（スタック、重量/枠はMVPでは簡易）
- BaseManager（休息/軽微なバフ付与の施設を1つ）

## 4. UI（MVP）
- MainHUD（HP/スタミナ/時間/資源）
- InventoryPanel（一覧/使用/破棄）
- MapPanel（簡易ノードマップ）
- セーブ/ロードUI

## 5. テスト/ツール
- プレイモード/エディタテストの導入方針
- 設定/データ検証（バリデータ）

## 6. ビルド/運用
- バージョン命名規則、ビルド番号、自動書き換え
- PC向けデバッグビルド手順

## デリバラブル（MVP完了の定義）
- 起動→メインメニュー→新規ゲーム→SURVIVAL簡易探索→資源取得→拠点で使用→セーブ→終了
- 重大クラッシュがない、セーブ/ロードが往復可能、最低限のUIが操作可能

