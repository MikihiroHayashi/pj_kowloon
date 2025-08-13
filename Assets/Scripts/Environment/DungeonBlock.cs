using UnityEngine;
using System;
using System.Collections.Generic;

namespace KowloonBreak.Environment
{
    [System.Serializable]
    public class DungeonBlock : MonoBehaviour
    {
        [Header("Block Configuration")]
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
        
        public Vector2Int BlockSize => blockSize;
        public DungeonBlockType BlockType => blockType;
        public float CellSize => cellSize;
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
        
        public Vector3 WorldSize => new Vector3(blockSize.x * cellSize, 0, blockSize.y * cellSize);
        
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
            Gizmos.color = GetGizmoColor();
            Gizmos.DrawWireCube(transform.position, WorldSize);
            
            Gizmos.color = Color.white;
            Vector3 labelPos = transform.position + Vector3.up * 2f;
            UnityEditor.Handles.Label(labelPos, $"{blockType}\n{blockSize.x}x{blockSize.y}");
        }
        
        private Color GetGizmoColor()
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