# Kowloon Break 開発仕様 概要サマリ（クリーン版）

本ドキュメントは `kowloon_break_dev_spec.md` を基に、判読できる内容をUTF-8で再構成したサマリです。原本に文字化けがあるため、正確性が必要な箇所は「要確認」としています。

## プロジェクト概要
- タイトル: クーロン・ブレイク（Kowloon Break）
- ジャンル: シネマティック・サバイバル・ストラテジーRPG
- エンジン: Unity 2022.3 LTS（URP）
- 対応プラットフォーム: PC(Steam) / PlayStation / Nintendo Switch
- 開発期間: 3年、チーム規模: 約10名、対象年齢: 14歳以上〜30代
- 方針（2025更新）: シンプル・イズ・ベスト（自動化・複雑機能の最小化、明確で読みやすいコード、必要最小限のログ）

## 技術基盤
- 言語/設計: C#、MVC/MVVM、FSM
- セーブ: JSONベース
- ローカライズ: 日英対応（他言語拡張は未定／要確認）
- 最小要件（目安）: DX11対応GPU、RAM 8GB以上、ストレージ15GB以上

## ゲーム進行フェーズ
1) SURVIVAL（生存）
   - ResourceManager（食料・水・薬・素材の管理）
   - BaseManager（拠点施設の建設・アップグレード）
   - ExplorationSystem（マップ探索・アイテム回収）
   - DialogueSystem（会話/交渉）
   - CraftingSystem（クラフト）
   - RelationshipSystem（仲間の信頼度）

2) DEFENSE（防衛）
   - WaveManager（襲撃イベント管理）
   - TowerDefenseController（リアルタイム戦闘制御）
   - InfectionSystem（感染拡大/治療）
   - UnitDeployment（仲間配置/戦術）
   - FortificationManager（防壁/トラップ管理）

3) ESCAPE（脱出）
   - EscapeRouteManager（脱出ルート管理）
   - FinalBattleController（最終戦/ボス戦）
   - EndingBranchManager（マルチエンディング分岐）
   - AllianceSystem（他勢力との協力/敵対）

## キャラクター/仲間
- TrustLevel（0-100）、役割（戦闘/探索/治療/建設/交渉）
- Stats, Skill, Health, Infection を保持

## 拠点/施設
- 代表施設: 宿舎/作業場/見張り台/菜園/医務室/武器庫 ほか
- 各施設は `Type, Level, RequiredResources, ProvidedEffects` を持つ

## 感染システム
- 都市全体の感染率、キャラクター別リスク、アウトブレイクイベント
- 時間経過/行動/環境で拡散、治療/隔離/予防で抑制

## レベルデザイン/マップ
- 多層階層（住宅/商業/工業/地下/屋上/廃棄）
- 危険度、部屋/エリア、階層間接続、利用可能資源

## UI/UX
- MainHUD, Inventory, Companion, BaseManagement, Map, Dialogue, Crafting, Tactical
- サイバーパンク風（ネオン配色、グリッチ/スキャンライン、混在フォント）

## オーディオ
- BGM: エレクトロニック × オーケストラ
- 環境音: 都市雑音/機械音/水滴/風音
- ボイス: 重要シーンのみ対応（要確認）
- API例: `PlayBGM`, `PlaySFX`, `PlayVoice`, `SetAmbientLoop`

## セーブデータ（例）
```json
{
  "gameVersion": "1.0.0",
  "playTime": 7200,
  "currentPhase": "DEFENSE",
  "currentDay": 45,
  "playerProgress": {
    "completedQuests": [],
    "unlockedAreas": []
  }
}
```

## メモ/要確認
- コンソール版の優先度・同発/後発、FPS/解像度目標
- カメラ視点/操作系/アクセシビリティ
- DEFENSEの戦闘テンポ（RT、ポーズ、スロー）とユニット上限
- セーブスロット/オートセーブ/互換性ポリシー
- Addressables/DI/シーン構成/ビルド環境
- TRC/レーティング想定（CERO/ESRB）
- 原本テキストのエンコード正常化（UTF-8）

