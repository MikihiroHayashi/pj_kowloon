Kowloon Break — AGENTS.md

目的
- このドキュメントは pj_kowloon（Unity 2022.3 LTS, URP）の実装構造・作法・変更手順をエージェント向けに要約したものです。ここに記載の方針を最優先で遵守してください。

環境・起動
- Unity: 2022.3 LTS（URP）
- プロジェクト: LocalData/Project/pj_kowloon
- 推奨実行: Test 用シーンでプレイモード検証
  - `Assets/Scripts/Setup/TestSceneSetup.cs` をシーンに配置し、`autoSetupOnStart` を有効にすると自動セットアップ（Player/Light/Camera/Managers/テストオブジェクト）が行われます。
- セーブ: `Application.persistentDataPath/save_<slot>.json` に JSON 保存（`Managers/GameManager.cs` 内 `GameSaveData`）

ディレクトリ指針（主）
- `Assets/Scripts/Core`: データモデル・型（`ItemData` ScriptableObject, `InventorySlot`, `Resource`, 各種 enum など）
- `Assets/Scripts/Managers`: シングルトン管理（`GameManager`, `PhaseManager`, `EnhancedResourceManager`, `ConsumableManager` など）
- `Assets/Scripts/Systems`: フェーズ別/機能別システム（`PhaseSystem` 抽象, `SurvivalSystem`, `DefenseSystem`, `EscapeSystem` 等）
- `Assets/Scripts/Environment`: ダンジョン/スポーン/破壊物などの環境系
- `Assets/Scripts/Characters`, `Assets/Scripts/Player`: 挙動・アニメーション関連
- `Assets/Scripts/UI`: `UIManager` と各パネル/UI要素
- `Assets/Scripts/Setup`: テスト/起動補助（`TestSceneSetup` など）

アーキテクチャ要点
- フェーズ管理（FSM）
  - `Core/GamePhase`: `SURVIVAL -> DEFENSE -> ESCAPE`
  - `Managers/GameManager`: ゲーム時間/日付/フェーズ遷移、セーブ/ロード、入力ハンドラの初期化。イベント: `OnPhaseChanged`, `OnDayChanged` ほか。
  - `Managers/PhaseManager`: 現在フェーズ時間の計測・既定時間での遷移、`PhaseData` 設定（デフォルト構成を内包）。
  - `Systems/PhaseSystem`: 各システムの基底。`ActivateSystem/DeactivateSystem` と `HandlePhaseChanged` を実装し、`GameManager.OnPhaseChanged` を購読。
- リソース/インベントリ
  - `Managers/EnhancedResourceManager`: リソース辞書と劣化タイマ、所持品（`InventorySlot[] tool/material`）と `ItemData` データベース。イベントを介した変更通知を提供。
  - `Core/ItemData`（ScriptableObject）, `InventorySlot`, `Resource`: スタック・耐久・消費・品質等の基本ロジック。
- UI
  - `UI/UIManager`: HUD/各パネル/通知/ダメージ表示/会話表示/仲間UI/ターゲット選択等のハブ。`GameManager` や `EnhancedResourceManager` と連携。
- 入力
  - `Core/InputManager`, `Core/GameplayInputHandler`（`GameManager.Start` で登録/切替）。

コーディング規約（このリポでの事実ベース）
- 名前空間: `KowloonBreak.<Area>` を使用（例: `KowloonBreak.Managers`, `KowloonBreak.Systems`）。
- 命名: クラス/メソッドは PascalCase、フィールド/変数は camelCase、定数は UPPER_CASE を踏襲。
- シングルトン Manager は `DontDestroyOnLoad` を適用し、必要に応じて `FindObjectOfType` で依存解決。
- イベント駆動: Manager 側がイベントを公開し、UI/Systems が購読。
- ログは最小限・可読性重視。複雑なエディタ拡張や自動化は避け、手動で明快に（仕様のシンプル方針に一致）。
- データ保存は JSON を維持（バイナリ化/暗号化を導入する場合は要合意）。

変更時のガイドライン
- フェーズ連動の新システム実装
 1) `Systems` にクラスを追加し `PhaseSystem` を継承。
 2) `HandlePhaseChanged` で対象フェーズのみ `ActivateSystem()`、それ以外で `DeactivateSystem()` を行う。
 3) 初期化/後処理は `OnSystemActivatedInternal/OnSystemDeactivatedInternal` で実装。
- 新アイテム/消費アイテム
 1) `Core/ItemData` の ScriptableObject を作成（アイコン/種別/耐久/効果など）。
 2) 所持/消費は `EnhancedResourceManager`（インベントリAPI）または `ConsumableManager` 経由を使用。
- リソース種/在庫UIの追加
 1) `Core/ResourceType` へ enum 追加。
 2) `EnhancedResourceManager` の初期化（`EnhancedResourceData[]`）に反映。
 3) 必要なら `UIManager` の表示とイベント購読を追加。
- UI 拡張
 1) UI Prefab/Panel を作成し `UIManager` に参照を追加。
 2) `InitializeUI/SubscribeToEvents/UpdateUI` へ最小限の追記。
- 保存データの拡張
 1) `GameSaveData` にフィールド追加。
 2) `CreateGameSaveData/LoadGameSaveData` にシリアライズ/デシリアライズ処理を追加（後方互換に留意）。

テスト/実行のヒント
- 最小動作確認は `TestSceneSetup` を使って 1 シーンで完結させる。
- システムごとの動作は `PhaseManager` のフェーズ遷移や `GameManager.ChangePhase` で強制遷移可能。
- インベントリ/消費動作は `EnhancedResourceManager` のパブリック API を直接呼び出して確認。

避けるべきこと
- 重いカスタムエディタ/自動設定の追加（仕様方針で非推奨）。
- 外部依存の追加（必要時は事前合意）。
- 既存命名/名前空間/シングルトン方針の逸脱。

よくある参照先（開始点）
- フェーズ: `Assets/Scripts/Core/GamePhase.cs`, `Assets/Scripts/Managers/GameManager.cs`, `Assets/Scripts/Managers/PhaseManager.cs`, `Assets/Scripts/Systems/SurvivalSystem.cs`
- リソース/所持品: `Assets/Scripts/Managers/EnhancedResourceManager.cs`, `Assets/Scripts/Core/ItemData.cs`, `Assets/Scripts/Core/InventorySlot.cs`, `Assets/Scripts/Core/Resource.cs`
- UI: `Assets/Scripts/UI/UIManager.cs`
- テスト: `Assets/Scripts/Setup/TestSceneSetup.cs`

PR/変更レビュー（エージェント用）
- 変更は最小で局所的に。周辺のスタイル・イベント駆動を踏襲。
- 影響範囲（フェーズ遷移、UIイベント、セーブ互換）を要確認。
- 大規模変更のみユーザー承認を必須とする。基本的な変更、確認に承認は必要なし。

