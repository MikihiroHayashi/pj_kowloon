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

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
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
            OnHealthChanged?.Invoke(HealthPercentage);
            OnStaminaChanged?.Invoke(StaminaPercentage);
        }

        private void UpdateStamina()
        {
            if (playerController.IsRunning && playerController.IsMoving)
            {
                ConsumeStamina(staminaDepletionRate * Time.deltaTime);
            }
            else if (currentStamina < maxStamina)
            {
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
            if (!IsAlive) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
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
            Debug.Log("Player has died!");
            OnPlayerDeath?.Invoke();
            
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
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