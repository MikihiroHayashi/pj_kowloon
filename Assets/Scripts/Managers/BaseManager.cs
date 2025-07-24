using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Managers
{
    public class BaseManager : MonoBehaviour
    {
        public static BaseManager Instance { get; private set; }

        [Header("Base Configuration")]
        [SerializeField] private BaseFacilityData[] availableFacilities;
        [SerializeField] private Vector3 basePosition;
        [SerializeField] private float baseRadius = 50f;

        [Header("Current Base Status")]
        [SerializeField] private int baseLevel = 1;
        [SerializeField] private float baseDefense = 10f;
        [SerializeField] private int maxFacilities = 10;

        private Dictionary<FacilityType, BaseFacility> facilities;
        private EnhancedResourceManager resourceManager;

        public int BaseLevel => baseLevel;
        public float BaseDefense => baseDefense;
        public int MaxFacilities => maxFacilities;
        public int CurrentFacilityCount => facilities.Count;

        public event Action<BaseFacility> OnFacilityBuilt;
        public event Action<BaseFacility> OnFacilityUpgraded;
        public event Action<BaseFacility> OnFacilityDestroyed;
        public event Action<int> OnBaseLevelChanged;

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
                InitializeBaseManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            resourceManager = EnhancedResourceManager.Instance;
        }

        private void InitializeBaseManager()
        {
            facilities = new Dictionary<FacilityType, BaseFacility>();
            
            if (availableFacilities == null || availableFacilities.Length == 0)
            {
                CreateDefaultFacilities();
            }

            // BaseManager初期化完了
        }

        private void CreateDefaultFacilities()
        {
            availableFacilities = new BaseFacilityData[]
            {
                new BaseFacilityData
                {
                    type = FacilityType.Dormitory,
                    name = "宿舎",
                    description = "仲間の休息と回復を提供",
                    maxLevel = 5,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 20 },
                        { ResourceType.Tools, 5 }
                    }
                },
                new BaseFacilityData
                {
                    type = FacilityType.Workshop,
                    name = "作業場",
                    description = "アイテムの製造と修理",
                    maxLevel = 5,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 30 },
                        { ResourceType.Tools, 10 },
                        { ResourceType.Electronics, 5 }
                    }
                },
                new BaseFacilityData
                {
                    type = FacilityType.Watchtower,
                    name = "見張り台",
                    description = "早期警戒と防御力向上",
                    maxLevel = 3,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 25 },
                        { ResourceType.Tools, 8 },
                        { ResourceType.Electronics, 3 }
                    }
                },
                new BaseFacilityData
                {
                    type = FacilityType.Garden,
                    name = "菜園",
                    description = "食料の継続的生産",
                    maxLevel = 4,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 15 },
                        { ResourceType.Tools, 3 },
                        { ResourceType.Water, 10 }
                    }
                },
                new BaseFacilityData
                {
                    type = FacilityType.Infirmary,
                    name = "医務室",
                    description = "治療と感染対策",
                    maxLevel = 5,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 25 },
                        { ResourceType.Medicine, 10 },
                        { ResourceType.Electronics, 5 }
                    }
                },
                new BaseFacilityData
                {
                    type = FacilityType.Arsenal,
                    name = "武器庫",
                    description = "武器と弾薬の保管・管理",
                    maxLevel = 3,
                    baseCost = new Dictionary<ResourceType, int>
                    {
                        { ResourceType.Materials, 40 },
                        { ResourceType.Tools, 15 },
                        { ResourceType.Electronics, 8 }
                    }
                }
            };
        }

        public bool CanBuildFacility(FacilityType type)
        {
            if (facilities.ContainsKey(type)) return false;
            if (CurrentFacilityCount >= maxFacilities) return false;

            BaseFacilityData facilityData = GetFacilityData(type);
            if (facilityData == null) return false;

            return HasRequiredResources(facilityData.baseCost);
        }

        public bool BuildFacility(FacilityType type)
        {
            if (!CanBuildFacility(type)) return false;

            BaseFacilityData facilityData = GetFacilityData(type);
            if (facilityData == null) return false;

            if (resourceManager.ConsumeMultipleResources(facilityData.baseCost))
            {
                var facility = new BaseFacility(type, 1, facilityData);
                facilities[type] = facility;
                
                ApplyFacilityEffects(facility);
                OnFacilityBuilt?.Invoke(facility);
                
                // 施設建設完了
                return true;
            }

            return false;
        }

        public bool CanUpgradeFacility(FacilityType type)
        {
            if (!facilities.TryGetValue(type, out BaseFacility facility)) return false;
            
            BaseFacilityData facilityData = GetFacilityData(type);
            if (facilityData == null || facility.Level >= facilityData.maxLevel) return false;

            Dictionary<ResourceType, int> upgradeCost = CalculateUpgradeCost(facilityData.baseCost, facility.Level);
            return HasRequiredResources(upgradeCost);
        }

        public bool UpgradeFacility(FacilityType type)
        {
            if (!CanUpgradeFacility(type)) return false;

            BaseFacility facility = facilities[type];
            BaseFacilityData facilityData = GetFacilityData(type);
            Dictionary<ResourceType, int> upgradeCost = CalculateUpgradeCost(facilityData.baseCost, facility.Level);

            if (resourceManager.ConsumeMultipleResources(upgradeCost))
            {
                RemoveFacilityEffects(facility);
                facility.UpgradeLevel();
                ApplyFacilityEffects(facility);
                
                OnFacilityUpgraded?.Invoke(facility);
                
                // 施設アップグレード完了
                return true;
            }

            return false;
        }

        public bool DestroyFacility(FacilityType type)
        {
            if (!facilities.TryGetValue(type, out BaseFacility facility)) return false;

            RemoveFacilityEffects(facility);
            facilities.Remove(type);
            
            OnFacilityDestroyed?.Invoke(facility);
            
            // 施設破壊完了
            return true;
        }

        private void ApplyFacilityEffects(BaseFacility facility)
        {
            switch (facility.Type)
            {
                case FacilityType.Watchtower:
                    baseDefense += facility.Level * 5f;
                    break;
                case FacilityType.Arsenal:
                    baseDefense += facility.Level * 3f;
                    break;
            }
        }

        private void RemoveFacilityEffects(BaseFacility facility)
        {
            switch (facility.Type)
            {
                case FacilityType.Watchtower:
                    baseDefense -= facility.Level * 5f;
                    break;
                case FacilityType.Arsenal:
                    baseDefense -= facility.Level * 3f;
                    break;
            }
        }

        private bool HasRequiredResources(Dictionary<ResourceType, int> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (!resourceManager.HasEnoughResources(requirement.Key, requirement.Value))
                {
                    return false;
                }
            }
            return true;
        }

        private Dictionary<ResourceType, int> CalculateUpgradeCost(Dictionary<ResourceType, int> baseCost, int currentLevel)
        {
            var upgradeCost = new Dictionary<ResourceType, int>();
            float multiplier = 1f + (currentLevel * 0.5f);

            foreach (var cost in baseCost)
            {
                upgradeCost[cost.Key] = Mathf.RoundToInt(cost.Value * multiplier);
            }

            return upgradeCost;
        }

        private BaseFacilityData GetFacilityData(FacilityType type)
        {
            foreach (var data in availableFacilities)
            {
                if (data.type == type) return data;
            }
            return null;
        }

        public BaseFacility GetFacility(FacilityType type)
        {
            return facilities.TryGetValue(type, out BaseFacility facility) ? facility : null;
        }

        public List<BaseFacility> GetAllFacilities()
        {
            return new List<BaseFacility>(facilities.Values);
        }

        public bool HasFacility(FacilityType type)
        {
            return facilities.ContainsKey(type);
        }

        public int GetFacilityLevel(FacilityType type)
        {
            BaseFacility facility = GetFacility(type);
            return facility?.Level ?? 0;
        }

        public void UpgradeBase()
        {
            baseLevel++;
            maxFacilities += 2;
            OnBaseLevelChanged?.Invoke(baseLevel);
            
            // 拠点レベルアップ完了
        }

        public float GetFacilityEffectiveness(FacilityType type)
        {
            BaseFacility facility = GetFacility(type);
            if (facility == null) return 0f;

            return facility.Level * 0.2f + 0.8f;
        }

        public void ProduceDailyResources()
        {
            if (HasFacility(FacilityType.Garden))
            {
                int gardenLevel = GetFacilityLevel(FacilityType.Garden);
                int foodProduction = gardenLevel * 2;
                resourceManager.AddResources(ResourceType.Food, foodProduction);
            }
        }
    }

    [Serializable]
    public class BaseFacility
    {
        public FacilityType Type { get; private set; }
        public int Level { get; private set; }
        public BaseFacilityData Data { get; private set; }

        public BaseFacility(FacilityType type, int level, BaseFacilityData data)
        {
            Type = type;
            Level = level;
            Data = data;
        }

        public void UpgradeLevel()
        {
            if (Level < Data.maxLevel)
            {
                Level++;
            }
        }
    }

    [Serializable]
    public class BaseFacilityData
    {
        public FacilityType type;
        public string name;
        public string description;
        public int maxLevel;
        public Dictionary<ResourceType, int> baseCost;
    }

    public enum FacilityType
    {
        Dormitory,
        Workshop,
        Watchtower,
        Garden,
        Infirmary,
        Arsenal
    }
}