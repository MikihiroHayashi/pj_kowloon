using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_AI_NAVIGATION
using Unity.AI.Navigation;
#endif

namespace KowloonBreak.Environment
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private Transform dungeonParent;
        [SerializeField] private DungeonRoadPrefabSet roadPrefabs;
        [SerializeField] private bool generateNavMesh = true;
        [SerializeField] private bool clearPreviousDungeon = true;
        
        [Header("NavMesh Settings")]
        [SerializeField] private bool forceNavMeshGeneration = false;

        private DungeonLayout currentLayout;
        private Dictionary<string, GameObject> generatedObjects;

        public static DungeonGenerator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeGenerator();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGenerator()
        {
            if (dungeonParent == null)
            {
                GameObject parentObject = new GameObject("Generated Dungeon");
                dungeonParent = parentObject.transform;
            }

            generatedObjects = new Dictionary<string, GameObject>();
            
            Debug.Log("Dungeon Generator Initialized");
        }

        public void GenerateDungeon(DungeonLayout layout)
        {
            if (layout == null)
            {
                Debug.LogError("DungeonLayout is null");
                return;
            }

            // 初期化確認
            if (generatedObjects == null)
            {
                Debug.Log("Initializing generatedObjects dictionary");
                generatedObjects = new Dictionary<string, GameObject>();
            }

            if (dungeonParent == null)
            {
                Debug.Log("Creating dungeonParent GameObject");
                GameObject parentObject = new GameObject("Generated Dungeon");
                dungeonParent = parentObject.transform;
            }

            currentLayout = layout;

            if (clearPreviousDungeon)
            {
                ClearPreviousDungeon();
            }

            Debug.Log($"Generating dungeon: {layout.layoutName}");

            GenerateRoads(layout);
            GeneratePieces(layout);

            if (generateNavMesh)
            {
                GenerateNavMesh();
            }

            Debug.Log("Dungeon generation completed");
        }

        private void ClearPreviousDungeon()
        {
            if (dungeonParent != null)
            {
                for (int i = dungeonParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(dungeonParent.GetChild(i).gameObject);
                }
            }

            if (generatedObjects != null)
            {
                generatedObjects.Clear();
            }
            else
            {
                generatedObjects = new Dictionary<string, GameObject>();
            }
        }

        private void GenerateRoads(DungeonLayout layout)
        {
            if (roadPrefabs == null)
            {
                Debug.LogWarning("Road prefab set is not assigned");
                return;
            }

            // 既存の道路パスがある場合はそれを使用、ない場合は新しく生成
            var roadPaths = layout.roadPaths;
            if (roadPaths == null || roadPaths.Count == 0)
            {
                Debug.Log("Generating new road paths...");
                var roadPathfinder = new RoadPathfinder(layout, roadPrefabs);
                roadPaths = roadPathfinder.GenerateRoadPaths();
                layout.roadPaths = roadPaths;
            }
            else
            {
                Debug.Log($"Using existing road paths: {roadPaths.Count} paths found");
            }

            foreach (var roadPath in roadPaths)
            {
                GenerateRoadPath(roadPath, layout.cellSize);
            }
        }

        private void GenerateRoadPath(RoadPath roadPath, float cellSize)
        {
            GameObject roadParent = new GameObject($"Road Path {roadPath.id}");
            roadParent.transform.SetParent(dungeonParent);

            foreach (var segment in roadPath.segments)
            {
                if (segment.prefab != null)
                {
                    Vector3 worldPosition = GridUtility.GridToWorldPosition(segment.position, cellSize);
                    // プレファブ自体の向きを考慮して回転は適用しない（プレファブ選択で対応）
                    Quaternion rotation = Quaternion.identity;

                    GameObject roadObject = Instantiate(segment.prefab, worldPosition, rotation, roadParent.transform);
                    roadObject.name = $"Road_{segment.roadType}_{segment.position.x}_{segment.position.y}";
                    
                    // デバッグ情報を追加
                    Debug.Log($"Placed road {segment.roadType} at grid {segment.position} -> world {worldPosition}");

                    generatedObjects[$"road_{segment.position.x}_{segment.position.y}"] = roadObject;
                }
                else
                {
                    Debug.LogWarning($"Missing prefab for road {segment.roadType} at {segment.position}");
                }
            }
        }

        private void GeneratePieces(DungeonLayout layout)
        {
            Debug.Log($"=== GeneratePieces Debug Info ===");
            Debug.Log($"Total pieces in layout: {layout.pieces.Count}");
            
            GameObject piecesParent = new GameObject("Dungeon Pieces");
            piecesParent.transform.SetParent(dungeonParent);

            int validPiecesCount = 0;
            int generatedPiecesCount = 0;

            foreach (var piece in layout.pieces)
            {
                Debug.Log($"Processing piece: {piece.type} at {piece.gridPosition} (Prefab: {(piece.prefab != null ? piece.prefab.name : "NULL")})");
                
                if (piece.prefab != null && piece.type != PieceType.RoadStart)
                {
                    validPiecesCount++;
                    try
                    {
                        GeneratePiece(piece, layout.cellSize, piecesParent.transform);
                        generatedPiecesCount++;
                        Debug.Log($"Successfully generated piece: {piece.type}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to generate piece {piece.type}: {e.Message}");
                    }
                }
                else
                {
                    if (piece.prefab == null)
                    {
                        Debug.LogWarning($"Piece {piece.type} at {piece.gridPosition} has no prefab assigned - consider running OnValidate to auto-repair");
                    }
                    if (piece.type == PieceType.RoadStart)
                        Debug.Log($"Skipping RoadStart piece (handled separately)");
                }
            }
            
            Debug.Log($"Valid pieces: {validPiecesCount}, Generated pieces: {generatedPiecesCount}");
            Debug.Log($"=== End GeneratePieces Debug ===");
        }

        private void GeneratePiece(DungeonPiece piece, float cellSize, Transform parent)
        {
            Vector3 worldPosition = GridUtility.GridToWorldPosition(piece.gridPosition, cellSize);

            Quaternion rotation = Quaternion.Euler(0, piece.rotation, 0);

            Debug.Log($"Piece {piece.type} at grid {piece.gridPosition} -> world {worldPosition} (cellSize: {cellSize})");

            GameObject pieceObject = Instantiate(piece.prefab, worldPosition, rotation, parent);
            pieceObject.name = $"Piece_{piece.type}_{piece.gridPosition.x}_{piece.gridPosition.y}";

            generatedObjects[piece.id] = pieceObject;
        }


        private void GenerateNavMesh()
        {
            if (!generateNavMesh && !forceNavMeshGeneration)
            {
                Debug.Log("NavMesh generation disabled");
                return;
            }

#if UNITY_AI_NAVIGATION
            try
            {
                NavMeshSurface[] navMeshSurfaces = FindObjectsOfType<NavMeshSurface>();
                
                if (navMeshSurfaces.Length > 0)
                {
                    foreach (var surface in navMeshSurfaces)
                    {
                        surface.BuildNavMesh();
                    }
                    Debug.Log("NavMesh generated using existing surfaces");
                }
                else if (forceNavMeshGeneration)
                {
                    GameObject navMeshObject = new GameObject("NavMesh Surface");
                    navMeshObject.transform.SetParent(dungeonParent);
                    
                    var navMeshSurface = navMeshObject.AddComponent<NavMeshSurface>();
                    navMeshSurface.BuildNavMesh();
                    
                    Debug.Log("NavMesh surface created and built");
                }
                else
                {
                    Debug.Log("No NavMesh surfaces found. Set 'Force NavMesh Generation' to true to create one automatically.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NavMesh generation failed: {e.Message}");
            }
#else
            // AI Navigation package not available
            if (generateNavMesh || forceNavMeshGeneration)
            {
                Debug.LogWarning("AI Navigation package not installed. NavMesh generation skipped.\n" +
                               "To enable NavMesh generation:\n" +
                               "1. Open Window > Package Manager\n" +
                               "2. Select 'Unity Registry'\n" +
                               "3. Search for 'AI Navigation'\n" +
                               "4. Install the package");
            }
#endif
        }

        public void RegenerateRoads()
        {
            if (currentLayout == null) return;

            ClearRoads();
            GenerateRoads(currentLayout);

            if (generateNavMesh)
            {
                GenerateNavMesh();
            }
        }

        private void ClearRoads()
        {
            var roadPaths = dungeonParent.Find("Road Path");
            if (roadPaths != null)
            {
                DestroyImmediate(roadPaths.gameObject);
            }

            var keysToRemove = new List<string>();
            foreach (var kvp in generatedObjects)
            {
                if (kvp.Key.StartsWith("road_"))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                generatedObjects.Remove(key);
            }
        }

        public GameObject GetGeneratedObject(string id)
        {
            return generatedObjects.TryGetValue(id, out GameObject obj) ? obj : null;
        }

        public List<GameObject> GetAllGeneratedObjects()
        {
            return new List<GameObject>(generatedObjects.Values);
        }

        public DungeonLayout GetCurrentLayout()
        {
            return currentLayout;
        }

        public void SetRoadPrefabSet(DungeonRoadPrefabSet prefabSet)
        {
            roadPrefabs = prefabSet;
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            if (currentLayout == null) return Vector3.zero;
            return GridUtility.GridToWorldPosition(gridPosition, currentLayout.cellSize);
        }

        public Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            if (currentLayout == null) return Vector2Int.zero;
            return GridUtility.WorldToGridPosition(worldPosition, currentLayout.cellSize);
        }
    }
}