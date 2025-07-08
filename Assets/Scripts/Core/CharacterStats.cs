using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class CharacterStats
    {
        [Header("Basic Stats")]
        [SerializeField] private int health = 100;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int stamina = 100;
        [SerializeField] private int maxStamina = 100;
        [SerializeField] private int strength = 10;
        [SerializeField] private int agility = 10;
        [SerializeField] private int intelligence = 10;
        [SerializeField] private int charisma = 10;

        public int Health 
        { 
            get => health; 
            set => health = Mathf.Clamp(value, 0, maxHealth); 
        }
        
        public int MaxHealth 
        { 
            get => maxHealth; 
            set => maxHealth = Mathf.Max(1, value); 
        }
        
        public int Stamina 
        { 
            get => stamina; 
            set => stamina = Mathf.Clamp(value, 0, maxStamina); 
        }
        
        public int MaxStamina 
        { 
            get => maxStamina; 
            set => maxStamina = Mathf.Max(1, value); 
        }
        
        public int Strength 
        { 
            get => strength; 
            set => strength = Mathf.Max(1, value); 
        }
        
        public int Agility 
        { 
            get => agility; 
            set => agility = Mathf.Max(1, value); 
        }
        
        public int Intelligence 
        { 
            get => intelligence; 
            set => intelligence = Mathf.Max(1, value); 
        }
        
        public int Charisma 
        { 
            get => charisma; 
            set => charisma = Mathf.Max(1, value); 
        }

        public bool IsAlive => health > 0;
        public float HealthPercentage => (float)health / maxHealth;
        public float StaminaPercentage => (float)stamina / maxStamina;

        public event Action<int> OnHealthChanged;
        public event Action<int> OnStaminaChanged;
        public event Action OnDeath;

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            int previousHealth = health;
            health = Mathf.Max(0, health - damage);
            
            OnHealthChanged?.Invoke(health);
            
            if (health == 0 && previousHealth > 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(int healAmount)
        {
            if (healAmount <= 0) return;

            health = Mathf.Min(maxHealth, health + healAmount);
            OnHealthChanged?.Invoke(health);
        }

        public void ConsumeStamina(int amount)
        {
            if (amount <= 0) return;

            stamina = Mathf.Max(0, stamina - amount);
            OnStaminaChanged?.Invoke(stamina);
        }

        public void RestoreStamina(int amount)
        {
            if (amount <= 0) return;

            stamina = Mathf.Min(maxStamina, stamina + amount);
            OnStaminaChanged?.Invoke(stamina);
        }

        public void LevelUpStat(StatType statType, int amount = 1)
        {
            switch (statType)
            {
                case StatType.Strength:
                    Strength += amount;
                    break;
                case StatType.Agility:
                    Agility += amount;
                    break;
                case StatType.Intelligence:
                    Intelligence += amount;
                    break;
                case StatType.Charisma:
                    Charisma += amount;
                    break;
            }
        }
    }

    public enum StatType
    {
        Strength,
        Agility,
        Intelligence,
        Charisma
    }
}