using System;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Exploration;
using KowloonBreak.UI;

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

        public float CurrentStamina => currentStamina;
        public float StaminaPercentage => currentStamina / maxStamina;
        public bool IsRunning => isRunning;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => moveDirection.magnitude > 0.1f;
        public float NoiseLevel => noiseLevel;
        public CharacterStats Stats => playerStats;

        public event Action<float> OnStaminaChanged;
        public event Action<bool> OnRunStateChanged;
        public event Action<bool> OnCrouchStateChanged;
        public event Action<float> OnNoiseChanged;

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
            currentStamina = maxStamina;
            
            Debug.Log("Enhanced Player Controller Initialized");
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
                    Debug.Log($"Detected by enemy: {enemy.name}");
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
            
            Debug.Log("Player died");
        }

        private void HandlePlayerTurnedZombie()
        {
            canMove = false;
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification("感染が進行し、意識を失った...", NotificationType.Error);
            }
            
            Debug.Log("Player turned into zombie");
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
        }
    }

    public interface IInteractable
    {
        void Interact(EnhancedPlayerController player);
    }
}