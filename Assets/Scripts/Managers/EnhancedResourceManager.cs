using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Managers
{
    public class EnhancedResourceManager : MonoBehaviour
    {
        public static EnhancedResourceManager Instance { get; private set; }

        [Header("Resource Configuration")]
        [SerializeField] private EnhancedResourceData[] initialResources;
        [SerializeField] private float deteriorationUpdateInterval = 60f;

        [Header("Inventory Configuration")]
        [SerializeField] private int toolSlots = 8;
        [SerializeField] private int materialSlots = 32;
        [SerializeField] private ItemData[] availableItems;
        
        [Header("Default Items (ScriptableObject Assets)")]
        [SerializeField] private ItemData pickaxeItem;
        [SerializeField] private ItemData ironPipeItem;
        [SerializeField] private ItemData scrapItem;

        [Header("Initial Items")]
        [SerializeField] private InitialItemData[] initialItems;

        private Dictionary<ResourceType, Resource> resources;
        private float deteriorationTimer;
        
        // インベントリシステム
        private InventorySlot[] toolInventory;
        private InventorySlot[] materialInventory;
        private Dictionary<string, ItemData> itemDatabase;

        public int ToolSlots => toolSlots;
        public int MaterialSlots => materialSlots;
        public InventorySlot[] ToolInventory => toolInventory;
        public InventorySlot[] MaterialInventory => materialInventory;

        // 既存のResourceManagerイベント
        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<ResourceType> OnResourceDepleted;
        public event Action<ResourceType, float> OnResourceQualityChanged;

        // 新しいインベントリイベント
        public event Action<int, InventorySlot> OnToolSlotChanged;
        public event Action<int, InventorySlot> OnMaterialSlotChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;

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
                InitializeEnhancedResourceManager();
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

        private void InitializeEnhancedResourceManager()
        {
            // 既存のリソースシステム初期化
            InitializeResourceSystem();
            
            // 新しいインベントリシステム初期化
            InitializeInventorySystem();
            
            // EnhancedResourceManager初期化完了
        }

        #region Resource System (既存機能)

        private void InitializeResourceSystem()
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
        }

        private void CreateDefaultResources()
        {
            initialResources = new EnhancedResourceData[]
            {
                new EnhancedResourceData { type = ResourceType.Food, initialAmount = 10, maxAmount = 100, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Water, initialAmount = 15, maxAmount = 100, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Medicine, initialAmount = 5, maxAmount = 50, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Materials, initialAmount = 20, maxAmount = 200, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Ammunition, initialAmount = 10, maxAmount = 100, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Fuel, initialAmount = 5, maxAmount = 50, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Electronics, initialAmount = 3, maxAmount = 30, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Clothing, initialAmount = 8, maxAmount = 80, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Tools, initialAmount = 5, maxAmount = 50, quality = 1f },
                new EnhancedResourceData { type = ResourceType.Information, initialAmount = 0, maxAmount = 20, quality = 1f }
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

        // 既存のResourceManagerメソッド
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
                return true;
            }
            
            return false;
        }

        public void AddResources(ResourceType type, int amount)
        {
            Resource resource = GetResource(type);
            if (resource != null)
            {
                resource.AddAmount(amount);
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

        #endregion

        #region Inventory System (新機能)

        private void InitializeInventorySystem()
        {
            // インベントリスロット初期化
            toolInventory = new InventorySlot[toolSlots];
            materialInventory = new InventorySlot[materialSlots];
            
            for (int i = 0; i < toolSlots; i++)
            {
                toolInventory[i] = new InventorySlot();
                toolInventory[i].OnSlotChanged += (slot) => OnToolSlotChanged?.Invoke(i, slot);
            }
            
            for (int i = 0; i < materialSlots; i++)
            {
                materialInventory[i] = new InventorySlot();
                materialInventory[i].OnSlotChanged += (slot) => OnMaterialSlotChanged?.Invoke(i, slot);
            }

            // アイテムデータベース作成
            CreateItemDatabase();
            
            // 初期アイテム追加
            AddInitialItems();
        }

        private void CreateItemDatabase()
        {
            itemDatabase = new Dictionary<string, ItemData>();
            
            if (availableItems == null || availableItems.Length == 0)
            {
                CreateDefaultItems();
            }

            foreach (var item in availableItems)
            {
                itemDatabase[item.itemName] = item;
            }
        }

        private void CreateDefaultItems()
        {
            var itemList = new List<ItemData>();
            
            // ScriptableObjectアセットから読み込み
            if (pickaxeItem != null) itemList.Add(pickaxeItem);
            if (ironPipeItem != null) itemList.Add(ironPipeItem);
            if (scrapItem != null) itemList.Add(scrapItem);
            
            // 追加のアイテムがある場合
            if (availableItems != null && availableItems.Length > 0)
            {
                itemList.AddRange(availableItems);
            }
            
            // フォールバック：アセットが設定されていない場合はランタイムで作成
            if (itemList.Count == 0)
            {
                Debug.LogWarning("ScriptableObject assets not assigned. Creating default items at runtime.");
                
                var pickaxe = ScriptableObject.CreateInstance<ItemData>();
                pickaxe.itemName = "つるはし";
                pickaxe.itemType = ItemType.Tool;
                pickaxe.description = "鉄塊を破壊するための道具";
                pickaxe.toolType = ToolType.Pickaxe;
                pickaxe.durability = 100;
                pickaxe.attackDamage = 2f;
                pickaxe.attackRange = 1.5f;
                pickaxe.maxStackSize = 1;

                var ironPipe = ScriptableObject.CreateInstance<ItemData>();
                ironPipe.itemName = "鉄パイプ";
                ironPipe.itemType = ItemType.Tool;
                ironPipe.description = "汎用的な武器として使用可能";
                ironPipe.toolType = ToolType.IronPipe;
                ironPipe.durability = 80;
                ironPipe.attackDamage = 1.5f;
                ironPipe.attackRange = 1.2f;
                ironPipe.maxStackSize = 1;

                var scrap = ScriptableObject.CreateInstance<ItemData>();
                scrap.itemName = "ガラクタ";
                scrap.itemType = ItemType.Material;
                scrap.description = "鉄塊から採取された金属片";
                scrap.materialType = MaterialType.Scrap;
                scrap.value = 1f;
                scrap.maxStackSize = 99;

                itemList.AddRange(new ItemData[] { pickaxe, ironPipe, scrap });
            }
            
            availableItems = itemList.ToArray();
        }

        private void AddInitialItems()
        {
            if (initialItems == null || initialItems.Length == 0)
            {
                // デフォルトの初期アイテム
                AddItem("つるはし", 1);
                AddItem("鉄パイプ", 1);
            }
            else
            {
                foreach (var initialItem in initialItems)
                {
                    AddItem(initialItem.itemName, initialItem.quantity);
                }
            }
        }

        public ItemData GetItemData(string itemName)
        {
            return itemDatabase.TryGetValue(itemName, out ItemData item) ? item : null;
        }

        public bool AddItem(string itemName, int quantity = 1, int durability = -1)
        {
            ItemData itemData = GetItemData(itemName);
            if (itemData == null)
            {
                return false;
            }

            return AddItem(itemData, quantity, durability);
        }

        public bool AddItem(ItemData itemData, int quantity = 1, int durability = -1)
        {
            if (itemData == null || quantity <= 0) return false;

            InventorySlot[] targetInventory = itemData.IsTool() ? toolInventory : materialInventory;
            int remainingQuantity = quantity;

            // まず既存のスロットにスタック可能かチェック
            for (int i = 0; i < targetInventory.Length && remainingQuantity > 0; i++)
            {
                if (targetInventory[i].CanAddItem(itemData, remainingQuantity))
                {
                    remainingQuantity = targetInventory[i].AddItem(itemData, remainingQuantity, durability);
                }
            }

            // 残りがあれば空のスロットに追加
            for (int i = 0; i < targetInventory.Length && remainingQuantity > 0; i++)
            {
                if (targetInventory[i].IsEmpty)
                {
                    remainingQuantity = targetInventory[i].AddItem(itemData, remainingQuantity, durability);
                }
            }

            int addedQuantity = quantity - remainingQuantity;
            if (addedQuantity > 0)
            {
                OnItemAdded?.Invoke(itemData, addedQuantity);
            }

            return remainingQuantity == 0;
        }

        public bool RemoveItem(string itemName, int quantity = 1)
        {
            ItemData itemData = GetItemData(itemName);
            if (itemData == null) return false;

            return RemoveItem(itemData, quantity);
        }

        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return false;

            InventorySlot[] targetInventory = itemData.IsTool() ? toolInventory : materialInventory;
            int remainingQuantity = quantity;

            // 後ろから削除していく
            for (int i = targetInventory.Length - 1; i >= 0 && remainingQuantity > 0; i--)
            {
                if (targetInventory[i].ItemData == itemData)
                {
                    int toRemove = Mathf.Min(remainingQuantity, targetInventory[i].Quantity);
                    if (targetInventory[i].RemoveItem(toRemove))
                    {
                        remainingQuantity -= toRemove;
                    }
                }
            }

            int removedQuantity = quantity - remainingQuantity;
            if (removedQuantity > 0)
            {
                OnItemRemoved?.Invoke(itemData, removedQuantity);
            }

            return remainingQuantity == 0;
        }

        public int GetItemCount(string itemName)
        {
            ItemData itemData = GetItemData(itemName);
            if (itemData == null) return 0;

            return GetItemCount(itemData);
        }

        public int GetItemCount(ItemData itemData)
        {
            if (itemData == null) return 0;

            InventorySlot[] targetInventory = itemData.IsTool() ? toolInventory : materialInventory;
            int totalCount = 0;

            for (int i = 0; i < targetInventory.Length; i++)
            {
                if (targetInventory[i].ItemData == itemData)
                {
                    totalCount += targetInventory[i].Quantity;
                }
            }

            return totalCount;
        }

        public bool HasItem(string itemName, int quantity = 1)
        {
            return GetItemCount(itemName) >= quantity;
        }

        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            return GetItemCount(itemData) >= quantity;
        }

        public InventorySlot GetToolSlot(int index)
        {
            if (index < 0 || index >= toolSlots) return null;
            return toolInventory[index];
        }

        public InventorySlot GetMaterialSlot(int index)
        {
            if (index < 0 || index >= materialSlots) return null;
            return materialInventory[index];
        }

        public List<InventorySlot> GetAllItems()
        {
            var allItems = new List<InventorySlot>();
            
            foreach (var slot in toolInventory)
            {
                if (!slot.IsEmpty)
                    allItems.Add(slot);
            }
            
            foreach (var slot in materialInventory)
            {
                if (!slot.IsEmpty)
                    allItems.Add(slot);
            }
            
            return allItems;
        }

        public bool IsInventoryFull()
        {
            return GetEmptySlotCount() == 0;
        }

        public int GetEmptySlotCount()
        {
            int emptyCount = 0;
            
            foreach (var slot in toolInventory)
            {
                if (slot.IsEmpty) emptyCount++;
            }
            
            foreach (var slot in materialInventory)
            {
                if (slot.IsEmpty) emptyCount++;
            }
            
            return emptyCount;
        }

        #endregion
    }

    [Serializable]
    public class EnhancedResourceData
    {
        public ResourceType type;
        public int initialAmount;
        public int maxAmount;
        public float quality = 1f;
    }

    [Serializable]
    public class InitialItemData
    {
        public string itemName;
        public int quantity = 1;
        public int durability = -1;
    }
}