using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Companion Command System")]
        [SerializeField] private GameObject commandButtonPrefab;
        [SerializeField] private Transform commandButtonContainer;
        [SerializeField] private Text companionNameText;
        [SerializeField] private Text companionStatusText;
        [SerializeField] private Text trustLevelText;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask companionLayerMask = -1;

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
            
            SubscribeToEvents();
            UpdateUI();
            InitializeCompanionCommandSystem();
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
            if (inputManager == null) return;

            // インベントリ開閉
            if (inputManager.IsInventoryPressed())
            {
                TogglePanel("Inventory");
            }

            // エスケープキーでパネルを閉じる
            if (inputManager.IsMenuPressed() && IsAnyPanelOpen)
            {
                CloseAllPanels();
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
            
            // Companion command input (InputManager経由)
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

            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
                return;

            // World座標をScreen座標に変換
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
            
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
            }
        }

        private void UpdateCompanionCommandSystem()
        {
            if (player == null) return;

            CheckForNearbyCompanions();
            UpdateInteractionPrompt();
        }

        private void CheckForNearbyCompanions()
        {
            if (player == null) return;

            KowloonBreak.Characters.CompanionAI nearestCompanion = null;
            float nearestDistance = float.MaxValue;

            Collider[] companionsInRange = Physics.OverlapSphere(player.position, interactionRange, companionLayerMask);

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

            currentNearbyCompanion = nearestCompanion;
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null) return;

            bool shouldShowPrompt = currentNearbyCompanion != null && !IsPanelOpen("CompanionCommand");
            
            if (interactionPrompt.activeInHierarchy != shouldShowPrompt)
            {
                interactionPrompt.SetActive(shouldShowPrompt);
            }

            if (shouldShowPrompt && interactionPromptText != null)
            {
                var companionChar = currentNearbyCompanion.GetComponent<KowloonBreak.Characters.CompanionCharacter>();
                string companionName = companionChar?.Name ?? "仲間";
                string keyName = GetCompanionCommandKeyName();
                interactionPromptText.text = $"{keyName}で{companionName}に命令";
            }
        }

        private void HandleCompanionCommandInput()
        {
            if (!IsPanelOpen("CompanionCommand") && currentNearbyCompanion != null)
            {
                OpenCompanionCommandUI(currentNearbyCompanion);
            }
            else if (IsPanelOpen("CompanionCommand"))
            {
                ClosePanel("CompanionCommand");
            }
        }

        public void OpenCompanionCommandUI(KowloonBreak.Characters.CompanionAI companion)
        {
            selectedCompanion = companion;
            OpenPanel("CompanionCommand");
            
            if (IsPanelOpen("CompanionCommand"))
            {
                UpdateCompanionInfo();
                GenerateCommandButtons();
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
                companionStatusText.text = $"状態: {status}";
            }

            if (trustLevelText != null)
            {
                int trustLevel = companionChar.TrustLevel;
                int intelligenceLevel = selectedCompanion.IntelligenceLevel;
                trustLevelText.text = $"信頼度: {trustLevel} (知能レベル: {intelligenceLevel})";
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
            
            foreach (KowloonBreak.Characters.CompanionCommand command in System.Enum.GetValues(typeof(KowloonBreak.Characters.CompanionCommand)))
            {
                if (selectedCompanion.CanExecuteCommand(command))
                {
                    commands.Add(command);
                }
            }
            
            return commands;
        }

        private void CreateCommandButton(KowloonBreak.Characters.CompanionCommand command)
        {
            var buttonObj = Instantiate(commandButtonPrefab, commandButtonContainer);
            var button = buttonObj.GetComponent<Button>();
            var buttonText = buttonObj.GetComponentInChildren<Text>();
            
            if (buttonText != null)
            {
                buttonText.text = GetCommandDisplayText(command);
            }

            if (button != null)
            {
                button.onClick.AddListener(() => ExecuteCompanionCommand(command));
                activeCommandButtons.Add(button);
            }
        }

        private string GetCommandDisplayText(KowloonBreak.Characters.CompanionCommand command)
        {
            var commandDescriptions = new Dictionary<KowloonBreak.Characters.CompanionCommand, string>
            {
                { KowloonBreak.Characters.CompanionCommand.Follow, "付いてこい" },
                { KowloonBreak.Characters.CompanionCommand.Stay, "ここで待て" },
                { KowloonBreak.Characters.CompanionCommand.Attack, "攻撃しろ" },
                { KowloonBreak.Characters.CompanionCommand.Defend, "守備しろ" },
                { KowloonBreak.Characters.CompanionCommand.MoveTo, "ここに移動しろ" },
                { KowloonBreak.Characters.CompanionCommand.Scout, "偵察に行け" },
                { KowloonBreak.Characters.CompanionCommand.Flank, "側面を取れ" },
                { KowloonBreak.Characters.CompanionCommand.Support, "援護しろ" },
                { KowloonBreak.Characters.CompanionCommand.Retreat, "撤退しろ" },
                { KowloonBreak.Characters.CompanionCommand.Advanced, "戦術行動" }
            };

            string description = commandDescriptions.ContainsKey(command) ? 
                commandDescriptions[command] : command.ToString();
                
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
                KowloonBreak.Characters.CompanionCommand.MoveTo => 3,
                KowloonBreak.Characters.CompanionCommand.Scout => 3,
                KowloonBreak.Characters.CompanionCommand.Flank => 4,
                KowloonBreak.Characters.CompanionCommand.Support => 4,
                KowloonBreak.Characters.CompanionCommand.Retreat => 5,
                KowloonBreak.Characters.CompanionCommand.Advanced => 5,
                _ => 1
            };
        }

        private void ExecuteCompanionCommand(KowloonBreak.Characters.CompanionCommand command)
        {
            if (selectedCompanion == null) return;

            bool success = false;

            switch (command)
            {
                case KowloonBreak.Characters.CompanionCommand.MoveTo:
                    Vector3 movePosition = player.position + player.forward * 5f;
                    success = selectedCompanion.ExecuteCommand(command, movePosition);
                    break;

                case KowloonBreak.Characters.CompanionCommand.Attack:
                    GameObject nearestEnemy = FindNearestEnemyForCompanion();
                    if (nearestEnemy != null)
                    {
                        success = selectedCompanion.ExecuteCommand(command, target: nearestEnemy);
                    }
                    else
                    {
                        ShowNotification("攻撃対象が見つかりません", NotificationType.Warning);
                        ClosePanel("CompanionCommand");
                        return;
                    }
                    break;

                default:
                    success = selectedCompanion.ExecuteCommand(command);
                    break;
            }

            if (success)
            {
                var commandDescriptions = new Dictionary<KowloonBreak.Characters.CompanionCommand, string>
                {
                    { KowloonBreak.Characters.CompanionCommand.Follow, "付いてこい" },
                    { KowloonBreak.Characters.CompanionCommand.Stay, "ここで待て" },
                    { KowloonBreak.Characters.CompanionCommand.Attack, "攻撃しろ" },
                    { KowloonBreak.Characters.CompanionCommand.Defend, "守備しろ" },
                    { KowloonBreak.Characters.CompanionCommand.MoveTo, "ここに移動しろ" },
                    { KowloonBreak.Characters.CompanionCommand.Scout, "偵察に行け" },
                    { KowloonBreak.Characters.CompanionCommand.Flank, "側面を取れ" },
                    { KowloonBreak.Characters.CompanionCommand.Support, "援護しろ" },
                    { KowloonBreak.Characters.CompanionCommand.Retreat, "撤退しろ" },
                    { KowloonBreak.Characters.CompanionCommand.Advanced, "戦術行動" }
                };
                
                string commandName = commandDescriptions.ContainsKey(command) ? 
                    commandDescriptions[command] : command.ToString();
                ShowNotification($"命令実行: {commandName}", NotificationType.Success);
            }
            else
            {
                ShowNotification("命令を実行できませんでした", NotificationType.Error);
            }

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
                if (collider.CompareTag("Enemy") || collider.GetComponent<KowloonBreak.Enemies.EnemyBase>() != null)
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