using System;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Exploration;
using KowloonBreak.UI;
using KowloonBreak.Managers;

namespace KowloonBreak.Player
{
    public enum MovementState
    {
        Normal,     // 通常移動
        Dodging,    // ダッジ中
        Stunned,    // スタン中
        Dead        // 死亡
    }
    
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
        [SerializeField] private float baseStealth = 0.3f;
        [SerializeField] private float crouchStealthBonus = 0.5f;
        [SerializeField] private float movementStealthPenalty = 0.2f;

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

        [Header("Movement Input Settings")]
        [SerializeField] private bool runToggleMode = false; // false: Hold to run, true: Toggle run
        [SerializeField] private bool crouchToggleMode = true; // false: Hold to crouch, true: Toggle crouch

        [Header("Tool Usage System")]
        [SerializeField] private LayerMask destructibleLayers = -1;
        [SerializeField] private float toolUsageCooldown = 0.5f;
        [SerializeField] private Transform toolUsagePoint;

        [Header("Item Pickup System")]
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private LayerMask itemLayers = -1;
        [SerializeField] private bool autoPickup = true;

        [Header("Damage Display")]
        [SerializeField] private Transform damageDisplayPoint;

        [Header("Dodge System")]
        [SerializeField] private float dodgeDistance = 4f;
        [SerializeField] private float dodgeDuration = 0.5f;
        [SerializeField] private float dodgeCooldown = 1.5f;
        [SerializeField] private float dodgeStaminaCost = 25f;
        [SerializeField] private bool canDodgeInAir = false;

        [Header("Audio")]
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private float footstepInterval = 0.5f;

        [Header("Visual Effects")]
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
        private MiningSystem miningSystem;

        private Vector3 moveDirection;
        private Vector3 velocity;
        private float currentStamina;
        private float noiseTimer;
        private float footstepTimer;
        private bool isRunning;
        private bool isCrouching;
        private bool isGrounded;
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

        // 移動状態管理
        private MovementState currentMovementState = MovementState.Normal;
        
        // ダッジシステム関連
        private float lastDodgeTime;
        private bool isInvincible;
        private Vector3 dodgeDirection;
        private float dodgeProgress;
        private Vector3 dodgeStartPosition;

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

        // Movement State Properties
        public MovementState CurrentMovementState => currentMovementState;
        public bool IsDodging => currentMovementState == MovementState.Dodging;
        public bool IsInvincible => isInvincible;
        public float NoiseLevel => noiseLevel;
        public float StealthLevel => CalculateStealthLevel();
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
        public event Action<float> OnStealthAttack;

        private void Start()
        {
            InitializePlayer();
            SetupInput();
        }

        private void Update()
        {
            HandleInput();
            
            if (canMove)
            {
                HandleMovement(); // 統一された移動処理
            }
            
            // その他のシステム処理
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
            // 入力設定の初期化
        }

        private void HandleInput()
        {
            if (InputManager.Instance == null) return;

            if (InputManager.Instance.IsInteractionPressed())
            {
                TryInteract();
            }

            // 走行モード入力処理
            HandleRunInput();

            // しゃがみモード入力処理
            HandleCrouchInput();

            // 道具選択
            HandleToolSelection();

            // 道具使用
            if (InputManager.Instance.IsUseToolPressed())
            {
                TryUseTool();
            }

            // ダッジ入力処理
            if (InputManager.Instance.IsDodgePressed())
            {
                TryDodge();
            }
        }

        private void HandleMovement()
        {
            Vector3 finalMovement = Vector3.zero;
            
            // 移動状態に応じて移動量を計算
            switch (currentMovementState)
            {
                case MovementState.Normal:
                    finalMovement = CalculateNormalMovement();
                    break;
                case MovementState.Dodging:
                    finalMovement = CalculateDodgeMovement();
                    break;
                case MovementState.Stunned:
                case MovementState.Dead:
                    finalMovement = Vector3.zero;
                    break;
            }
            
            // 重力を統一的に適用
            ApplyGravity(ref finalMovement);
            
            // 単一のMove呼び出し
            characterController.Move(finalMovement);
            
            // エフェクトとアニメーション更新
            UpdateMovementEffects();
            UpdateAnimatorStates();
        }
        
        private Vector3 CalculateNormalMovement()
        {
            Vector2 inputVector = InputManager.Instance != null ? 
                InputManager.Instance.GetMovementInput() : 
                new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            // ワールド空間での移動方向を計算
            Vector3 direction = new Vector3(inputVector.x, 0f, inputVector.y);
            moveDirection = direction.normalized;

            // 移動方向にキャラクターを向ける
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }

            float currentSpeed = GetCurrentSpeed();
            return moveDirection * currentSpeed * Time.deltaTime;
        }
        
        private Vector3 CalculateDodgeMovement()
        {
            if (dodgeProgress >= 1f)
            {
                // ダッジ終了
                SetMovementState(MovementState.Normal);
                return Vector3.zero;
            }
            
            // ダッジの移動量を計算（イージング適用）
            float easedProgress = CalculateDodgeEasing(dodgeProgress);
            float previousEasedProgress = CalculateDodgeEasing(Mathf.Max(0f, dodgeProgress - Time.deltaTime / dodgeDuration));
            
            float deltaProgress = easedProgress - previousEasedProgress;
            Vector3 dodgeMovement = dodgeDirection * dodgeDistance * deltaProgress;
            
            dodgeProgress += Time.deltaTime / dodgeDuration;
            
            return dodgeMovement;
        }
        
        private float CalculateDodgeEasing(float t)
        {
            // イージングカーブ（最初は速く、最後は遅く）
            return 1f - Mathf.Pow(1f - t, 3f);
        }
        
        private void ApplyGravity(ref Vector3 movement)
        {
            isGrounded = characterController.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;
            movement.y = velocity.y * Time.deltaTime;
        }
        
        private void SetMovementState(MovementState newState)
        {
            if (currentMovementState != newState)
            {
                MovementState previousState = currentMovementState;
                currentMovementState = newState;
                
                OnMovementStateChanged(previousState, newState);
            }
        }
        
        private void OnMovementStateChanged(MovementState from, MovementState to)
        {
            switch (to)
            {
                case MovementState.Normal:
                    // 通常状態に復帰
                    break;
                case MovementState.Dodging:
                    // ダッジ開始処理はTryDodge()で実行済み
                    break;
                case MovementState.Stunned:
                    // スタン状態の処理
                    break;
                case MovementState.Dead:
                    // 死亡状態の処理
                    break;
            }
        }

        private float GetCurrentSpeed()
        {
            float baseSpeed = walkSpeed;

            // 移動モードに応じて速度を決定
            if (crouchModeEnabled)
            {
                baseSpeed = crouchSpeed;
            }
            else if (runModeEnabled && currentStamina > 0)
            {
                baseSpeed = runSpeed;
            }
            else
            {
                baseSpeed = walkSpeed;
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

        /// <summary>
        /// 現在の隠密レベルを計算（0.0 = 全く隠れていない、1.0 = 完全に隠れている）
        /// </summary>
        private float CalculateStealthLevel()
        {
            float stealth = baseStealth;

            // しゃがみボーナス
            if (crouchModeEnabled)
            {
                stealth += crouchStealthBonus;
            }

            // 移動ペナルティ
            if (IsMoving)
            {
                float movementPenalty = movementStealthPenalty;

                if (runModeEnabled)
                {
                    movementPenalty *= 2f; // 走行時はペナルティ倍増
                }
                else if (crouchModeEnabled)
                {
                    movementPenalty *= 0.5f; // しゃがみ移動はペナルティ軽減
                }

                stealth -= movementPenalty;
            }

            // 健康状態ペナルティ
            if (isInfected)
            {
                stealth -= 0.1f; // 感染時は隠密性が下がる
            }

            if (currentHealth < maxHealth * 0.3f)
            {
                stealth -= 0.15f; // 重傷時は隠密性が下がる
            }

            return Mathf.Clamp01(stealth);
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
            if (InputManager.Instance != null)
            {
                if (runToggleMode)
                {
                    // トグルモード: キーを押すたびにON/OFF切り替え
                    if (InputManager.Instance.IsRunDown())
                    {
                        ToggleRunMode();
                    }
                }
                else
                {
                    // ホールドモード: キーを押している間だけON
                    if (InputManager.Instance.IsRunDown())
                    {
                        SetRunMode(true);
                    }
                    else if (InputManager.Instance.IsRunUp())
                    {
                        SetRunMode(false);
                    }
                }
            }
            else
            {
                HandleRunInputFallback();
            }
        }
        
        private void HandleRunInputFallback()
        {
            // フォールバック処理は削除し、InputManagerを必須とする
            Debug.LogWarning("[EnhancedPlayerController] InputManager is not available. Input handling disabled.");
        }

        /// <summary>
        /// しゃがみ入力を処理
        /// </summary>
        private void HandleCrouchInput()
        {
            if (InputManager.Instance != null)
            {
                if (crouchToggleMode)
                {
                    // トグルモード: キーを押すたびにON/OFF切り替え
                    if (InputManager.Instance.IsCrouchDown())
                    {
                        ToggleCrouchMode();
                    }
                }
                else
                {
                    // ホールドモード: キーを押している間だけON
                    if (InputManager.Instance.IsCrouchDown())
                    {
                        SetCrouchMode(true);
                    }
                    else if (InputManager.Instance.IsCrouchUp())
                    {
                        SetCrouchMode(false);
                    }
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
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowNotification("スタミナが不足しています", NotificationType.Warning);
                    }
                    return;
                }

                // しゃがみ中は走行不可
                if (crouchModeEnabled)
                {
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

            // アニメーターのCrouchパラメーターを更新
            if (animatorController != null)
            {
                animatorController.SetCrouch(crouchModeEnabled);
            }

            if (previousState != crouchModeEnabled)
            {
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
            
            // 移動無効時は通常状態に戻す
            if (!enabled && currentMovementState == MovementState.Dodging)
            {
                SetMovementState(MovementState.Normal);
            }
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
            miningSystem = GetComponent<MiningSystem>();

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
            if (InputManager.Instance != null)
            {
                // LB/RB (Q/E) でツール切り替え
                if (InputManager.Instance.IsToolPreviousPressed())
                {
                    SelectPreviousTool();
                }
                else if (InputManager.Instance.IsToolNextPressed())
                {
                    SelectNextTool();
                }
                
                // レガシー: 1-8キーでの直接選択も残す
                int selectedTool = InputManager.Instance.GetToolSelectionInput();
                if (selectedTool >= 0)
                {
                    SelectTool(selectedTool);
                }
            }
        }

        private void SelectTool(int index)
        {
            if (index < 0 || index >= 8) return;
            
            selectedToolIndex = index;
            var selectedSlot = resourceManager?.GetToolSlot(selectedToolIndex);
            OnToolSelected?.Invoke(selectedToolIndex, selectedSlot);

            // HUDコントローラーにも通知
            if (toolSelectionHUD != null)
            {
                toolSelectionHUD.SelectTool(index);
            }
            
            // UIフィードバック
            if (UIManager.Instance != null && selectedSlot != null && !selectedSlot.IsEmpty)
            {
                string toolName = selectedSlot.ItemData.itemName;
                UIManager.Instance.ShowNotification($"ツール選択: {toolName}", NotificationType.Info);
            }
        }
        
        private void SelectPreviousTool()
        {
            if (resourceManager == null) return;
            
            int currentIndex = selectedToolIndex;
            int attempts = 0;
            
            do {
                selectedToolIndex = (selectedToolIndex - 1 + 8) % 8;
                attempts++;
                
                var slot = resourceManager.GetToolSlot(selectedToolIndex);
                if (slot != null && !slot.IsEmpty)
                {
                    SelectTool(selectedToolIndex);
                    return;
                }
            } while (selectedToolIndex != currentIndex && attempts < 8);
            
            // 利用可能なツールがない場合は元のインデックスに戻す
            selectedToolIndex = currentIndex;
        }
        
        private void SelectNextTool()
        {
            if (resourceManager == null) return;
            
            int currentIndex = selectedToolIndex;
            int attempts = 0;
            
            do {
                selectedToolIndex = (selectedToolIndex + 1) % 8;
                attempts++;
                
                var slot = resourceManager.GetToolSlot(selectedToolIndex);
                if (slot != null && !slot.IsEmpty)
                {
                    SelectTool(selectedToolIndex);
                    return;
                }
            } while (selectedToolIndex != currentIndex && attempts < 8);
            
            // 利用可能なツールがない場合は元のインデックスに戻す
            selectedToolIndex = currentIndex;
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
            if (isUsingTool)
            {
                return;
            }

            if (Time.time - lastToolUsageTime < toolUsageCooldown)
            {
                return;
            }

            // リソースマネージャーの状態を確認
            if (resourceManager == null)
            {
                Debug.LogError("[EnhancedPlayerController] ResourceManager is null! Cannot get tool slot.");
                return;
            }

            var selectedTool = resourceManager.GetToolSlot(selectedToolIndex);
            if (selectedTool == null || selectedTool.IsEmpty)
            {
                Debug.LogWarning("[EnhancedPlayerController] No tool selected or slot is empty!");
                return;
            }

            if (!selectedTool.ItemData.IsTool())
            {
                return;
            }

            isUsingTool = true;
            lastToolUsageTime = Time.time;

            // 採掘系ツール（つるはし、鉄パイプ）の場合はMiningSystemを使用
            if (miningSystem != null && IsMiningTool(selectedTool))
            {
                bool miningSuccess = miningSystem.TryMineWithCurrentTool();
                if (miningSuccess)
                {
                    // 採掘対象が見つかった場合、アニメーションを再生
                    Debug.Log("[EnhancedPlayerController] Mining target found, playing animation");
                    StartToolUsage(selectedTool);
                }
                else
                {
                    // 採掘対象が見つからない場合は通常のツール使用処理にフォールバック
                    Debug.Log("[EnhancedPlayerController] No mining target found, falling back to normal tool usage");
                    StartToolUsage(selectedTool);
                }
            }
            else
            {
                // 通常のツール使用処理
                StartToolUsage(selectedTool);
            }
        }
        
        private bool IsMiningTool(InventorySlot toolSlot)
        {
            if (toolSlot?.ItemData == null) return false;
            return toolSlot.ItemData.toolType == ToolType.Pickaxe || 
                   toolSlot.ItemData.toolType == ToolType.IronPipe;
        }

        /// <summary>
        /// 道具使用を開始（道具種別に応じて動作を分岐）
        /// </summary>
        private void StartToolUsage(InventorySlot toolSlot)
        {
            // 使用中の道具データを保存（アニメーションイベントで使用）
            currentUsedTool = toolSlot;

            var toolType = toolSlot.ItemData.toolType;

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
                }
                else
                {
                    Debug.LogError("[EnhancedPlayerController] PlayerAnimatorController not found even after manual search!");
                    return;
                }
            }

            // 道具の種類に応じてアニメーションとアクションを決定

            switch (toolType)
            {
                case ToolType.Pickaxe:
                    // つるはし：掘削アニメーション（Dig）
                    animatorController.TriggerDig();
                    PlayToolUsageEffects(toolSlot.ItemData, "dig");
                    break;

                case ToolType.IronPipe:
                    // 鉄パイプ：攻撃アニメーション（Attack）
                    animatorController.TriggerAttack();
                    PlayToolUsageEffects(toolSlot.ItemData, "attack");
                    break;

                default:
                    // デフォルト：攻撃アニメーション
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
            if (currentUsedTool == null || currentUsedTool.IsEmpty)
            {
                Debug.LogWarning("[EnhancedPlayerController] No current tool available for effect execution");
                return;
            }

            var itemData = currentUsedTool.ItemData;
            var toolType = itemData.toolType;

            // 採掘系ツールの場合はMiningSystemを使用
            if (miningSystem != null && IsMiningTool(currentUsedTool))
            {
                Debug.Log("[EnhancedPlayerController] Executing mining damage through MiningSystem");
                miningSystem.ExecuteMiningDamage();
            }
            else
            {
                // 通常のツール使用効果を実行
                switch (toolType)
                {
                    case ToolType.Pickaxe:
                        ExecuteDiggingEffect(currentUsedTool);
                        break;

                    case ToolType.IronPipe:
                        ExecuteAttackEffect(currentUsedTool);
                        break;

                    default:
                        ExecuteAttackEffect(currentUsedTool);
                        break;
                }
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

            // ステルス状態をチェック
            bool isStealthAttack = IsInStealthMode();


            // 攻撃範囲内の破壊可能オブジェクトを検索
            Collider[] hitObjects = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);


            bool hitSomething = false;

            foreach (var hitCollider in hitObjects)
            {
                var destructible = hitCollider.GetComponent<IDestructible>();
                if (destructible != null)
                {
                    if (destructible.CanBeDestroyedBy(itemData.toolType))
                    {
                        destructible.TakeDamage(damage, itemData.toolType);
                        hitSomething = true;

                        // 敵に攻撃した場合の特別処理
                        var enemyBase = hitCollider.GetComponent<KowloonBreak.Enemies.EnemyBase>();
                        if (enemyBase != null)
                        {
                            if (isStealthAttack)
                            {
                                OnStealthAttack?.Invoke(damage * 3f); // ステルス攻撃イベント発火
                            }
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            // 道具の耐久度を減らす
            if (hitSomething)
            {
                toolSlot.UseDurability(1);
            }
            else
            {
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

            // ステルス状態をチェック
            bool isStealthAttack = IsInStealthMode();


            // 掘削範囲内の掘削可能オブジェクトを検索
            Collider[] hitObjects = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);


            bool dugSomething = false;

            foreach (var hitCollider in hitObjects)
            {
                var destructible = hitCollider.GetComponent<IDestructible>();
                if (destructible != null)
                {
                    if (destructible.CanBeDestroyedBy(itemData.toolType))
                    {
                        destructible.TakeDamage(damage, itemData.toolType);
                        dugSomething = true;

                        // 敵に対する掘削攻撃の場合の特別処理
                        var enemyBase = hitCollider.GetComponent<KowloonBreak.Enemies.EnemyBase>();
                        if (enemyBase != null && isStealthAttack)
                        {
                            OnStealthAttack?.Invoke(damage * 3f); // ステルス攻撃イベント発火
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }

            // 道具の耐久度を減らす
            if (dugSomething)
            {
                toolSlot.UseDurability(1);
            }
            else
            {
            }
        }

        private void PlayToolUsageEffects(ItemData itemData, string usageType)
        {

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
            if (!IsAlive || isInvincible) return;


            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            // ダメージテキストを表示
            ShowDamageText(damage);

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

        /// <summary>
        /// プレイヤーのダメージテキストを表示
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        private void ShowDamageText(float damage)
        {
            if (UIManager.Instance != null)
            {
                // ダメージ表示位置を決定（専用オブジェクトがあればそれを使用、なければデフォルト位置）
                Vector3 damagePosition = damageDisplayPoint != null
                    ? damageDisplayPoint.position
                    : transform.position + Vector3.up * 1.5f;

                UIManager.Instance.ShowDamageText(damagePosition, damage, false);
            }
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
            OnPlayerDeath?.Invoke();

            SetMovementState(MovementState.Dead);

            // PlayerAnimatorControllerでDeathアニメーションを再生
            if (animatorController != null)
            {
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
        /// 現在ステルス状態かどうかを判定
        /// </summary>
        public bool IsInStealthMode()
        {
            // 高いステルスレベル（0.6以上）で、しゃがみ状態の場合をステルス状態とする
            return StealthLevel >= 0.6f && crouchModeEnabled;
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
                float actualSpeed = CalculateActualSpeed();
                animatorController.SetRealSpeed(actualSpeed);
                
                // しゃがみ状態も毎フレーム同期（安全のため）
                animatorController.SetCrouch(crouchModeEnabled);
            }
        }

        /// <summary>
        /// 実際の移動速度を計算してアニメーターに送信
        /// </summary>
        /// <returns>実際の移動速度 (units/sec)</returns>
        private float CalculateActualSpeed()
        {
            // 移動していない場合は0
            if (moveDirection.magnitude < 0.1f)
                return 0f;

            // 現在の実際の移動速度を取得
            float actualSpeed = GetCurrentSpeed();
            
            // CharacterControllerの実際の速度も考慮（より正確な計算）
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float realTimeSpeed = horizontalVelocity.magnitude;
            
            // より正確な値を使用（実際の移動が発生している場合は実測値を優先）
            if (realTimeSpeed > 0.1f)
            {
                return realTimeSpeed;
            }
            
            return actualSpeed;
        }

        #region Animation Event Methods

        /// <summary>
        /// 道具使用アニメーション終了時の処理
        /// </summary>
        public void OnToolUsageAnimationEnd()
        {
            isUsingTool = false;
            currentUsedTool = null;
            
            // MiningSystemの保留状態もクリア
            if (miningSystem != null)
            {
                miningSystem.ClearPendingMining();
            }
        }

        /// <summary>
        /// 後方互換性のため（旧攻撃システム）
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            OnToolUsageAnimationEnd();
        }

        /// <summary>
        /// 後方互換性のため（旧攻撃システム）
        /// </summary>
        public void ExecuteAttackDamage()
        {
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
                    break;
            }
        }

        #endregion

        #region Dodge System

        /// <summary>
        /// ダッジを試行する
        /// </summary>
        private void TryDodge()
        {
            // ダッジの実行条件をチェック
            if (!CanDodge()) return;

            // スタミナを消費
            currentStamina -= dodgeStaminaCost;
            currentStamina = Mathf.Max(0f, currentStamina);
            OnStaminaChanged?.Invoke(currentStamina);

            // ダッジ方向を決定（通常の移動と同じワールド空間基準）
            Vector2 inputVector = InputManager.Instance != null ? 
                InputManager.Instance.GetMovementInputRaw() : 
                new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (Mathf.Abs(inputVector.x) > 0.1f || Mathf.Abs(inputVector.y) > 0.1f)
            {
                // 通常の移動と同じワールド空間での方向計算
                Vector3 direction = new Vector3(inputVector.x, 0f, inputVector.y);
                dodgeDirection = direction.normalized;
            }
            else
            {
                // 入力がない場合は前方向にダッジ
                dodgeDirection = transform.forward;
            }
            
            // ダッジ状態を初期化
            dodgeProgress = 0f;
            dodgeStartPosition = transform.position;
            
            // 移動状態をダッジに変更
            SetMovementState(MovementState.Dodging);

            // クールダウンを設定
            lastDodgeTime = Time.time;

            // アニメーション再生
            if (animatorController != null)
            {
                animatorController.TriggerDodge();
            }
        }

        /// <summary>
        /// ダッジが実行可能かチェック
        /// </summary>
        private bool CanDodge()
        {
            // 基本条件チェック
            if (!IsAlive || currentMovementState != MovementState.Normal || !canMove) return false;

            // クールダウンチェック
            if (Time.time - lastDodgeTime < dodgeCooldown) return false;

            // スタミナチェック
            if (currentStamina < dodgeStaminaCost) return false;

            // 空中でのダッジ可否チェック
            if (!canDodgeInAir && !isGrounded) return false;

            return true;
        }


        /// <summary>
        /// 無敵時間を設定（PlayerAnimationEventHandlerから呼び出し）
        /// </summary>
        /// <param name="invincible">無敵状態</param>
        public void SetInvincible(bool invincible)
        {
            isInvincible = invincible;
        }

        /// <summary>
        /// 移動状態を強制的に設定（スタンやノックバック等で使用）
        /// </summary>
        public void SetMovementStateForced(MovementState state)
        {
            SetMovementState(state);
        }
        
        /// <summary>
        /// スタン状態を設定（指定時間後に自動で解除）
        /// </summary>
        public void SetStunned(float duration)
        {
            if (currentMovementState != MovementState.Dead)
            {
                SetMovementState(MovementState.Stunned);
                StartCoroutine(RecoverFromStun(duration));
            }
        }
        
        private System.Collections.IEnumerator RecoverFromStun(float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (currentMovementState == MovementState.Stunned)
            {
                SetMovementState(MovementState.Normal);
            }
        }

        #endregion
    }

    public interface IInteractable
    {
        void Interact(EnhancedPlayerController player);
    }
}