using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using KowloonBreak.Core;

namespace KowloonBreak.Environment
{
    public class KowloonLevelManager : MonoBehaviour
    {
        public static KowloonLevelManager Instance { get; private set; }

        [Header("Level Configuration")]
        [SerializeField] private KowloonLevel[] availableLevels;
        [SerializeField] private KowloonLevel currentLevel;
        [SerializeField] private int currentFloor = 1;
        [SerializeField] private Vector3 playerSpawnPoint;

        [Header("Level Generation")]
        [SerializeField] private bool enableProceduralGeneration = false;
        [SerializeField] private int maxFloorsPerLevel = 10;
        [SerializeField] private LevelGenerationSettings generationSettings;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private GameObject transitionUI;
        [SerializeField] private AudioClip transitionSound;

        private Dictionary<int, KowloonLevel> loadedLevels;
        private LevelTransition currentTransition;
        private bool isTransitioning;

        public KowloonLevel CurrentLevel => currentLevel;
        public int CurrentFloor => currentFloor;
        public bool IsTransitioning => isTransitioning;

        public event Action<KowloonLevel> OnLevelLoaded;
        public event Action<KowloonLevel> OnLevelUnloaded;
        public event Action<int> OnFloorChanged;
        public event Action<LevelTransition> OnTransitionStarted;
        public event Action<LevelTransition> OnTransitionCompleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLevelManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeLevelManager()
        {
            loadedLevels = new Dictionary<int, KowloonLevel>();
            
            if (availableLevels == null || availableLevels.Length == 0)
            {
                CreateDefaultLevels();
            }

            if (generationSettings == null)
            {
                generationSettings = CreateDefaultGenerationSettings();
            }

            LoadInitialLevel();
            
            Debug.Log("Kowloon Level Manager Initialized");
        }

        private void CreateDefaultLevels()
        {
            availableLevels = new KowloonLevel[]
            {
                CreateLevel(1, LevelType.Underground, "地下区画", 0.8f),
                CreateLevel(2, LevelType.Industrial, "工業区画", 0.7f),
                CreateLevel(3, LevelType.Residential, "居住区画", 0.5f),
                CreateLevel(4, LevelType.Commercial, "商業区画", 0.4f),
                CreateLevel(5, LevelType.Residential, "中層住宅", 0.3f),
                CreateLevel(6, LevelType.Commercial, "上層商業", 0.2f),
                CreateLevel(7, LevelType.Rooftop, "屋上区画", 0.6f)
            };
        }

        private KowloonLevel CreateLevel(int floor, LevelType type, string name, float danger)
        {
            var level = new KowloonLevel
            {
                FloorNumber = floor,
                Type = type,
                Name = name,
                DangerLevel = danger,
                Rooms = GenerateDefaultRooms(type),
                Connections = GenerateConnections(floor),
                AvailableResources = GenerateResources(type),
                IsExplored = false,
                IsUnlocked = floor == 1
            };
            return level;
        }

        private List<Room> GenerateDefaultRooms(LevelType type)
        {
            var rooms = new List<Room>();
            
            switch (type)
            {
                case LevelType.Residential:
                    rooms.AddRange(new[]
                    {
                        new Room { Name = "アパート1", Type = RoomType.Living, IsAccessible = true },
                        new Room { Name = "アパート2", Type = RoomType.Living, IsAccessible = true },
                        new Room { Name = "共用廊下", Type = RoomType.Corridor, IsAccessible = true },
                        new Room { Name = "階段", Type = RoomType.Stairway, IsAccessible = true }
                    });
                    break;
                    
                case LevelType.Commercial:
                    rooms.AddRange(new[]
                    {
                        new Room { Name = "商店1", Type = RoomType.Shop, IsAccessible = true },
                        new Room { Name = "商店2", Type = RoomType.Shop, IsAccessible = true },
                        new Room { Name = "食堂", Type = RoomType.Restaurant, IsAccessible = true },
                        new Room { Name = "メイン通路", Type = RoomType.Corridor, IsAccessible = true }
                    });
                    break;
                    
                case LevelType.Industrial:
                    rooms.AddRange(new[]
                    {
                        new Room { Name = "工場エリア", Type = RoomType.Factory, IsAccessible = true },
                        new Room { Name = "倉庫", Type = RoomType.Storage, IsAccessible = true },
                        new Room { Name = "機械室", Type = RoomType.Utility, IsAccessible = false },
                        new Room { Name = "作業通路", Type = RoomType.Corridor, IsAccessible = true }
                    });
                    break;
                    
                case LevelType.Underground:
                    rooms.AddRange(new[]
                    {
                        new Room { Name = "下水道", Type = RoomType.Sewer, IsAccessible = true },
                        new Room { Name = "地下室", Type = RoomType.Basement, IsAccessible = false },
                        new Room { Name = "電気室", Type = RoomType.Utility, IsAccessible = false },
                        new Room { Name = "メンテナンス通路", Type = RoomType.Corridor, IsAccessible = true }
                    });
                    break;
                    
                case LevelType.Rooftop:
                    rooms.AddRange(new[]
                    {
                        new Room { Name = "屋上", Type = RoomType.Rooftop, IsAccessible = true },
                        new Room { Name = "給水塔", Type = RoomType.Utility, IsAccessible = false },
                        new Room { Name = "屋上小屋", Type = RoomType.Storage, IsAccessible = true }
                    });
                    break;
            }
            
            return rooms;
        }

        private List<Connection> GenerateConnections(int floor)
        {
            var connections = new List<Connection>();
            
            if (floor > 1)
            {
                connections.Add(new Connection
                {
                    TargetFloor = floor - 1,
                    ConnectionType = ConnectionType.Stairway,
                    IsAccessible = true,
                    RequiredKey = null
                });
            }
            
            if (floor < maxFloorsPerLevel)
            {
                connections.Add(new Connection
                {
                    TargetFloor = floor + 1,
                    ConnectionType = ConnectionType.Stairway,
                    IsAccessible = false,
                    RequiredKey = $"Key_Floor_{floor + 1}"
                });
            }
            
            return connections;
        }

        private List<Resource> GenerateResources(LevelType type)
        {
            var resources = new List<Resource>();
            
            switch (type)
            {
                case LevelType.Residential:
                    resources.Add(new Resource(ResourceType.Food, UnityEngine.Random.Range(2, 8)));
                    resources.Add(new Resource(ResourceType.Water, UnityEngine.Random.Range(1, 5)));
                    resources.Add(new Resource(ResourceType.Clothing, UnityEngine.Random.Range(1, 4)));
                    break;
                    
                case LevelType.Commercial:
                    resources.Add(new Resource(ResourceType.Food, UnityEngine.Random.Range(5, 15)));
                    resources.Add(new Resource(ResourceType.Medicine, UnityEngine.Random.Range(1, 6)));
                    resources.Add(new Resource(ResourceType.Electronics, UnityEngine.Random.Range(1, 3)));
                    break;
                    
                case LevelType.Industrial:
                    resources.Add(new Resource(ResourceType.Materials, UnityEngine.Random.Range(10, 25)));
                    resources.Add(new Resource(ResourceType.Tools, UnityEngine.Random.Range(2, 8)));
                    resources.Add(new Resource(ResourceType.Fuel, UnityEngine.Random.Range(1, 5)));
                    break;
                    
                case LevelType.Underground:
                    resources.Add(new Resource(ResourceType.Materials, UnityEngine.Random.Range(5, 15)));
                    resources.Add(new Resource(ResourceType.Water, UnityEngine.Random.Range(2, 10)));
                    break;
                    
                case LevelType.Rooftop:
                    resources.Add(new Resource(ResourceType.Electronics, UnityEngine.Random.Range(2, 6)));
                    resources.Add(new Resource(ResourceType.Information, UnityEngine.Random.Range(1, 3)));
                    break;
            }
            
            return resources;
        }

        private LevelGenerationSettings CreateDefaultGenerationSettings()
        {
            return new LevelGenerationSettings
            {
                MinRoomsPerLevel = 4,
                MaxRoomsPerLevel = 12,
                ResourceSpawnChance = 0.7f,
                EnemySpawnChance = 0.3f,
                SecretRoomChance = 0.15f,
                HazardSpawnChance = 0.2f
            };
        }

        private void LoadInitialLevel()
        {
            if (availableLevels != null && availableLevels.Length > 0)
            {
                LoadLevel(1);
            }
        }

        public bool CanAccessFloor(int targetFloor)
        {
            if (availableLevels == null) return false;
            
            foreach (var level in availableLevels)
            {
                if (level.FloorNumber == targetFloor)
                {
                    return level.IsUnlocked;
                }
            }
            
            return false;
        }

        public void LoadLevel(int floorNumber)
        {
            if (isTransitioning) return;
            
            KowloonLevel targetLevel = GetLevelByFloor(floorNumber);
            if (targetLevel == null)
            {
                Debug.LogWarning($"Level {floorNumber} not found");
                return;
            }
            
            if (!targetLevel.IsUnlocked)
            {
                Debug.LogWarning($"Level {floorNumber} is locked");
                return;
            }
            
            StartLevelTransition(targetLevel);
        }

        private void StartLevelTransition(KowloonLevel targetLevel)
        {
            var transition = new LevelTransition
            {
                FromLevel = currentLevel,
                ToLevel = targetLevel,
                TransitionType = TransitionType.Standard,
                Duration = transitionDuration
            };
            
            currentTransition = transition;
            isTransitioning = true;
            
            OnTransitionStarted?.Invoke(transition);
            
            StartCoroutine(ExecuteLevelTransition(transition));
        }

        private System.Collections.IEnumerator ExecuteLevelTransition(LevelTransition transition)
        {
            if (transitionUI != null)
            {
                transitionUI.SetActive(true);
            }
            
            if (transitionSound != null)
            {
                AudioSource.PlayClipAtPoint(transitionSound, UnityEngine.Camera.main.transform.position);
            }
            
            yield return new WaitForSeconds(transition.Duration * 0.5f);
            
            if (transition.FromLevel != null)
            {
                UnloadLevel(transition.FromLevel);
            }
            
            LoadLevelInternal(transition.ToLevel);
            
            yield return new WaitForSeconds(transition.Duration * 0.5f);
            
            if (transitionUI != null)
            {
                transitionUI.SetActive(false);
            }
            
            CompleteLevelTransition(transition);
        }

        private void LoadLevelInternal(KowloonLevel level)
        {
            currentLevel = level;
            currentFloor = level.FloorNumber;
            
            loadedLevels[level.FloorNumber] = level;
            
            level.IsExplored = true;
            
            OnLevelLoaded?.Invoke(level);
            OnFloorChanged?.Invoke(currentFloor);
            
            Debug.Log($"Loaded level: Floor {level.FloorNumber} - {level.Name}");
        }

        private void UnloadLevel(KowloonLevel level)
        {
            if (loadedLevels.ContainsKey(level.FloorNumber))
            {
                loadedLevels.Remove(level.FloorNumber);
            }
            
            OnLevelUnloaded?.Invoke(level);
            
            Debug.Log($"Unloaded level: Floor {level.FloorNumber} - {level.Name}");
        }

        private void CompleteLevelTransition(LevelTransition transition)
        {
            isTransitioning = false;
            OnTransitionCompleted?.Invoke(transition);
            
            Debug.Log($"Level transition completed: {transition.ToLevel.Name}");
        }

        public KowloonLevel GetLevelByFloor(int floorNumber)
        {
            if (availableLevels == null) return null;
            
            foreach (var level in availableLevels)
            {
                if (level.FloorNumber == floorNumber)
                {
                    return level;
                }
            }
            
            return null;
        }

        public void UnlockFloor(int floorNumber)
        {
            KowloonLevel level = GetLevelByFloor(floorNumber);
            if (level != null)
            {
                level.IsUnlocked = true;
                Debug.Log($"Floor {floorNumber} unlocked");
            }
        }

        public void LockFloor(int floorNumber)
        {
            KowloonLevel level = GetLevelByFloor(floorNumber);
            if (level != null)
            {
                level.IsUnlocked = false;
                Debug.Log($"Floor {floorNumber} locked");
            }
        }

        public List<int> GetAccessibleFloors()
        {
            var accessibleFloors = new List<int>();
            
            if (availableLevels != null)
            {
                foreach (var level in availableLevels)
                {
                    if (level.IsUnlocked)
                    {
                        accessibleFloors.Add(level.FloorNumber);
                    }
                }
            }
            
            return accessibleFloors;
        }

        public List<KowloonLevel> GetExploredLevels()
        {
            var exploredLevels = new List<KowloonLevel>();
            
            if (availableLevels != null)
            {
                foreach (var level in availableLevels)
                {
                    if (level.IsExplored)
                    {
                        exploredLevels.Add(level);
                    }
                }
            }
            
            return exploredLevels;
        }

        public float GetOverallExplorationProgress()
        {
            if (availableLevels == null || availableLevels.Length == 0) return 0f;
            
            int exploredCount = 0;
            foreach (var level in availableLevels)
            {
                if (level.IsExplored) exploredCount++;
            }
            
            return (float)exploredCount / availableLevels.Length;
        }
    }

    [Serializable]
    public class KowloonLevel
    {
        public int FloorNumber;
        public LevelType Type;
        public string Name;
        public List<Room> Rooms;
        public List<Connection> Connections;
        public List<Resource> AvailableResources;
        public float DangerLevel;
        public bool IsExplored;
        public bool IsUnlocked;
    }

    [Serializable]
    public class Room
    {
        public string Name;
        public RoomType Type;
        public bool IsAccessible;
        public bool IsExplored;
        public string RequiredKey;
        public Vector3 Position;
        public Vector3 Size;
    }

    [Serializable]
    public class Connection
    {
        public int TargetFloor;
        public ConnectionType ConnectionType;
        public bool IsAccessible;
        public string RequiredKey;
        public Vector3 Position;
    }

    [Serializable]
    public class LevelTransition
    {
        public KowloonLevel FromLevel;
        public KowloonLevel ToLevel;
        public TransitionType TransitionType;
        public float Duration;
    }

    [Serializable]
    public class LevelGenerationSettings
    {
        public int MinRoomsPerLevel;
        public int MaxRoomsPerLevel;
        public float ResourceSpawnChance;
        public float EnemySpawnChance;
        public float SecretRoomChance;
        public float HazardSpawnChance;
    }

    public enum LevelType
    {
        Residential,
        Commercial,
        Industrial,
        Underground,
        Rooftop,
        Abandoned
    }

    public enum RoomType
    {
        Living,
        Shop,
        Restaurant,
        Factory,
        Storage,
        Utility,
        Corridor,
        Stairway,
        Sewer,
        Basement,
        Rooftop
    }

    public enum ConnectionType
    {
        Stairway,
        Elevator,
        Ladder,
        Tunnel,
        Bridge
    }

    public enum TransitionType
    {
        Standard,
        Emergency,
        Stealth,
        Combat
    }
}