using System;
using UnityEngine;

namespace KowloonBreak.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float staminaDepletionRate = 20f;

        [Header("Status")]
        [SerializeField] private bool isInfected = false;
        [SerializeField] private float infectionLevel = 0f;

        public event Action<float> OnHealthChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<bool> OnInfectionStatusChanged;
        public event Action OnPlayerDeath;

        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        
        public float Stamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercentage => currentStamina / maxStamina;
        
        public bool IsInfected => isInfected;
        public float InfectionLevel => infectionLevel;
        public bool IsAlive => currentHealth > 0f;

        private EnhancedPlayerController enhancedPlayerController;

        private void Awake()
        {
            enhancedPlayerController = GetComponent<EnhancedPlayerController>();
        }

        private void Start()
        {
            InitializeStats();
        }

        private void Update()
        {
            UpdateStamina();
            UpdateInfection();
        }

        private void InitializeStats()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            Debug.Log($"[PlayerStats] InitializeStats: Health={currentHealth}/{maxHealth}, HealthPercentage={HealthPercentage}");
            Debug.Log($"[PlayerStats] InitializeStats: OnHealthChanged subscribers count: {OnHealthChanged?.GetInvocationList()?.Length ?? 0}");
            OnHealthChanged?.Invoke(HealthPercentage);
            OnStaminaChanged?.Invoke(StaminaPercentage);
        }

        private void UpdateStamina()
        {
            if (enhancedPlayerController != null)
            {
                bool isRunning = enhancedPlayerController.IsRunning;
                bool isMoving = enhancedPlayerController.IsMoving;
                
                if (isRunning && isMoving)
                {
                    ConsumeStamina(staminaDepletionRate * Time.deltaTime);
                }
                else if (currentStamina < maxStamina)
                {
                    RegenerateStamina(staminaRegenRate * Time.deltaTime);
                }
            }
            else if (currentStamina < maxStamina)
            {
                // EnhancedPlayerControllerがない場合はスタミナを回復
                RegenerateStamina(staminaRegenRate * Time.deltaTime);
            }
        }

        private void UpdateInfection()
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
            Debug.Log($"[PlayerStats] TakeDamage called: damage={damage}, currentHealth={currentHealth}, IsAlive={IsAlive}");
            
            if (!IsAlive) 
            {
                Debug.Log("[PlayerStats] TakeDamage: Player is already dead, ignoring damage");
                return;
            }

            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float newHealthPercentage = HealthPercentage;
            
            Debug.Log($"[PlayerStats] TakeDamage: Health changed from {oldHealth} to {currentHealth} (percentage: {newHealthPercentage})");
            Debug.Log($"[PlayerStats] TakeDamage: OnHealthChanged subscribers count: {OnHealthChanged?.GetInvocationList()?.Length ?? 0}");
            
            OnHealthChanged?.Invoke(newHealthPercentage);
            Debug.Log($"[PlayerStats] TakeDamage: OnHealthChanged event fired with percentage: {newHealthPercentage}");

            if (currentHealth <= 0f)
            {
                Debug.Log("[PlayerStats] TakeDamage: Player health reached 0, calling Die()");
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(HealthPercentage);
        }

        public void ConsumeStamina(float amount)
        {
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            OnStaminaChanged?.Invoke(StaminaPercentage);
        }

        public void RegenerateStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(StaminaPercentage);
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
            Debug.Log("[PlayerStats] Player has died!");
            OnPlayerDeath?.Invoke();
            
            // プレイヤーの移動を無効化
            if (enhancedPlayerController != null)
            {
                Debug.Log("[PlayerStats] Disabling player movement via EnhancedPlayerController");
                enhancedPlayerController.SetMovementEnabled(false);
            }
            else
            {
                Debug.LogWarning("[PlayerStats] EnhancedPlayerController not found - movement not disabled");
            }
            
            // PlayerAnimatorControllerでDeathアニメーションを再生
            var animatorController = GetComponent<PlayerAnimatorController>();
            if (animatorController != null)
            {
                Debug.Log("[PlayerStats] Triggering Death animation via PlayerAnimatorController");
                animatorController.TriggerDeath();
            }
            else
            {
                Debug.LogWarning("[PlayerStats] PlayerAnimatorController not found on player");
            }
        }

        public bool CanRun()
        {
            return currentStamina > 10f && IsAlive;
        }

        public void RestoreToFull()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            SetInfectionStatus(false);
            
            OnHealthChanged?.Invoke(HealthPercentage);
            OnStaminaChanged?.Invoke(StaminaPercentage);
        }
    }
}