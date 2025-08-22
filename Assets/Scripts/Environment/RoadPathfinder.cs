using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KowloonBreak.Environment
{
    public class RoadPathfinder
    {
        private DungeonLayout layout;
        private DungeonPiece[,] grid;
        private DungeonRoadPrefabSet roadPrefabs;

        public RoadPathfinder(DungeonLayout dungeonLayout, DungeonRoadPrefabSet prefabSet)
        {
            layout = dungeonLayout;
            roadPrefabs = prefabSet;
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            grid = new DungeonPiece[layout.gridSize.x, layout.gridSize.y];
            
            foreach (var piece in layout.pieces)
            {
                PlacePieceOnGrid(piece);
            }
        }

        private void PlacePieceOnGrid(DungeonPiece piece)
        {
            for (int x = 0; x < piece.size.x; x++)
            {
                for (int y = 0; y < piece.size.y; y++)
                {
                    Vector2Int pos = piece.gridPosition + new Vector2Int(x, y);
                    if (GridUtility.IsValidGridPosition(pos, layout.gridSize))
                    {
                        grid[pos.x, pos.y] = piece;
                    }
                }
            }
        }

        public List<RoadPath> GenerateRoadPaths()
        {
            var roadPaths = new List<RoadPath>();
            var roadGroups = GetRoadStartPointsByGroup();

            if (roadGroups.Count == 0)
            {
                Debug.LogWarning("No road start points found");
                return roadPaths;
            }

            Debug.Log($"Found {roadGroups.Count} road groups");

            // 各グループ内で道路を生成
            foreach (var group in roadGroups)
            {
                int groupId = group.Key;
                var startPoints = group.Value;

                // 単独のRoadStartピースも処理する（隣接建物がある場合）
                if (startPoints.Count < 1)
                {
                    Debug.LogWarning($"Road group {groupId} has no start points");
                    continue;
                }

                if (startPoints.Count == 1)
                {
                    // 単独ピースの場合は隣接建物チェック
                    var singlePoint = startPoints[0];
                    if (HasAdjacentBuilding(singlePoint))
                    {
                        Debug.Log($"Creating road for single RoadStart at {singlePoint} in group {groupId} with adjacent building");
                        var singlePath = CreateSinglePointPath(groupId, singlePoint);
                        if (singlePath != null)
                        {
                            roadPaths.Add(singlePath);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Road group {groupId} has only 1 start point and no adjacent buildings");
                    }
                    continue;
                }

                Debug.Log($"Generating roads for group {groupId} with {startPoints.Count} start points");

                var groupPath = GeneratePathForGroup(groupId, startPoints);
                if (groupPath != null)
                {
                    roadPaths.Add(groupPath);
                }
            }

            // 交差点を検知して統合
            ProcessIntersections(roadPaths);

            return roadPaths;
        }

        private Dictionary<int, List<Vector2Int>> GetRoadStartPointsByGroup()
        {
            var roadGroups = new Dictionary<int, List<Vector2Int>>();

            foreach (var piece in layout.pieces)
            {
                if (piece.type == PieceType.RoadStart || piece.isRoadStartPoint)
                {
                    int groupId = piece.roadGroupId;
                    if (groupId <= 0) groupId = 1; // デフォルトグループ

                    if (!roadGroups.ContainsKey(groupId))
                    {
                        roadGroups[groupId] = new List<Vector2Int>();
                    }
                    
                    roadGroups[groupId].Add(piece.gridPosition);
                    Debug.Log($"Added road start point {piece.gridPosition} to group {groupId}");
                }
            }

            return roadGroups;
        }

        private RoadPath GeneratePathForGroup(int groupId, List<Vector2Int> startPoints)
        {
            var connectedPoints = new HashSet<Vector2Int>();
            var roadPath = new RoadPath
            {
                id = System.Guid.NewGuid().ToString(),
                pathPoints = new List<Vector2Int>(),
                roadGroupId = groupId
            };

            var currentPoint = startPoints[0];
            roadPath.pathPoints.Add(currentPoint);
            connectedPoints.Add(currentPoint);

            while (connectedPoints.Count < startPoints.Count)
            {
                Vector2Int nextPoint = FindNearestUnconnectedPoint(currentPoint, startPoints, connectedPoints);
                
                if (nextPoint == Vector2Int.zero)
                {
                    break;
                }

                var pathSegment = FindPath(currentPoint, nextPoint);
                
                if (pathSegment != null && pathSegment.Count > 1)
                {
                    for (int i = 1; i < pathSegment.Count; i++)
                    {
                        roadPath.pathPoints.Add(pathSegment[i]);
                    }
                    connectedPoints.Add(nextPoint);
                    currentPoint = nextPoint;
                }
                else
                {
                    break;
                }
            }

            if (roadPath.pathPoints.Count > 1)
            {
                roadPath.segments = GenerateRoadSegments(roadPath.pathPoints, groupId);
                roadPath.isComplete = connectedPoints.Count == startPoints.Count;
                Debug.Log($"Road path for group {groupId} generated with {roadPath.pathPoints.Count} points");
                return roadPath;
            }
            else
            {
                Debug.LogWarning($"Failed to generate road path for group {groupId} - insufficient points");
                return null;
            }
        }

        private List<Vector2Int> GetRoadStartPoints()
        {
            var startPoints = new List<Vector2Int>();
            
            foreach (var piece in layout.pieces)
            {
                // PieceType.RoadStart または isRoadStartPoint が true のピースを検索
                if (piece.type == PieceType.RoadStart || piece.isRoadStartPoint)
                {
                    startPoints.Add(piece.gridPosition);
                }
            }
            
            Debug.Log($"Road start points found: {startPoints.Count}");
            if (startPoints.Count == 0)
            {
                Debug.LogWarning("No road start points found. Place Road Start pieces to generate roads.");
            }

            return startPoints;
        }

        private Vector2Int FindNearestUnconnectedPoint(Vector2Int current, List<Vector2Int> allPoints, HashSet<Vector2Int> connected)
        {
            Vector2Int nearest = Vector2Int.zero;
            float minDistance = float.MaxValue;

            foreach (var point in allPoints)
            {
                if (connected.Contains(point))
                    continue;

                float distance = Vector2Int.Distance(current, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            // パス検索開始前にチェック
            if (!IsPassable(start))
            {
                Debug.LogError($"Start position {start} is not passable!");
                return null;
            }
            if (!IsPassable(end))
            {
                Debug.LogError($"End position {end} is not passable!");  
                return null;
            }

            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, end) };

            int iterations = 0;
            int maxIterations = layout.gridSize.x * layout.gridSize.y; // グリッドサイズに応じた上限
            
            while (openSet.Count > 0)
            {
                var current = GetLowestFScore(openSet, fScore);
                
                iterations++;
                if (iterations > maxIterations) // 無限ループ防止
                {
                    Debug.LogError($"FindPath: Path search timed out after {iterations} iterations ({start} -> {end})");
                    break;
                }
                
                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!IsPassable(neighbor))
                        continue;

                    float tentativeGScore = gScore.GetValueOrDefault(current, float.MaxValue) + 1;

                    if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // パス検索失敗の詳細分析
            Debug.LogWarning($"No path found: {start} -> {end} (searched {iterations} nodes)");
            
            // 直線距離をチェック
            float directDistance = Vector2Int.Distance(start, end);
            Debug.Log($"Direct distance: {directDistance:F1} units");
            
            // 周辺の障害物をチェック
            int blockedCells = 0;
            int totalCells = 0;
            Vector2Int min = new Vector2Int(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y));
            Vector2Int max = new Vector2Int(Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));
            
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    totalCells++;
                    if (!IsPassable(pos))
                    {
                        blockedCells++;
                    }
                }
            }
            
            float blockageRatio = (float)blockedCells / totalCells;
            Debug.Log($"Area blockage: {blockedCells}/{totalCells} ({blockageRatio:P1}) - Consider moving Road Start points or removing blocking buildings");
            
            return null;
        }

        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
        {
            Vector2Int lowest = openSet[0];
            float lowestScore = fScore.GetValueOrDefault(lowest, float.MaxValue);

            for (int i = 1; i < openSet.Count; i++)
            {
                float score = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                if (score < lowestScore)
                {
                    lowest = openSet[i];
                    lowestScore = score;
                }
            }

            return lowest;
        }

        private List<Vector2Int> GetNeighbors(Vector2Int position)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var direction in directions)
            {
                Vector2Int neighbor = position + direction;
                if (GridUtility.IsValidGridPosition(neighbor, layout.gridSize))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        private bool IsPassable(Vector2Int position)
        {
            if (!GridUtility.IsValidGridPosition(position, layout.gridSize))
                return false;

            var piece = grid[position.x, position.y];
            
            // 空のセルは通行可能
            if (piece == null)
                return true;
                
            // Road Start ポイントは通行可能
            if (piece.type == PieceType.RoadStart || piece.isRoadStartPoint)
                return true;
                
            // その他のピース（建物など）は通行不可
            return false;
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        private List<RoadSegment> GenerateRoadSegments(List<Vector2Int> pathPoints, int groupId = 1)
        {
            var segments = new List<RoadSegment>();

            for (int i = 0; i < pathPoints.Count; i++)
            {
                var segment = new RoadSegment
                {
                    position = pathPoints[i],
                    roadGroupId = groupId
                };

                segment.roadType = DetermineRoadType(pathPoints, i);
                segment.rotation = DetermineRotation(pathPoints, i, segment.roadType);
                segment.prefab = GetRoadPrefab(segment.roadType, segment.rotation);
                
                // デバッグ情報
                if (IsRoadStartPoint(pathPoints[i]))
                {
                    var connections = GetConnections(pathPoints, i);
                    int connectionCount = 0;
                    string connectionInfo = "";
                    for (int j = 0; j < connections.Length; j++)
                    {
                        if (connections[j] != Vector2Int.zero)
                        {
                            connectionCount++;
                            string[] dirNames = {"North", "East", "South", "West"};
                            connectionInfo += $"{dirNames[j]} ";
                        }
                    }
                    Debug.Log($"RoadStart segment at {pathPoints[i]} (Group {groupId}): Connections={connectionCount} ({connectionInfo}), Type={segment.roadType}, Rotation={segment.rotation}, Prefab={segment.prefab?.name}");
                }

                segments.Add(segment);
            }

            return segments;
        }

        private RoadType DetermineRoadType(List<Vector2Int> pathPoints, int index)
        {
            Vector2Int currentPos = pathPoints[index];
            Vector2Int[] connections = GetConnections(pathPoints, index);
            int connectionCount = 0;
            
            foreach (var connection in connections)
            {
                if (connection != Vector2Int.zero)
                    connectionCount++;
            }

            switch (connectionCount)
            {
                case 1: return RoadType.EndCap;
                case 2: 
                    return IsCorner(connections) ? RoadType.Corner : RoadType.Straight;
                case 3: return RoadType.TJunction;
                case 4: return RoadType.Cross;
                default: return RoadType.Straight;
            }
        }

        private Vector2Int[] GetConnections(List<Vector2Int> pathPoints, int index)
        {
            Vector2Int current = pathPoints[index];
            Vector2Int[] connections = new Vector2Int[4];
            var directions = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            bool isRoadStart = IsRoadStartPoint(current);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = current + directions[i];
                bool hasConnection = false;
                
                // 通常の道路パス内の接続をチェック
                if (pathPoints.Contains(neighbor))
                {
                    connections[i] = directions[i];
                    hasConnection = true;
                    Debug.Log($"Path connection: {current} -> {neighbor} in direction {GetDirectionName(i)}");
                }
                
                // RoadStartピースの場合のみ、隣接Buildingも道路接続として扱う（重複チェック回避）
                if (isRoadStart && !hasConnection)
                {
                    var piece = GetPieceAtPosition(neighbor);
                    if (piece != null && piece.type == PieceType.Building)
                    {
                        connections[i] = directions[i];
                        Debug.Log($"RoadStart at {current} treating Building at {neighbor} as road connection in direction {GetDirectionName(i)}");
                    }
                }
            }

            return connections;
        }

        private string GetDirectionName(int directionIndex)
        {
            string[] dirNames = {"North", "East", "South", "West"};
            return directionIndex >= 0 && directionIndex < dirNames.Length ? dirNames[directionIndex] : "Unknown";
        }

        private bool HasAdjacentBuilding(Vector2Int position)
        {
            var directions = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            
            foreach (var direction in directions)
            {
                var neighbor = position + direction;
                var piece = GetPieceAtPosition(neighbor);
                if (piece != null && piece.type == PieceType.Building)
                {
                    return true;
                }
            }
            
            return false;
        }

        private RoadPath CreateSinglePointPath(int groupId, Vector2Int point)
        {
            var roadPath = new RoadPath
            {
                id = System.Guid.NewGuid().ToString(),
                pathPoints = new List<Vector2Int> { point },
                roadGroupId = groupId,
                isComplete = true
            };

            roadPath.segments = GenerateRoadSegments(roadPath.pathPoints, groupId);
            Debug.Log($"Created single point road path for group {groupId} at {point}");
            return roadPath;
        }

        private bool IsCorner(Vector2Int[] connections)
        {
            int connectionCount = 0;
            bool hasAdjacent = false;

            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] != Vector2Int.zero)
                {
                    connectionCount++;
                    
                    int nextIndex = (i + 1) % connections.Length;
                    if (connections[nextIndex] != Vector2Int.zero)
                    {
                        hasAdjacent = true;
                    }
                }
            }

            return connectionCount == 2 && hasAdjacent;
        }

        private float DetermineRotation(List<Vector2Int> pathPoints, int index, RoadType roadType)
        {
            Vector2Int[] connections = GetConnections(pathPoints, index);
            
            switch (roadType)
            {
                case RoadType.Straight:
                    return connections[0] != Vector2Int.zero || connections[2] != Vector2Int.zero ? 0f : 90f;
                
                case RoadType.Corner:
                    return GetCornerRotation(connections);
                
                case RoadType.EndCap:
                    return GetEndCapRotation(connections);
                
                case RoadType.TJunction:
                    return GetTJunctionRotation(connections);
                
                default:
                    return 0f;
            }
        }

        private float GetCornerRotation(Vector2Int[] connections)
        {
            if (connections[0] != Vector2Int.zero && connections[1] != Vector2Int.zero) return 0f;
            if (connections[1] != Vector2Int.zero && connections[2] != Vector2Int.zero) return 90f;
            if (connections[2] != Vector2Int.zero && connections[3] != Vector2Int.zero) return 180f;
            if (connections[3] != Vector2Int.zero && connections[0] != Vector2Int.zero) return 270f;
            return 0f;
        }

        private float GetEndCapRotation(Vector2Int[] connections)
        {
            // 接続方向の逆方向を向くように設定
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] != Vector2Int.zero)
                {
                    return i * 90f;
                }
            }
            return 0f;
        }

        private float GetTJunctionRotation(Vector2Int[] connections)
        {
            // 接続されていない方向（開いている方向）を特定
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == Vector2Int.zero)
                {
                    return i * 90f;
                }
            }
            return 0f;
        }

        private GameObject GetRoadPrefab(RoadType roadType, float rotation)
        {
            if (roadPrefabs == null)
                return null;

            // 回転角度から方向を正しく計算
            int directionIndex = Mathf.RoundToInt(rotation / 90f) % 4;
            if (directionIndex < 0) directionIndex += 4;
            
            Direction direction = (Direction)directionIndex;
            return roadPrefabs.GetRoadPrefab(roadType, direction);
        }
        
        // RoadStartポイントかどうか判定
        private bool IsRoadStartPoint(Vector2Int position)
        {
            foreach (var piece in layout.pieces)
            {
                if (piece.gridPosition == position && 
                    (piece.type == PieceType.RoadStart || piece.isRoadStartPoint))
                {
                    return true;
                }
            }
            return false;
        }
        
        
        // 指定位置のピースを取得
        private DungeonPiece GetPieceAtPosition(Vector2Int position)
        {
            foreach (var piece in layout.pieces)
            {
                if (IsPositionInPiece(position, piece))
                {
                    return piece;
                }
            }
            return null;
        }
        
        // 位置がピース内にあるかチェック
        private bool IsPositionInPiece(Vector2Int position, DungeonPiece piece)
        {
            return position.x >= piece.gridPosition.x &&
                   position.x < piece.gridPosition.x + piece.size.x &&
                   position.y >= piece.gridPosition.y &&
                   position.y < piece.gridPosition.y + piece.size.y;
        }

        // 交差点を検知して統合処理
        private void ProcessIntersections(List<RoadPath> roadPaths)
        {
            if (roadPaths.Count < 2) return;

            Debug.Log("Processing intersections between road groups");

            // 全ての道路セグメントの位置を収集
            var segmentsByPosition = new Dictionary<Vector2Int, List<RoadSegment>>();

            foreach (var roadPath in roadPaths)
            {
                foreach (var segment in roadPath.segments)
                {
                    if (!segmentsByPosition.ContainsKey(segment.position))
                    {
                        segmentsByPosition[segment.position] = new List<RoadSegment>();
                    }
                    segmentsByPosition[segment.position].Add(segment);
                }
            }

            // 交差点（複数のグループが重なる位置）を検知
            var intersections = new Dictionary<Vector2Int, List<int>>();

            foreach (var kvp in segmentsByPosition)
            {
                var position = kvp.Key;
                var segments = kvp.Value;
                var groups = segments.Select(s => s.roadGroupId).Distinct().ToList();

                if (groups.Count > 1)
                {
                    intersections[position] = groups;
                    Debug.Log($"Intersection found at {position} between groups: {string.Join(", ", groups)}");
                }
            }

            // 交差点のセグメントを更新
            foreach (var intersection in intersections)
            {
                var position = intersection.Key;
                var intersectingGroups = intersection.Value;
                var segments = segmentsByPosition[position];

                // 最初のセグメントを交差点セグメントとして使用
                var primarySegment = segments[0];
                primarySegment.intersectionGroups = new List<int>(intersectingGroups);

                // 交差点用の道路タイプとプレファブを決定
                UpdateIntersectionSegment(primarySegment, intersectingGroups);

                // 他のセグメントを削除（重複を避ける）
                for (int i = 1; i < segments.Count; i++)
                {
                    var segmentToRemove = segments[i];
                    foreach (var roadPath in roadPaths)
                    {
                        roadPath.segments.RemoveAll(s => s.position == position && s.roadGroupId == segmentToRemove.roadGroupId);
                    }
                }

                Debug.Log($"Updated intersection at {position}: Type={primarySegment.roadType}, Groups=[{string.Join(", ", intersectingGroups)}]");
            }
        }

        private void UpdateIntersectionSegment(RoadSegment segment, List<int> intersectingGroups)
        {
            // 交差点の接続方向を計算
            var allConnections = new List<Vector2Int>();

            // 全ての交差するグループからの接続を考慮
            foreach (var roadPath in layout.roadPaths)
            {
                if (intersectingGroups.Contains(roadPath.roadGroupId))
                {
                    var segmentIndex = roadPath.pathPoints.FindIndex(p => p == segment.position);
                    if (segmentIndex >= 0)
                    {
                        var connections = GetConnections(roadPath.pathPoints, segmentIndex);
                        for (int i = 0; i < connections.Length; i++)
                        {
                            if (connections[i] != Vector2Int.zero)
                            {
                                allConnections.Add(connections[i]);
                            }
                        }
                    }
                }
            }

            // 重複を除去
            allConnections = allConnections.Distinct().ToList();
            int connectionCount = allConnections.Count;

            // 接続数に応じて道路タイプを決定
            switch (connectionCount)
            {
                case 2:
                    segment.roadType = AreConnectionsOpposite(allConnections) ? RoadType.Straight : RoadType.Corner;
                    break;
                case 3:
                    segment.roadType = RoadType.TJunction;
                    break;
                case 4:
                    segment.roadType = RoadType.Cross;
                    break;
                default:
                    segment.roadType = RoadType.Cross; // デフォルトで十字路
                    break;
            }

            // 回転を再計算
            segment.rotation = DetermineIntersectionRotation(allConnections, segment.roadType);
            segment.prefab = GetRoadPrefab(segment.roadType, segment.rotation);
        }

        private bool AreConnectionsOpposite(List<Vector2Int> connections)
        {
            if (connections.Count != 2) return false;
            
            var dir1 = connections[0];
            var dir2 = connections[1];
            
            return (dir1.x == -dir2.x && dir1.y == -dir2.y);
        }

        private float DetermineIntersectionRotation(List<Vector2Int> connections, RoadType roadType)
        {
            if (connections.Count == 0) return 0f;

            // 最初の接続方向を基準に回転を決定
            var primaryConnection = connections[0];
            
            if (primaryConnection == Vector2Int.up) return 0f;
            if (primaryConnection == Vector2Int.right) return 90f;
            if (primaryConnection == Vector2Int.down) return 180f;
            if (primaryConnection == Vector2Int.left) return 270f;
            
            return 0f;
        }
    }
}