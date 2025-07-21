using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Managers;

namespace KowloonBreak.Environment
{
    public class ObjectSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private SpawnObjectData[] spawnObjects;
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float gameTimeRespawnHours = 24f; // ゲーム時間24時間
        [SerializeField] private float realTimeRespawnMinutes = 10f; // リアル時間10分
        
        [Header("Spawn Limits")]
        [SerializeField] private int maxTotalObjects = 20;
        [SerializeField] private bool useGameTimeForRespawn = true;
        
        private List<SpawnedObjectInfo> spawnedObjects = new List<SpawnedObjectInfo>();
        private GameManager gameManager;
        private float lastSpawnTime;
        
        public SpawnArea SpawnArea => spawnArea;
        public int CurrentObjectCount => spawnedObjects.Count;
        public int MaxTotalObjects => maxTotalObjects;
        
        private void Awake()
        {
            // SpawnAreaが設定されていない場合は自動で取得
            if (spawnArea == null)
            {
                spawnArea = GetComponent<SpawnArea>();
            }
            
            // SpawnAreaが見つからない場合は作成
            if (spawnArea == null)
            {
                spawnArea = gameObject.AddComponent<SpawnArea>();
            }
        }
        
        private void Start()
        {
            gameManager = GameManager.Instance;
            
            if (spawnOnStart)
            {
                SpawnInitialObjects();
            }
            
            lastSpawnTime = GetCurrentTime();
        }
        
        private void Update()
        {
            CheckForRespawns();
            CleanupDestroyedObjects();
        }
        
        private void SpawnInitialObjects()
        {
            if (spawnObjects == null || spawnObjects.Length == 0)
            {
                CreateDefaultSpawnObjects();
            }
            
            foreach (var spawnObjectData in spawnObjects)
            {
                int spawnCount = Random.Range(spawnObjectData.minCount, spawnObjectData.maxCount + 1);
                
                for (int i = 0; i < spawnCount; i++)
                {
                    if (spawnedObjects.Count >= maxTotalObjects)
                    {
                        Debug.LogWarning("Maximum object count reached");
                        break;
                    }
                    
                    SpawnObject(spawnObjectData);
                }
            }
            
            Debug.Log($"Spawned {spawnedObjects.Count} objects initially");
        }
        
        private void CreateDefaultSpawnObjects()
        {
            // デフォルトの鉄塊スポーン設定
            spawnObjects = new SpawnObjectData[]
            {
                new SpawnObjectData
                {
                    objectName = "IronScrap",
                    prefab = null, // 実行時に作成
                    minCount = 3,
                    maxCount = 6,
                    spawnChance = 1f,
                    objectRadius = 0.5f
                }
            };
        }
        
        private void SpawnObject(SpawnObjectData spawnObjectData)
        {
            if (Random.Range(0f, 1f) > spawnObjectData.spawnChance)
                return;
            
            if (spawnArea.TryGetSpawnPosition(out Vector3 spawnPosition, spawnObjectData.objectRadius))
            {
                GameObject spawnedObject = CreateObject(spawnObjectData, spawnPosition);
                
                if (spawnedObject != null)
                {
                    var spawnedInfo = new SpawnedObjectInfo
                    {
                        gameObject = spawnedObject,
                        spawnData = spawnObjectData,
                        spawnPosition = spawnPosition,
                        spawnTime = GetCurrentTime()
                    };
                    
                    spawnedObjects.Add(spawnedInfo);
                    
                    // 破壊イベントを監視
                    var destructible = spawnedObject.GetComponent<DestructibleObject>();
                    if (destructible != null)
                    {
                        destructible.OnDestroyed += OnObjectDestroyed;
                    }
                    
                    Debug.Log($"Spawned {spawnObjectData.objectName} at {spawnPosition}");
                }
            }
        }
        
        private GameObject CreateObject(SpawnObjectData spawnObjectData, Vector3 position)
        {
            GameObject spawnedObject = null;
            
            if (spawnObjectData.prefab != null)
            {
                // プレハブが指定されている場合
                spawnedObject = Instantiate(spawnObjectData.prefab, position, Quaternion.identity);
            }
            else
            {
                // プレハブが指定されていない場合は動的に作成
                spawnedObject = CreateDefaultObject(spawnObjectData, position);
            }
            
            return spawnedObject;
        }
        
        private GameObject CreateDefaultObject(SpawnObjectData spawnObjectData, Vector3 position)
        {
            GameObject obj = null;
            
            switch (spawnObjectData.objectName)
            {
                case "IronScrap":
                    obj = CreateIronScrapObject(position);
                    break;
                default:
                    Debug.LogWarning($"Unknown object type: {spawnObjectData.objectName}");
                    break;
            }
            
            return obj;
        }
        
        private GameObject CreateIronScrapObject(Vector3 position)
        {
            GameObject obj = new GameObject("IronScrap");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one;
            
            // IronScrapコンポーネントを追加
            var ironScrap = obj.AddComponent<IronScrap>();
            
            // タグを設定
            obj.tag = "DestructibleObject";
            
            return obj;
        }
        
        private void OnObjectDestroyed(DestructibleObject destructible)
        {
            // 破壊されたオブジェクトの情報を更新
            var spawnedInfo = spawnedObjects.Find(info => info.gameObject == destructible.gameObject);
            if (spawnedInfo != null)
            {
                spawnedInfo.isDestroyed = true;
                spawnedInfo.destroyTime = GetCurrentTime();
                
                // スポーンエリアから位置を削除
                spawnArea.UnregisterSpawnedPosition(spawnedInfo.spawnPosition);
                
                Debug.Log($"Object {destructible.gameObject.name} was destroyed");
            }
        }
        
        private void CheckForRespawns()
        {
            float currentTime = GetCurrentTime();
            float respawnInterval = GetRespawnInterval();
            
            // 破壊されたオブジェクトのリスポーンをチェック
            var objectsToRespawn = new List<SpawnedObjectInfo>();
            
            foreach (var spawnedInfo in spawnedObjects)
            {
                if (spawnedInfo.isDestroyed && 
                    currentTime - spawnedInfo.destroyTime >= respawnInterval)
                {
                    objectsToRespawn.Add(spawnedInfo);
                }
            }
            
            // リスポーン実行
            foreach (var info in objectsToRespawn)
            {
                RespawnObject(info);
            }
        }
        
        private void RespawnObject(SpawnedObjectInfo spawnedInfo)
        {
            if (spawnedObjects.Count >= maxTotalObjects)
                return;
            
            // 新しい位置を取得
            if (spawnArea.TryGetSpawnPosition(out Vector3 newPosition, spawnedInfo.spawnData.objectRadius))
            {
                GameObject respawnedObject = CreateObject(spawnedInfo.spawnData, newPosition);
                
                if (respawnedObject != null)
                {
                    // 古い情報を削除
                    spawnedObjects.Remove(spawnedInfo);
                    
                    // 新しい情報を追加
                    var newSpawnedInfo = new SpawnedObjectInfo
                    {
                        gameObject = respawnedObject,
                        spawnData = spawnedInfo.spawnData,
                        spawnPosition = newPosition,
                        spawnTime = GetCurrentTime()
                    };
                    
                    spawnedObjects.Add(newSpawnedInfo);
                    
                    // 破壊イベントを監視
                    var destructible = respawnedObject.GetComponent<DestructibleObject>();
                    if (destructible != null)
                    {
                        destructible.OnDestroyed += OnObjectDestroyed;
                    }
                    
                    Debug.Log($"Respawned {spawnedInfo.spawnData.objectName} at {newPosition}");
                }
            }
        }
        
        private void CleanupDestroyedObjects()
        {
            // 削除されたGameObjectの情報をクリーンアップ
            spawnedObjects.RemoveAll(info => info.gameObject == null);
        }
        
        private float GetCurrentTime()
        {
            if (useGameTimeForRespawn && gameManager != null)
            {
                return gameManager.GameTime / 3600f; // 時間単位に変換
            }
            else
            {
                return Time.time / 60f; // 分単位に変換
            }
        }
        
        private float GetRespawnInterval()
        {
            if (useGameTimeForRespawn)
            {
                return gameTimeRespawnHours;
            }
            else
            {
                return realTimeRespawnMinutes;
            }
        }
        
        public void SpawnObjectManually(string objectName, Vector3 position)
        {
            var spawnData = System.Array.Find(spawnObjects, data => data.objectName == objectName);
            if (spawnData != null)
            {
                GameObject spawnedObject = CreateObject(spawnData, position);
                
                if (spawnedObject != null)
                {
                    var spawnedInfo = new SpawnedObjectInfo
                    {
                        gameObject = spawnedObject,
                        spawnData = spawnData,
                        spawnPosition = position,
                        spawnTime = GetCurrentTime()
                    };
                    
                    spawnedObjects.Add(spawnedInfo);
                    spawnArea.RegisterSpawnedPosition(position);
                    
                    var destructible = spawnedObject.GetComponent<DestructibleObject>();
                    if (destructible != null)
                    {
                        destructible.OnDestroyed += OnObjectDestroyed;
                    }
                }
            }
        }
        
        public void ClearAllObjects()
        {
            foreach (var info in spawnedObjects)
            {
                if (info.gameObject != null)
                {
                    DestroyImmediate(info.gameObject);
                }
            }
            
            spawnedObjects.Clear();
            spawnArea.ClearSpawnedPositions();
        }
        
        public void SetMaxTotalObjects(int maxObjects)
        {
            maxTotalObjects = maxObjects;
        }
        
        public void SetRespawnTime(float gameTimeHours, float realTimeMinutes)
        {
            gameTimeRespawnHours = gameTimeHours;
            realTimeRespawnMinutes = realTimeMinutes;
        }
        
        public void SetUseGameTimeForRespawn(bool useGameTime)
        {
            useGameTimeForRespawn = useGameTime;
        }
        
        public List<SpawnedObjectInfo> GetSpawnedObjects()
        {
            return new List<SpawnedObjectInfo>(spawnedObjects);
        }
        
        public int GetActiveObjectCount()
        {
            return spawnedObjects.FindAll(info => !info.isDestroyed).Count;
        }
        
        public int GetDestroyedObjectCount()
        {
            return spawnedObjects.FindAll(info => info.isDestroyed).Count;
        }
    }
    
    [System.Serializable]
    public class SpawnObjectData
    {
        public string objectName;
        public GameObject prefab;
        public int minCount = 1;
        public int maxCount = 3;
        [Range(0f, 1f)]
        public float spawnChance = 1f;
        public float objectRadius = 0.5f;
    }
    
    [System.Serializable]
    public class SpawnedObjectInfo
    {
        public GameObject gameObject;
        public SpawnObjectData spawnData;
        public Vector3 spawnPosition;
        public float spawnTime;
        public bool isDestroyed;
        public float destroyTime;
    }
}