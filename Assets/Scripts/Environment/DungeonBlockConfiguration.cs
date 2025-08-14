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
                _ => Color.gray
            };
        }
        
        public string GetDisplayName()
        {
            return $"{blockType} ({size.x}x{size.y})";
        }
    }
}