# クーロン・ブレイク：Kowloon Break 開発仕様書

## プロジェクト概要

### 基本情報
- **タイトル**: クーロン・ブレイク：Kowloon Break
- **ジャンル**: シネマティック・サバイバルストラテジーRPG
- **エンジン**: Unity 2022.3 LTS (推奨)
- **プラットフォーム**: PC (Steam) / PlayStation / Nintendo Switch
- **開発期間**: 3年
- **チーム規模**: 10名
- **ターゲット**: 14歳以上〜30代のゲーマー層

### プロジェクトファイルパス
/mnt/d/UnityProject/pj_kowloon/pj_kowloon

### ゲームコンセプト
「閉鎖都市サバイバル」×「ギャング抗争」×「ゾンビ感染」×「脱出劇」

### 開発方針 (2025年更新)
**シンプル・イズ・ベスト**: 複雑な自動化システムや過度なデバッグ機能は避け、核となるゲームプレイに集中する
- 自動設定機能の最小化
- デバッグ機能の簡略化
- 手動セットアップによる開発者の理解促進
- 必要最小限のログ出力
- 明確で読みやすいコード構造を優先

## 技術仕様

### エンジン・環境
- **Unity バージョン**: 2022.3 LTS
- **レンダリングパイプライン**: Universal Render Pipeline (URP)
- **プラットフォーム**: Windows / macOS / Linux (Steam)
- **最小システム要件**: 
  - GPU: DirectX 11対応
  - RAM: 8GB以上
  - ストレージ: 15GB以上

### プログラミング要件
- **主要言語**: C#
- **アーキテクチャパターン**: MVC または MVVM
- **状態管理**: Finite State Machine (FSM)
- **セーブシステム**: JSON ベースのセーブデータ
- **ローカライゼーション**: 日本語・英語対応

## ゲームシステム設計

### 1. ゲーム進行フェーズシステム

#### Phase 1: SURVIVAL（生存フェーズ）
```csharp
// 主要システム
- ResourceManager: 食料・水・薬・素材の管理
- BaseManager: 拠点施設の建設・アップグレード
- ExplorationSystem: マップ探索・アイテム回収
- DialogueSystem: NPC交渉・選択肢システム
- CraftingSystem: アイテム作成システム
- RelationshipSystem: 仲間との信頼度管理
```

#### Phase 2: DEFENSE（防衛フェーズ）
```csharp
// 主要システム
- WaveManager: 襲撃イベントの管理
- TowerDefenseController: リアルタイム戦闘制御
- InfectionSystem: 感染拡大・治療システム
- UnitDeployment: 仲間配置・戦術システム
- FortificationManager: 防壁・トラップ管理
```

#### Phase 3: ESCAPE（脱出フェーズ）
```csharp
// 主要システム
- EscapeRouteManager: 脱出ルート管理
- FinalBattleController: ボス戦システム
- EndingBranchManager: マルチエンディング管理
- AllianceSystem: 他勢力との協力・敵対
```

### 2. キャラクターシステム

#### 仲間管理システム
```csharp
public class CompanionCharacter
{
    public string Name { get; set; }
    public int TrustLevel { get; set; }  // 信頼度 (0-100)
    public CharacterStats Stats { get; set; }
    public List<Skill> Skills { get; set; }
    public HealthStatus Health { get; set; }
    public InfectionStatus Infection { get; set; }
    public CharacterRole Role { get; set; }  // 戦闘・探索・交渉等
}

public enum CharacterRole
{
    Fighter,    // 戦闘特化
    Scout,      // 探索特化
    Medic,      // 治療特化
    Engineer,   // 建設・修理特化
    Negotiator  // 交渉特化
}
```

### 3. 拠点管理システム

#### 施設システム
```csharp
public class BaseFacility
{
    public FacilityType Type { get; set; }
    public int Level { get; set; }
    public List<Resource> RequiredResources { get; set; }
    public List<Effect> ProvidedEffects { get; set; }
}

public enum FacilityType
{
    Dormitory,      // 宿舎（回復・信頼度向上）
    Workshop,       // 作業場（クラフト）
    Watchtower,     // 見張り台（防衛力向上）
    Garden,         // 菜園（食料生産）
    Infirmary,      // 医務室（治療・感染対策）
    Arsenal         // 武器庫（武器・弾薬管理）
}
```

### 4. 感染システム

#### 感染メカニクス
```csharp
public class InfectionManager
{
    public float CityInfectionRate { get; set; }    // 都市全体の感染率
    public Dictionary<string, float> CharacterInfectionRisk { get; set; }
    public List<InfectionEvent> ActiveOutbreaks { get; set; }
    
    public void UpdateInfectionSpread(float deltaTime)
    {
        // 時間経過・行動・環境により感染率変動
        // 治療・隔離・予防により感染率低下
    }
}
```

## レベルデザイン・マップ構造

### 多層階層型マップシステム
```csharp
public class KowloonLevel
{
    public int FloorNumber { get; set; }      // 階層番号
    public LevelType Type { get; set; }       // 地区タイプ
    public List<Room> Rooms { get; set; }     // 部屋・エリア
    public List<Connection> Connections { get; set; }  // 他階層への接続
    public List<Resource> AvailableResources { get; set; }
    public float DangerLevel { get; set; }    // 危険度
}

public enum LevelType
{
    Residential,    // 居住区
    Commercial,     // 商業区
    Industrial,     // 工業区
    Underground,    // 地下区
    Rooftop,        // 屋上区
    Abandoned       // 廃墟区
}
```

### エリア設計
- **下層部**: スラム街・工場・下水道
- **中層部**: 商店街・住宅・オフィス
- **上層部**: 高級住宅・ペントハウス・屋上
- **特殊エリア**: 病院・学校・警察署・ギャング拠点

## UI/UXデザイン

### メインUI構成
```csharp
// 主要UIパネル
- MainHUD: HP・スタミナ・時間・感染率表示
- InventoryPanel: アイテム管理
- CompanionPanel: 仲間状態確認
- BaseManagementPanel: 拠点管理画面
- MapPanel: 探索マップ
- DialoguePanel: 会話・選択肢UI
- CraftingPanel: アイテム作成UI
- TacticalPanel: 防衛戦配置UI
```

### サイバーパンク風UIデザイン
- ネオンカラー（青・紫・オレンジ）を基調
- グリッチエフェクト・スキャンライン
- 日本語・英語・中国語フォントの混在表現
- ターミナル風インターフェース

## アート・ビジュアル仕様

### 3Dアートスタイル
- **テイスト**: セル調 + リアル系のハイブリッド
- **カラーパレット**: 暗めのトーン + ネオンアクセント
- **ライティング**: 動的ライティング・ボリューメトリックフォグ
- **ポストプロセス**: ブルーム・色収差・フィルムグレイン

### キャラクターデザイン
```csharp
// キャラクターモデリング仕様
- ポリゴン数: 8,000-15,000 tris (PC版)
- テクスチャ解像度: 2048x2048 (メイン) / 1024x1024 (サブ)
- リギング: Humanoid対応
- フェイシャルアニメーション: ブレンドシェイプベース
```

### 環境デザイン
- **建築スタイル**: 九龍城砦をモチーフとした密集建築
- **マテリアル**: 錆・コンクリート・ネオン・プラスチック
- **プロップ**: 看板・配管・電線・ゴミ・落書き

## オーディオ設計

### 音楽・音響方針
- **BGM**: エレクトロニック + オーケストラのハイブリッド
- **環境音**: 都市雑音・機械音・水滴・風音
- **キャラクターボイス**: 部分ボイス対応（重要シーンのみ）
- **効果音**: リアリスティック + サイバーパンク風加工

### サウンドシステム
```csharp
public class AudioManager
{
    public void PlayBGM(string trackName, float fadeTime = 2f)
    public void PlaySFX(string clipName, Vector3 position = default)
    public void PlayVoice(string voiceClip, float delay = 0f)
    public void SetAmbientLoop(string ambientName, float volume = 1f)
}
```

## データ管理・セーブシステム

### セーブデータ構造
```json
{
  "gameVersion": "1.0.0",
  "playTime": 7200,
  "currentPhase": "DEFENSE",
  "currentDay": 45,
  "playerProgress": {
    "completedQuests": [],
    "unlockedAreas": [],
    "discoveredItems": []
  },
  "baseStatus": {
    "facilities": [],
    "resources": {},
    "defenseLevel": 3
  },
  "companions": [
    {
      "id": "companion_01",
      "name": "ケン",
      "trustLevel": 75,
      "healthStatus": "healthy",
      "infectionStatus": "clean"
    }
  ],
  "cityStatus": {
    "infectionRate": 0.34,
    "activeThreats": [],
    "factionRelations": {}
  }
}
```

## パフォーマンス最適化

### 最適化方針
- **LOD (Level of Detail)**: 距離に応じたモデル品質調整
- **オクルージョンカリング**: 見えないオブジェクトの描画省略
- **バッチング**: 同一マテリアルのオブジェクト統合
- **テクスチャストリーミング**: 必要に応じたテクスチャ読み込み
- **オブジェクトプーリング**: 頻繁に生成されるオブジェクトの再利用

### ターゲットパフォーマンス
- **フレームレート**: 60 FPS (PC) / 30 FPS (Switch)
- **解像度**: 1920x1080 (PC) / 1280x720 (Switch)
- **メモリ使用量**: 4GB以下 (Switch対応)

## 開発フェーズ・マイルストーン

### Year 1: プロトタイプ・コアシステム
- **Month 1-3**: プロジェクト設計・技術検証
- **Month 4-6**: キャラクター・移動・基本UI実装
- **Month 7-9**: 拠点管理・探索システム実装
- **Month 10-12**: 第1フェーズ(SURVIVAL)プロトタイプ完成

### Year 2: ゲームプレイ・コンテンツ制作
- **Month 13-15**: 防衛システム・戦闘実装
- **Month 16-18**: ストーリー・ダイアログシステム
- **Month 19-21**: アート・アニメーション制作
- **Month 22-24**: 第2フェーズ(DEFENSE)実装完了

### Year 3: 統合・調整・リリース準備
- **Month 25-27**: 第3フェーズ(ESCAPE)実装
- **Month 28-30**: バランス調整・バグ修正
- **Month 31-33**: ローカライゼーション・最適化
- **Month 34-36**: Steam配信準備・マーケティング

## リスク管理

### 技術的リスク
- **C#プログラミング学習**: 外部エンジニア協力・教育計画
- **3D最適化**: 早期プロトタイプでパフォーマンス検証
- **複雑なゲームシステム**: 段階的実装・モジュール化

### 開発リスク
- **スコープクリープ**: 機能優先順位の明確化
- **アート制作量**: アセット共通化・プロシージャル生成活用
- **品質管理**: 定期的なプレイテスト・フィードバック収集

## Claude Code開発時の重要ポイント

### 設計原則
1. **モジュール性**: システムごとに独立したクラス設計
2. **拡張性**: 新機能追加が容易な構造
3. **保守性**: 可読性の高いコード・適切なコメント
4. **再利用性**: 共通処理の抽象化・継承活用
5. **シンプル性**: 複雑な自動化より手動の明確性を優先

### 実装方針 (2025年更新)
- **手動セットアップ**: 自動設定機能は最小限に抑制
- **デバッグ簡略化**: 必要最小限のログ出力のみ
- **エディター機能削減**: 複雑なカスタムエディターは避ける
- **段階的実装**: 一度に多機能を実装せず、シンプルから拡張
- **コア機能優先**: ゲームプレイに直結する機能を最優先

### 推奨ツール・アセット
- **Cinemachine**: カメラワーク制御
- **Timeline**: カットシーン制作
- **ProBuilder**: レベルプロトタイピング
- **DOTween**: アニメーション制御
- **TextMeshPro**: UI テキスト表示

### コードスタイル
```csharp
// 命名規則
public class PlayerController  // PascalCase (クラス・メソッド)
private float moveSpeed;       // camelCase (フィールド・変数)
public const int MAX_HEALTH = 100;  // UPPER_CASE (定数)

// 基本構造例
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private GamePhase currentPhase;
    
    private void Start() => InitializeGame();
    private void Update() => UpdateGameState();
    
    private void InitializeGame()
    {
        // 初期化処理
    }
}
```

## まとめ

『クーロン・ブレイク』は技術的挑戦とクリエイティブな表現が両立した意欲的なプロジェクトです。段階的な開発アプローチと適切なリスク管理により、魅力的なインディーゲームとしてSteamでの成功を目指しましょう。

**2025年更新 - シンプル実装方針**:
- 複雑な自動化システムを排除し、手動による明確な開発プロセスを採用
- コア機能（ゲームプレイ）に集中し、補助的なツールは最小限に抑制
- 段階的な実装により、確実に動作する基盤を構築してから拡張
- デバッグ機能は必要最小限に留め、パフォーマンスとコードの読みやすさを重視

Claude Codeでの開発では、この仕様書とシンプル化方針を基に具体的な実装を進め、プロトタイプから段階的に機能を拡張していくことを推奨します。