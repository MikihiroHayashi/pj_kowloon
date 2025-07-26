using System;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Exploration;
using KowloonBreak.UI;
using KowloonBreak.Managers;

namespace KowloonBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class EnhancedPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Camera Settings")]
        [SerializeField] private UnityEngine.Camera playerCamera;
        [SerializeField] private Transform cameraFollowTarget;

        [Header("Stealth System")]
        [SerializeField] private float stealthRadius = 5f;
        [SerializeField] private float noiseLevel = 0f;
        [SerializeField] private float maxNoiseLevel = 100f;
        [SerializeField] private LayerMask enemyLayers = -1;

        [Header("Health System")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        
        [Header("Stamina System")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 20f;
        [SerializeField] private float runStaminaCost = 30f;
        
        [Header("Infection System")]
        [SerializeField] private bool isInfected = false;
        [SerializeField] private float infectionLevel = 0f;

        [Header("Interaction System")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayers = -1;
        [SerializeField] private KeyCode interactionKey = KeyCode.F;
        [SerializeField] private KeyCode flashlightKey = KeyCode.T;
        [SerializeField] private KeyCode useToolKey = KeyCode.E;
        
        [Header("Movement Input Settings")]
        [SerializeField] private KeyCode runToggleKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchToggleKey = KeyCode.LeftControl;
        [SerializeField] private bool runToggleMode = false; // false: Hold to run, true: Toggle run
        [SerializeField] private bool crouchToggleMode = true; // false: Hold to crouch, true: Toggle crouch
        
        [Header("Tool Usage System")]
        [SerializeField] private float toolUsageRange = 1.5f;
        [SerializeField] private LayerMask destructibleLayers = -1;
        [SerializeField] private float toolUsageCooldown = 0.5f;
        [SerializeField] private Transform toolUsagePoint;
        
        [Header("Item Pickup System")]
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private LayerMask itemLayers = -1;
        [SerializeField] private bool autoPickup = true;

        [Header("Audio")]
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float footstepInterval = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject flashlight;
        [SerializeField] private ParticleSystem breathingParticles;
        [SerializeField] private Transform cameraShakeTarget;
        [SerializeField] private float cameraShakeIntensity = 0.1f;
        [SerializeField] private Renderer playerRenderer;

        private CharacterController characterController;
        private AudioSource audioSource;
        private CharacterStats playerStats;
        private HealthStatus playerHealth;
        private PlayerAnimatorController animatorController;
        
        // Health Events
        public event Action<float> OnHealthChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<bool> OnInfectionStatusChanged;
        public event Action OnPlayerDeath;
        private InfectionStatus playerInfection;
        private EnhancedResourceManager resourceManager;
        private ToolSelectionHUDController toolSelectionHUD;

        private Vector3 moveDirection;
        private Vector3 velocity;
        private float currentStamina;
        private float noiseTimer;
        private float footstepTimer;
        private bool isRunning;
        private bool isCrouching;
        private bool isGrounded;
        private bool flashlightEnabled;
        private bool canMove = true;
        
        // 移動モード状態管理
        private bool runModeEnabled = false; // 走行モード有効/無効
        private bool crouchModeEnabled = false; // しゃがみモード有効/無効
        
        // 道具使用システム関連
        private float lastToolUsageTime;
        private bool isUsingTool;
        private InventorySlot currentUsedTool;
        
        // 道具選択関連
        private int selectedToolIndex = 0;

        // Health Properties
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0f;
        
        // Stamina Properties
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercentage => currentStamina / maxStamina;
        
        // Infection Properties
        public bool IsInfected => isInfected;
        public float InfectionLevel => infectionLevel;
        
        // Movement Properties
        public bool IsRunning => isRunning;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => moveDirection.magnitude > 0.1f;
        public float NoiseLevel => noiseLevel;
        public CharacterStats Stats => playerStats;
        public InventorySlot SelectedTool => resourceManager?.GetToolSlot(selectedToolIndex);
        public int SelectedToolIndex => selectedToolIndex;
        
        // Movement Mode Properties
        public bool RunModeEnabled => runModeEnabled;
        public bool CrouchModeEnabled => crouchModeEnabled;
        public float CurrentMoveSpeed => GetCurrentSpeed();

        public event Action<bool> OnRunStateChanged;
        public event Action<bool> OnCrouchStateChanged;
        public event Action<float> OnNoiseChanged;
        public event Action<int, InventorySlot> OnToolSelected;

        private void Start()
        {
            InitializePlayer();
            SetupInput();
        }

        private void Update()
        {
            if (!canMove) return;

            HandleInput();
            HandleMovement();
            HandleStamina();
            HandleHealth();
            HandleInfection();
            HandleNoise();
            HandleAudio();
            HandleToolUsage();
            HandleItemPickup();
        }

        private void InitializePlayer()
        {
            characterController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            animatorController = GetComponent<PlayerAnimatorController>();
            
            if (playerCamera == null)
            {
                playerCamera = UnityEngine.Camera.main;
            }

            SetupCameraFollowTarget();
            InitializeStats();
            InitializeInventorySystem();
            
            // Health and Stamina initialization
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            
            // Fire initial events
            OnHealthChanged?.Invoke(HealthPercentage);
            OnStaminaChanged?.Invoke(StaminaPercentage);
            
            // 道具使用ポイントを設定
            SetupToolUsagePoint();
            
            // プレイヤーRendererを自動取得
            if (playerRenderer == null)
            {
                playerRenderer = GetComponentInChildren<Renderer>();
            }
            
            // アニメーターコントローラーの確認
            if (animatorController == null)
            {
                Debug.LogWarning("[EnhancedPlayerController] PlayerAnimatorController not found! Tool usage animations will not work.");
            }
            
            // EnhancedPlayerController初期化完了
        }

        private void SetupCameraFollowTarget()
        {
            // カメラフォローターゲットは手動で設定
        }

        private void InitializeStats()
        {
            playerStats = new CharacterStats();
            playerHealth = new HealthStatus();
            playerInfection = new InfectionStatus();
            
            playerStats.OnDeath += HandlePlayerDeath;
            playerInfection.OnTurnedZombie += HandlePlayerTurnedZombie;
        }

        private void SetupInput()
        {
            if (flashlight != null)
            {
                flashlight.SetActive(false);
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(interactionKey))
            {
                TryInteract();
            }
            
            if (Input.GetKeyDown(flashlightKey))
            {
                ToggleFlashlight();
            }
            
            // 走行モード入力処理
            HandleRunInput();
            
            // しゃがみモード入力処理
            HandleCrouchInput();
            
            // 道具選択 (1-8キー)
            HandleToolSelection();
            
            // 道具使用 (Eキー)
            if (Input.GetKeyDown(useToolKey))
            {
                TryUseTool();
            }
        }

        private void HandleMovement()
        {
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // ワールド空間での移動方向を計算
            Vector3 direction = new Vector3(horizontal, 0f, vertical);
            moveDirection = direction.normalized;
            
            // 移動方向にキャラクターを向ける
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            
            float currentSpeed = GetCurrentSpeed();
            Vector3 move = moveDirection * currentSpeed;
            
            characterController.Move(move * Time.deltaTime);
            
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
            
            UpdateMovementEffects();
            UpdateAnimatorStates();
        }

        private float GetCurrentSpeed()
        {
            float baseSpeed = walkSpeed;
            
            // 移動モードに応じて速度を決定
            if (crouchModeEnabled)
            {
                baseSpeed = crouchSpeed;
                Debug.Log($"[EnhancedPlayerController] Using crouch speed: {baseSpeed}");
            }
            else if (runModeEnabled && currentStamina > 0)
            {
                baseSpeed = runSpeed;
                Debug.Log($"[EnhancedPlayerController] Using run speed: {baseSpeed}");
            }
            else
            {
                baseSpeed = walkSpeed;
                Debug.Log($"[EnhancedPlayerController] Using walk speed: {baseSpeed}");
            }
            
            // 健康状態とインフェクションによるペナルティ
            float healthPenalty = currentHealth < maxHealth * 0.5f ? 0.7f : 1f;
            float infectionPenalty = isInfected ? (1f - (infectionLevel / 100f * 0.5f)) : 1f;
            
            float finalSpeed = baseSpeed * healthPenalty * infectionPenalty;
            
            return finalSpeed;
        }


        private void HandleStamina()
        {
            // スタミナ消費条件：走行モードが有効 かつ 実際に移動している
            bool shouldConsumeStamina = runModeEnabled && IsMoving;
            
            // インフェクション状態でも追加消費
            bool infectionConsumption = isInfected && infectionLevel > 50f;
            
            if (shouldConsumeStamina || infectionConsumption)
            {
                float totalConsumption = 0f;
                
                // 走行によるスタミナ消費
                if (shouldConsumeStamina)
                {
                    totalConsumption += runStaminaCost;
                }
                
                // インフェクションによる追加消費
                if (infectionConsumption)
                {
                    float infectionCost = runStaminaCost * 0.5f;
                    totalConsumption += infectionCost;
                }
                
                // スタミナを消費
                if (currentStamina > 0f)
                {
                    currentStamina -= totalConsumption * Time.deltaTime;
                    currentStamina = Mathf.Max(0f, currentStamina);
                    
                    // スタミナが尽きた場合の処理
                    if (currentStamina <= 0f && runModeEnabled)
                    {
                        SetRunMode(false);
                        
                        // UI通知
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ShowNotification("スタミナが切れました", NotificationType.Warning);
                        }
                    }
                }
            }
            else if (currentStamina < maxStamina)
            {
                // スタミナ回復処理
                float regenRate = staminaRegenRate;
                
                // インフェクション状態では回復速度が半減
                if (isInfected)
                {
                    regenRate *= 0.5f;
                }
                
                // しゃがみ中は回復速度が向上
                if (crouchModeEnabled && !IsMoving)
                {
                    regenRate *= 1.5f;
                }
                
                float previousStamina = currentStamina;
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
                
                // 回復のデバッグ情報（5秒に1回程度）
                if (Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[EnhancedPlayerController] Stamina regenerating: {previousStamina:F1} -> {currentStamina:F1} (rate: {regenRate:F1}/sec)");
                }
            }
            
            // スタミナ変更イベントを発火（絶対値で送信）
            OnStaminaChanged?.Invoke(currentStamina);
        }

        private void HandleNoise()
        {
            float targetNoise = CalculateNoiseLevel();
            noiseLevel = Mathf.Lerp(noiseLevel, targetNoise, Time.deltaTime * 2f);
            
            OnNoiseChanged?.Invoke(noiseLevel);
            
            CheckNearbyEnemies();
        }

        private float CalculateNoiseLevel()
        {
            if (!IsMoving) return 0f;
            
            float baseNoise = 20f; // 通常歩行のノイズレベル
            
            if (runModeEnabled)
            {
                baseNoise = 60f; // 走行時は高いノイズレベル
            }
            else if (crouchModeEnabled)
            {
                baseNoise = 5f; // しゃがみ時は低いノイズレベル
            }
            
            if (playerHealth.Condition == HealthCondition.Injured)
            {
                baseNoise *= 1.3f;
            }
            
            return Mathf.Clamp(baseNoise, 0f, maxNoiseLevel);
        }

        private void CheckNearbyEnemies()
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, stealthRadius, enemyLayers);
            
            foreach (var enemy in nearbyEnemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                float detectionChance = (noiseLevel / maxNoiseLevel) * (1f - (distance / stealthRadius));
                
                if (UnityEngine.Random.Range(0f, 1f) < detectionChance * Time.deltaTime)
                {
                    // TODO: Trigger enemy detection
                }
            }
        }

        private void HandleAudio()
        {
            if (IsMoving && isGrounded)
            {
                footstepTimer += Time.deltaTime;
                
                float currentFootstepInterval = footstepInterval;
                
                // 移動モードに応じて足音間隔を調整
                if (runModeEnabled) 
                {
                    currentFootstepInterval *= 0.6f; // 走行時は足音が早い
                }
                else if (crouchModeEnabled) 
                {
                    currentFootstepInterval *= 1.5f; // しゃがみ時は足音が遅い
                }
                
                if (footstepTimer >= currentFootstepInterval)
                {
                    PlayFootstepSound();
                    footstepTimer = 0f;
                }
            }
        }

        private void UpdateMovementEffects()
        {
            if (breathingParticles != null)
            {
                var emission = breathingParticles.emission;
                bool shouldEmit = (currentStamina < maxStamina * 0.3f) || 
                                 (isInfected && infectionLevel >= 50f);
                emission.enabled = shouldEmit;
                
                if (shouldEmit)
                {
                    emission.rateOverTime = 10f + (1f - StaminaPercentage) * 20f;
                }
            }
        }

        /// <summary>
        /// 走行入力を処理
        /// </summary>
        private void HandleRunInput()
        {
            if (runToggleMode)
            {
                // トグルモード: キーを押すたびにON/OFF切り替え
                if (Input.GetKeyDown(runToggleKey))
                {
                    ToggleRunMode();
                }
            }
            else
            {
                // ホールドモード: キーを押している間だけON
                if (Input.GetKeyDown(runToggleKey))
                {
                    SetRunMode(true);
                }
                else if (Input.GetKeyUp(runToggleKey))
                {
                    SetRunMode(false);
                }
            }
        }
        
        /// <summary>
        /// しゃがみ入力を処理
        /// </summary>
        private void HandleCrouchInput()
        {
            if (crouchToggleMode)
            {
                // トグルモード: キーを押すたびにON/OFF切り替え
                if (Input.GetKeyDown(crouchToggleKey))
                {
                    ToggleCrouchMode();
                }
            }
            else
            {
                // ホールドモード: キーを押している間だけON
                if (Input.GetKeyDown(crouchToggleKey))
                {
                    SetCrouchMode(true);
                }
                else if (Input.GetKeyUp(crouchToggleKey))
                {
                    SetCrouchMode(false);
                }
            }
        }
        
        /// <summary>
        /// 走行モードをトグル
        /// </summary>
        public void ToggleRunMode()
        {
            SetRunMode(!runModeEnabled);
        }
        
        /// <summary>
        /// しゃがみモードをトグル
        /// </summary>
        public void ToggleCrouchMode()
        {
            SetCrouchMode(!crouchModeEnabled);
        }
        
        /// <summary>
        /// 走行モードを設定
        /// </summary>
        public void SetRunMode(bool enabled)
        {
            // 走行モードを有効にする場合の条件チェック
            if (enabled)
            {
                // スタミナが少ない場合は走行不可
                if (currentStamina <= 5f) // 最低5のスタミナが必要
                {
                    Debug.Log($"[EnhancedPlayerController] Cannot enable run mode: Low stamina ({currentStamina:F1} <= 5)");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowNotification("スタミナが不足しています", NotificationType.Warning);
                    }
                    return;
                }
                
                // しゃがみ中は走行不可
                if (crouchModeEnabled)
                {
                    Debug.Log("[EnhancedPlayerController] Cannot enable run mode: Currently crouching");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowNotification("しゃがみ中は走行できません", NotificationType.Warning);
                    }
                    return;
                }
            }
            
            bool previousState = runModeEnabled;
            runModeEnabled = enabled;
            isRunning = enabled;
            
            if (previousState != runModeEnabled)
            {
                Debug.Log($"[EnhancedPlayerController] Run mode {(runModeEnabled ? "ENABLED" : "DISABLED")} (Stamina: {currentStamina:F1}/{maxStamina})");
                OnRunStateChanged?.Invoke(isRunning);
                
                // UIフィードバック
                if (UIManager.Instance != null)
                {
                    string message = runModeEnabled ? "走行モード: ON" : "走行モード: OFF";
                    NotificationType notificationType = runModeEnabled ? NotificationType.Info : NotificationType.Info;
                    UIManager.Instance.ShowNotification(message, notificationType);
                }
            }
        }
        
        /// <summary>
        /// しゃがみモードを設定
        /// </summary>
        public void SetCrouchMode(bool enabled)
        {
            bool previousState = crouchModeEnabled;
            crouchModeEnabled = enabled;
            isCrouching = enabled;
            
            // しゃがみ有効時は走行を無効化
            if (enabled && runModeEnabled)
            {
                SetRunMode(false);
            }
            
            // CharacterControllerの高さと中心点を調整
            if (crouchModeEnabled)
            {
                characterController.height = 1f;
                characterController.center = new Vector3(0, 0.5f, 0);
            }
            else
            {
                characterController.height = 2f;
                characterController.center = new Vector3(0, 1f, 0);
            }
            
            if (previousState != crouchModeEnabled)
            {
                Debug.Log($"[EnhancedPlayerController] Crouch mode {(crouchModeEnabled ? "ENABLED" : "DISABLED")}");
                OnCrouchStateChanged?.Invoke(isCrouching);
                
                // UIフィードバック
                if (UIManager.Instance != null)
                {
                    string message = crouchModeEnabled ? "しゃがみモード: ON" : "しゃがみモード: OFF";
                    UIManager.Instance.ShowNotification(message, NotificationType.Info);
                }
            }
        }
        
        // 後方互換性のため旧メソッドを保持
        private void SetRunning(bool running)
        {
            SetRunMode(running);
        }

        private void SetCrouching(bool crouching)
        {
            SetCrouchMode(crouching);
        }


        private void TryInteract()
        {
            var explorationSystem = ExplorationSystem.Instance;
            if (explorationSystem != null && !explorationSystem.IsSearching)
            {
                explorationSystem.TryStartSearch();
                return;
            }
            
            Collider[] interactables = Physics.OverlapSphere(transform.position, interactionRange, interactionLayers);
            
            if (interactables.Length > 0)
            {
                var nearest = GetNearestInteractable(interactables);
                if (nearest != null)
                {
                    var interactable = nearest.GetComponent<IInteractable>();
                    interactable?.Interact(this);
                }
            }
        }

        private Collider GetNearestInteractable(Collider[] interactables)
        {
            Collider nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var interactable in interactables)
            {
                float distance = Vector3.Distance(transform.position, interactable.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }
            
            return nearest;
        }

        private void ToggleFlashlight()
        {
            flashlightEnabled = !flashlightEnabled;
            
            if (flashlight != null)
            {
                flashlight.SetActive(flashlightEnabled);
            }
            
            if (UIManager.Instance != null)
            {
                string message = flashlightEnabled ? "懐中電灯: ON" : "懐中電灯: OFF";
                UIManager.Instance.ShowNotification(message, NotificationType.Info);
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            
            AudioClip footstep = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
            
            // 移動モードに応じてボリュームを調整
            float volume = 0.5f; // デフォルト（歩行）
            if (crouchModeEnabled)
            {
                volume = 0.2f; // しゃがみ時はより小さく（静音性向上）
            }
            else if (runModeEnabled)
            {
                volume = 0.9f; // 走行時はより大きく
            }
            
            audioSource.PlayOneShot(footstep, volume);
        }



        private void HandlePlayerDeath()
        {
            canMove = false;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("あなたは死亡しました...", NotificationType.Error);
            }
            
            // プレイヤー死亡処理完了
        }

        private void HandlePlayerTurnedZombie()
        {
            canMove = false;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("感染が進行し、意識を失った...", NotificationType.Error);
            }
            
            // プレイヤーゾンビ化処理完了
        }

        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            
            // int版のTakeDamageはfloat版を呼び出す
            TakeDamage((float)damage);
            
            if (cameraShakeTarget != null)
            {
                StartCoroutine(CameraShake(0.3f));
            }
        }

        public void Heal(int healAmount)
        {
            playerStats.Heal(healAmount);
        }

        public void SetMovementEnabled(bool enabled)
        {
            canMove = enabled;
        }

        private System.Collections.IEnumerator CameraShake(float duration)
        {
            Vector3 originalPosition = cameraShakeTarget.localPosition;
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * cameraShakeIntensity;
                cameraShakeTarget.localPosition = originalPosition + randomOffset;
                
                yield return null;
            }
            
            cameraShakeTarget.localPosition = originalPosition;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stealthRadius);
        }

        #region Tool Usage System
        
        private void InitializeInventorySystem()
        {
            resourceManager = EnhancedResourceManager.Instance;
            toolSelectionHUD = FindObjectOfType<ToolSelectionHUDController>();
            
            if (toolSelectionHUD != null)
            {
                toolSelectionHUD.OnToolSelected += HandleToolSelectionChanged;
            }
        }
        
        private void SetupToolUsagePoint()
        {
            if (toolUsagePoint == null)
            {
                GameObject toolUsagePointGO = new GameObject("ToolUsagePoint");
                toolUsagePointGO.transform.SetParent(transform);
                toolUsagePointGO.transform.localPosition = Vector3.forward * 1f;
                toolUsagePoint = toolUsagePointGO.transform;
            }
        }
        
        private void HandleToolSelection()
        {
            // 1-8キーで道具選択
            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectTool(i);
                    break;
                }
            }
        }
        
        private void SelectTool(int index)
        {
            selectedToolIndex = index;
            var selectedSlot = resourceManager?.GetToolSlot(selectedToolIndex);
            OnToolSelected?.Invoke(selectedToolIndex, selectedSlot);
            
            // HUDコントローラーにも通知
            if (toolSelectionHUD != null)
            {
                toolSelectionHUD.SelectTool(index);
            }
            
            // 道具選択完了
        }
        
        private void HandleToolSelectionChanged(int index, InventorySlot slot)
        {
            selectedToolIndex = index;
        }
        
        private void HandleToolUsage()
        {
            if (isUsingTool)
            {
                if (Time.time - lastToolUsageTime >= toolUsageCooldown)
                {
                    isUsingTool = false;
                }
            }
        }
        
        private void TryUseTool()
        {
            Debug.Log("[EnhancedPlayerController] TryUseTool called");
            
            if (isUsingTool) 
            {
                Debug.Log("[EnhancedPlayerController] Already using tool, ignoring input");
                return;
            }
            
            if (Time.time - lastToolUsageTime < toolUsageCooldown) 
            {
                Debug.Log($"[EnhancedPlayerController] Tool usage on cooldown: {Time.time - lastToolUsageTime:F2}s < {toolUsageCooldown}s");
                return;
            }
            
            // リソースマネージャーの状態を確認
            if (resourceManager == null)
            {
                Debug.LogError("[EnhancedPlayerController] ResourceManager is null! Cannot get tool slot.");
                return;
            }
            
            Debug.Log($"[EnhancedPlayerController] Selected tool index: {selectedToolIndex}");
            
            var selectedTool = resourceManager.GetToolSlot(selectedToolIndex);
            if (selectedTool == null) 
            {
                Debug.LogError($"[EnhancedPlayerController] Tool slot {selectedToolIndex} is null!");
                
                // 利用可能なツールスロットを確認
                for (int i = 0; i < 8; i++)
                {
                    var slot = resourceManager.GetToolSlot(i);
                    if (slot != null && !slot.IsEmpty)
                    {
                        Debug.Log($"[EnhancedPlayerController] Available tool in slot {i}: {slot.ItemData?.itemName}");
                    }
                }
                return;
            }
            
            if (selectedTool.IsEmpty) 
            {
                Debug.LogWarning("[EnhancedPlayerController] Selected tool slot is empty!");
                
                // 利用可能なツールスロットを確認
                for (int i = 0; i < 8; i++)
                {
                    var slot = resourceManager.GetToolSlot(i);
                    if (slot != null && !slot.IsEmpty)
                    {
                        Debug.Log($"[EnhancedPlayerController] Available tool in slot {i}: {slot.ItemData?.itemName}");
                    }
                }
                return;
            }
            
            if (!selectedTool.ItemData.IsTool()) 
            {
                Debug.Log($"[EnhancedPlayerController] Selected item is not a tool: {selectedTool.ItemData.itemName}");
                return;
            }
            
            Debug.Log($"[EnhancedPlayerController] Using tool: {selectedTool.ItemData.itemName} (Type: {selectedTool.ItemData.toolType})");
            
            isUsingTool = true;
            lastToolUsageTime = Time.time;
            
            // 道具の種類に応じた処理開始
            StartToolUsage(selectedTool);
        }
        
        /// <summary>
        /// 道具使用を開始（道具種別に応じて動作を分岐）
        /// </summary>
        private void StartToolUsage(InventorySlot toolSlot)
        {
            // 使用中の道具データを保存（アニメーションイベントで使用）
            currentUsedTool = toolSlot;
            
            var toolType = toolSlot.ItemData.toolType;
            Debug.Log($"[EnhancedPlayerController] Starting tool usage: {toolSlot.ItemData.itemName} (Type: {toolType})");
            
            // アニメーターコントローラーの確認
            if (animatorController == null)
            {
                Debug.LogError("[EnhancedPlayerController] PlayerAnimatorController is null! Cannot play tool usage animation.");
                
                // 手動でコンポーネントを検索
                animatorController = GetComponent<PlayerAnimatorController>();
                if (animatorController == null)
                {
                    animatorController = GetComponentInChildren<PlayerAnimatorController>();
                }
                
                if (animatorController != null)
                {
                    Debug.Log("[EnhancedPlayerController] Found PlayerAnimatorController after manual search!");
                }
                else
                {
                    Debug.LogError("[EnhancedPlayerController] PlayerAnimatorController not found even after manual search!");
                    return;
                }
            }
            
            // 道具の種類に応じてアニメーションとアクションを決定
            Debug.Log($"[EnhancedPlayerController] Tool type detected: {toolType}");
            
            switch (toolType)
            {
                case ToolType.Pickaxe:
                    // つるはし：掘削アニメーション（Dig）
                    Debug.Log("[EnhancedPlayerController] PICKAXE detected -> Using TriggerDig()");
                    animatorController.TriggerDig();
                    PlayToolUsageEffects(toolSlot.ItemData, "dig");
                    break;
                    
                case ToolType.IronPipe:
                    // 鉄パイプ：攻撃アニメーション（Attack）
                    Debug.Log("[EnhancedPlayerController] IRON PIPE detected -> Using TriggerAttack()");
                    animatorController.TriggerAttack();
                    PlayToolUsageEffects(toolSlot.ItemData, "attack");
                    break;
                    
                default:
                    // デフォルト：攻撃アニメーション
                    Debug.Log($"[EnhancedPlayerController] UNKNOWN tool type {toolType} -> Using TriggerAttack() as default");
                    animatorController.TriggerAttack();
                    PlayToolUsageEffects(toolSlot.ItemData, "attack");
                    break;
            }
        }
        
        /// <summary>
        /// アニメーションイベントから呼ばれる道具使用効果実行
        /// </summary>
        public void ExecuteToolUsageEffect()
        {
            Debug.Log("[EnhancedPlayerController] ExecuteToolUsageEffect called from animation event");
            
            if (currentUsedTool == null || currentUsedTool.IsEmpty) 
            {
                Debug.LogWarning("[EnhancedPlayerController] No current tool available for effect execution");
                return;
            }
            
            var itemData = currentUsedTool.ItemData;
            var toolType = itemData.toolType;
            
            Debug.Log($"[EnhancedPlayerController] Executing tool effect for: {itemData.itemName} (Type: {toolType})");
            
            // 道具の種類に応じて効果を実行
            Debug.Log($"[EnhancedPlayerController] Executing effect for tool type: {toolType}");
            
            switch (toolType)
            {
                case ToolType.Pickaxe:
                    Debug.Log("[EnhancedPlayerController] Executing DIGGING effect for Pickaxe");
                    ExecuteDiggingEffect(currentUsedTool);
                    break;
                    
                case ToolType.IronPipe:
                    Debug.Log("[EnhancedPlayerController] Executing ATTACK effect for IronPipe");
                    ExecuteAttackEffect(currentUsedTool);
                    break;
                    
                default:
                    Debug.Log($"[EnhancedPlayerController] Unknown tool type: {toolType}, executing ATTACK effect as default");
                    ExecuteAttackEffect(currentUsedTool);
                    break;
            }
        }
        
        /// <summary>
        /// 攻撃効果を実行（鉄パイプなど）
        /// </summary>
        private void ExecuteAttackEffect(InventorySlot toolSlot)
        {
            var itemData = toolSlot.ItemData;
            float range = itemData.attackRange;
            float damage = itemData.attackDamage;
            
            Debug.Log($"[EnhancedPlayerController] Executing attack effect: {damage} damage with range: {range}");
            
            // 攻撃範囲内の破壊可能オブジェクトを検索
            Collider[] hitObjects = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);
            
            Debug.Log($"[EnhancedPlayerController] Found {hitObjects.Length} objects in attack range");
            
            bool hitSomething = false;
            
            foreach (var hitCollider in hitObjects)
            {
                var destructible = hitCollider.GetComponent<IDestructible>();
                if (destructible != null)
                {
                    if (destructible.CanBeDestroyedBy(itemData.toolType))
                    {
                        Debug.Log($"[EnhancedPlayerController] Dealing {damage} damage to {hitCollider.name}");
                        destructible.TakeDamage(damage, itemData.toolType);
                        hitSomething = true;
                    }
                    else
                    {
                        Debug.Log($"[EnhancedPlayerController] {hitCollider.name} cannot be destroyed by {itemData.toolType}");
                    }
                }
                else
                {
                    Debug.Log($"[EnhancedPlayerController] {hitCollider.name} is not destructible");
                }
            }
            
            // 道具の耐久度を減らす
            if (hitSomething)
            {
                Debug.Log($"[EnhancedPlayerController] Hit something, reducing tool durability");
                toolSlot.UseDurability(1);
            }
            else
            {
                Debug.Log($"[EnhancedPlayerController] No valid targets hit");
            }
        }
        
        /// <summary>
        /// 掘削効果を実行（つるはしなど）
        /// </summary>
        private void ExecuteDiggingEffect(InventorySlot toolSlot)
        {
            var itemData = toolSlot.ItemData;
            float range = itemData.attackRange;
            float damage = itemData.attackDamage;
            
            Debug.Log($"[EnhancedPlayerController] Executing digging effect: {damage} digging power with range: {range}");
            
            // 掘削範囲内の掘削可能オブジェクトを検索
            Collider[] hitObjects = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);
            
            Debug.Log($"[EnhancedPlayerController] Found {hitObjects.Length} objects in digging range");
            
            bool dugSomething = false;
            
            foreach (var hitCollider in hitObjects)
            {
                var destructible = hitCollider.GetComponent<IDestructible>();
                if (destructible != null)
                {
                    if (destructible.CanBeDestroyedBy(itemData.toolType))
                    {
                        Debug.Log($"[EnhancedPlayerController] Digging {hitCollider.name} with {damage} digging power");
                        destructible.TakeDamage(damage, itemData.toolType);
                        dugSomething = true;
                    }
                    else
                    {
                        Debug.Log($"[EnhancedPlayerController] {hitCollider.name} cannot be dug by {itemData.toolType}");
                    }
                }
                else
                {
                    Debug.Log($"[EnhancedPlayerController] {hitCollider.name} is not diggable");
                }
            }
            
            // 道具の耐久度を減らす
            if (dugSomething)
            {
                Debug.Log($"[EnhancedPlayerController] Dug something, reducing tool durability");
                toolSlot.UseDurability(1);
            }
            else
            {
                Debug.Log($"[EnhancedPlayerController] No valid targets dug");
            }
        }
        
        private void PlayToolUsageEffects(ItemData itemData, string usageType)
        {
            Debug.Log($"[EnhancedPlayerController] Playing {usageType} effects for {itemData.itemName}");
            
            // 道具使用音の再生
            if (audioSource != null)
            {
                // TODO: 道具と使用タイプに応じた音を再生
                switch (usageType)
                {
                    case "attack":
                        // 攻撃音
                        break;
                    case "dig":
                        // 掘削音
                        break;
                    default:
                        // デフォルト音
                        break;
                }
            }
            
            // 道具使用エフェクトの再生
            // TODO: パーティクルエフェクトなどを追加
        }
        
        #endregion
        
        #region Item Pickup System
        
        private void HandleItemPickup()
        {
            if (!autoPickup) return;
            
            Collider[] itemsInRange = Physics.OverlapSphere(transform.position, pickupRange, itemLayers);
            
            foreach (var itemCollider in itemsInRange)
            {
                var droppedItem = itemCollider.GetComponent<DroppedItem>();
                if (droppedItem != null)
                {
                    // DroppedItemは自動的にプレイヤーに反応するため、特別な処理は不要
                    // 必要に応じて追加の処理を実装
                }
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.OnDeath -= HandlePlayerDeath;
            }
            
            if (playerInfection != null)
            {
                playerInfection.OnTurnedZombie -= HandlePlayerTurnedZombie;
            }
            
            if (toolSelectionHUD != null)
            {
                toolSelectionHUD.OnToolSelected -= HandleToolSelectionChanged;
            }
        }
        
        // ===== HEALTH SYSTEM METHODS =====
        
        private void HandleHealth()
        {
            // スタミナが0で走行を続けようとする場合、健康にダメージ
            if (currentStamina <= 0f && runModeEnabled && IsMoving)
            {
                float overtimeDamage = 2f; // 過労ダメージ（1秒あたり2ダメージ）
                TakeDamage(overtimeDamage * Time.deltaTime);
                
                // 過労ダメージの警告（5秒に1回程度）
                if (Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning("[EnhancedPlayerController] Taking overtime damage due to stamina depletion while running!");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowNotification("過労により体力が減少しています！", NotificationType.Error);
                    }
                }
            }
        }
        
        private void HandleInfection()
        {
            if (isInfected)
            {
                infectionLevel += 0.1f * Time.deltaTime;
                
                if (infectionLevel >= 100f)
                {
                    TakeDamage(1f * Time.deltaTime);
                }
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            
            Debug.Log($"[EnhancedPlayerController] TakeDamage called: damage={damage}, currentHealth={currentHealth}");
            
            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);
            
            // ダメージエフェクトを開始
            StartCoroutine(DamageEffect());
            
            OnHealthChanged?.Invoke(HealthPercentage);
            
            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(HealthPercentage);
        }
        
        public void SetInfectionStatus(bool infected)
        {
            if (isInfected != infected)
            {
                isInfected = infected;
                if (!infected)
                {
                    infectionLevel = 0f;
                }
                OnInfectionStatusChanged?.Invoke(isInfected);
            }
        }
        
        public void TreatInfection(float treatmentAmount)
        {
            if (isInfected)
            {
                infectionLevel = Mathf.Max(0f, infectionLevel - treatmentAmount);
                if (infectionLevel <= 0f)
                {
                    SetInfectionStatus(false);
                }
            }
        }
        
        private void Die()
        {
            Debug.Log("[EnhancedPlayerController] Player has died!");
            OnPlayerDeath?.Invoke();
            
            SetMovementEnabled(false);
            
            // PlayerAnimatorControllerでDeathアニメーションを再生
            if (animatorController != null)
            {
                Debug.Log("[EnhancedPlayerController] Triggering Death animation via PlayerAnimatorController");
                animatorController.TriggerDeath();
            }
            else
            {
                Debug.LogWarning("[EnhancedPlayerController] PlayerAnimatorController not found on player");
            }
        }
        
        /// <summary>
        /// 走行可能かどうかを判定
        /// </summary>
        public bool CanRun()
        {
            return currentStamina > 5f && IsAlive && !crouchModeEnabled;
        }
        
        /// <summary>
        /// スタミナの状態情報を取得
        /// </summary>
        public string GetStaminaStatusInfo()
        {
            if (currentStamina <= 0f)
                return "疲労困憊";
            else if (currentStamina <= maxStamina * 0.25f)
                return "疲労";
            else if (currentStamina <= maxStamina * 0.5f)
                return "やや疲労";
            else if (currentStamina >= maxStamina * 0.9f)
                return "絶好調";
            else
                return "良好";
        }
        
        /// <summary>
        /// ダメージエフェクト（マテリアルの_Damage_Amountを一瞬1にする）
        /// </summary>
        private System.Collections.IEnumerator DamageEffect()
        {
            if (playerRenderer != null && playerRenderer.material != null)
            {
                // _Damage_Amountを1に設定
                playerRenderer.material.SetFloat("_Damage_Amount", 1f);
                
                // 0.1秒待機
                yield return new WaitForSeconds(0.1f);
                
                // _Damage_Amountを0に戻す
                if (playerRenderer != null && playerRenderer.material != null)
                {
                    playerRenderer.material.SetFloat("_Damage_Amount", 0f);
                }
            }
        }
        
        /// <summary>
        /// アニメーター状態を更新
        /// </summary>
        private void UpdateAnimatorStates()
        {
            if (animatorController != null)
            {
                float normalizedSpeed = CalculateNormalizedSpeed();
                animatorController.SetSpeed(normalizedSpeed);
            }
        }
        
        /// <summary>
        /// アニメーター用の正規化された速度を計算
        /// </summary>
        /// <returns>PlayerAnimatorControllerで設定された速度値</returns>
        private float CalculateNormalizedSpeed()
        {
            if (animatorController == null)
                return 0f;
            
            // 移動していない場合は停止
            if (moveDirection.magnitude < 0.1f)
                return animatorController.IdleSpeed;
            
            // しゃがみモード中
            if (crouchModeEnabled)
                return animatorController.CrouchSpeed;
            
            // 走行モード中（スタミナがある場合のみ）
            if (runModeEnabled && currentStamina > 0f)
                return animatorController.RunSpeed;
            
            // 通常歩行
            return animatorController.WalkSpeed;
        }
        
        #region Animation Event Methods
        
        /// <summary>
        /// 道具使用アニメーション終了時の処理
        /// </summary>
        public void OnToolUsageAnimationEnd()
        {
            Debug.Log("[EnhancedPlayerController] Tool usage animation ended");
            isUsingTool = false;
            currentUsedTool = null;
        }
        
        /// <summary>
        /// 後方互換性のため（旧攻撃システム）
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            Debug.Log("[EnhancedPlayerController] Attack animation ended (legacy call)");
            OnToolUsageAnimationEnd();
        }
        
        /// <summary>
        /// 後方互換性のため（旧攻撃システム）
        /// </summary>
        public void ExecuteAttackDamage()
        {
            Debug.Log("[EnhancedPlayerController] ExecuteAttackDamage called (legacy) - redirecting to ExecuteToolUsageEffect");
            ExecuteToolUsageEffect();
        }
        
        /// <summary>
        /// アニメーションから足音を再生
        /// </summary>
        public void PlayFootstepFromAnimation()
        {
            PlayFootstepSound();
        }
        
        /// <summary>
        /// カスタムアニメーションイベントの処理
        /// </summary>
        public void HandleCustomAnimationEvent(string eventName)
        {
            switch (eventName)
            {
                case "combo_window":
                    // コンボ受付時間の処理
                    break;
                case "invincible_start":
                    // 無敵時間開始
                    break;
                case "invincible_end":
                    // 無敵時間終了
                    break;
                default:
                    Debug.Log($"[EnhancedPlayerController] Unhandled animation event: {eventName}");
                    break;
            }
        }
        
        #endregion
    }

    public interface IInteractable
    {
        void Interact(EnhancedPlayerController player);
    }
}