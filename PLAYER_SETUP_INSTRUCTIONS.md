# Player Setup Instructions

## MainシーンでのPlayerオブジェクト設定手順

### 1. PlayerSetupコンポーネントの追加
1. MainシーンでGameObjectを作成（名前: "PlayerSetupManager"）
2. 作成したGameObjectに`PlayerSetup`コンポーネントを追加
3. Inspectorで設定を確認・調整

### 2. 設定可能な項目

#### Player Configuration
- **Player Name**: プレイヤーオブジェクトの名前（デフォルト: "Player"）
- **Spawn Position**: 開始位置
- **Spawn Rotation**: 開始時の回転

#### Controller Settings
- **Walk Speed**: 歩行速度（デフォルト: 3f）
- **Run Speed**: 走行速度（デフォルト: 6f）
- **Crouch Speed**: しゃがみ速度（デフォルト: 1.5f）
- **Max Stamina**: 最大スタミナ（デフォルト: 100f）
- **Stamina Regen Rate**: スタミナ回復速度（デフォルト: 20f）
- **Run Stamina Cost**: 走行時のスタミナ消費（デフォルト: 30f）

#### Camera Settings
- **Camera Offset**: カメラの相対位置（デフォルト: (0, 8, -10)）
- **Camera Rotation X**: カメラの固定X回転角度（デフォルト: 30度）
- **Follow Damping**: カメラの追従スムージング（デフォルト: 1f）

#### Interaction Settings
- **Interaction Range**: インタラクション可能距離（デフォルト: 3f）
- **Interaction Key**: インタラクションキー（デフォルト: E）
- **Flashlight Key**: 懐中電灯キー（デフォルト: F）

### 3. 自動セットアップの実行

#### 方法1: 自動実行
- `Auto Setup On Start`をチェック（デフォルト: ON）
- Playボタンを押すと自動的にセットアップが実行されます

#### 方法2: 手動実行
1. PlayerSetupコンポーネントを右クリック
2. "Setup Player"を選択
3. または、Inspectorの"Setup Player"ボタンをクリック

### 4. 自動生成される内容

#### Player GameObject
- CharacterController（半径0.5f、高さ2f）
- AudioSource（3D音響設定）
- EnhancedPlayerController（プレイヤー制御）
- PlayerVisuals（青いカプセル形状）

#### Camera System
- CameraFollowTarget（カメラ追従用）
- CinemachineSetup（Cinemachine Virtual Camera自動設定）

### 5. Cinemachineパッケージのインストール

#### 必要な場合
1. Window → Package Manager
2. "Cinemachine"を検索
3. Install をクリック

#### インストール後
- スクリプトが自動的にCinemachine Virtual Cameraを設定
- 固定角度でプレイヤーを追従するカメラが作成されます

### 6. 操作方法

#### 移動
- **WASD**: 移動
- **Left Shift**: 走行
- **Left Control**: しゃがみ

#### インタラクション
- **E**: インタラクション/探索
- **F**: 懐中電灯オン/オフ

#### カメラ
- 固定角度でプレイヤーを追従
- マウスでの回転は無効

### 7. トラブルシューティング

#### エラーが発生した場合
1. Consoleウィンドウでエラーを確認
2. 必要なパッケージがインストールされているか確認
3. PlayerSetupコンポーネントの"Remove Player"で一度削除してから再実行

#### カメラが機能しない場合
1. Main Cameraが存在することを確認
2. Cinemachineパッケージがインストールされているか確認
3. フォールバック用のSimpleCameraFollowが動作しているか確認

### 8. カスタマイズ

#### 設定値の調整
- PlayerSetupコンポーネントのInspectorで各種設定を調整可能
- 設定後に"Setup Player"を再実行して反映

#### 見た目の変更
- PlayerVisuals子オブジェクトを独自のモデルに置き換え可能
- Materialを変更して色やテクスチャを調整可能

### 9. 完了確認

セットアップが成功すると以下が確認できます：
- Consoleに"Player setup completed!"が表示
- Hierarchyに"Player"オブジェクトが作成
- Play時にWASDで移動、カメラが追従することを確認