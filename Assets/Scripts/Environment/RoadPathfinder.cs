using System;
using System.Collections.Generic;
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
            var roadStartPoints = GetRoadStartPoints();

            if (roadStartPoints.Count < 2)
            {
                Debug.LogWarning("少なくとも2つの道路起点が必要です");
                return roadPaths;
            }

            var connectedPoints = new HashSet<Vector2Int>();
            var roadPath = new RoadPath
            {
                id = System.Guid.NewGuid().ToString(),
                pathPoints = new List<Vector2Int>()
            };

            var currentPoint = roadStartPoints[0];
            roadPath.pathPoints.Add(currentPoint);
            connectedPoints.Add(currentPoint);

            while (connectedPoints.Count < roadStartPoints.Count)
            {
                Vector2Int nextPoint = FindNearestUnconnectedPoint(currentPoint, roadStartPoints, connectedPoints);
                
                if (nextPoint == Vector2Int.zero)
                    break;

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
                roadPath.segments = GenerateRoadSegments(roadPath.pathPoints);
                roadPath.isComplete = connectedPoints.Count == roadStartPoints.Count;
                roadPaths.Add(roadPath);
            }

            return roadPaths;
        }

        private List<Vector2Int> GetRoadStartPoints()
        {
            var startPoints = new List<Vector2Int>();
            
            foreach (var piece in layout.pieces)
            {
                if (piece.isRoadStartPoint)
                {
                    startPoints.Add(piece.gridPosition);
                }
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
            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, end) };

            while (openSet.Count > 0)
            {
                var current = GetLowestFScore(openSet, fScore);
                
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
            return piece == null || piece.isRoadStartPoint;
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

        private List<RoadSegment> GenerateRoadSegments(List<Vector2Int> pathPoints)
        {
            var segments = new List<RoadSegment>();

            for (int i = 0; i < pathPoints.Count; i++)
            {
                var segment = new RoadSegment
                {
                    position = pathPoints[i]
                };

                segment.roadType = DetermineRoadType(pathPoints, i);
                segment.rotation = DetermineRotation(pathPoints, i, segment.roadType);
                segment.prefab = GetRoadPrefab(segment.roadType, segment.rotation);

                segments.Add(segment);
            }

            return segments;
        }

        private RoadType DetermineRoadType(List<Vector2Int> pathPoints, int index)
        {
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

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighbor = current + directions[i];
                if (pathPoints.Contains(neighbor))
                {
                    connections[i] = directions[i];
                }
            }

            return connections;
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
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] != Vector2Int.zero)
                {
                    return i * 90f + 180f;
                }
            }
            return 0f;
        }

        private float GetTJunctionRotation(Vector2Int[] connections)
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i] == Vector2Int.zero)
                {
                    return i * 90f + 180f;
                }
            }
            return 0f;
        }

        private GameObject GetRoadPrefab(RoadType roadType, float rotation)
        {
            if (roadPrefabs == null)
                return null;

            Direction direction = (Direction)(Mathf.RoundToInt(rotation / 90f) % 4);
            return roadPrefabs.GetRoadPrefab(roadType, direction);
        }
    }
}