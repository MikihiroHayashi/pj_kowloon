using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    /// <summary>
    /// ゲームのUI全体を管理するマネージャークラス
    /// HUD表示、パネル管理、通知システムを担当
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject mainHUD;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject companionPanel;
        [SerializeField] private GameObject companionCommandPanel;
        [SerializeField] private GameObject baseManagementPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private GameObject tacticalPanel;
        [SerializeField] private GameObject pauseMenu;

        [Header("HUD Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Slider infectionSlider;
        [SerializeField] private Slider stealthSlider;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI timeText;

        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI foodText;
        [SerializeField] private TextMeshProUGUI waterText;
        [SerializeField] private TextMeshProUGUI medicineText;
        [SerializeField] private TextMeshProUGUI materialsText;

        [Header("Notification System")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private float notificationDuration = 3f;

        [Header("Damage Display")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private Transform damageContainer;
        
        [Header("Dialogue Display")]
        [SerializeField] private GameObject dialogueTextPrefab;
        [SerializeField] private float dialogueDuration = 2.5f;
        
        // Public access for DialogueTextPrefab
        public GameObject DialogueTextPrefab => dialogueTextPrefab;
        
        [Header("Companion Health Bar System")]
        [SerializeField] private GameObject companionHealthBarPrefab;

        // Public access for CompanionHealthBarPrefab
        public GameObject CompanionHealthBarPrefab => companionHealthBarPrefab;

        [Header("Enemy Health Bar System")]
        [SerializeField] private GameObject enemyHealthBarPrefab;

        // Public access for EnemyHealthBarPrefab
        public GameObject EnemyHealthBarPrefab => enemyHealthBarPrefab;

        [Header("Game Over System")]
        [SerializeField] private GameOverUI gameOverUI;

        [Header("Companion Command System")]
        [SerializeField] private GameObject commandButtonPrefab;
        [SerializeField] private Transform commandButtonContainer;
        [SerializeField] private TextMeshProUGUI companionNameText;
        [SerializeField] private TextMeshProUGUI companionStatusText;
        [SerializeField] private TextMeshProUGUI trustLevelText;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask companionLayerMask = -1;
        
        [Header("Command Text Settings")]
        [SerializeField] private string followCommandText = "付いてこい";
        [SerializeField] private string stayCommandText = "ここで待て";
        [SerializeField] private string attackCommandText = "攻撃しろ";
        [SerializeField] private string defendCommandText = "守備しろ";
        [SerializeField] private string supportCommandText = "援護しろ";
        [SerializeField] private string retreatCommandText = "撤退しろ";
        [SerializeField] private string advancedCommandText = "戦術行動";
        
        [Header("Notification Text Settings")]
        [SerializeField] private string commandExecutedPrefix = "命令実行: ";
        [SerializeField] private string commandFailedText = "命令を実行できませんでした";
        [SerializeField] private string noTargetFoundText = "攻撃対象が見つかりません";
        
        [Header("Debug - InteractionPrompt Positioning")]
        [SerializeField] private bool debugPromptPositioning = false;
        
        [Header("Debug - Companion Command System")]
        [SerializeField] private bool debugCompanionDetection = true;
        [SerializeField] private bool debugCommandExecution = true;
        [SerializeField] private bool debugPanelControls = false;

        private Dictionary<string, GameObject> activePanels;
        private List<GameObject> activeNotifications;
        private GameManager gameManager;
        private EnhancedResourceManager resourceManager;
        private InfectionManager infectionManager;
        private KowloonBreak.Player.EnhancedPlayerController enhancedPlayerController;
        
        // Companion Command System
        private KowloonBreak.Characters.CompanionAI currentNearbyCompanion;
        private KowloonBreak.Characters.CompanionAI selectedCompanion;
        private List<Button> activeCommandButtons = new List<Button>();
        private Transform player;
        private KowloonBreak.Core.InputManager inputManager;
        
        // Companion Health Bar System
        private Dictionary<KowloonBreak.Characters.CompanionAI, CompanionHealthBar> activeHealthBars = new Dictionary<KowloonBreak.Characters.CompanionAI, CompanionHealthBar>();

        // Enemy Health Bar System
        private Dictionary<KowloonBreak.Enemies.EnemyBase, EnemyHealthBar> activeEnemyHealthBars = new Dictionary<KowloonBreak.Enemies.EnemyBase, EnemyHealthBar>();
        
        // Dialogue Text Management System
        private Dictionary<KowloonBreak.Characters.CompanionAI, DialogueText> activeDialogueTexts = new Dictionary<KowloonBreak.Characters.CompanionAI, DialogueText>();
        private bool hasActiveDialogueForNearbyCompanion = false;
        
        // コマンド更新管理
        private bool lastCombatState = false;
        private float lastCommandUpdateTime = 0f;
        private const float COMMAND_UPDATE_INTERVAL = 0.5f; // 0.5秒間隔で更新チェック
        
        // InteractionPrompt 3D positioning
        private UnityEngine.Camera mainCamera;
        private RectTransform interactionPromptRectTransform;

        public bool IsAnyPanelOpen => activePanels.Count > 0;

        public event Action<string> OnPanelOpened;
        public event Action<string> OnPanelClosed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Managerオブジェクトをルートに移動してからDontDestroyOnLoadを適用
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            resourceManager = EnhancedResourceManager.Instance;
            infectionManager = InfectionManager.Instance;
            inputManager = KowloonBreak.Core.InputManager.Instance;
            
            enhancedPlayerController = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
            
            // Find player transform
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            
            // EventSystemの存在を確認・作成
            EnsureEventSystemExists();
            
            SubscribeToEvents();
            UpdateUI();
            InitializeCompanionCommandSystem();
            InitializeGameOverSystem();
        }

        private void Update()
        {
            UpdateHUD();
            HandleInput();
            UpdateCompanionCommandSystem();
        }

        private void InitializeUI()
        {
            activePanels = new Dictionary<string, GameObject>();
            activeNotifications = new List<GameObject>();
            
            CloseAllPanels();
            
            // CompanionCommandPanelを明示的に非表示にする
            if (companionCommandPanel != null)
            {
                companionCommandPanel.SetActive(false);
            }
            
            ShowMainHUD();
        }

        /// <summary>
        /// 各マネージャーのイベントに購読する
        /// </summary>
        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged += UpdatePhaseDisplay;
                gameManager.OnDayChanged += UpdateDayDisplay;
            }

            if (resourceManager != null)
            {
                resourceManager.OnResourceChanged += UpdateResourceDisplay;
            }

            if (infectionManager != null)
            {
                infectionManager.OnCityInfectionRateChanged += UpdateInfectionDisplay;
            }
            
            // プレイヤーのヘルス・スタミナ変更イベントに直接購読
            if (enhancedPlayerController != null)
            {
                enhancedPlayerController.OnHealthChanged += UpdateHealthBar;
                enhancedPlayerController.OnStaminaChanged += UpdateStaminaBar;
                enhancedPlayerController.OnStealthAttack += HandleStealthAttack;
                
                // 初期値を設定
                UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                UpdateStealthBar(enhancedPlayerController.StealthLevel);
            }
            else
            {
                // 後から検索を試行
                StartCoroutine(FindPlayerControllerLater());
            }
        }
        
        /// <summary>
        /// プレイヤーコントローラーを後から検索
        /// </summary>
        private System.Collections.IEnumerator FindPlayerControllerLater()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.5f);
                
                enhancedPlayerController = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
                if (enhancedPlayerController != null)
                {
                    enhancedPlayerController.OnHealthChanged += UpdateHealthBar;
                    enhancedPlayerController.OnStaminaChanged += UpdateStaminaBar;
                    enhancedPlayerController.OnStealthAttack += HandleStealthAttack;
                    
                    // 初期値を設定
                    UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                    UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                    UpdateStealthBar(enhancedPlayerController.StealthLevel);
                    break;
                }
            }
            
            // EnhancedPlayerControllerが見つからない場合はサイレントに終了
        }
        

        private void UpdateHUD()
        {
            if (gameManager != null)
            {
                UpdateTimeDisplay();
            }
            
            // 隠密レベルを動的に更新
            if (enhancedPlayerController != null)
            {
                UpdateStealthBar(enhancedPlayerController.StealthLevel);
            }
        }

        private void HandleInput()
        {
            if (inputManager == null) 
            {
                if (debugCommandExecution)
                    Debug.LogWarning("[UIManager] HandleInput: inputManager is null");
                return;
            }

            // インベントリ開閉
            if (inputManager.IsInventoryPressed())
            {
                TogglePanel("Inventory");
            }

            // Bボタン（メニューキー）でパネルを閉じる
            if (inputManager.IsMenuPressed() && IsAnyPanelOpen)
            {
                if (debugPanelControls)
                    Debug.Log("[UIManager] Menu button pressed - closing panels");
                
                // CompanionCommandパネルが開いている場合は個別に処理
                if (IsPanelOpen("CompanionCommand"))
                {
                    if (debugPanelControls)
                        Debug.Log("[UIManager] Closing CompanionCommand panel with B button");
                    ClosePanel("CompanionCommand");
                }
                else
                {
                    if (debugPanelControls)
                        Debug.Log("[UIManager] Closing all panels with B button");
                    CloseAllPanels();
                }
            }

            // その他のUIショートカット (直接Input.GetKeyDownを使用)
            if (Input.GetKeyDown(KeyCode.M))
            {
                TogglePanel("Map");
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                TogglePanel("Companion");
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                TogglePanel("BaseManagement");
            }
            
            // Companion command input
            if (inputManager.IsCompanionCommandPressed())
            {
                HandleCompanionCommandInput();
            }
        }

        private void UpdateTimeDisplay()
        {
            if (timeText != null)
            {
                float gameTime = gameManager.GameTime;
                int hours = Mathf.FloorToInt(gameTime / 3600f);
                int minutes = Mathf.FloorToInt((gameTime % 3600f) / 60f);
                timeText.text = $"{hours:00}:{minutes:00}";
            }
        }

        private void UpdatePhaseDisplay(GamePhase newPhase)
        {
            if (phaseText != null)
            {
                phaseText.text = newPhase switch
                {
                    GamePhase.SURVIVAL => "生存フェーズ",
                    GamePhase.DEFENSE => "防衛フェーズ",
                    GamePhase.ESCAPE => "脱出フェーズ",
                    _ => "不明"
                };
            }
        }

        private void UpdateDayDisplay(int newDay)
        {
            if (dayText != null)
            {
                dayText.text = newDay.ToString();
            }
        }

        private void UpdateResourceDisplay(ResourceType resourceType, int amount)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    if (foodText != null) foodText.text = amount.ToString();
                    break;
                case ResourceType.Water:
                    if (waterText != null) waterText.text = amount.ToString();
                    break;
                case ResourceType.Medicine:
                    if (medicineText != null) medicineText.text = amount.ToString();
                    break;
                case ResourceType.Materials:
                    if (materialsText != null) materialsText.text = amount.ToString();
                    break;
            }
        }

        private void UpdateInfectionDisplay(float infectionRate)
        {
            if (infectionSlider != null)
            {
                infectionSlider.value = infectionRate;
                
                Color sliderColor = infectionRate switch
                {
                    >= 0.8f => Color.red,
                    >= 0.6f => Color.yellow,
                    >= 0.4f => new Color(1f, 0.5f, 0f),
                    >= 0.2f => Color.green,
                    _ => Color.white
                };
                
                var fillArea = infectionSlider.fillRect.GetComponent<Image>();
                if (fillArea != null)
                {
                    fillArea.color = sliderColor;
                }
            }
        }

        public void ShowMainHUD()
        {
            if (mainHUD != null)
            {
                mainHUD.SetActive(true);
            }
        }

        public void HideMainHUD()
        {
            if (mainHUD != null)
            {
                mainHUD.SetActive(false);
            }
        }

        public void OpenPanel(string panelName)
        {
            GameObject panel = GetPanelByName(panelName);
            if (panel != null && !activePanels.ContainsKey(panelName))
            {
                panel.SetActive(true);
                activePanels[panelName] = panel;
                OnPanelOpened?.Invoke(panelName);
                
                // CompanionCommandパネルが開かれた場合、InteractionPromptを即座に非表示
                if (panelName == "CompanionCommand")
                {
                    UpdateInteractionPrompt();
                }
                
                if (gameManager != null)
                {
                    gameManager.PauseGame();
                }
            }
        }

        public void ClosePanel(string panelName)
        {
            if (activePanels.TryGetValue(panelName, out GameObject panel))
            {
                panel.SetActive(false);
                activePanels.Remove(panelName);
                OnPanelClosed?.Invoke(panelName);
                
                // CompanionCommandパネルが閉じられた場合、InteractionPromptを即座に再表示チェック
                if (panelName == "CompanionCommand")
                {
                    UpdateInteractionPrompt();
                }
                
                if (activePanels.Count == 0 && gameManager != null)
                {
                    gameManager.ResumeGame();
                }
            }
        }

        public void TogglePanel(string panelName)
        {
            if (activePanels.ContainsKey(panelName))
            {
                ClosePanel(panelName);
            }
            else
            {
                OpenPanel(panelName);
            }
        }

        public void CloseAllPanels()
        {
            var panelNames = new List<string>(activePanels.Keys);
            foreach (var panelName in panelNames)
            {
                ClosePanel(panelName);
            }
        }

        private GameObject GetPanelByName(string panelName)
        {
            return panelName switch
            {
                "Inventory" => inventoryPanel,
                "Companion" => companionPanel,
                "CompanionCommand" => companionCommandPanel,
                "BaseManagement" => baseManagementPanel,
                "Map" => mapPanel,
                "Dialogue" => dialoguePanel,
                "Crafting" => craftingPanel,
                "Tactical" => tacticalPanel,
                "Pause" => pauseMenu,
                _ => null
            };
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            if (notificationPrefab != null && notificationContainer != null)
            {
                GameObject notification = Instantiate(notificationPrefab, notificationContainer);
                var notificationComponent = notification.GetComponent<NotificationUI>();
                
                if (notificationComponent != null)
                {
                    notificationComponent.Setup(message, type, notificationDuration);
                }
                
                activeNotifications.Add(notification);
                
                Destroy(notification, notificationDuration + 1f);
            }
        }

        /// <summary>
        /// ヘルスバーを更新（EnhancedPlayerControllerのイベントから呼び出される）
        /// </summary>
        /// <param name="healthPercentage">ヘルス割合（0.0-1.0）</param>
        public void UpdateHealthBar(float healthPercentage)
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Clamp01(healthPercentage);
                
                // ヘルスレベルに応じて色を変更
                UpdateHealthBarColor(healthPercentage);
            }
        }
        
        /// <summary>
        /// ヘルスバーの色を更新
        /// </summary>
        private void UpdateHealthBarColor(float healthPercentage)
        {
            if (healthSlider != null)
            {
                Color barColor = healthPercentage switch
                {
                    <= 0.1f => Color.red,        // 10%以下：赤
                    <= 0.25f => new Color(1f, 0.5f, 0f), // 25%以下：オレンジ
                    <= 0.5f => Color.yellow,     // 50%以下：黄色
                    _ => Color.green             // 50%以上：緑
                };
                
                var fillArea = healthSlider.fillRect?.GetComponent<Image>();
                if (fillArea != null)
                {
                    fillArea.color = barColor;
                }
            }
        }

        /// <summary>
        /// スタミナバーを更新（EnhancedPlayerControllerのイベントから呼び出される）
        /// </summary>
        /// <param name="currentStamina">現在のスタミナ値</param>
        public void UpdateStaminaBar(float currentStamina)
        {
            if (staminaSlider != null && enhancedPlayerController != null)
            {
                // スタミナの絶対値をパーセンテージに変換
                float staminaPercentage = currentStamina / enhancedPlayerController.MaxStamina;
                staminaSlider.value = Mathf.Clamp01(staminaPercentage);
                
                // スタミナレベルに応じて色を変更
                UpdateStaminaBarColor(staminaPercentage);
                
            }
        }
        
        /// <summary>
        /// スタミナバーの色を更新
        /// </summary>
        private void UpdateStaminaBarColor(float staminaPercentage)
        {
            if (staminaSlider != null)
            {
                Color barColor = staminaPercentage switch
                {
                    <= 0.1f => Color.red,        // 10%以下：赤
                    <= 0.25f => new Color(1f, 0.5f, 0f), // 25%以下：オレンジ
                    <= 0.5f => Color.yellow,     // 50%以下：黄色
                    _ => Color.green             // 50%以上：緑
                };
                
                var fillArea = staminaSlider.fillRect?.GetComponent<Image>();
                if (fillArea != null)
                {
                    fillArea.color = barColor;
                }
            }
        }

        /// <summary>
        /// 隠密バーを更新
        /// </summary>
        /// <param name="stealthLevel">隠密レベル（0.0-1.0）</param>
        public void UpdateStealthBar(float stealthLevel)
        {
            if (stealthSlider != null)
            {
                stealthSlider.value = Mathf.Clamp01(stealthLevel);
                
                // 隠密レベルに応じて色を変更
                UpdateStealthBarColor(stealthLevel);
            }
        }
        
        /// <summary>
        /// 隠密バーの色を更新
        /// </summary>
        private void UpdateStealthBarColor(float stealthLevel)
        {
            if (stealthSlider != null)
            {
                Color barColor = stealthLevel switch
                {
                    >= 0.8f => Color.blue,       // 80%以上：青（完全隠密）
                    >= 0.6f => Color.cyan,       // 60%以上：シアン（高隠密）
                    >= 0.4f => Color.green,      // 40%以上：緑（中隠密）
                    >= 0.2f => Color.yellow,     // 20%以上：黄色（低隠密）
                    _ => Color.red               // 20%未満：赤（隠密なし）
                };
                
                var fillArea = stealthSlider.fillRect?.GetComponent<Image>();
                if (fillArea != null)
                {
                    fillArea.color = barColor;
                }
            }
        }

        /// <summary>
        /// ステルス攻撃イベントのハンドラー
        /// </summary>
        /// <param name="totalDamage">3倍になった総ダメージ</param>
        private void HandleStealthAttack(float totalDamage)
        {
            ShowNotification($"ステルス攻撃！ {totalDamage:F0} ダメージ", NotificationType.Success);
            
            // ステルス攻撃エフェクト（画面フラッシュなど）
            StartCoroutine(StealthAttackEffect());
        }

        /// <summary>
        /// ステルス攻撃の視覚エフェクト
        /// </summary>
        private System.Collections.IEnumerator StealthAttackEffect()
        {
            // 画面全体に薄い青色のフラッシュエフェクト
            GameObject flashEffect = new GameObject("StealthFlash");
            Canvas canvas = flashEffect.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 最前面に表示
            
            Image flashImage = flashEffect.AddComponent<Image>();
            flashImage.color = new Color(0f, 0.8f, 1f, 0.3f); // 薄い青色
            flashImage.raycastTarget = false;
            
            // フェードアウト
            float duration = 0.5f;
            float elapsedTime = 0f;
            Color originalColor = flashImage.color;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(originalColor.a, 0f, elapsedTime / duration);
                flashImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            
            // クリーンアップ
            Destroy(flashEffect);
        }

        /// <summary>
        /// ワールド座標にダメージテキストを表示
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isCritical">クリティカルダメージかどうか</param>
        public void ShowDamageText(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (damageTextPrefab == null || damageContainer == null)
                return;

            UnityEngine.Camera cameraForDamage = UnityEngine.Camera.main;
            if (cameraForDamage == null)
                return;

            // World座標をScreen座標に変換
            Vector3 screenPos = cameraForDamage.WorldToScreenPoint(worldPosition);
            
            // 画面外の場合は表示しない
            if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width || 
                screenPos.y < 0 || screenPos.y > Screen.height)
                return;

            // ダメージテキストオブジェクトを生成
            GameObject damageObj = Instantiate(damageTextPrefab, damageContainer);
            RectTransform rectTransform = damageObj.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                RectTransform containerRect = damageContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    // CanvasのRender ModeがScreen Space - Overlayの場合
                    Canvas canvas = damageContainer.GetComponentInParent<Canvas>();
                    UnityEngine.Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
                    
                    // Screen座標をCanvas座標に変換
                    Vector2 canvasPos;
                    bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        containerRect,
                        screenPos,
                        canvasCamera,
                        out canvasPos);

                    if (success)
                    {
                        rectTransform.localPosition = canvasPos;
                    }
                    else
                    {
                        rectTransform.localPosition = Vector3.zero;
                    }
                }
            }

            // DamageTextコンポーネントを初期化
            DamageText damageComponent = damageObj.GetComponent<DamageText>();
            if (damageComponent != null)
            {
                damageComponent.Initialize(damage, isCritical);
            }
        }

        /// <summary>
        /// 指定位置にセリフテキストを表示（非推奨：コンパニオンはCreateDialogueTextForCompanionを使用）
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <param name="dialogue">表示するセリフ</param>
        public void ShowDialogueText(Vector3 worldPosition, string dialogue)
        {
            if (string.IsNullOrEmpty(dialogue) || dialogueTextPrefab == null || damageContainer == null)
                return;

            UnityEngine.Camera cameraForDialogue = UnityEngine.Camera.main;
            if (cameraForDialogue == null)
                return;

            // World座標をScreen座標に変換
            Vector3 screenPos = cameraForDialogue.WorldToScreenPoint(worldPosition);
            
            // 画面外の場合は表示しない
            if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width || 
                screenPos.y < 0 || screenPos.y > Screen.height)
                return;

            // セリフテキストオブジェクトを生成
            GameObject dialogueObj = Instantiate(dialogueTextPrefab, damageContainer);
            RectTransform rectTransform = dialogueObj.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                RectTransform containerRect = damageContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    // CanvasのRender ModeがScreen Space - Overlayの場合
                    Canvas canvas = damageContainer.GetComponentInParent<Canvas>();
                    UnityEngine.Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
                    
                    // Screen座標をCanvas座標に変換
                    Vector2 canvasPos;
                    bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        containerRect,
                        screenPos,
                        canvasCamera,
                        out canvasPos);

                    if (success)
                    {
                        rectTransform.localPosition = canvasPos;
                    }
                    else
                    {
                        rectTransform.localPosition = Vector3.zero;
                    }
                }
            }

            // DialogueTextコンポーネントを初期化
            DialogueText dialogueComponent = dialogueObj.GetComponent<DialogueText>();
            if (dialogueComponent != null)
            {
                dialogueComponent.Initialize(dialogue, dialogueDuration);
            }
        }
        
        /// <summary>
        /// コンパニオン用のDialogueTextを作成（頭上追従機能付き）
        /// </summary>
        /// <param name="companion">追従対象のコンパニオン</param>
        /// <param name="dialogue">表示するセリフ</param>
        /// <returns>作成されたDialogueText GameObject</returns>
        public GameObject CreateDialogueTextForCompanion(KowloonBreak.Characters.CompanionAI companion, string dialogue)
        {
            if (string.IsNullOrEmpty(dialogue) || dialogueTextPrefab == null || damageContainer == null)
                return null;

            // 既存のDialogueTextがある場合は削除
            if (activeDialogueTexts.ContainsKey(companion))
            {
                DialogueText existingDialogue = activeDialogueTexts[companion];
                if (existingDialogue != null)
                {
                    existingDialogue.ForceDestroy();
                }
                activeDialogueTexts.Remove(companion);
            }

            // DialogueTextオブジェクトを生成
            GameObject dialogueObj = Instantiate(dialogueTextPrefab, damageContainer);

            // DialogueTextコンポーネントを初期化（コンパニオン追従モード）
            DialogueText dialogueComponent = dialogueObj.GetComponent<DialogueText>();
            if (dialogueComponent != null)
            {
                dialogueComponent.InitializeForCompanion(companion, dialogue, dialogueDuration);
                
                // 削除時のコールバックを設定
                dialogueComponent.OnDialogueDestroyed += () => {
                    OnDialogueTextDestroyed(companion);
                };
                
                // 辞書に追加
                activeDialogueTexts[companion] = dialogueComponent;
            }
            
            return dialogueObj;
        }
        
        /// <summary>
        /// DialogueTextが削除された際のコールバック
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        private void OnDialogueTextDestroyed(KowloonBreak.Characters.CompanionAI companion)
        {
            if (activeDialogueTexts.ContainsKey(companion))
            {
                activeDialogueTexts.Remove(companion);
            }
            
            // CompanionAIにも通知
            if (companion != null)
            {
                companion.OnDialogueTextDestroyed();
            }
            
            // InteractionPromptの表示状態を即座に更新
            UpdateInteractionPrompt();
        }

        public void UpdateUI()
        {
            if (resourceManager != null)
            {
                UpdateResourceDisplay(ResourceType.Food, resourceManager.GetResourceAmount(ResourceType.Food));
                UpdateResourceDisplay(ResourceType.Water, resourceManager.GetResourceAmount(ResourceType.Water));
                UpdateResourceDisplay(ResourceType.Medicine, resourceManager.GetResourceAmount(ResourceType.Medicine));
                UpdateResourceDisplay(ResourceType.Materials, resourceManager.GetResourceAmount(ResourceType.Materials));
            }

            if (gameManager != null)
            {
                UpdatePhaseDisplay(gameManager.CurrentPhase);
                UpdateDayDisplay(gameManager.CurrentDay);
            }

            if (infectionManager != null)
            {
                UpdateInfectionDisplay(infectionManager.CityInfectionRate);
            }
            
            // プレイヤーのヘルスとスタミナを更新
            if (enhancedPlayerController != null)
            {
                UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                UpdateStealthBar(enhancedPlayerController.StealthLevel);
            }
        }

        public bool IsPanelOpen(string panelName)
        {
            return activePanels.ContainsKey(panelName);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= UpdatePhaseDisplay;
                gameManager.OnDayChanged -= UpdateDayDisplay;
            }

            if (resourceManager != null)
            {
                resourceManager.OnResourceChanged -= UpdateResourceDisplay;
            }

            if (infectionManager != null)
            {
                infectionManager.OnCityInfectionRateChanged -= UpdateInfectionDisplay;
            }
            
            // プレイヤーコントローラーのイベント購読を解除
            if (enhancedPlayerController != null)
            {
                enhancedPlayerController.OnHealthChanged -= UpdateHealthBar;
                enhancedPlayerController.OnStaminaChanged -= UpdateStaminaBar;
                enhancedPlayerController.OnStealthAttack -= HandleStealthAttack;
            }
        }

        #region Companion Command System

        private void InitializeCompanionCommandSystem()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
                interactionPromptRectTransform = interactionPrompt.GetComponent<RectTransform>();
            }
            
            // メインカメラの参照を取得（DamageTextと同じ方法）
            mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<UnityEngine.Camera>();
            }
        }

        private void UpdateCompanionCommandSystem()
        {
            if (player == null) return;

            CheckForNearbyCompanions();
            UpdateInteractionPrompt();
            
            // コマンドパネルが開いている場合、コンパニオンの状態変化に応じて更新
            if (IsPanelOpen("CompanionCommand") && selectedCompanion != null)
            {
                UpdateCompanionCommandPanel();
            }
        }
        
        /// <summary>
        /// コンパニオンコマンドパネルの内容を更新
        /// </summary>
        private void UpdateCompanionCommandPanel()
        {
            if (selectedCompanion == null) return;
            
            // 更新頻度を制限
            if (Time.time - lastCommandUpdateTime < COMMAND_UPDATE_INTERVAL)
            {
                // 頻繁な更新は情報のみ
                UpdateCompanionInfo();
                return;
            }
            
            // 戦闘状態の変化をチェック
            bool currentCombatState = IsCompanionInCombat();
            
            if (currentCombatState != lastCombatState)
            {
                // 戦闘状態が変化した場合のみコマンドボタンを再生成
                lastCombatState = currentCombatState;
                lastCommandUpdateTime = Time.time;
                
                // 現在選択されているボタンのインデックスを保存
                int selectedIndex = GetCurrentSelectedButtonIndex();
                
                // コンパニオン情報を更新
                UpdateCompanionInfo();
                
                // コマンドボタンを再生成
                GenerateCommandButtons();
                
                // フォーカスを復元
                RestoreButtonFocus(selectedIndex);
            }
            else
            {
                // 戦闘状態に変化がない場合は情報のみ更新
                UpdateCompanionInfo();
            }
        }
        
        /// <summary>
        /// 現在選択されているボタンのインデックスを取得
        /// </summary>
        private int GetCurrentSelectedButtonIndex()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
                return 0;
            
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            
            for (int i = 0; i < activeCommandButtons.Count; i++)
            {
                if (activeCommandButtons[i] != null && activeCommandButtons[i].gameObject == selected)
                {
                    return i;
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// 指定インデックスのボタンにフォーカスを復元
        /// </summary>
        private void RestoreButtonFocus(int index)
        {
            if (activeCommandButtons.Count == 0) return;
            
            // インデックスを有効範囲にクランプ
            index = Mathf.Clamp(index, 0, activeCommandButtons.Count - 1);
            
            StartCoroutine(SetFocusToButtonAfterFrame(index));
        }
        
        /// <summary>
        /// 1フレーム後に指定ボタンにフォーカスを設定
        /// </summary>
        private System.Collections.IEnumerator SetFocusToButtonAfterFrame(int index)
        {
            yield return null;
            
            if (index < activeCommandButtons.Count && activeCommandButtons[index] != null)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    eventSystem.SetSelectedGameObject(activeCommandButtons[index].gameObject);
                }
            }
        }

        private void CheckForNearbyCompanions()
        {
            if (player == null) 
            {
                if (debugCompanionDetection)
                    Debug.LogWarning("[UIManager] CheckForNearbyCompanions: player is null");
                return;
            }


            KowloonBreak.Characters.CompanionAI nearestCompanion = null;
            float nearestDistance = float.MaxValue;

            // まずLayerMaskを使って検索
            Collider[] companionsInRange = Physics.OverlapSphere(player.position, interactionRange, companionLayerMask);
            
            
            // LayerMaskで見つからない場合、フォールバック検索（全レイヤー対象）
            if (companionsInRange.Length == 0)
            {
                Collider[] allColliders = Physics.OverlapSphere(player.position, interactionRange);
                var companionColliders = new System.Collections.Generic.List<Collider>();
                
                foreach (var col in allColliders)
                {
                    var compAI = col.GetComponent<KowloonBreak.Characters.CompanionAI>();
                    if (compAI != null)
                    {
                        companionColliders.Add(col);
                    }
                }
                
                companionsInRange = companionColliders.ToArray();
            }

            foreach (var collider in companionsInRange)
            {
                var companion = collider.GetComponent<KowloonBreak.Characters.CompanionAI>();
                if (companion != null)
                {
                    float distance = Vector3.Distance(player.position, companion.transform.position);
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestCompanion = companion;
                    }
                }
            }

            bool companionChanged = currentNearbyCompanion != nearestCompanion;
            currentNearbyCompanion = nearestCompanion;
            
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null) return;

            // DialogueTextが表示されているかチェック
            UpdateDialogueTextStatus();

            // コンパニオンが近くにいて、かつCompanionCommandパネルが開いていなく、かつDialogueTextが表示されていない場合のみ表示
            bool shouldShowPrompt = currentNearbyCompanion != null && 
                                   !IsPanelOpen("CompanionCommand") && 
                                   !hasActiveDialogueForNearbyCompanion;
            
            if (shouldShowPrompt)
            {
                // コンパニオンの頭上位置を取得
                Transform promptAnchor = GetCompanionPromptAnchor(currentNearbyCompanion);
                if (promptAnchor != null)
                {
                    Vector3 worldPosition = promptAnchor.position;
                    
                    // アンカーがコンパニオン自身の場合は上方向にオフセット
                    if (promptAnchor == currentNearbyCompanion.transform)
                    {
                        worldPosition += Vector3.up * 2.0f; // 2メートル上
                    }
                    
                    // ワールド座標をスクリーン座標に変換
                    bool isVisible = UpdatePromptPosition(worldPosition);
                    
                    // カメラの視野内にある場合のみ表示
                    shouldShowPrompt = isVisible;
                }
                else
                {
                    // アンカーが見つからない場合は表示しない
                    shouldShowPrompt = false;
                }
            }
            
            if (interactionPrompt.activeInHierarchy != shouldShowPrompt)
            {
                interactionPrompt.SetActive(shouldShowPrompt);
            }

            if (shouldShowPrompt && interactionPromptText != null)
            {
                interactionPromptText.text = "話す";
            }
        }

        private void HandleCompanionCommandInput()
        {
            // シンプルなトグル処理
            if (IsPanelOpen("CompanionCommand"))
            {
                ClosePanel("CompanionCommand");
            }
            else if (currentNearbyCompanion != null)
            {
                OpenCompanionCommandUI(currentNearbyCompanion);
            }
            else
            {
                ShowNotification("近くにコンパニオンがいません", NotificationType.Warning);
            }
        }

        public void OpenCompanionCommandUI(KowloonBreak.Characters.CompanionAI companion)
        {
            selectedCompanion = companion;
            
            // 戦闘状態を初期化
            lastCombatState = IsCompanionInCombat();
            lastCommandUpdateTime = Time.time;
            
            OpenPanel("CompanionCommand");
            
            if (IsPanelOpen("CompanionCommand"))
            {
                UpdateCompanionInfo();
                GenerateCommandButtons();
                StartCoroutine(SetInitialFocusAfterFrame());
            }
        }

        private void UpdateCompanionInfo()
        {
            if (selectedCompanion == null) return;

            var companionChar = selectedCompanion.GetComponent<KowloonBreak.Characters.CompanionCharacter>();
            if (companionChar == null) return;

            if (companionNameText != null)
            {
                companionNameText.text = companionChar.Name;
            }

            if (companionStatusText != null)
            {
                string status = GetCompanionStatusText(selectedCompanion.CurrentState);
                bool isInCombat = IsCompanionInCombat();
                string combatIndicator = isInCombat ? " [戦闘中]" : " [通常時]";
                companionStatusText.text = $"状態: {status}{combatIndicator}";
            }

            if (trustLevelText != null)
            {
                int trustLevel = companionChar.TrustLevel;
                trustLevelText.text = trustLevel.ToString();
            }
        }

        private string GetCompanionStatusText(KowloonBreak.Characters.AIState state)
        {
            return state switch
            {
                KowloonBreak.Characters.AIState.Follow => "追従中",
                KowloonBreak.Characters.AIState.Combat => "戦闘中",
                KowloonBreak.Characters.AIState.Idle => "待機中",
                KowloonBreak.Characters.AIState.Explore => "移動中",
                KowloonBreak.Characters.AIState.Support => "援護中",
                _ => "不明"
            };
        }

        private void GenerateCommandButtons()
        {
            ClearCommandButtons();

            if (commandButtonContainer == null || commandButtonPrefab == null || selectedCompanion == null)
                return;

            var availableCommands = GetAvailableCommands();

            foreach (var command in availableCommands)
            {
                CreateCommandButton(command);
            }
        }

        private List<KowloonBreak.Characters.CompanionCommand> GetAvailableCommands()
        {
            var commands = new List<KowloonBreak.Characters.CompanionCommand>();
            bool isInCombat = IsCompanionInCombat();
            
            foreach (KowloonBreak.Characters.CompanionCommand command in System.Enum.GetValues(typeof(KowloonBreak.Characters.CompanionCommand)))
            {
                // コンパニオンが実行可能かチェック
                if (!selectedCompanion.CanExecuteCommand(command))
                    continue;
                
                // 戦闘状態に応じてコマンドをフィルタリング
                if (ShouldShowCommandInCurrentState(command, isInCombat))
                {
                    commands.Add(command);
                }
            }
            
            return commands;
        }
        
        /// <summary>
        /// コンパニオンが戦闘中かどうかを判定
        /// </summary>
        private bool IsCompanionInCombat()
        {
            if (selectedCompanion == null) return false;
            
            // Combat状態、またはSupport状態（戦闘支援）の場合は戦闘中とみなす
            var state = selectedCompanion.CurrentState;
            return state == KowloonBreak.Characters.AIState.Combat || 
                   state == KowloonBreak.Characters.AIState.Support ||
                   selectedCompanion.HasTarget; // ターゲットがいる場合も戦闘中とみなす
        }
        
        /// <summary>
        /// 現在の状態（戦闘中/通常時）でコマンドを表示すべきかを判定
        /// </summary>
        private bool ShouldShowCommandInCurrentState(KowloonBreak.Characters.CompanionCommand command, bool isInCombat)
        {
            // 戦闘関連コマンド
            var combatCommands = new[]
            {
                KowloonBreak.Characters.CompanionCommand.Attack,
                KowloonBreak.Characters.CompanionCommand.Defend,
                KowloonBreak.Characters.CompanionCommand.Retreat,
                KowloonBreak.Characters.CompanionCommand.Advanced
            };
            
            // 通常時関連コマンド
            var normalCommands = new[]
            {
                KowloonBreak.Characters.CompanionCommand.Follow,
                KowloonBreak.Characters.CompanionCommand.Stay,
                KowloonBreak.Characters.CompanionCommand.Support
            };
            
            // 戦闘中の場合
            if (isInCombat)
            {
                // 戦闘関連コマンドのみ表示
                return System.Array.IndexOf(combatCommands, command) >= 0;
            }
            else
            {
                // 通常時は通常関連コマンドのみ表示
                return System.Array.IndexOf(normalCommands, command) >= 0;
            }
        }

        private void CreateCommandButton(KowloonBreak.Characters.CompanionCommand command)
        {
            var buttonObj = Instantiate(commandButtonPrefab, commandButtonContainer);
            var button = buttonObj.GetComponent<Button>();
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = GetCommandDisplayText(command);
            }

            if (button != null)
            {
                button.onClick.AddListener(() => ExecuteCompanionCommand(command));
                
                // UIナビゲーションを明示的に設定
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.Automatic;
                button.navigation = nav;
                
                activeCommandButtons.Add(button);
            }
        }

        private string GetCommandDisplayText(KowloonBreak.Characters.CompanionCommand command)
        {
            string description = command switch
            {
                KowloonBreak.Characters.CompanionCommand.Follow => followCommandText,
                KowloonBreak.Characters.CompanionCommand.Stay => stayCommandText,
                KowloonBreak.Characters.CompanionCommand.Attack => attackCommandText,
                KowloonBreak.Characters.CompanionCommand.Defend => defendCommandText,
                KowloonBreak.Characters.CompanionCommand.Support => supportCommandText,
                KowloonBreak.Characters.CompanionCommand.Retreat => retreatCommandText,
                KowloonBreak.Characters.CompanionCommand.Advanced => advancedCommandText,
                _ => command.ToString()
            };
                
            int requiredLevel = GetRequiredIntelligenceLevel(command);
            if (requiredLevel > 1)
            {
                description += $" (Lv.{requiredLevel})";
            }
            
            return description;
        }

        private int GetRequiredIntelligenceLevel(KowloonBreak.Characters.CompanionCommand command)
        {
            return command switch
            {
                KowloonBreak.Characters.CompanionCommand.Follow => 1,
                KowloonBreak.Characters.CompanionCommand.Stay => 1,
                KowloonBreak.Characters.CompanionCommand.Attack => 2,
                KowloonBreak.Characters.CompanionCommand.Defend => 2,
                KowloonBreak.Characters.CompanionCommand.Support => 4,
                KowloonBreak.Characters.CompanionCommand.Retreat => 5,
                KowloonBreak.Characters.CompanionCommand.Advanced => 5,
                _ => 1
            };
        }

        private void ExecuteCompanionCommand(KowloonBreak.Characters.CompanionCommand command)
        {
            if (selectedCompanion == null)
            {
                Debug.LogError("[UIManager] ExecuteCompanionCommand: selectedCompanion is null");
                return;
            }


            bool success = false;

            switch (command)
            {
                case KowloonBreak.Characters.CompanionCommand.Follow:
                case KowloonBreak.Characters.CompanionCommand.Stay:
                case KowloonBreak.Characters.CompanionCommand.Support:
                    // シンプルなコマンド - 追加パラメーターなし
                    success = selectedCompanion.ExecuteCommand(command);
                    break;


                case KowloonBreak.Characters.CompanionCommand.Attack:
                    GameObject nearestEnemy = FindNearestEnemyForCompanion();
                    if (nearestEnemy != null)
                    {
                        success = selectedCompanion.ExecuteCommand(command, target: nearestEnemy);
                    }
                    else
                    {
                        ShowNotification(noTargetFoundText, NotificationType.Warning);
                        // ターゲットなしの場合は少し待ってパネルを閉じる
                        StartCoroutine(CloseCommandPanelAfterDelay(1.5f));
                        return;
                    }
                    break;

                case KowloonBreak.Characters.CompanionCommand.Defend:
                    // 防衛モード
                    success = selectedCompanion.ExecuteCommand(command);
                    break;


                case KowloonBreak.Characters.CompanionCommand.Retreat:
                    // 撤退
                    success = selectedCompanion.ExecuteCommand(command);
                    break;

                case KowloonBreak.Characters.CompanionCommand.Advanced:
                    // 高度な戦術行動
                    success = selectedCompanion.ExecuteCommand(command);
                    break;

                default:
                    success = selectedCompanion.ExecuteCommand(command);
                    break;
            }

            // 結果のフィードバック
            if (success)
            {
                string commandName = command switch
                {
                    KowloonBreak.Characters.CompanionCommand.Follow => followCommandText,
                    KowloonBreak.Characters.CompanionCommand.Stay => stayCommandText,
                    KowloonBreak.Characters.CompanionCommand.Attack => attackCommandText,
                    KowloonBreak.Characters.CompanionCommand.Defend => defendCommandText,
                    KowloonBreak.Characters.CompanionCommand.Support => supportCommandText,
                    KowloonBreak.Characters.CompanionCommand.Retreat => retreatCommandText,
                    KowloonBreak.Characters.CompanionCommand.Advanced => advancedCommandText,
                    _ => command.ToString()
                };
                
                ShowNotification($"{commandExecutedPrefix}{commandName}", NotificationType.Success);
                
                if (debugPanelControls)
                    Debug.Log($"[UIManager] Command '{commandName}' executed successfully - closing panel immediately");
                
                // 成功時は即座にパネルを閉じる
                ClosePanel("CompanionCommand");
            }
            else
            {
                ShowNotification(commandFailedText, NotificationType.Error);
                
                // 失敗時は少し待ってからパネルを閉じる
                StartCoroutine(CloseCommandPanelAfterDelay(1.5f));
            }
        }

        /// <summary>
        /// 指定時間後にコマンドパネルを閉じる
        /// </summary>
        private System.Collections.IEnumerator CloseCommandPanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClosePanel("CompanionCommand");
        }

        private GameObject FindNearestEnemyForCompanion()
        {
            if (selectedCompanion == null) return null;

            float searchRadius = 15f;
            Collider[] potentialEnemies = Physics.OverlapSphere(selectedCompanion.transform.position, searchRadius);
            
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in potentialEnemies)
            {
                // より厳密な敵の判定
                var enemyBase = collider.GetComponent<KowloonBreak.Enemies.EnemyBase>();
                if (enemyBase != null && !enemyBase.IsDestroyed)
                {
                    float distance = Vector3.Distance(selectedCompanion.transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.gameObject;
                    }
                }
            }


            return nearestEnemy;
        }

        private void ClearCommandButtons()
        {
            foreach (var button in activeCommandButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            activeCommandButtons.Clear();
        }

        private string GetCompanionCommandKeyName()
        {
            if (inputManager == null || inputManager.Settings == null) 
                return "Tキー";

            var binding = inputManager.Settings.companionCommandInput;
            var currentDevice = inputManager.CurrentDevice;

            switch (currentDevice)
            {
                case KowloonBreak.Core.InputDevice.KeyboardMouse:
                    if (binding.keyboardKey != KeyCode.None)
                        return $"{binding.keyboardKey}キー";
                    else if (binding.alternativeKey != KeyCode.None)
                        return $"{binding.alternativeKey}キー";
                    break;
                case KowloonBreak.Core.InputDevice.Controller:
                    return "Yボタン";
            }

            return "Tキー";
        }

        /// <summary>
        /// コンパニオンのInteractionPrompt表示用アンカーを取得
        /// </summary>
        private Transform GetCompanionPromptAnchor(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null) return null;
            
            // CompanionAIのフィールドからアンカーを取得
            Transform anchor = companion.InteractionPromptAnchor;
            if (anchor != null) return anchor;
            
            // アンカーが設定されていない場合はコンパニオン自身の位置を返す
            // （UpdateInteractionPromptで自動オフセットされる）
            return companion.transform;
        }

        /// <summary>
        /// ワールド座標をスクリーン座標に変換してInteractionPromptの位置を更新
        /// DamageContainerと同じ仕組みを使用
        /// </summary>
        private bool UpdatePromptPosition(Vector3 worldPosition)
        {
            if (mainCamera == null || interactionPromptRectTransform == null || damageContainer == null)
            {
                return false;
            }

            // ワールド座標をスクリーン座標に変換（DamageTextと同じ方法）
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            
            
            // 画面外の場合は非表示（DamageTextと同じ条件）
            if (screenPosition.z < 0 || screenPosition.x < 0 || screenPosition.x > Screen.width || 
                screenPosition.y < 0 || screenPosition.y > Screen.height)
            {
                return false;
            }

            // DamageContainerを基準とした座標変換（DamageTextと同じ方法）
            RectTransform containerRect = damageContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                // CanvasのRender ModeがScreen Space - Overlayの場合
                Canvas canvas = damageContainer.GetComponentInParent<Canvas>();
                UnityEngine.Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
                
                // Screen座標をCanvas座標に変換（DamageTextと同じ方法）
                Vector2 canvasPosition;
                bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    containerRect,
                    screenPosition,
                    canvasCamera,
                    out canvasPosition);


                if (success)
                {
                    // DamageTextと同じようにlocalPositionを使用
                    interactionPromptRectTransform.localPosition = canvasPosition;
                    
                    
                    return true;
                }
                else
                {
                    // 変換に失敗した場合は画面中央に表示
                    interactionPromptRectTransform.localPosition = Vector3.zero;
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// DialogueTextの表示状態をチェック
        /// </summary>
        private void UpdateDialogueTextStatus()
        {
            // 近くのコンパニオンのDialogueTextが表示されているかチェック
            hasActiveDialogueForNearbyCompanion = false;
            
            if (currentNearbyCompanion != null && activeDialogueTexts.ContainsKey(currentNearbyCompanion))
            {
                DialogueText dialogueText = activeDialogueTexts[currentNearbyCompanion];
                hasActiveDialogueForNearbyCompanion = dialogueText != null && dialogueText.gameObject.activeInHierarchy;
            }
        }

        /// <summary>
        /// 1フレーム待ってから最初のボタンにフォーカスを設定
        /// </summary>
        private System.Collections.IEnumerator SetInitialFocusAfterFrame()
        {
            // 1フレーム待つ（ボタンが完全に生成されるまで）
            yield return null;
            
            SetInitialButtonFocus();
        }

        /// <summary>
        /// CompanionCommandパネルの最初のボタンにフォーカスを設定
        /// </summary>
        private void SetInitialButtonFocus()
        {
            if (activeCommandButtons.Count > 0 && activeCommandButtons[0] != null)
            {
                // EventSystemが存在することを確認
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    // 最初のボタンを選択状態に設定
                    eventSystem.SetSelectedGameObject(activeCommandButtons[0].gameObject);
                    
                }
                else
                {
                    Debug.LogWarning("[UIManager] EventSystem not found! UI navigation will not work properly.");
                }
            }
            else
            {
                // コマンドボタンが利用できない場合（特に問題なし）
            }
        }

        /// <summary>
        /// EventSystemの状態をチェック・修復
        /// </summary>
        public void EnsureEventSystemExists()
        {
            if (EventSystem.current == null)
            {
                // EventSystemが存在しない場合は新規作成
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                
            }
        }

        #endregion
        
        #region Companion Health Bar System
        
        /// <summary>
        /// コンパニオンのヘルスバーを作成
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        /// <returns>作成されたヘルスバーオブジェクト</returns>
        public GameObject CreateHealthBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || companionHealthBarPrefab == null || damageContainer == null)
                return null;
            
            // 既に存在する場合は削除
            if (activeHealthBars.ContainsKey(companion))
            {
                RemoveHealthBarForCompanion(companion);
            }
            
            // ヘルスバーオブジェクトを生成
            GameObject healthBarObj = Instantiate(companionHealthBarPrefab, damageContainer);
            
            // CompanionHealthBarコンポーネントを初期化
            CompanionHealthBar healthBarComponent = healthBarObj.GetComponent<CompanionHealthBar>();
            if (healthBarComponent != null)
            {
                healthBarComponent.InitializeForCompanion(companion);
                
                // 削除時のコールバックを設定
                healthBarComponent.OnHealthBarDestroyed += () => {
                    OnHealthBarDestroyed(companion);
                };
                
                // 辞書に追加
                activeHealthBars[companion] = healthBarComponent;
            }
            
            return healthBarObj;
        }
        
        /// <summary>
        /// コンパニオンのヘルスバーを削除
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        public void RemoveHealthBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || !activeHealthBars.ContainsKey(companion))
                return;
            
            CompanionHealthBar healthBar = activeHealthBars[companion];
            if (healthBar != null)
            {
                healthBar.ForceDestroy();
            }
            
            activeHealthBars.Remove(companion);
        }
        
        /// <summary>
        /// コンパニオンが破壊された際のヘルスバー削除通知
        /// </summary>
        /// <param name="companion">破壊されたコンパニオン</param>
        public void OnCompanionDestroyed(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || !activeHealthBars.ContainsKey(companion))
                return;
            
            CompanionHealthBar healthBar = activeHealthBars[companion];
            if (healthBar != null)
            {
                // フェードアウト後に削除
                healthBar.OnCompanionDestroyed();
            }
            
            activeHealthBars.Remove(companion);
        }
        
        /// <summary>
        /// ヘルスバーが削除された際のコールバック
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        private void OnHealthBarDestroyed(KowloonBreak.Characters.CompanionAI companion)
        {
            if (activeHealthBars.ContainsKey(companion))
            {
                activeHealthBars.Remove(companion);
            }
        }
        
        /// <summary>
        /// 全てのコンパニオンヘルスバーをクリア
        /// </summary>
        public void ClearAllCompanionHealthBars()
        {
            var companionsToRemove = new List<KowloonBreak.Characters.CompanionAI>(activeHealthBars.Keys);
            
            foreach (var companion in companionsToRemove)
            {
                RemoveHealthBarForCompanion(companion);
            }
            
            activeHealthBars.Clear();
        }
        
        /// <summary>
        /// 指定されたコンパニオンのヘルスバーが存在するかチェック
        /// </summary>
        /// <param name="companion">チェック対象のコンパニオン</param>
        /// <returns>ヘルスバーが存在する場合true</returns>
        public bool HasHealthBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            return companion != null && activeHealthBars.ContainsKey(companion);
        }

        #endregion

        #region Enemy Health Bar System

        /// <summary>
        /// 敵のヘルスバーを作成
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        /// <returns>作成されたヘルスバーオブジェクト</returns>
        public GameObject CreateHealthBarForEnemy(KowloonBreak.Enemies.EnemyBase enemy)
        {
            if (enemy == null || enemyHealthBarPrefab == null || damageContainer == null)
                return null;

            // 既に存在する場合は削除
            if (activeEnemyHealthBars.ContainsKey(enemy))
            {
                RemoveHealthBarForEnemy(enemy);
            }

            // ヘルスバーオブジェクトを生成
            GameObject healthBarObj = Instantiate(enemyHealthBarPrefab, damageContainer);

            // EnemyHealthBarコンポーネントを初期化
            EnemyHealthBar healthBarComponent = healthBarObj.GetComponent<EnemyHealthBar>();
            if (healthBarComponent != null)
            {
                healthBarComponent.InitializeForEnemy(enemy);

                // 削除時のコールバックを設定
                healthBarComponent.OnHealthBarDestroyed += () => {
                    OnEnemyHealthBarDestroyed(enemy);
                };

                // 辞書に追加
                activeEnemyHealthBars[enemy] = healthBarComponent;
            }

            return healthBarObj;
        }

        /// <summary>
        /// 敵のヘルスバーを削除
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        public void RemoveHealthBarForEnemy(KowloonBreak.Enemies.EnemyBase enemy)
        {
            if (enemy == null || !activeEnemyHealthBars.ContainsKey(enemy))
                return;

            EnemyHealthBar healthBar = activeEnemyHealthBars[enemy];
            if (healthBar != null)
            {
                healthBar.ForceDestroy();
            }

            activeEnemyHealthBars.Remove(enemy);
        }

        /// <summary>
        /// 敵が破壊された際のヘルスバー削除通知
        /// </summary>
        /// <param name="enemy">破壊された敵</param>
        public void OnEnemyDestroyed(KowloonBreak.Enemies.EnemyBase enemy)
        {
            if (enemy == null || !activeEnemyHealthBars.ContainsKey(enemy))
                return;

            EnemyHealthBar healthBar = activeEnemyHealthBars[enemy];
            if (healthBar != null)
            {
                // フェードアウト後に削除
                healthBar.OnEnemyDestroyed();
            }

            activeEnemyHealthBars.Remove(enemy);
        }

        /// <summary>
        /// 敵のヘルスバーが削除された際のコールバック
        /// </summary>
        /// <param name="enemy">対象の敵</param>
        private void OnEnemyHealthBarDestroyed(KowloonBreak.Enemies.EnemyBase enemy)
        {
            if (activeEnemyHealthBars.ContainsKey(enemy))
            {
                activeEnemyHealthBars.Remove(enemy);
            }
        }

        /// <summary>
        /// 全ての敵ヘルスバーをクリア
        /// </summary>
        public void ClearAllEnemyHealthBars()
        {
            var enemiesToRemove = new List<KowloonBreak.Enemies.EnemyBase>(activeEnemyHealthBars.Keys);

            foreach (var enemy in enemiesToRemove)
            {
                RemoveHealthBarForEnemy(enemy);
            }

            activeEnemyHealthBars.Clear();
        }

        /// <summary>
        /// 指定された敵のヘルスバーが存在するかチェック
        /// </summary>
        /// <param name="enemy">チェック対象の敵</param>
        /// <returns>ヘルスバーが存在する場合true</returns>
        public bool HasHealthBarForEnemy(KowloonBreak.Enemies.EnemyBase enemy)
        {
            return enemy != null && activeEnemyHealthBars.ContainsKey(enemy);
        }

        #endregion

        #region Game Over System

        /// <summary>
        /// ゲームオーバーシステムの初期化
        /// </summary>
        private void InitializeGameOverSystem()
        {
            if (gameOverUI != null)
            {
                // Game Over UIのイベントを購読
                gameOverUI.OnRetryRequested += HandleRetryRequested;
                gameOverUI.OnMainMenuRequested += HandleMainMenuRequested;

                Debug.Log("[UIManager] Game Over system initialized");
            }
            else
            {
                Debug.LogWarning("[UIManager] GameOverUI component not assigned");
            }
        }

        /// <summary>
        /// ゲームオーバーUIを表示
        /// </summary>
        public void ShowGameOver()
        {
            if (gameOverUI != null)
            {
                Debug.Log("[UIManager] Showing Game Over screen");
                gameOverUI.ShowGameOverUI();
            }
            else
            {
                Debug.LogError("[UIManager] Cannot show Game Over - GameOverUI not assigned");
            }
        }

        /// <summary>
        /// ゲームオーバーUIを非表示
        /// </summary>
        public void HideGameOver()
        {
            if (gameOverUI != null)
            {
                gameOverUI.HideGameOverUI();
            }
        }

        /// <summary>
        /// リトライが要求された時の処理
        /// </summary>
        private void HandleRetryRequested()
        {
            Debug.Log("[UIManager] Retry requested - initiating game restart");

            // リトライ処理を開始
            StartCoroutine(RestartGame());
        }

        /// <summary>
        /// メインメニューが要求された時の処理
        /// </summary>
        private void HandleMainMenuRequested()
        {
            Debug.Log("[UIManager] Main menu requested");

            // メインメニューに戻る処理
            // 実装は後で追加（シーン管理が必要）
            ShowNotification("メインメニュー機能は準備中です", NotificationType.Info);
        }

        /// <summary>
        /// ゲーム再開始処理
        /// </summary>
        private System.Collections.IEnumerator RestartGame()
        {
            // プレイヤーを復活・リセット
            yield return StartCoroutine(RespawnPlayer());

            // コンパニオンをプレイヤーの近くにワープ
            RespawnCompanions();

            // その他のゲーム状態をリセット
            ResetGameState();

            Debug.Log("[UIManager] Game restart completed");
        }

        /// <summary>
        /// プレイヤーを復活させる
        /// </summary>
        private System.Collections.IEnumerator RespawnPlayer()
        {
            if (enhancedPlayerController != null)
            {
                Debug.Log("[UIManager] Respawning player");

                // プレイヤーを開始位置に移動
                Vector3 spawnPosition = GetPlayerSpawnPosition();
                enhancedPlayerController.transform.position = spawnPosition;

                // HP、スタミナを全快
                enhancedPlayerController.RestoreFullHealth();
                enhancedPlayerController.RestoreFullStamina();

                // IF（感染率）を0に
                if (infectionManager != null)
                {
                    infectionManager.ResetPlayerInfection();
                }

                // プレイヤーを生存状態に復活
                enhancedPlayerController.Revive();

                yield return new WaitForSeconds(0.1f); // 少し待機
            }
            else
            {
                Debug.LogError("[UIManager] Cannot respawn player - EnhancedPlayerController not found");
            }
        }

        /// <summary>
        /// コンパニオンをプレイヤーの近くに復活させる
        /// </summary>
        private void RespawnCompanions()
        {
            if (enhancedPlayerController == null) return;

            // すべてのコンパニオンを検索
            var companions = FindObjectsOfType<KowloonBreak.Characters.CompanionAI>();

            Vector3 playerPosition = enhancedPlayerController.transform.position;

            for (int i = 0; i < companions.Length; i++)
            {
                var companion = companions[i];
                if (companion != null)
                {
                    // プレイヤーの周りにランダムに配置
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-3f, 3f),  // X軸方向のランダムオフセット
                        0f,
                        UnityEngine.Random.Range(-3f, 3f)   // Z軸方向のランダムオフセット
                    );

                    Vector3 companionSpawnPosition = playerPosition + offset;
                    companion.transform.position = companionSpawnPosition;

                    // コンパニオンのHPを全快
                    companion.RestoreFullHealth();

                    Debug.Log($"[UIManager] Respawned companion {companion.name} at {companionSpawnPosition}");
                }
            }
        }

        /// <summary>
        /// プレイヤーのスポーン位置を取得
        /// </summary>
        private Vector3 GetPlayerSpawnPosition()
        {
            try
            {
                // 実際のスポーンポイントがある場合はそれを使用
                GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawn");
                if (spawnPoint != null)
                {
                    return spawnPoint.transform.position;
                }
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[UIManager] PlayerSpawn tag is not defined: {ex.Message}");
            }

            // "Player"タグのオブジェクトを探してその位置を基準にする
            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Debug.Log("[UIManager] Using current player position as spawn point");
                    return player.transform.position;
                }
            }
            catch (UnityException ex)
            {
                Debug.LogWarning($"[UIManager] Player tag is not defined: {ex.Message}");
            }

            // EnhancedPlayerControllerを直接探す
            var playerController = UnityEngine.Object.FindObjectOfType<Player.EnhancedPlayerController>();
            if (playerController != null)
            {
                Debug.Log("[UIManager] Using EnhancedPlayerController position as spawn point");
                return playerController.transform.position;
            }

            // 最終フォールバック：原点から少し上
            Debug.LogWarning("[UIManager] No spawn point found, using default position (0, 1, 0)");
            return new Vector3(0f, 1f, 0f);
        }

        /// <summary>
        /// ゲーム状態をリセット
        /// </summary>
        private void ResetGameState()
        {
            // 時間スケールを元に戻す
            Time.timeScale = 1f;

            // カーソルを元の状態に戻す
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // その他のゲーム状態リセット
            // 必要に応じて追加

            Debug.Log("[UIManager] Game state reset completed");
        }

        /// <summary>
        /// ゲームオーバー状態かどうか
        /// </summary>
        public bool IsGameOverActive => gameOverUI != null && gameOverUI.IsGameOverActive;

        #endregion

        #region Debug and Diagnostics
        
        
        #endregion
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}