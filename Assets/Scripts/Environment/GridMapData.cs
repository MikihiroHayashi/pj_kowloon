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
                    }
                }
            }
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
            if (cells == null || cells.Length != gridSize.x * gridSize.y)
            {
                InitializeGrid();
            }
        }
    }
}