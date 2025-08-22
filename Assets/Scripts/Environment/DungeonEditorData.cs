using System;
using System.Collections.Generic;
using UnityEngine;

namespace KowloonBreak.Environment
{
    [CreateAssetMenu(fileName = "New Dungeon Layout", menuName = "Kowloon Break/Dungeon Layout")]
    public class DungeonLayout : ScriptableObject
    {
        [Header("Grid Configuration")]
        public Vector2Int gridSize = new Vector2Int(10, 10);
        public float cellSize = 5f;
        
        [Header("Pieces")]
        public List<DungeonPiece> pieces = new List<DungeonPiece>();
        
        [Header("Roads")]
        public List<RoadPath> roadPaths = new List<RoadPath>();
        
        [Header("Settings")]
        public string layoutName = "New Layout";
        public LevelType levelType = LevelType.Residential;
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(layoutName))
            {
                layoutName = name;
            }
            
            // グリッドサイズの最小値制限
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            
            // セルサイズの最小値制限
            cellSize = Mathf.Max(0.1f, cellSize);
            
            // ピースのIDが空の場合は生成
            foreach (var piece in pieces)
            {
                if (string.IsNullOrEmpty(piece.id))
                {
                    piece.id = System.Guid.NewGuid().ToString();
                }
            }
            
            // プレファブが失われているピースの自動修復
            RepairMissingPrefabs();
            
            // 道路パスのIDが空の場合は生成
            foreach (var roadPath in roadPaths)
            {
                if (string.IsNullOrEmpty(roadPath.id))
                {
                    roadPath.id = System.Guid.NewGuid().ToString();
                }
            }
        }
        
        public void ClearLayout()
        {
            pieces.Clear();
            roadPaths.Clear();
        }
        
        public DungeonPiece GetPieceAt(Vector2Int gridPosition)
        {
            foreach (var piece in pieces)
            {
                if (IsPositionInPiece(gridPosition, piece))
                {
                    return piece;
                }
            }
            return null;
        }
        
        private bool IsPositionInPiece(Vector2Int position, DungeonPiece piece)
        {
            return position.x >= piece.gridPosition.x &&
                   position.x < piece.gridPosition.x + piece.size.x &&
                   position.y >= piece.gridPosition.y &&
                   position.y < piece.gridPosition.y + piece.size.y;
        }
        
        public bool CanPlacePiece(Vector2Int position, Vector2Int size)
        {
            return GridUtility.CanPlacePiece(position, size, gridSize, GetGridArray());
        }
        
        private DungeonPiece[,] GetGridArray()
        {
            var grid = new DungeonPiece[gridSize.x, gridSize.y];
            
            foreach (var piece in pieces)
            {
                for (int x = 0; x < piece.size.x; x++)
                {
                    for (int y = 0; y < piece.size.y; y++)
                    {
                        Vector2Int pos = piece.gridPosition + new Vector2Int(x, y);
                        if (GridUtility.IsValidGridPosition(pos, gridSize))
                        {
                            grid[pos.x, pos.y] = piece;
                        }
                    }
                }
            }
            
            return grid;
        }
        
        private void RepairMissingPrefabs()
        {
            #if UNITY_EDITOR
            var library = FindDungeonPieceLibrary();
            if (library == null) return;
            
            int repairedCount = 0;
            foreach (var piece in pieces)
            {
                if (piece.prefab == null)
                {
                    var matchingTemplate = library.FindPieceTemplate(piece.type, piece.size);
                    if (matchingTemplate != null && matchingTemplate.prefab != null)
                    {
                        piece.prefab = matchingTemplate.prefab;
                        repairedCount++;
                        UnityEngine.Debug.Log($"Repaired missing prefab for piece {piece.type} at {piece.gridPosition}");
                    }
                }
            }
            
            if (repairedCount > 0)
            {
                UnityEngine.Debug.Log($"Repaired {repairedCount} missing prefab references");
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            #endif
        }
        
        #if UNITY_EDITOR
        private DungeonPieceLibrary FindDungeonPieceLibrary()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:DungeonPieceLibrary");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var library = UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonPieceLibrary>(path);
                if (library != null && library.TargetLevelType == levelType)
                {
                    return library;
                }
            }
            
            // レベルタイプが一致しない場合は最初に見つかったライブラリを返す
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonPieceLibrary>(path);
            }
            
            return null;
        }
        #endif
    }

    [Serializable]
    public class DungeonPiece
    {
        public string id;
        public PieceType type;
        public Vector2Int size;
        public Vector2Int gridPosition;
        public float rotation;
        public GameObject prefab;
        public bool isRoadStartPoint;
    }

    [Serializable]
    public class RoadPath
    {
        public string id;
        public List<Vector2Int> pathPoints = new List<Vector2Int>();
        public List<RoadSegment> segments = new List<RoadSegment>();
        public bool isComplete;
    }

    [Serializable]
    public class RoadSegment
    {
        public Vector2Int position;
        public RoadType roadType;
        public float rotation;
        public GameObject prefab;
        public Vector2Int[] connections = new Vector2Int[4];
    }

    [Serializable]
    public class DungeonSettings
    {
        [Header("Asset References")]
        public DungeonPieceLibrary pieceLibrary;
        public DungeonRoadPrefabSet roadPrefabSet;
        
        [Header("Default Settings")]
        public float defaultCellSize = 5f;
        public Vector2Int defaultGridSize = new Vector2Int(20, 20);
    }

    public enum PieceType
    {
        Building,
        RoadStart,
        Obstacle,
        Decoration,
        SpawnPoint,
        ExitPoint
    }

    public enum RoadType
    {
        Straight,
        Corner,
        TJunction,
        Cross,
        EndCap
    }

    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public static class GridUtility
    {
        public static Vector3 GridToWorldPosition(Vector2Int gridPos, float cellSize)
        {
            // グリッド座標からワールド座標への変換（センター配置）
            return new Vector3(
                gridPos.x * cellSize + cellSize * 0.5f, 
                0, 
                gridPos.y * cellSize + cellSize * 0.5f
            );
        }

        public static Vector2Int WorldToGridPosition(Vector3 worldPos, float cellSize)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / cellSize),
                Mathf.FloorToInt(worldPos.z / cellSize)
            );
        }

        public static bool IsValidGridPosition(Vector2Int pos, Vector2Int gridSize)
        {
            return pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0 && pos.y < gridSize.y;
        }

        public static bool CanPlacePiece(Vector2Int position, Vector2Int pieceSize, Vector2Int gridSize, DungeonPiece[,] grid)
        {
            for (int x = 0; x < pieceSize.x; x++)
            {
                for (int y = 0; y < pieceSize.y; y++)
                {
                    Vector2Int checkPos = position + new Vector2Int(x, y);
                    
                    if (!IsValidGridPosition(checkPos, gridSize))
                        return false;
                    
                    if (grid[checkPos.x, checkPos.y] != null)
                        return false;
                }
            }
            
            return true;
        }

        public static Vector2Int GetDirectionVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Vector2Int.up;
                case Direction.East: return Vector2Int.right;
                case Direction.South: return Vector2Int.down;
                case Direction.West: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }

        public static Direction GetOppositeDirection(Direction direction)
        {
            return (Direction)(((int)direction + 2) % 4);
        }
    }
}