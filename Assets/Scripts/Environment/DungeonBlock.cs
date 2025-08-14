using UnityEngine;
using System;
using System.Collections.Generic;

namespace KowloonBreak.Environment
{
    [System.Serializable]
    public class DungeonBlock : MonoBehaviour
    {
        [Header("Block Configuration")]
        [SerializeField] private DungeonBlockConfiguration configuration;
        
        // 後方互換性のために保持
        [Header("Legacy Settings (Use Configuration instead)")]
        [SerializeField] private Vector2Int blockSize = new Vector2Int(5, 5);
        [SerializeField] private DungeonBlockType blockType;
        [SerializeField] private float cellSize = 1f;
        
        [Header("Connection Points")]
        [SerializeField] private Transform[] northConnectors;
        [SerializeField] private Transform[] southConnectors;
        [SerializeField] private Transform[] eastConnectors;
        [SerializeField] private Transform[] westConnectors;
        
        [Header("Spawn Points")]
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] itemSpawnPoints;
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("Environment")]
        [SerializeField] private GameObject[] decorativeObjects;
        [SerializeField] private GameObject[] obstacles;
        [SerializeField] private GameObject[] interactableObjects;
        
        private Vector2Int gridPosition;
        private bool isOccupied = false;
        
        // 新しいConfiguration経由でのアクセス
        public Vector2Int BlockSize => configuration?.size ?? blockSize;
        public DungeonBlockType BlockType => configuration?.blockType ?? blockType;  
        public float CellSize => cellSize;
        public DungeonBlockConfiguration Configuration => configuration;
        public Vector2Int GridPosition 
        { 
            get => gridPosition; 
            set => gridPosition = value; 
        }
        public bool IsOccupied 
        { 
            get => isOccupied; 
            set => isOccupied = value; 
        }
        
        public Vector3 WorldSize => configuration?.GetWorldSize(cellSize) ?? new Vector3(blockSize.x * cellSize, 0, blockSize.y * cellSize);
        
        public void InitializeFromConfiguration(DungeonBlockConfiguration config, float cellSize)
        {
            this.configuration = config;
            this.cellSize = cellSize;
            
            // 後方互換性のためにlegacy設定も更新
            if (config != null)
            {
                this.blockSize = config.size;
                this.blockType = config.blockType;
                
                // Configurationが有効か確認
                config.ValidateAndFix();
            }
        }
        
        public void SetGridPosition(Vector2Int gridPos)
        {
            this.gridPosition = gridPos;
            if (configuration != null)
            {
                transform.position = configuration.GetWorldPosition(gridPos, cellSize);
            }
            else
            {
                transform.position = GetWorldPosition(gridPos, cellSize);
            }
        }
        
        public bool CanConnectTo(DungeonBlock otherBlock, Direction direction)
        {
            if (otherBlock == null) return false;
            
            Transform[] myConnectors = GetConnectors(direction);
            Transform[] otherConnectors = otherBlock.GetConnectors(GetOppositeDirection(direction));
            
            return myConnectors != null && otherConnectors != null && 
                   myConnectors.Length > 0 && otherConnectors.Length > 0;
        }
        
        public Transform[] GetConnectors(Direction direction)
        {
            return direction switch
            {
                Direction.North => northConnectors,
                Direction.South => southConnectors,
                Direction.East => eastConnectors,
                Direction.West => westConnectors,
                _ => null
            };
        }
        
        private Direction GetOppositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => Direction.North
            };
        }
        
        public Vector3 GetWorldPosition(Vector2Int gridPos, float cellSize)
        {
            return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
        }
        
        public void SetPosition(Vector2Int gridPos, float cellSize)
        {
            this.gridPosition = gridPos;
            transform.position = GetWorldPosition(gridPos, cellSize);
        }
        
        public void ActivateBlock(bool active)
        {
            gameObject.SetActive(active);
            isOccupied = active;
        }
        
        public Transform GetRandomEnemySpawnPoint()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return null;
            return enemySpawnPoints[UnityEngine.Random.Range(0, enemySpawnPoints.Length)];
        }
        
        public Transform GetRandomItemSpawnPoint()
        {
            if (itemSpawnPoints == null || itemSpawnPoints.Length == 0) return null;
            return itemSpawnPoints[UnityEngine.Random.Range(0, itemSpawnPoints.Length)];
        }
        
        public Transform GetPlayerSpawnPoint()
        {
            return playerSpawnPoint;
        }
        
        public void EnableDecorations(bool enable)
        {
            if (decorativeObjects != null)
            {
                foreach (var obj in decorativeObjects)
                {
                    if (obj != null) obj.SetActive(enable);
                }
            }
        }
        
        public void EnableObstacles(bool enable)
        {
            if (obstacles != null)
            {
                foreach (var obj in obstacles)
                {
                    if (obj != null) obj.SetActive(enable);
                }
            }
        }
        
        public void EnableInteractables(bool enable)
        {
            if (interactableObjects != null)
            {
                foreach (var obj in interactableObjects)
                {
                    if (obj != null) obj.SetActive(enable);
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            // エディターが選択されている場合はカスタムエディターに任せる
            if (UnityEditor.Selection.activeGameObject == gameObject)
                return;
                
            Gizmos.color = GetGizmoColor();
            Vector3 size = WorldSize;
            Vector3 center = transform.position + new Vector3(size.x * 0.5f, 0, size.z * 0.5f);
            
            Gizmos.DrawWireCube(center, size);
            
            // ラベル表示（エディター非選択時のみ）
            #if UNITY_EDITOR
            Gizmos.color = Color.white;
            Vector3 labelPos = center + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"{BlockType}\n{BlockSize.x}x{BlockSize.y}");
            #endif
        }
        
        private Color GetGizmoColor()
        {
            if (configuration != null)
            {
                return configuration.debugColor;
            }
            
            // レガシー表示
            return DungeonBlockConfiguration.GetDefaultColor(BlockType);
        }
    }
    
    [Serializable]
    public enum DungeonBlockType
    {
        Room,
        Corridor,
        Junction,
        Special,
        Entrance,
        Exit
    }
    
    [Serializable]
    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}