using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Managers;
using KowloonBreak.UI;

namespace KowloonBreak.Exploration
{
    public class ExplorationSystem : MonoBehaviour
    {
        public static ExplorationSystem Instance { get; private set; }

        [Header("Exploration Configuration")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableLayers = -1;
        [SerializeField] private LayerMask explorationLayers = -1;
        [SerializeField] private float explorationRadius = 2f;

        [Header("Discovery Settings")]
        [SerializeField] private float discoveryTime = 2f;
        [SerializeField] private float searchCooldown = 1f;
        [SerializeField] private int maxDiscoveryAttempts = 3;

        [Header("Loot Configuration")]
        [SerializeField] private LootTable[] lootTables;
        [SerializeField] private float baseLootChance = 0.6f;
        [SerializeField] private float rareLootChance = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioClip discoverySound;
        [SerializeField] private AudioClip searchSound;
        [SerializeField] private AudioClip failureSound;

        private Transform playerTransform;
        private Dictionary<string, ExplorationPoint> explorationPoints;
        private Dictionary<string, DiscoveredItem> discoveredItems;
        private float lastSearchTime;
        private bool isSearching;

        public bool IsSearching => isSearching;
        public int TotalDiscoveries => discoveredItems.Count;

        public event Action<ExplorationPoint> OnExplorationPointDiscovered;
        public event Action<DiscoveredItem> OnItemDiscovered;
        public event Action<string, float> OnSearchProgress;
        public event Action OnSearchCompleted;
        public event Action OnSearchFailed;

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
                InitializeExplorationSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupPlayerReference();
            GenerateExplorationPoints();
        }

        private void Update()
        {
            UpdateNearbyExploration();
            HandleInput();
        }

        private void InitializeExplorationSystem()
        {
            explorationPoints = new Dictionary<string, ExplorationPoint>();
            discoveredItems = new Dictionary<string, DiscoveredItem>();
            
            if (lootTables == null || lootTables.Length == 0)
            {
                CreateDefaultLootTables();
            }
            
            Debug.Log("Exploration System Initialized");
        }

        private void CreateDefaultLootTables()
        {
            lootTables = new LootTable[]
            {
                new LootTable
                {
                    tableName = "Common Items",
                    items = new LootItem[]
                    {
                        new LootItem { itemName = "缶詰", resourceType = ResourceType.Food, amount = 1, dropChance = 0.4f },
                        new LootItem { itemName = "水ボトル", resourceType = ResourceType.Water, amount = 1, dropChance = 0.3f },
                        new LootItem { itemName = "古い服", resourceType = ResourceType.Clothing, amount = 1, dropChance = 0.2f },
                        new LootItem { itemName = "スクラップ", resourceType = ResourceType.Materials, amount = 2, dropChance = 0.5f }
                    }
                },
                new LootTable
                {
                    tableName = "Medical Supplies",
                    items = new LootItem[]
                    {
                        new LootItem { itemName = "包帯", resourceType = ResourceType.Medicine, amount = 1, dropChance = 0.3f },
                        new LootItem { itemName = "抗生物質", resourceType = ResourceType.Medicine, amount = 1, dropChance = 0.1f },
                        new LootItem { itemName = "痛み止め", resourceType = ResourceType.Medicine, amount = 1, dropChance = 0.2f }
                    }
                },
                new LootTable
                {
                    tableName = "Electronics",
                    items = new LootItem[]
                    {
                        new LootItem { itemName = "回路基板", resourceType = ResourceType.Electronics, amount = 1, dropChance = 0.15f },
                        new LootItem { itemName = "バッテリー", resourceType = ResourceType.Electronics, amount = 1, dropChance = 0.25f },
                        new LootItem { itemName = "ケーブル", resourceType = ResourceType.Electronics, amount = 1, dropChance = 0.3f }
                    }
                },
                new LootTable
                {
                    tableName = "Information",
                    items = new LootItem[]
                    {
                        new LootItem { itemName = "手記", resourceType = ResourceType.Information, amount = 1, dropChance = 0.1f },
                        new LootItem { itemName = "地図の断片", resourceType = ResourceType.Information, amount = 1, dropChance = 0.05f },
                        new LootItem { itemName = "写真", resourceType = ResourceType.Information, amount = 1, dropChance = 0.08f }
                    }
                }
            };
        }

        private void SetupPlayerReference()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void GenerateExplorationPoints()
        {
            var levelManager = KowloonLevelManager.Instance;
            if (levelManager == null || levelManager.CurrentLevel == null) return;

            foreach (var room in levelManager.CurrentLevel.Rooms)
            {
                if (room.IsAccessible)
                {
                    CreateExplorationPoint(room);
                }
            }
        }

        private void CreateExplorationPoint(Room room)
        {
            string pointId = $"Room_{room.Name}_{UnityEngine.Random.Range(1000, 9999)}";
            
            var explorationPoint = new ExplorationPoint
            {
                Id = pointId,
                Name = $"{room.Name}の調査地点",
                Position = room.Position + UnityEngine.Random.insideUnitSphere * 3f,
                Type = GetExplorationTypeFromRoom(room.Type),
                IsDiscovered = false,
                SearchAttempts = 0,
                DifficultyLevel = CalculateDifficulty(room),
                LootTableName = SelectLootTable(room.Type)
            };

            explorationPoints[pointId] = explorationPoint;
        }

        private ExplorationType GetExplorationTypeFromRoom(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Living => ExplorationType.Container,
                RoomType.Shop => ExplorationType.Container,
                RoomType.Storage => ExplorationType.HiddenCache,
                RoomType.Utility => ExplorationType.Debris,
                RoomType.Factory => ExplorationType.Debris,
                RoomType.Sewer => ExplorationType.Debris,
                _ => ExplorationType.Container
            };
        }

        private float CalculateDifficulty(Room room)
        {
            float baseDifficulty = 0.3f;
            
            if (!room.IsExplored) baseDifficulty += 0.2f;
            if (room.RequiredKey != null) baseDifficulty += 0.3f;
            
            return Mathf.Clamp01(baseDifficulty + UnityEngine.Random.Range(-0.1f, 0.2f));
        }

        private string SelectLootTable(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Living => "Common Items",
                RoomType.Shop => "Common Items",
                RoomType.Storage => "Common Items",
                RoomType.Utility => "Electronics",
                RoomType.Factory => "Electronics",
                _ => "Common Items"
            };
        }

        private void UpdateNearbyExploration()
        {
            if (playerTransform == null) return;

            foreach (var explorationPoint in explorationPoints.Values)
            {
                float distance = Vector3.Distance(playerTransform.position, explorationPoint.Position);
                
                if (distance <= explorationRadius && !explorationPoint.IsDiscovered)
                {
                    DiscoverExplorationPoint(explorationPoint);
                }
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.E) && !isSearching)
            {
                TryStartSearch();
            }
        }

        private void DiscoverExplorationPoint(ExplorationPoint point)
        {
            point.IsDiscovered = true;
            OnExplorationPointDiscovered?.Invoke(point);
            
            PlayDiscoverySound();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"調査地点を発見: {point.Name}", NotificationType.Info);
            }
            
            Debug.Log($"Exploration point discovered: {point.Name}");
        }

        public void TryStartSearch()
        {
            if (Time.time - lastSearchTime < searchCooldown) return;
            if (isSearching) return;

            ExplorationPoint nearestPoint = GetNearestExplorationPoint();
            if (nearestPoint != null)
            {
                StartSearch(nearestPoint);
            }
        }

        private ExplorationPoint GetNearestExplorationPoint()
        {
            if (playerTransform == null) return null;

            ExplorationPoint nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var point in explorationPoints.Values)
            {
                if (!point.IsDiscovered || point.SearchAttempts >= maxDiscoveryAttempts) continue;

                float distance = Vector3.Distance(playerTransform.position, point.Position);
                if (distance <= interactionRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        private void StartSearch(ExplorationPoint point)
        {
            isSearching = true;
            lastSearchTime = Time.time;
            point.SearchAttempts++;
            
            StartCoroutine(SearchCoroutine(point));
            
            PlaySearchSound();
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"調査中: {point.Name}", NotificationType.Info);
            }
        }

        private System.Collections.IEnumerator SearchCoroutine(ExplorationPoint point)
        {
            float searchTime = discoveryTime * (1f + point.DifficultyLevel);
            float elapsedTime = 0f;
            
            while (elapsedTime < searchTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / searchTime;
                
                OnSearchProgress?.Invoke(point.Id, progress);
                yield return null;
            }
            
            bool searchSuccess = DetermineSearchSuccess(point);
            
            if (searchSuccess)
            {
                CompleteSuccessfulSearch(point);
            }
            else
            {
                CompleteFailedSearch(point);
            }
            
            isSearching = false;
            OnSearchCompleted?.Invoke();
        }

        private bool DetermineSearchSuccess(ExplorationPoint point)
        {
            float baseSuccessChance = baseLootChance;
            float difficultyPenalty = point.DifficultyLevel * 0.3f;
            float attemptPenalty = (point.SearchAttempts - 1) * 0.15f;
            
            float finalChance = baseSuccessChance - difficultyPenalty - attemptPenalty;
            return UnityEngine.Random.Range(0f, 1f) < finalChance;
        }

        private void CompleteSuccessfulSearch(ExplorationPoint point)
        {
            var discoveredItem = GenerateLoot(point);
            if (discoveredItem != null)
            {
                discoveredItems[discoveredItem.Id] = discoveredItem;
                OnItemDiscovered?.Invoke(discoveredItem);
                
                var resourceManager = EnhancedResourceManager.Instance;
                if (resourceManager != null)
                {
                    resourceManager.AddResources(discoveredItem.ResourceType, discoveredItem.Amount);
                }
                
                PlayDiscoverySound();
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowNotification(
                        $"{discoveredItem.Name} を発見! x{discoveredItem.Amount}", 
                        NotificationType.Success
                    );
                }
                
                Debug.Log($"Item discovered: {discoveredItem.Name} x{discoveredItem.Amount}");
            }
        }

        private void CompleteFailedSearch(ExplorationPoint point)
        {
            OnSearchFailed?.Invoke();
            
            PlayFailureSound();
            
            if (UIManager.Instance != null)
            {
                string message = point.SearchAttempts >= maxDiscoveryAttempts 
                    ? "この場所にはもう何もないようだ..." 
                    : "何も見つからなかった...";
                
                UIManager.Instance.ShowNotification(message, NotificationType.Warning);
            }
            
            Debug.Log($"Search failed at: {point.Name} (Attempt {point.SearchAttempts})");
        }

        private DiscoveredItem GenerateLoot(ExplorationPoint point)
        {
            LootTable table = GetLootTable(point.LootTableName);
            if (table == null) return null;

            foreach (var lootItem in table.items)
            {
                if (UnityEngine.Random.Range(0f, 1f) < lootItem.dropChance)
                {
                    return new DiscoveredItem
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        Name = lootItem.itemName,
                        ResourceType = lootItem.resourceType,
                        Amount = lootItem.amount,
                        DiscoveryTime = System.DateTime.Now,
                        Location = point.Name
                    };
                }
            }

            return null;
        }

        private LootTable GetLootTable(string tableName)
        {
            foreach (var table in lootTables)
            {
                if (table.tableName == tableName)
                {
                    return table;
                }
            }
            return lootTables.Length > 0 ? lootTables[0] : null;
        }

        private void PlayDiscoverySound()
        {
            if (discoverySound != null)
            {
                AudioSource.PlayClipAtPoint(discoverySound, playerTransform.position);
            }
        }

        private void PlaySearchSound()
        {
            if (searchSound != null)
            {
                AudioSource.PlayClipAtPoint(searchSound, playerTransform.position);
            }
        }

        private void PlayFailureSound()
        {
            if (failureSound != null)
            {
                AudioSource.PlayClipAtPoint(failureSound, playerTransform.position);
            }
        }

        public List<ExplorationPoint> GetDiscoveredPoints()
        {
            var discovered = new List<ExplorationPoint>();
            foreach (var point in explorationPoints.Values)
            {
                if (point.IsDiscovered)
                {
                    discovered.Add(point);
                }
            }
            return discovered;
        }

        public List<DiscoveredItem> GetDiscoveredItems()
        {
            return new List<DiscoveredItem>(discoveredItems.Values);
        }

        public float GetExplorationProgress()
        {
            if (explorationPoints.Count == 0) return 0f;
            
            int discoveredCount = 0;
            foreach (var point in explorationPoints.Values)
            {
                if (point.IsDiscovered) discoveredCount++;
            }
            
            return (float)discoveredCount / explorationPoints.Count;
        }

        public void AddCustomExplorationPoint(Vector3 position, string name, ExplorationType type)
        {
            string pointId = $"Custom_{System.Guid.NewGuid()}";
            
            var explorationPoint = new ExplorationPoint
            {
                Id = pointId,
                Name = name,
                Position = position,
                Type = type,
                IsDiscovered = false,
                SearchAttempts = 0,
                DifficultyLevel = 0.3f,
                LootTableName = "Common Items"
            };
            
            explorationPoints[pointId] = explorationPoint;
        }
    }

    [Serializable]
    public class ExplorationPoint
    {
        public string Id;
        public string Name;
        public Vector3 Position;
        public ExplorationType Type;
        public bool IsDiscovered;
        public int SearchAttempts;
        public float DifficultyLevel;
        public string LootTableName;
    }

    [Serializable]
    public class DiscoveredItem
    {
        public string Id;
        public string Name;
        public ResourceType ResourceType;
        public int Amount;
        public System.DateTime DiscoveryTime;
        public string Location;
    }

    [Serializable]
    public class LootTable
    {
        public string tableName;
        public LootItem[] items;
    }

    [Serializable]
    public class LootItem
    {
        public string itemName;
        public ResourceType resourceType;
        public int amount;
        public float dropChance;
    }

    public enum ExplorationType
    {
        Container,
        HiddenCache,
        Debris,
        SecretArea,
        Document
    }
}