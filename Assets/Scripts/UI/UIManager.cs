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

        [Header("Panel Controllers")]
        [SerializeField] private InventoryDialogController inventoryDialogController;

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

        [Header("Companion Infection Bar System")]
        [SerializeField] private GameObject companionInfectionBarPrefab;

        // Public access for CompanionInfectionBarPrefab
        public GameObject CompanionInfectionBarPrefab => companionInfectionBarPrefab;

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

        [Header("Infection Interaction System")]
        [SerializeField] private InfectedInteractionUI infectedInteractionUI;
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
        private KowloonBreak.Player.EnhancedPlayerController enhancedPlayerController;
        
        // Companion Command System
        private KowloonBreak.Characters.CompanionAI currentNearbyCompanion;
        private KowloonBreak.Characters.CompanionAI selectedCompanion;
        private List<Button> activeCommandButtons = new List<Button>();
        private Transform player;
        private KowloonBreak.Core.InputManager inputManager;
        
        // Companion Health Bar System
        private Dictionary<KowloonBreak.Characters.CompanionAI, CompanionHealthBar> activeHealthBars = new Dictionary<KowloonBreak.Characters.CompanionAI, CompanionHealthBar>();

        // Companion Infection Bar System
        private Dictionary<KowloonBreak.Characters.CompanionAI, CompanionInfectionBar> activeInfectionBars = new Dictionary<KowloonBreak.Characters.CompanionAI, CompanionInfectionBar>();

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
            Instance = this;
            InitializeUI();
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            resourceManager = EnhancedResourceManager.Instance;
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
            UpdateUIState();
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
                resourceManager.OnToolSlotChanged += UpdateToolInventoryUI;
                resourceManager.OnMaterialSlotChanged += UpdateMaterialInventoryUI;
                resourceManager.OnItemAdded += OnInventoryItemAdded;
                resourceManager.OnItemRemoved += OnInventoryItemRemoved;
            }

            
            // プレイヤーのヘルス・スタミナ・感染レベル変更イベントに直接購読
            if (enhancedPlayerController != null)
            {
                enhancedPlayerController.OnHealthChanged += UpdateHealthBar;
                enhancedPlayerController.OnStaminaChanged += UpdateStaminaBar;
                enhancedPlayerController.OnInfectionLevelChanged += UpdatePlayerInfectionBar;
                enhancedPlayerController.OnStealthAttack += HandleStealthAttack;

                // 初期値を設定
                UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                UpdateStealthBar(enhancedPlayerController.StealthLevel);
                UpdatePlayerInfectionBar(enhancedPlayerController.InfectionLevel / 100f);
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
                    enhancedPlayerController.OnInfectionLevelChanged += UpdatePlayerInfectionBar;
                    enhancedPlayerController.OnStealthAttack += HandleStealthAttack;

                    // 初期値を設定
                    UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                    UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                    UpdateStealthBar(enhancedPlayerController.StealthLevel);
                    UpdatePlayerInfectionBar(enhancedPlayerController.InfectionLevel / 100f);
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
                return;
            }

            // 入力処理は各InputHandlerに移譲
            // GameplayInputHandler: インベントリ開閉、UIショートカット
            // InventoryInputHandler: インベントリ閉じる
            // その他のInputHandler: 各UI固有の入力処理

            // ここではInputHandlerがアクティブな時のみ処理を実行
            var currentHandler = inputManager?.GetCurrentInputHandler();
            if (currentHandler == null) return;

            string handlerName = currentHandler.GetType().Name;

            // GameplayInputHandler経由でのみUI操作を許可
            if (handlerName == "GameplayInputHandler")
            {
                // インベントリ開閉（Tabキー / Viewボタン）
                // ※GameplayInputHandlerで処理済みだが、念のため残す
                if (inputManager.IsInventoryPressed())
                {
                    TogglePanel("Inventory");
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

                // メニューキーでパネルを閉じる（Inventory以外）
                if (inputManager.IsMenuPressed() && IsAnyPanelOpen)
                {
                    if (!IsPanelOpen("Inventory"))
                    {
                        if (debugPanelControls)
                            Debug.Log("[UIManager] Closing panels via menu key");
                        CloseAllPanels();
                    }
                }
            }

            // キャンセル入力でアクティブなダイアログを閉じる（全コンテキストで有効）
            HandleDialogCancelInput(inputManager);
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


        /// <summary>
        /// プレイヤーの個人的な感染レベルを更新（EnhancedPlayerControllerのイベントから呼び出される）
        /// </summary>
        /// <param name="infectionLevel">感染レベル（0.0-1.0）</param>
        private void UpdatePlayerInfectionBar(float infectionLevel)
        {
            if (infectionSlider != null)
            {
                infectionSlider.value = Mathf.Clamp01(infectionLevel);

                // 感染レベルに応じて色を変更
                Color sliderColor = infectionLevel switch
                {
                    >= 0.8f => Color.red,        // 80%以上：赤（重篤）
                    >= 0.6f => new Color(1f, 0.5f, 0f), // 60%以上：オレンジ（危険）
                    >= 0.4f => Color.yellow,     // 40%以上：黄色（警戒）
                    >= 0.2f => new Color(0.5f, 1f, 0.5f), // 20%以上：薄緑（軽微）
                    _ => Color.white             // 20%未満：白（正常）
                };

                var fillArea = infectionSlider.fillRect?.GetComponent<Image>();
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
                // UIContextManagerに通知
                var contextManager = KowloonBreak.Core.UIContextManager.Instance;
                if (contextManager != null)
                {
                    var uiContext = GetUIContextFromPanelName(panelName);
                    contextManager.PushContext(uiContext);
                }

                // Inventoryパネルの場合は専用コントローラーを使用
                if (panelName == "Inventory" && inventoryDialogController != null)
                {
                    Debug.Log($"[UIManager] Opening Inventory. panel: {panel?.name}, panel active before: {panel?.activeSelf}, controller: {inventoryDialogController?.gameObject.name}");

                    try
                    {
                        panel.SetActive(true);  // パネルのGameObjectをアクティブ化
                        Debug.Log($"[UIManager] panel active after SetActive(true): {panel?.activeSelf}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[UIManager] Error activating panel: {e}");
                    }

                    try
                    {
                        inventoryDialogController.OpenInventory();
                        Debug.Log("[UIManager] inventoryDialogController.OpenInventory() completed");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[UIManager] Error opening inventory: {e}");
                    }

                    activePanels[panelName] = panel;
                    OnPanelOpened?.Invoke(panelName);
                }
                else
                {
                    panel.SetActive(true);
                    activePanels[panelName] = panel;
                    OnPanelOpened?.Invoke(panelName);
                }

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
                // UIContextManagerに通知
                var contextManager = KowloonBreak.Core.UIContextManager.Instance;
                if (contextManager != null)
                {
                    contextManager.PopContext();
                }

                // Inventoryパネルの場合は専用コントローラーを使用
                if (panelName == "Inventory" && inventoryDialogController != null)
                {
                    inventoryDialogController.CloseInventory();
                    panel.SetActive(false);  // パネルのGameObjectを非アクティブ化
                }
                else
                {
                    panel.SetActive(false);
                }

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

            // アクティブなパネルをすべて閉じる
            var panelNames = new List<string>(activePanels.Keys);
            foreach (var panelName in panelNames)
            {
                ClosePanel(panelName);
            }

            // 念のため、各パネルを直接非アクティブにする
            var allPanels = new[]
            {
                inventoryPanel,
                companionPanel,
                companionCommandPanel,
                baseManagementPanel,
                mapPanel,
                dialoguePanel,
                craftingPanel,
                tacticalPanel,
                pauseMenu
            };

            foreach (var panel in allPanels)
            {
                if (panel != null && panel.activeInHierarchy)
                {
                    panel.SetActive(false);
                }
            }

            // アクティブパネル辞書をクリア
            activePanels.Clear();

            // ゲームの一時停止状態を解除
            if (gameManager != null)
            {
                gameManager.ResumeGame();
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

        private KowloonBreak.Core.UIContext GetUIContextFromPanelName(string panelName)
        {
            return panelName switch
            {
                "Inventory" => KowloonBreak.Core.UIContext.Inventory,
                "Companion" => KowloonBreak.Core.UIContext.Gameplay, // 仲間パネルは移動可能
                "CompanionCommand" => KowloonBreak.Core.UIContext.CompanionCommand,
                "BaseManagement" => KowloonBreak.Core.UIContext.BaseManagement,
                "Map" => KowloonBreak.Core.UIContext.Map,
                "Dialogue" => KowloonBreak.Core.UIContext.Dialogue,
                "Crafting" => KowloonBreak.Core.UIContext.Crafting,
                "Tactical" => KowloonBreak.Core.UIContext.TacticalPause,
                "Pause" => KowloonBreak.Core.UIContext.PauseMenu,
                _ => KowloonBreak.Core.UIContext.Gameplay
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
        /// ワールド座標にダメージテキストを表示（ダメージタイプ指定版）
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="damageType">ダメージタイプ</param>
        public void ShowDamageText(Vector3 worldPosition, float damage, DamageType damageType)
        {
            bool isCritical = damageType == DamageType.Critical;
            ShowDamageText(worldPosition, damage, isCritical);

            // 感染ダメージの場合は追加処理（必要に応じて）
            if (damageType == DamageType.Infection)
            {
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
            {
                return null;
            }

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

            
            // プレイヤーのヘルス・スタミナ・感染レベルを更新
            if (enhancedPlayerController != null)
            {
                UpdateHealthBar(enhancedPlayerController.HealthPercentage);
                UpdateStaminaBar(enhancedPlayerController.CurrentStamina);
                UpdateStealthBar(enhancedPlayerController.StealthLevel);
                UpdatePlayerInfectionBar(enhancedPlayerController.InfectionLevel / 100f);
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
                resourceManager.OnToolSlotChanged -= UpdateToolInventoryUI;
                resourceManager.OnMaterialSlotChanged -= UpdateMaterialInventoryUI;
                resourceManager.OnItemAdded -= OnInventoryItemAdded;
                resourceManager.OnItemRemoved -= OnInventoryItemRemoved;
            }

            
            // プレイヤーコントローラーのイベント購読を解除
            if (enhancedPlayerController != null)
            {
                enhancedPlayerController.OnHealthChanged -= UpdateHealthBar;
                enhancedPlayerController.OnStaminaChanged -= UpdateStaminaBar;
                enhancedPlayerController.OnInfectionLevelChanged -= UpdatePlayerInfectionBar;
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

            // コンパニオンが近くにいて、かつCompanionCommandパネルが開いていなく、かつDialogueTextが表示されていない、かつInfectedInteractionUIが表示されていない場合のみ表示
            bool shouldShowPrompt = currentNearbyCompanion != null &&
                                   !IsPanelOpen("CompanionCommand") &&
                                   !hasActiveDialogueForNearbyCompanion &&
                                   !IsInfectedInteractionUIActive();
            
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
            // アクティブなUIを閉じる処理
            if (IsPanelOpen("CompanionCommand"))
            {
                ClosePanel("CompanionCommand");
                return;
            }

            if (IsInfectedInteractionUIActive())
            {
                HideInfectedInteractionUI();
                return;
            }

            // 近くにコンパニオンがいない場合
            if (currentNearbyCompanion == null)
            {
                ShowNotification("近くにコンパニオンがいません", NotificationType.Warning);
                return;
            }

            // コンパニオンの感染状態をチェック
            var companionCharacter = currentNearbyCompanion.GetComponent<KowloonBreak.Characters.CompanionCharacter>();
            if (companionCharacter != null && companionCharacter.Infection.IsInfected)
            {
                ShowInfectedInteractionUI(companionCharacter);
            }
            else
            {
                OpenCompanionCommandUI(currentNearbyCompanion);
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

        #region Companion Infection Bar System

        /// <summary>
        /// コンパニオンの感染バーを作成
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        /// <returns>作成された感染バーオブジェクト</returns>
        public GameObject CreateInfectionBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || companionInfectionBarPrefab == null || damageContainer == null)
            {
                Debug.LogWarning("[UIManager] Cannot create infection bar - missing references");
                return null;
            }

            // 既存の感染バーがある場合は削除
            if (activeInfectionBars.ContainsKey(companion))
            {
                RemoveInfectionBarForCompanion(companion);
            }

            // 感染バーオブジェクトを生成
            GameObject infectionBarObj = Instantiate(companionInfectionBarPrefab, damageContainer);

            // CompanionInfectionBarコンポーネントを初期化
            CompanionInfectionBar infectionBarComponent = infectionBarObj.GetComponent<CompanionInfectionBar>();
            if (infectionBarComponent != null)
            {
                infectionBarComponent.InitializeForCompanion(companion);

                // 削除時のコールバックを設定
                infectionBarComponent.OnInfectionBarDestroyed += () => {
                    OnInfectionBarDestroyed(companion);
                };

                // 辞書に追加
                activeInfectionBars[companion] = infectionBarComponent;

                Debug.Log($"[UIManager] Created infection bar for companion: {companion.name}");
                return infectionBarObj;
            }
            else
            {
                Debug.LogError("[UIManager] CompanionInfectionBar component not found on prefab");
                Destroy(infectionBarObj);
                return null;
            }
        }

        /// <summary>
        /// コンパニオンの感染バーを即座に削除
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        public void RemoveInfectionBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || !activeInfectionBars.ContainsKey(companion))
                return;

            CompanionInfectionBar infectionBar = activeInfectionBars[companion];
            if (infectionBar != null)
            {
                infectionBar.ForceDestroy();
            }

            activeInfectionBars.Remove(companion);
            Debug.Log($"[UIManager] Removed infection bar for companion: {companion.name}");
        }

        /// <summary>
        /// コンパニオンの感染バーを削除（フェードアウト付き）
        /// </summary>
        /// <param name="companion">対象のコンパニオン</param>
        public void HideInfectionBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion == null || !activeInfectionBars.ContainsKey(companion))
                return;

            CompanionInfectionBar infectionBar = activeInfectionBars[companion];
            if (infectionBar != null)
            {
                // フェードアウト後に削除
                infectionBar.OnCompanionDestroyed();
            }

            Debug.Log($"[UIManager] Hiding infection bar for companion: {companion.name}");
        }

        /// <summary>
        /// 感染バーが削除された時の処理
        /// </summary>
        /// <param name="companion">感染バーが削除されたコンパニオン</param>
        private void OnInfectionBarDestroyed(KowloonBreak.Characters.CompanionAI companion)
        {
            if (companion != null && activeInfectionBars.ContainsKey(companion))
            {
                activeInfectionBars.Remove(companion);
                Debug.Log($"[UIManager] Infection bar destroyed for companion: {companion.name}");
            }
        }

        /// <summary>
        /// 全てのコンパニオン感染バーをクリア
        /// </summary>
        public void ClearAllCompanionInfectionBars()
        {
            var companionsToRemove = new List<KowloonBreak.Characters.CompanionAI>(activeInfectionBars.Keys);

            foreach (var companion in companionsToRemove)
            {
                RemoveInfectionBarForCompanion(companion);
            }

            activeInfectionBars.Clear();
        }

        /// <summary>
        /// 指定されたコンパニオンの感染バーが存在するかチェック
        /// </summary>
        /// <param name="companion">チェック対象のコンパニオン</param>
        /// <returns>感染バーが存在する場合true</returns>
        public bool HasInfectionBarForCompanion(KowloonBreak.Characters.CompanionAI companion)
        {
            return companion != null && activeInfectionBars.ContainsKey(companion);
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
            // GameOverUIが見つからない場合は検索
            if (gameOverUI == null)
            {
                Debug.Log("[UIManager] GameOverUI not assigned, searching in scene...");
                gameOverUI = FindObjectOfType<GameOverUI>();
            }

            if (gameOverUI != null)
            {
                // 既存のイベント購読を一度解除（重複防止）
                try
                {
                    gameOverUI.OnRetryRequested -= HandleRetryRequested;
                    gameOverUI.OnMainMenuRequested -= HandleMainMenuRequested;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UIManager] Error unsubscribing Game Over events: {ex.Message}");
                }

                // Game Over UIのイベントを購読
                gameOverUI.OnRetryRequested += HandleRetryRequested;
                gameOverUI.OnMainMenuRequested += HandleMainMenuRequested;

                Debug.Log("[UIManager] Game Over system initialized successfully");
            }
            else
            {
                Debug.LogWarning("[UIManager] GameOverUI component not found in scene - fallback mode will be used");
            }
        }

        /// <summary>
        /// ゲームオーバーUIを表示
        /// </summary>
        public void ShowGameOver()
        {
            // ゲームオーバー時にすべてのUIパネルを強制的に閉じる
            Debug.Log("[UIManager] Forcing all panels to close before showing Game Over");
            CloseAllPanels();

            // コンパニオン関連の状態もリセット
            selectedCompanion = null;
            currentNearbyCompanion = null;

            // GameOverUIが設定されていない場合は検索
            if (gameOverUI == null)
            {
                gameOverUI = FindObjectOfType<GameOverUI>();
            }

            // ゲームオーバーUIを表示
            if (gameOverUI != null)
            {
                Debug.Log("[UIManager] Showing Game Over screen");
                gameOverUI.ShowGameOverUI();
            }
            else
            {
                Debug.LogWarning("[UIManager] GameOverUI not found - using fallback mode");

                // フォールバック: シンプルなゲームオーバー処理
                ShowFallbackGameOver();
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
            Debug.Log("[UIManager] Retry requested - restarting game");

            // GameManagerのRestartGame()を呼び出してシーンをリロード
            if (gameManager != null)
            {
                gameManager.RestartGame();
            }
            else
            {
                Debug.LogWarning("[UIManager] GameManager not found - direct scene reload");
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
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
        /// UI要素（スライダー、テキストなど）の参照を再取得
        /// </summary>
        private void RefindUIElements()
        {
            Debug.Log("[UIManager] Re-finding UI elements in new scene");

            // MainHUD再検索（名前検索も試行）
            RefindMainHUD();

            // スライダー要素を再検索
            RefindSliders();

            // テキスト要素を再検索
            RefindTextElements();

            // コンテナ要素を再検索
            RefindContainers();

            Debug.Log("[UIManager] UI elements refinding completed");
        }

        /// <summary>
        /// MainHUDを再検索
        /// </summary>
        private void RefindMainHUD()
        {
            if (mainHUD == null)
            {
                mainHUD = GameObject.Find("MainHUD");
            }

            if (mainHUD == null)
            {
                // HUD関連の名前で検索
                var hudNames = new[] { "HUD", "MainUI", "PlayerHUD", "UI_Main", "Canvas_HUD" };
                foreach (var hudName in hudNames)
                {
                    mainHUD = GameObject.Find(hudName);
                    if (mainHUD != null)
                    {
                        Debug.Log($"[UIManager] Found MainHUD by alternative name: {hudName}");
                        break;
                    }
                }
            }

            if (mainHUD == null)
            {
                // Canvas内でHUD関連オブジェクトを検索
                var canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    var hudObj = canvas.transform.Find("MainHUD") ??
                                canvas.transform.Find("HUD") ??
                                canvas.transform.Find("PlayerHUD");
                    if (hudObj != null)
                    {
                        mainHUD = hudObj.gameObject;
                        Debug.Log($"[UIManager] Found MainHUD in Canvas: {canvas.name}");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// スライダー要素を再検索
        /// </summary>
        private void RefindSliders()
        {
            Debug.Log("[UIManager] Re-finding sliders");

            // MainHUDから検索
            if (mainHUD != null)
            {
                var sliders = mainHUD.GetComponentsInChildren<Slider>();
                foreach (var slider in sliders)
                {
                    if (slider.name.Contains("Health") || slider.name.Contains("HP"))
                    {
                        healthSlider = slider;
                        // ヘルススライダーの入力を無効化してコントローラー操作を防ぐ
                        healthSlider.interactable = false;
                    }
                    else if (slider.name.Contains("Stamina") || slider.name.Contains("SP"))
                    {
                        staminaSlider = slider;
                        // スタミナスライダーの入力を無効化
                        staminaSlider.interactable = false;
                    }
                    else if (slider.name.Contains("Infection") || slider.name.Contains("Virus"))
                    {
                        infectionSlider = slider;
                        // 感染スライダーの入力を無効化
                        infectionSlider.interactable = false;
                    }
                    else if (slider.name.Contains("Stealth") || slider.name.Contains("Hide"))
                    {
                        stealthSlider = slider;
                        // ステルススライダーの入力を無効化
                        stealthSlider.interactable = false;
                    }
                }

                // インデックスベースのフォールバック
                if (sliders.Length >= 1 && healthSlider == null)
                {
                    healthSlider = sliders[0];
                    healthSlider.interactable = false;
                }
                if (sliders.Length >= 2 && staminaSlider == null)
                {
                    staminaSlider = sliders[1];
                    staminaSlider.interactable = false;
                }
                if (sliders.Length >= 3 && infectionSlider == null)
                {
                    infectionSlider = sliders[2];
                    infectionSlider.interactable = false;
                }
                if (sliders.Length >= 4 && stealthSlider == null)
                {
                    stealthSlider = sliders[3];
                    stealthSlider.interactable = false;
                }
            }

            // MainHUDで見つからない場合は全Canvas検索
            if (healthSlider == null || infectionSlider == null)
            {
                Debug.Log("[UIManager] Sliders not found in MainHUD, searching all Canvases");
                SearchSlidersInAllCanvases();
            }

            // 重要なスライダーのみ確認
            if (healthSlider == null) Debug.LogWarning("[UIManager] Health Slider not found");
            if (infectionSlider == null) Debug.LogWarning("[UIManager] Infection Slider not found");
        }

        /// <summary>
        /// 全Canvas内でスライダーを検索
        /// </summary>
        private void SearchSlidersInAllCanvases()
        {
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var allSliders = canvas.GetComponentsInChildren<Slider>();
                foreach (var slider in allSliders)
                {
                    if (healthSlider == null && (slider.name.Contains("Health") || slider.name.Contains("HP")))
                    {
                        healthSlider = slider;
                        healthSlider.interactable = false;
                    }
                    else if (infectionSlider == null && (slider.name.Contains("Infection") || slider.name.Contains("Virus")))
                    {
                        infectionSlider = slider;
                        infectionSlider.interactable = false;
                    }
                    else if (staminaSlider == null && (slider.name.Contains("Stamina") || slider.name.Contains("SP")))
                    {
                        staminaSlider = slider;
                        staminaSlider.interactable = false;
                    }
                    else if (stealthSlider == null && (slider.name.Contains("Stealth") || slider.name.Contains("Hide")))
                    {
                        stealthSlider = slider;
                        stealthSlider.interactable = false;
                    }
                }
            }
        }

        /// <summary>
        /// テキスト要素を再検索
        /// </summary>
        private void RefindTextElements()
        {
            if (mainHUD != null)
            {
                var texts = mainHUD.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("Day")) dayText = text;
                    else if (text.name.Contains("Phase")) phaseText = text;
                    else if (text.name.Contains("Time")) timeText = text;
                    else if (text.name.Contains("Food")) foodText = text;
                    else if (text.name.Contains("Water")) waterText = text;
                    else if (text.name.Contains("Medicine")) medicineText = text;
                    else if (text.name.Contains("Materials")) materialsText = text;
                }
            }
        }

        /// <summary>
        /// コンテナ要素を再検索
        /// </summary>
        private void RefindContainers()
        {
            // Notification関連の要素を再検索
            if (notificationContainer == null)
            {
                var canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    var container = canvas.transform.Find("NotificationContainer") ??
                                   canvas.transform.Find("Notifications") ??
                                   canvas.transform.Find("NotificationPanel");
                    if (container != null)
                    {
                        notificationContainer = container;
                        Debug.Log($"[UIManager] Found NotificationContainer in Canvas: {canvas.name}");
                        break;
                    }
                }
            }

            // DamageContainer再検索
            if (damageContainer == null)
            {
                var canvases = FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    var container = canvas.transform.Find("DamageContainer") ??
                                   canvas.transform.Find("DamageNumbers") ??
                                   canvas.transform.Find("FloatingText");
                    if (container != null)
                    {
                        damageContainer = container;
                        Debug.Log($"[UIManager] Found DamageContainer in Canvas: {canvas.name}");
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// フォールバック用のシンプルなゲームオーバー処理
        /// </summary>
        private void ShowFallbackGameOver()
        {
            Debug.Log("[UIManager] Using fallback Game Over handling");

            // 時間を停止
            Time.timeScale = 0f;

            // カーソルを表示
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 通知でリトライ方法を表示
            ShowNotification("ゲームオーバー！ Rキーでリトライ", NotificationType.Error);

            // キーボード入力でリトライを受け付ける
            StartCoroutine(HandleFallbackRetryInput());
        }

        /// <summary>
        /// フォールバック時のリトライ入力処理
        /// </summary>
        private System.Collections.IEnumerator HandleFallbackRetryInput()
        {
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Debug.Log("[UIManager] Fallback retry triggered by R key");
                    HandleRetryRequested();
                    yield break;
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("[UIManager] Fallback quit triggered by ESC key");
                    Application.Quit();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// ゲームオーバー状態かどうか
        /// </summary>
        public bool IsGameOverActive => gameOverUI != null && gameOverUI.IsGameOverActive;

        #endregion

        #region Infected Interaction System

        /// <summary>
        /// 感染インタラクションUIを表示
        /// </summary>
        /// <param name="infectedCharacter">感染したコンパニオン</param>
        public void ShowInfectedInteractionUI(KowloonBreak.Characters.CompanionCharacter infectedCharacter)
        {
            if (infectedInteractionUI != null)
            {
                // 感染インタラクションUI表示時は通常のインタラクトプロンプトを非表示
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }

                infectedInteractionUI.ShowInteractionUI(infectedCharacter);
            }
            else
            {
                Debug.LogError("[UIManager] InfectedInteractionUI not assigned!");
            }
        }

        /// <summary>
        /// 感染インタラクションUIを非表示
        /// </summary>
        public void HideInfectedInteractionUI()
        {
            if (infectedInteractionUI != null)
            {
                infectedInteractionUI.HideUI();

                // 感染インタラクションUI非表示時はインタラクトプロンプトの状態を再評価
                // Update()で自動的に更新されるので、ここでは特別な処理は不要
            }
        }

        /// <summary>
        /// 感染インタラクションUIが表示されているかチェック
        /// </summary>
        /// <returns>UIが表示されている場合true</returns>
        public bool IsInfectedInteractionUIActive()
        {
            return infectedInteractionUI != null && infectedInteractionUI.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// InfectedInteractionUIコンポーネントへの参照を取得
        /// </summary>
        public InfectedInteractionUI InfectedInteractionUI => infectedInteractionUI;

        /// <summary>
        /// プレイヤー操作をブロックするUIが開いているかチェック
        /// </summary>
        /// <returns>操作ブロック用UIが開いている場合true</returns>
        public bool IsPlayerInputBlocked()
        {
            return IsPanelOpen("Inventory") ||
                   IsPanelOpen("CompanionCommand") ||
                   IsInfectedInteractionUIActive();
        }

        /// <summary>
        /// UI表示に応じてカーソルとTimeScaleを制御
        /// </summary>
        private void UpdateUIState()
        {
            bool isUIOpen = IsPlayerInputBlocked();

            if (isUIOpen)
            {
                // UI表示時: カーソル表示
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // InfectedInteractionUI以外は時間停止
                if (!IsInfectedInteractionUIActive())
                {
                    Time.timeScale = 0f;
                }
            }
            else
            {
                // UI非表示時: カーソル非表示、時間再開
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
            }
        }

        #endregion

        #region Dialog Cancel Input Management

        /// <summary>
        /// 統一的なダイアログキャンセル入力処理
        /// </summary>
        /// <param name="inputManager">InputManagerインスタンス</param>
        private void HandleDialogCancelInput(KowloonBreak.Core.InputManager inputManager)
        {
            if (inputManager == null || !inputManager.IsCancelPressed()) return;

            // キャンセル可能なパネルの優先順位順で処理
            var cancelableDialogs = new[]
            {
                "CompanionCommand",
                "Inventory"
            };

            foreach (var panelName in cancelableDialogs)
            {
                if (IsPanelOpen(panelName))
                {
                    ClosePanel(panelName);
                    return; // 最初に見つかったダイアログのみ閉じる
                }
            }

            // 感染インタラクションUIが開いている場合
            if (IsInfectedInteractionUIActive())
            {
                HideInfectedInteractionUI();
            }
        }

        #endregion

        #region Inventory UI Updates

        /// <summary>
        /// ツールインベントリスロットの表示を更新
        /// </summary>
        private void UpdateToolInventoryUI(int slotIndex, InventorySlot slot)
        {
            Debug.Log($"[UIManager] Tool inventory slot {slotIndex} updated: {(slot.IsEmpty ? "Empty" : $"{slot.ItemData.itemName} x{slot.Quantity}")}");

            // TODO: 実際のツールインベントリUIコンポーネントを取得して更新
            // Example: toolInventoryUI?.UpdateSlot(slotIndex, slot);
        }

        /// <summary>
        /// マテリアルインベントリスロットの表示を更新
        /// </summary>
        private void UpdateMaterialInventoryUI(int slotIndex, InventorySlot slot)
        {
            Debug.Log($"[UIManager] Material inventory slot {slotIndex} updated: {(slot.IsEmpty ? "Empty" : $"{slot.ItemData.itemName} x{slot.Quantity}")}");

            // TODO: 実際のマテリアルインベントリUIコンポーネントを取得して更新
            // Example: materialInventoryUI?.UpdateSlot(slotIndex, slot);
        }

        /// <summary>
        /// アイテム追加時の通知表示
        /// </summary>
        private void OnInventoryItemAdded(ItemData itemData, int quantity)
        {
            string message = $"{itemData.itemName} x{quantity} を獲得しました";
            ShowNotification(message, NotificationType.Success);
            Debug.Log($"[UIManager] Item added: {message}");
        }

        /// <summary>
        /// アイテム削除時の通知表示
        /// </summary>
        private void OnInventoryItemRemoved(ItemData itemData, int quantity)
        {
            string message = $"{itemData.itemName} x{quantity} を消費しました";
            ShowNotification(message, NotificationType.Info);
            Debug.Log($"[UIManager] Item removed: {message}");
        }

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