using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Managers
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Resource Configuration")]
        [SerializeField] private ResourceData[] initialResources;
        [SerializeField] private float deteriorationUpdateInterval = 60f;

        private Dictionary<ResourceType, Resource> resources;
        private float deteriorationTimer;

        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<ResourceType> OnResourceDepleted;
        public event Action<ResourceType, float> OnResourceQualityChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeResourceManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateResourceDeterioration();
        }

        private void InitializeResourceManager()
        {
            resources = new Dictionary<ResourceType, Resource>();

            if (initialResources == null || initialResources.Length == 0)
            {
                CreateDefaultResources();
            }

            foreach (var resourceData in initialResources)
            {
                var resource = new Resource(resourceData.type, resourceData.initialAmount, resourceData.maxAmount);
                resource.SetQuality(resourceData.quality);
                
                resource.OnAmountChanged += (amount) => OnResourceChanged?.Invoke(resourceData.type, amount);
                resource.OnQualityChanged += (quality) => OnResourceQualityChanged?.Invoke(resourceData.type, quality);
                
                resources[resourceData.type] = resource;
            }

            Debug.Log("Resource Manager Initialized");
        }

        private void CreateDefaultResources()
        {
            initialResources = new ResourceData[]
            {
                new ResourceData { type = ResourceType.Food, initialAmount = 10, maxAmount = 100, quality = 1f },
                new ResourceData { type = ResourceType.Water, initialAmount = 15, maxAmount = 100, quality = 1f },
                new ResourceData { type = ResourceType.Medicine, initialAmount = 5, maxAmount = 50, quality = 1f },
                new ResourceData { type = ResourceType.Materials, initialAmount = 20, maxAmount = 200, quality = 1f },
                new ResourceData { type = ResourceType.Ammunition, initialAmount = 10, maxAmount = 100, quality = 1f },
                new ResourceData { type = ResourceType.Fuel, initialAmount = 5, maxAmount = 50, quality = 1f },
                new ResourceData { type = ResourceType.Electronics, initialAmount = 3, maxAmount = 30, quality = 1f },
                new ResourceData { type = ResourceType.Clothing, initialAmount = 8, maxAmount = 80, quality = 1f },
                new ResourceData { type = ResourceType.Tools, initialAmount = 5, maxAmount = 50, quality = 1f },
                new ResourceData { type = ResourceType.Information, initialAmount = 0, maxAmount = 20, quality = 1f }
            };
        }

        private void UpdateResourceDeterioration()
        {
            deteriorationTimer += Time.deltaTime;
            
            if (deteriorationTimer >= deteriorationUpdateInterval)
            {
                deteriorationTimer = 0f;
                
                foreach (var resource in resources.Values)
                {
                    resource.UpdateDeterioration(deteriorationUpdateInterval);
                    
                    if (resource.Amount == 0)
                    {
                        OnResourceDepleted?.Invoke(resource.ResourceType);
                    }
                }
            }
        }

        public bool HasResource(ResourceType type)
        {
            return resources.ContainsKey(type);
        }

        public Resource GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out Resource resource) ? resource : null;
        }

        public int GetResourceAmount(ResourceType type)
        {
            Resource resource = GetResource(type);
            return resource?.Amount ?? 0;
        }

        public float GetResourceQuality(ResourceType type)
        {
            Resource resource = GetResource(type);
            return resource?.Quality ?? 0f;
        }

        public bool HasEnoughResources(ResourceType type, int amount)
        {
            Resource resource = GetResource(type);
            return resource != null && resource.HasEnough(amount);
        }

        public bool ConsumeResources(ResourceType type, int amount)
        {
            Resource resource = GetResource(type);
            if (resource != null && resource.ConsumeAmount(amount))
            {
                Debug.Log($"Consumed {amount} {type}. Remaining: {resource.Amount}");
                return true;
            }
            
            Debug.LogWarning($"Cannot consume {amount} {type}. Available: {resource?.Amount ?? 0}");
            return false;
        }

        public void AddResources(ResourceType type, int amount)
        {
            Resource resource = GetResource(type);
            if (resource != null)
            {
                resource.AddAmount(amount);
                Debug.Log($"Added {amount} {type}. Total: {resource.Amount}");
            }
        }

        public bool ConsumeMultipleResources(Dictionary<ResourceType, int> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (!HasEnoughResources(requirement.Key, requirement.Value))
                {
                    return false;
                }
            }

            foreach (var requirement in requirements)
            {
                ConsumeResources(requirement.Key, requirement.Value);
            }

            return true;
        }

        public void AddMultipleResources(Dictionary<ResourceType, int> additions)
        {
            foreach (var addition in additions)
            {
                AddResources(addition.Key, addition.Value);
            }
        }

        public float GetTotalResourceValue()
        {
            float totalValue = 0f;
            foreach (var resource in resources.Values)
            {
                totalValue += resource.GetEffectiveValue();
            }
            return totalValue;
        }

        public ResourceType GetMostAbundantResource()
        {
            ResourceType mostAbundant = ResourceType.Food;
            int maxAmount = 0;

            foreach (var kvp in resources)
            {
                if (kvp.Value.Amount > maxAmount)
                {
                    maxAmount = kvp.Value.Amount;
                    mostAbundant = kvp.Key;
                }
            }

            return mostAbundant;
        }

        public ResourceType GetMostDepletedResource()
        {
            ResourceType mostDepleted = ResourceType.Food;
            float minPercentage = 1f;

            foreach (var kvp in resources)
            {
                float percentage = (float)kvp.Value.Amount / kvp.Value.MaxAmount;
                if (percentage < minPercentage)
                {
                    minPercentage = percentage;
                    mostDepleted = kvp.Key;
                }
            }

            return mostDepleted;
        }

        public Dictionary<ResourceType, int> GetAllResourceAmounts()
        {
            var amounts = new Dictionary<ResourceType, int>();
            foreach (var kvp in resources)
            {
                amounts[kvp.Key] = kvp.Value.Amount;
            }
            return amounts;
        }

        public void SetResourceAmount(ResourceType type, int amount)
        {
            Resource resource = GetResource(type);
            resource?.SetAmount(amount);
        }

        public void SetResourceQuality(ResourceType type, float quality)
        {
            Resource resource = GetResource(type);
            resource?.SetQuality(quality);
        }
    }

    [Serializable]
    public class ResourceData
    {
        public ResourceType type;
        public int initialAmount;
        public int maxAmount;
        public float quality = 1f;
    }
}