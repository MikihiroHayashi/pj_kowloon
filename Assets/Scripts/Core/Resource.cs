using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class Resource
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int amount;
        [SerializeField] private int maxAmount;
        [SerializeField] private float quality = 1f;
        [SerializeField] private float deteriorationRate = 0f;

        public ResourceType ResourceType => resourceType;
        public int Amount => amount;
        public int MaxAmount => maxAmount;
        public float Quality => quality;
        public float DeteriorationRate => deteriorationRate;

        public event Action<int> OnAmountChanged;
        public event Action<float> OnQualityChanged;

        public Resource(ResourceType type, int initialAmount, int maxAmount = 999)
        {
            resourceType = type;
            amount = initialAmount;
            this.maxAmount = maxAmount;
        }

        public void AddAmount(int value)
        {
            if (value <= 0) return;

            amount = Mathf.Min(maxAmount, amount + value);
            OnAmountChanged?.Invoke(amount);
        }

        public bool ConsumeAmount(int value)
        {
            if (value <= 0 || amount < value) return false;

            amount -= value;
            OnAmountChanged?.Invoke(amount);
            return true;
        }

        public void SetAmount(int value)
        {
            amount = Mathf.Clamp(value, 0, maxAmount);
            OnAmountChanged?.Invoke(amount);
        }

        public void UpdateDeterioration(float deltaTime)
        {
            if (deteriorationRate <= 0f) return;

            quality = Mathf.Max(0f, quality - deteriorationRate * deltaTime);
            OnQualityChanged?.Invoke(quality);

            if (quality <= 0f)
            {
                amount = Mathf.Max(0, amount - 1);
                quality = 1f;
                OnAmountChanged?.Invoke(amount);
            }
        }

        public void SetQuality(float newQuality)
        {
            quality = Mathf.Clamp01(newQuality);
            OnQualityChanged?.Invoke(quality);
        }

        public bool HasEnough(int requiredAmount)
        {
            return amount >= requiredAmount;
        }

        public float GetEffectiveValue()
        {
            return amount * quality;
        }
    }

}