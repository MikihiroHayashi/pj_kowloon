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

        private Dictionary<string, GameObject> activePanels;
        private List<GameObject> activeNotifications;
        private GameManager gameManager;
        private EnhancedResourceManager resourceManager;
        private InfectionManager infectionManager;
        private KowloonBreak.Player.EnhancedPlayerController enhancedPlayerController;

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
            
            enhancedPlayerController = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
            
            SubscribeToEvents();
            UpdateUI();
        }

        private void Update()
        {
            UpdateHUD();
            HandleInput();
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
            // インベントリ開閉 (I キー)
            if (Input.GetKeyDown(KeyCode.I))
            {
                TogglePanel("Inventory");
            }

            // エスケープキーでパネルを閉じる
            if (Input.GetKeyDown(KeyCode.Escape) && IsAnyPanelOpen)
            {
                CloseAllPanels();
            }

            // その他のUIショートカット
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
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}