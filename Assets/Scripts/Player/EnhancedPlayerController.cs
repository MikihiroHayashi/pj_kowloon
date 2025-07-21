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

        [Header("Stamina System")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 20f;
        [SerializeField] private float runStaminaCost = 30f;

        [Header("Interaction System")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayers = -1;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private KeyCode flashlightKey = KeyCode.F;
        
        [Header("Combat System")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask destructibleLayers = -1;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private Transform attackPoint;
        
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

        private CharacterController characterController;
        private AudioSource audioSource;
        private CharacterStats playerStats;
        private HealthStatus playerHealth;
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
        
        // 攻撃システム関連
        private float lastAttackTime;
        private bool isAttacking;
        
        // 道具選択関連
        private int selectedToolIndex = 0;

        public float CurrentStamina => currentStamina;
        public float StaminaPercentage => currentStamina / maxStamina;
        public bool IsRunning => isRunning;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => moveDirection.magnitude > 0.1f;
        public float NoiseLevel => noiseLevel;
        public CharacterStats Stats => playerStats;
        public InventorySlot SelectedTool => resourceManager?.GetToolSlot(selectedToolIndex);
        public int SelectedToolIndex => selectedToolIndex;

        public event Action<float> OnStaminaChanged;
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
            HandleNoise();
            HandleAudio();
            HandleCombat();
            HandleItemPickup();
            UpdateUI();
        }

        private void InitializePlayer()
        {
            characterController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            
            if (playerCamera == null)
            {
                playerCamera = UnityEngine.Camera.main;
            }

            SetupCameraFollowTarget();
            InitializeStats();
            InitializeInventorySystem();
            currentStamina = maxStamina;
            
            // 攻撃ポイントを設定
            SetupAttackPoint();
            
            // EnhancedPlayerController初期化完了
        }

        private void SetupCameraFollowTarget()
        {
            if (cameraFollowTarget == null)
            {
                // カメラ追従用のターゲットオブジェクトを作成
                GameObject followTargetGO = new GameObject("CameraFollowTarget");
                followTargetGO.transform.SetParent(transform);
                followTargetGO.transform.localPosition = Vector3.zero;
                
                cameraFollowTarget = followTargetGO.transform;
                
                // Cinemachine セットアップを追加
                var cinemachineSetup = followTargetGO.AddComponent<KowloonBreak.Camera.CinemachineSetup>();
                cinemachineSetup.FollowTarget = transform;
            }
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
            
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                SetRunning(true);
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                SetRunning(false);
            }
            
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                SetCrouching(!isCrouching);
            }
            
            // 道具選択 (1-8キー)
            HandleToolSelection();
            
            // 攻撃 (左クリック)
            if (Input.GetMouseButtonDown(0))
            {
                TryAttack();
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
        }

        private float GetCurrentSpeed()
        {
            float baseSpeed = walkSpeed;
            
            if (isCrouching)
            {
                baseSpeed = crouchSpeed;
            }
            else if (isRunning && currentStamina > 0)
            {
                baseSpeed = runSpeed;
            }
            
            float healthPenalty = playerHealth.GetMovementPenalty();
            float infectionPenalty = playerInfection.GetPerformancePenalty();
            
            return baseSpeed * (1f - healthPenalty - infectionPenalty);
        }


        private void HandleStamina()
        {
            bool isConsumingStamina = (isRunning && IsMoving) || 
                                     (playerInfection.Level == InfectionLevel.Infected);
            
            if (isConsumingStamina && currentStamina > 0)
            {
                float consumption = runStaminaCost;
                if (playerInfection.Level == InfectionLevel.Infected)
                {
                    consumption *= 1.5f;
                }
                
                currentStamina -= consumption * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);
                
                if (currentStamina <= 0f)
                {
                    SetRunning(false);
                }
            }
            else if (currentStamina < maxStamina)
            {
                float regenRate = staminaRegenRate;
                if (playerHealth.Condition != HealthCondition.Healthy)
                {
                    regenRate *= 0.5f;
                }
                
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
            
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
            
            float baseNoise = 20f;
            
            if (isRunning)
            {
                baseNoise = 60f;
            }
            else if (isCrouching)
            {
                baseNoise = 5f;
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
                if (isRunning) currentFootstepInterval *= 0.6f;
                if (isCrouching) currentFootstepInterval *= 1.5f;
                
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
                                 (playerInfection.Level >= InfectionLevel.Infected);
                emission.enabled = shouldEmit;
                
                if (shouldEmit)
                {
                    emission.rateOverTime = 10f + (1f - StaminaPercentage) * 20f;
                }
            }
        }

        private void SetRunning(bool running)
        {
            if (running && currentStamina <= 0f) return;
            if (running && isCrouching) return;
            
            isRunning = running;
            OnRunStateChanged?.Invoke(isRunning);
        }

        private void SetCrouching(bool crouching)
        {
            isCrouching = crouching;
            
            if (isCrouching)
            {
                characterController.height = 1f;
                characterController.center = new Vector3(0, 0.5f, 0);
                SetRunning(false);
            }
            else
            {
                characterController.height = 2f;
                characterController.center = new Vector3(0, 1f, 0);
            }
            
            OnCrouchStateChanged?.Invoke(isCrouching);
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
            
            float volume = isCrouching ? 0.3f : (isRunning ? 0.8f : 0.5f);
            audioSource.PlayOneShot(footstep, volume);
        }


        private void UpdateUI()
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                uiManager.UpdateHealthBar(playerStats.Health, playerStats.MaxHealth);
                uiManager.UpdateStaminaBar(currentStamina, maxStamina);
            }
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
            playerStats.TakeDamage(damage);
            
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

        #region Combat System
        
        private void InitializeInventorySystem()
        {
            resourceManager = EnhancedResourceManager.Instance;
            toolSelectionHUD = FindObjectOfType<ToolSelectionHUDController>();
            
            if (toolSelectionHUD != null)
            {
                toolSelectionHUD.OnToolSelected += HandleToolSelectionChanged;
            }
        }
        
        private void SetupAttackPoint()
        {
            if (attackPoint == null)
            {
                GameObject attackPointGO = new GameObject("AttackPoint");
                attackPointGO.transform.SetParent(transform);
                attackPointGO.transform.localPosition = Vector3.forward * 1f;
                attackPoint = attackPointGO.transform;
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
        
        private void HandleCombat()
        {
            if (isAttacking)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    isAttacking = false;
                }
            }
        }
        
        private void TryAttack()
        {
            if (isAttacking) return;
            if (Time.time - lastAttackTime < attackCooldown) return;
            
            var selectedTool = resourceManager?.GetToolSlot(selectedToolIndex);
            if (selectedTool == null || selectedTool.IsEmpty) return;
            
            if (!selectedTool.ItemData.IsTool()) return;
            
            isAttacking = true;
            lastAttackTime = Time.time;
            
            PerformAttack(selectedTool);
        }
        
        private void PerformAttack(InventorySlot toolSlot)
        {
            var itemData = toolSlot.ItemData;
            float range = itemData.attackRange;
            float damage = itemData.attackDamage;
            
            // 攻撃範囲内の破壊可能オブジェクトを検索
            Collider[] hitObjects = Physics.OverlapSphere(attackPoint.position, range, destructibleLayers);
            
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
                        
                        // オブジェクト破壊処理完了
                    }
                }
            }
            
            // 道具の耐久度を減らす
            if (hitSomething)
            {
                toolSlot.UseDurability(1);
            }
            
            // 攻撃アニメーションやエフェクトを追加可能
            PlayAttackEffects(itemData);
        }
        
        private void PlayAttackEffects(ItemData itemData)
        {
            // 攻撃音の再生
            if (audioSource != null)
            {
                // TODO: 道具に応じた攻撃音を再生
            }
            
            // 攻撃エフェクトの再生
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
    }

    public interface IInteractable
    {
        void Interact(EnhancedPlayerController player);
    }
}