using UnityEngine;

namespace KowloonBreak.Environment
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "DungeonBlockConfiguration", menuName = "Kowloon Break/Dungeon Block Configuration")]
    public class DungeonBlockConfiguration : ScriptableObject
    {
        [Header("Block Definition")]
        public GameObject prefab;
        public DungeonBlockType blockType = DungeonBlockType.Room;
        public Vector2Int size = new Vector2Int(5, 5);
        
        [Header("Generation Settings")]
        public float spawnWeight = 1f;
        public int maxInstances = -1;
        
        [Header("Visual Settings")]
        public Color debugColor = Color.white;
        public Material defaultMaterial;
        
        [Header("Road Settings (Road Type Only)")]
        [SerializeField] private GameObject horizontalRoadPrefab;
        [SerializeField] private GameObject verticalRoadPrefab;
        [SerializeField] private GameObject cornerNEPrefab;
        [SerializeField] private GameObject cornerNWPrefab;
        [SerializeField] private GameObject cornerSEPrefab;
        [SerializeField] private GameObject cornerSWPrefab;
        [SerializeField] private GameObject crossPrefab;
        [SerializeField] private GameObject tJunctionNPrefab;
        [SerializeField] private GameObject tJunctionSPrefab;
        [SerializeField] private GameObject tJunctionEPrefab;
        [SerializeField] private GameObject tJunctionWPrefab;
        [SerializeField] private GameObject endCapPrefab;
        [SerializeField] private GameObject singleRoadPrefab;
        
        public DungeonBlockConfiguration()
        {
            blockType = DungeonBlockType.Room;
            size = new Vector2Int(5, 5);
            spawnWeight = 1f;
            maxInstances = -1;
            debugColor = GetDefaultColor(blockType);
        }
        
        public bool IsValid()
        {
            return size.x > 0 && size.y > 0 && spawnWeight >= 0;
        }
        
        public void ValidateAndFix()
        {
            if (size.x <= 0 || size.y <= 0)
            {
                Debug.LogWarning($"Invalid size ({size.x}, {size.y}) for {blockType} - fixing to (5,5)");
                size = new Vector2Int(5, 5);
            }
            
            if (spawnWeight < 0)
            {
                Debug.LogWarning($"Invalid spawn weight {spawnWeight} for {blockType} - fixing to 1.0");
                spawnWeight = 1f;
            }
            
            if (debugColor == Color.clear)
            {
                debugColor = GetDefaultColor(blockType);
            }
        }
        
        public Vector3 GetWorldSize(float cellSize)
        {
            return new Vector3(size.x * cellSize, 0, size.y * cellSize);
        }
        
        public Vector3 GetWorldPosition(Vector2Int gridPosition, float cellSize)
        {
            return new Vector3(gridPosition.x * cellSize, 0, gridPosition.y * cellSize);
        }
        
        public static Color GetDefaultColor(DungeonBlockType blockType)
        {
            return blockType switch
            {
                DungeonBlockType.Room => Color.green,
                DungeonBlockType.Corridor => Color.blue,
                DungeonBlockType.Junction => Color.yellow,
                DungeonBlockType.Special => Color.magenta,
                DungeonBlockType.Entrance => Color.cyan,
                DungeonBlockType.Exit => Color.red,
                DungeonBlockType.Road => new Color(0.8f, 0.6f, 0.4f), // 茶色っぽい色
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// 道路サイズに応じた色を取得
        /// </summary>
        public static Color GetRoadColorBySize(Vector2Int size)
        {
            // サイズで色の明度を変える
            float intensity = Mathf.Clamp01(0.5f + (size.x * size.y) * 0.02f);
            return new Color(0.8f * intensity, 0.6f * intensity, 0.4f * intensity);
        }
        
        public string GetDisplayName()
        {
            return $"{blockType} ({size.x}x{size.y})";
        }
        
        /// <summary>
        /// 道路タイプの場合のみ、指定方向のPrefabを取得
        /// </summary>
        public GameObject GetRoadPrefab(RoadDirection direction)
        {
            if (blockType != DungeonBlockType.Road)
            {
                Debug.LogWarning($"GetRoadPrefab called on non-road block type: {blockType}");
                return prefab; // 通常のprefabを返す
            }
            
            return direction switch
            {
                RoadDirection.Horizontal => horizontalRoadPrefab,
                RoadDirection.Vertical => verticalRoadPrefab,
                RoadDirection.CornerNE => cornerNEPrefab,
                RoadDirection.CornerNW => cornerNWPrefab,
                RoadDirection.CornerSE => cornerSEPrefab,
                RoadDirection.CornerSW => cornerSWPrefab,
                RoadDirection.Cross => crossPrefab,
                RoadDirection.TJunctionN => tJunctionNPrefab,
                RoadDirection.TJunctionS => tJunctionSPrefab,
                RoadDirection.TJunctionE => tJunctionEPrefab,
                RoadDirection.TJunctionW => tJunctionWPrefab,
                RoadDirection.EndCap => endCapPrefab,
                RoadDirection.Single => singleRoadPrefab,
                _ => horizontalRoadPrefab ?? prefab
            };
        }
        
        /// <summary>
        /// 道路設定が有効かチェック
        /// </summary>
        public bool HasValidRoadConfiguration()
        {
            if (blockType != DungeonBlockType.Road) return true; // 道路以外は常に有効
            
            return horizontalRoadPrefab != null || verticalRoadPrefab != null || 
                   crossPrefab != null || singleRoadPrefab != null;
        }
    }
}