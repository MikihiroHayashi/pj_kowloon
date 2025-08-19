using UnityEngine;
using System.Collections.Generic;

namespace KowloonBreak.Environment
{
    /// <summary>
    /// エディター専用のダンジョン管理クラス
    /// 3Dダンジョンブロックの配置・管理のみを行う
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private Vector2Int dungeonSize = new Vector2Int(100, 100);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private bool showDebugGizmos = true;
        
        [Header("Generated Blocks")]
        [SerializeField] private List<DungeonBlock> generatedBlocks = new List<DungeonBlock>();
        
        public Vector2Int DungeonSize => dungeonSize;
        public float CellSize => cellSize;
        public bool ShowDebugGizmos => showDebugGizmos;

        private void Awake()
        {
            // エディター専用なので、実行時の複雑な初期化は不要
            if (generatedBlocks == null)
                generatedBlocks = new List<DungeonBlock>();
        }

        /// <summary>
        /// エディターから生成されたブロックを登録
        /// </summary>
        public void RegisterBlock(DungeonBlock block)
        {
            if (block != null && !generatedBlocks.Contains(block))
            {
                generatedBlocks.Add(block);
            }
        }

        /// <summary>
        /// 全ブロックをクリア
        /// </summary>
        public void ClearAllBlocks()
        {
            generatedBlocks.Clear();
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public DungeonStats GetStats()
        {
            var stats = new DungeonStats();
            
            foreach (var block in generatedBlocks)
            {
                if (block == null) continue;
                
                switch (block.BlockType)
                {
                    case DungeonBlockType.Room:
                        stats.roomCount++;
                        break;
                    case DungeonBlockType.Corridor:
                        stats.corridorCount++;
                        break;
                    case DungeonBlockType.Junction:
                        stats.junctionCount++;
                        break;
                    case DungeonBlockType.Special:
                        stats.specialCount++;
                        break;
                    case DungeonBlockType.Road:
                        stats.roadCount++;
                        break;
                }
            }
            
            stats.totalBlocks = generatedBlocks.Count;
            return stats;
        }

        /// <summary>
        /// 全てのブロックを取得（エディター用）
        /// </summary>
        public List<DungeonBlock> GetAllBlocks()
        {
            // null要素を除去
            generatedBlocks.RemoveAll(block => block == null);
            return new List<DungeonBlock>(generatedBlocks);
        }

        /// <summary>
        /// 指定タイプのブロックを取得
        /// </summary>
        public List<DungeonBlock> GetBlocksByType(DungeonBlockType blockType)
        {
            var result = new List<DungeonBlock>();
            foreach (var block in generatedBlocks)
            {
                if (block != null && block.BlockType == blockType)
                {
                    result.Add(block);
                }
            }
            return result;
        }

        /// <summary>
        /// 指定位置のブロックを取得
        /// </summary>
        public DungeonBlock GetBlockAt(Vector2Int gridPosition)
        {
            foreach (var block in generatedBlocks)
            {
                if (block != null && block.GridPosition == gridPosition)
                {
                    return block;
                }
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = Color.white;
            Vector3 dungeonWorldSize = new Vector3(dungeonSize.x * cellSize, 0, dungeonSize.y * cellSize);
            Gizmos.DrawWireCube(transform.position + dungeonWorldSize * 0.5f, dungeonWorldSize);
        }
    }

    /// <summary>
    /// ダンジョン統計情報
    /// </summary>
    [System.Serializable]
    public class DungeonStats
    {
        public int totalBlocks;
        public int roomCount;
        public int corridorCount;
        public int junctionCount;
        public int specialCount;
        public int roadCount;
    }

    // Legacy classes - 互換性のために残すが使用は非推奨
    [System.Serializable]
    [System.Obsolete("Use DungeonBlockConfiguration instead")]
    public class DungeonBlockData
    {
        public GameObject prefab;
        public DungeonBlockType blockType = DungeonBlockType.Room;
        public Vector2Int size = new Vector2Int(5, 5);
        public float spawnWeight = 1f;
        public int maxInstances = -1;
    }

    // Legacy grid class - 互換性のために残すが使用は非推奨  
    [System.Obsolete("Grid functionality moved to GridMapData")]
    public class DungeonGrid
    {
        private int width, height;
        public int Width => width;
        public int Height => height;
        
        public DungeonGrid(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
        
        public bool IsOccupied(int x, int y) => false;
        public void OccupyArea(Vector2Int position, Vector2Int size, DungeonBlock block) { }
        public DungeonBlock GetBlock(int x, int y) => null;
    }
}