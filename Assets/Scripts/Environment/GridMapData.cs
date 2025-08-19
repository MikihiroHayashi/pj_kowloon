using UnityEngine;
using System;

namespace KowloonBreak.Environment
{
    [CreateAssetMenu(fileName = "GridMapData", menuName = "Kowloon Break/Grid Map Data")]
    public class GridMapData : ScriptableObject
    {
        [Header("Grid Settings")]
        public Vector2Int gridSize = new Vector2Int(50, 50);
        public float cellSize = 1f;
        
        [Header("Map Data")]
        [SerializeField] private GridCell[] cells;
        
        // Editor用のpublicアクセサー
        public GridCell[] Cells => cells;
        
        [System.Serializable]
        public struct GridCell
        {
            public DungeonBlockType blockType;
            public bool isOccupied;
            public Vector2Int configurationSize; // ブロックのサイズ（1x1, 5x5等）
            public int configurationIndex; // どのDungeonBlockConfigurationを使用するか
        }
        
        public void InitializeGrid()
        {
            Debug.Log($"[GridMapData] InitializeGrid called for {name} - Size: {gridSize.x}x{gridSize.y}");
            Debug.Log($"[GridMapData] InitializeGrid stack trace: {System.Environment.StackTrace}");
            
            int totalCells = gridSize.x * gridSize.y;
            cells = new GridCell[totalCells];
            
            for (int i = 0; i < totalCells; i++)
            {
                cells[i] = new GridCell
                {
                    blockType = DungeonBlockType.Room,
                    isOccupied = false,
                    configurationSize = Vector2Int.one,
                    configurationIndex = -1
                };
            }
            
            Debug.Log($"[GridMapData] Grid initialized with {totalCells} cells");
        }
        
        public void ResizeGrid(Vector2Int newSize)
        {
            var oldSize = gridSize;
            var oldCells = cells;
            
            gridSize = newSize;
            InitializeGrid();
            
            // 既存データをコピー（範囲内のみ）
            if (oldCells != null)
            {
                int minX = Mathf.Min(oldSize.x, newSize.x);
                int minY = Mathf.Min(oldSize.y, newSize.y);
                
                for (int x = 0; x < minX; x++)
                {
                    for (int y = 0; y < minY; y++)
                    {
                        int oldIndex = y * oldSize.x + x;
                        int newIndex = y * newSize.x + x;
                        
                        if (oldIndex < oldCells.Length)
                        {
                            cells[newIndex] = oldCells[oldIndex];
                        }
                    }
                }
            }
        }
        
        public GridCell GetCell(int x, int y)
        {
            if (IsValidPosition(x, y))
            {
                int index = y * gridSize.x + x;
                return cells[index];
            }
            return new GridCell();
        }
        
        public void SetCell(int x, int y, GridCell cell)
        {
            if (IsValidPosition(x, y))
            {
                int index = y * gridSize.x + x;
                cells[index] = cell;
            }
        }
        
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < gridSize.x && y >= 0 && y < gridSize.y;
        }
        
        public void ClearGrid()
        {
            InitializeGrid();
        }
        
        public void FillArea(Vector2Int startPos, Vector2Int size, DungeonBlockType blockType, int configIndex = -1)
        {
            Debug.Log($"[GridMapData] FillArea called: pos=({startPos.x},{startPos.y}), size=({size.x},{size.y}), type={blockType}, configIndex={configIndex}");
            
            int cellsModified = 0;
            for (int x = startPos.x; x < startPos.x + size.x && x < gridSize.x; x++)
            {
                for (int y = startPos.y; y < startPos.y + size.y && y < gridSize.y; y++)
                {
                    if (IsValidPosition(x, y))
                    {
                        var cell = new GridCell
                        {
                            blockType = blockType,
                            isOccupied = true,
                            configurationSize = size,
                            configurationIndex = configIndex
                        };
                        SetCell(x, y, cell);
                        cellsModified++;
                    }
                }
            }
            
            Debug.Log($"[GridMapData] FillArea completed: {cellsModified} cells modified");
        }
        
        public void ClearArea(Vector2Int startPos, Vector2Int size)
        {
            for (int x = startPos.x; x < startPos.x + size.x && x < gridSize.x; x++)
            {
                for (int y = startPos.y; y < startPos.y + size.y && y < gridSize.y; y++)
                {
                    if (IsValidPosition(x, y))
                    {
                        var cell = new GridCell
                        {
                            blockType = DungeonBlockType.Room,
                            isOccupied = false,
                            configurationSize = Vector2Int.one,
                            configurationIndex = -1
                        };
                        SetCell(x, y, cell);
                    }
                }
            }
        }
        
        public bool CanPlaceBlock(Vector2Int position, Vector2Int size)
        {
            if (position.x + size.x > gridSize.x || position.y + size.y > gridSize.y)
                return false;
                
            for (int x = position.x; x < position.x + size.x; x++)
            {
                for (int y = position.y; y < position.y + size.y; y++)
                {
                    if (!IsValidPosition(x, y) || GetCell(x, y).isOccupied)
                        return false;
                }
            }
            return true;
        }
        
        public int GetOccupiedCellCount()
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].isOccupied)
                    count++;
            }
            return count;
        }
        
        public float GetOccupancyPercentage()
        {
            return (float)GetOccupiedCellCount() / cells.Length * 100f;
        }
        
        private void OnValidate()
        {
            // 一時的にOnValidateを無効化してテスト
            Debug.Log($"[GridMapData] OnValidate called for {name} - TEMPORARILY DISABLED FOR TESTING");
            Debug.Log($"[GridMapData] Current state - cells: {(cells == null ? "NULL" : cells.Length.ToString())}, gridSize: {gridSize}");
            
            // 以下をコメントアウト
            /*
            // データが未初期化の場合のみ初期化
            if (cells == null || cells.Length == 0)
            {
                Debug.Log($"[GridMapData] OnValidate: Initializing grid for {name}");
                InitializeGrid();
            }
            // サイズが変更された場合は、既存データを保持しながらリサイズ
            else if (cells.Length != gridSize.x * gridSize.y)
            {
                Debug.Log($"[GridMapData] OnValidate: Resizing grid for {name} from {cells.Length} to {gridSize.x * gridSize.y}");
                ResizeGridKeepingData();
            }
            */
        }
        
        /// <summary>
        /// 既存データを保持しながらリサイズする新しいメソッド
        /// </summary>
        private void ResizeGridKeepingData()
        {
            var oldCells = cells;
            var oldSize = gridSize;
            
            // 旧サイズを推定（完全ではないが最善の努力）
            int oldWidth = 0, oldHeight = 0;
            if (oldCells.Length > 0)
            {
                // 平方根から推定
                int sqrtLength = Mathf.RoundToInt(Mathf.Sqrt(oldCells.Length));
                if (sqrtLength * sqrtLength == oldCells.Length)
                {
                    oldWidth = oldHeight = sqrtLength;
                }
                else
                {
                    // 現在のgridSizeを基準に逆算
                    oldWidth = gridSize.x;
                    oldHeight = oldCells.Length / oldWidth;
                }
            }
            
            // 新しい配列を作成
            int totalCells = gridSize.x * gridSize.y;
            cells = new GridCell[totalCells];

            // まず全セルを初期化
            for (int i = 0; i < totalCells; i++)
            {
                cells[i] = new GridCell
                {
                    blockType = DungeonBlockType.Room,
                    isOccupied = false,
                    configurationSize = Vector2Int.one,
                    configurationIndex = -1
                };
            }

            // 既存データをコピー（範囲内のみ）
            if (oldCells != null && oldCells.Length > 0 && oldWidth > 0 && oldHeight > 0)
            {
                int minX = Mathf.Min(oldWidth, gridSize.x);
                int minY = Mathf.Min(oldHeight, gridSize.y);
                
                for (int x = 0; x < minX; x++)
                {
                    for (int y = 0; y < minY; y++)
                    {
                        int oldIndex = y * oldWidth + x;
                        int newIndex = y * gridSize.x + x;
                        
                        if (oldIndex < oldCells.Length && newIndex < cells.Length)
                        {
                            cells[newIndex] = oldCells[oldIndex];
                        }
                    }
                }
                
                Debug.Log($"[GridMapData] Preserved {minX}x{minY} cells during resize");
            }
        }
    }
}