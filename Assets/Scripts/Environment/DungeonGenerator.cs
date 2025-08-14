using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KowloonBreak.Environment
{
    public class DungeonGenerator : MonoBehaviour
    {
        public static DungeonGenerator Instance { get; private set; }
        
        [Header("Generation Settings")]
        [SerializeField] private Vector2Int dungeonSize = new Vector2Int(100, 100);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int generationSeed = 12345;
        [SerializeField] private bool useRandomSeed = true;
        
        [Header("Block Configurations")]
        [SerializeField] private DungeonBlockConfiguration[] blockConfigurations;
        
        // Legacy support
        [Header("Legacy Block Prefabs (Deprecated)")]
        [SerializeField] private DungeonBlockData[] blockPrefabs;
        
        [Header("Generation Rules")]
        [SerializeField] private float roomDensity = 0.3f;
        [SerializeField] private float corridorDensity = 0.4f;
        [SerializeField] private float junctionDensity = 0.2f;
        [SerializeField] private float specialRoomDensity = 0.1f;
        [SerializeField] private int minSpacing = 0;  // ブロック間の最小間隔
        [SerializeField] private bool fillEmptySpaces = true;  // 空きスペースを小ブロックで埋める
        [SerializeField] private bool preventOverlap = true;  // 重複防止
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logGenerationProcess = false;
        
        private DungeonGrid dungeonGrid;
        private List<DungeonBlock> activeDungeonBlocks;
        private Dictionary<Vector2Int, DungeonBlock> placedBlocks;
        
        public DungeonGrid Grid => dungeonGrid;
        public Vector2Int DungeonSize => dungeonSize;
        public float CellSize => cellSize;
        
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
            activeDungeonBlocks = new List<DungeonBlock>();
            placedBlocks = new Dictionary<Vector2Int, DungeonBlock>();
            
            if (blockConfigurations == null || blockConfigurations.Length == 0)
            {
                CreateDefaultBlockConfigurations();
            }
            
            Debug.Log("Dungeon Generator Initialized");
        }
        
        private void CreateDefaultBlockConfigurations()
        {
            blockConfigurations = DungeonBlockFactory.GetDefaultConfigurations();
            
            // 作成したデフォルトブロックの妥当性を検証
            Debug.Log("=== Created Default Block Configurations ===");
            for (int i = 0; i < blockConfigurations.Length; i++)
            {
                var config = blockConfigurations[i];
                config.ValidateAndFix();
                Debug.Log($"Block {i}: {config.GetDisplayName()} weight:{config.spawnWeight} max:{config.maxInstances}");
            }
        }
        
        public void GenerateDungeon()
        {
            // 初期化確認
            if (activeDungeonBlocks == null || placedBlocks == null)
            {
                InitializeGenerator();
            }
            
            // blockConfigurations の状態確認・修正
            if (blockConfigurations == null || blockConfigurations.Length == 0)
            {
                Debug.LogWarning("Block Configurations is null or empty, creating default configurations...");
                CreateDefaultBlockConfigurations();
                
                if (blockConfigurations == null || blockConfigurations.Length == 0)
                {
                    throw new System.InvalidOperationException("Failed to create default block configurations");
                }
            }
            
            // 各Configurationの妥当性確認
            for (int i = 0; i < blockConfigurations.Length; i++)
            {
                if (blockConfigurations[i] == null)
                {
                    Debug.LogError($"Block Configuration at index {i} is null!");
                    throw new System.NullReferenceException($"Block Configuration at index {i} is null");
                }
                
                blockConfigurations[i].ValidateAndFix();
            }
            
            if (useRandomSeed)
            {
                generationSeed = System.DateTime.Now.Millisecond;
            }
            
            UnityEngine.Random.InitState(generationSeed);
            
            ClearExistingDungeon();
            InitializeDungeonGrid();
            GenerateBlocks();
            
            if (logGenerationProcess)
            {
                Debug.Log($"Dungeon generated with seed: {generationSeed}, Blocks placed: {activeDungeonBlocks.Count}");
            }
        }
        
        private void ClearExistingDungeon()
        {
            if (activeDungeonBlocks != null)
            {
                foreach (var block in activeDungeonBlocks)
                {
                    if (block != null)
                    {
                        DestroyImmediate(block.gameObject);
                    }
                }
                activeDungeonBlocks.Clear();
            }
            
            placedBlocks?.Clear();
        }
        
        private void InitializeDungeonGrid()
        {
            dungeonGrid = new DungeonGrid(dungeonSize.x, dungeonSize.y);
        }
        
        private void GenerateBlocks()
        {
            var availableBlocks = GetWeightedBlockList();
            int totalBlocks = Mathf.RoundToInt(dungeonSize.x * dungeonSize.y * (roomDensity + corridorDensity + junctionDensity + specialRoomDensity));
            int placedCount = 0;
            int maxAttempts = totalBlocks * 10;
            int attemptCount = 0;
            
            while (placedCount < totalBlocks && attemptCount < maxAttempts)
            {
                attemptCount++;
                
                var blockConfig = GetRandomWeightedBlock(availableBlocks);
                if (blockConfig == null) continue;
                
                Vector2Int position = GetRandomValidPosition(blockConfig.size);
                if (position.x == -1) continue;
                
                if (CanPlaceBlock(position, blockConfig.size))
                {
                    PlaceBlock(blockConfig, position);
                    placedCount++;
                }
            }
        }
        
        private List<DungeonBlockConfiguration> GetWeightedBlockList()
        {
            var weightedList = new List<DungeonBlockConfiguration>();
            
            if (blockConfigurations == null)
            {
                Debug.LogError("blockConfigurations is null!");
                return weightedList;
            }
            
            foreach (var config in blockConfigurations)
            {
                if (config == null)
                {
                    Debug.LogWarning("Null configuration found in blockConfigurations array, skipping...");
                    continue;
                }
                
                if (!config.IsValid())
                {
                    Debug.LogWarning($"Skipping invalid block configuration: {config.GetDisplayName()}");
                    continue;
                }
                
                // 重み付きリストに追加
                int weight = Mathf.RoundToInt(config.spawnWeight);
                for (int i = 0; i < weight; i++)
                {
                    weightedList.Add(config);
                }
            }
            
            if (weightedList.Count == 0)
            {
                Debug.LogError("No valid block configurations available for generation! Recreating default configurations...");
                CreateDefaultBlockConfigurations();
                
                // 再帰的に再試行（1回のみ）
                if (blockConfigurations != null && blockConfigurations.Length > 0)
                {
                    return GetWeightedBlockList();
                }
            }
            
            Debug.Log($"Generated weighted block list with {weightedList.Count} entries from {blockConfigurations?.Length ?? 0} configurations");
            return weightedList;
        }
        
        private DungeonBlockConfiguration GetRandomWeightedBlock(List<DungeonBlockConfiguration> weightedList)
        {
            if (weightedList.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, weightedList.Count);
            return weightedList[randomIndex];
        }
        
        private Vector2Int GetRandomValidPosition(Vector2Int blockSize)
        {
            int maxAttempts = 100;
            float gridSize = 2.5f; // 2.5x2.5グリッドサイズ
            
            for (int i = 0; i < maxAttempts; i++)
            {
                // 2.5の倍数の位置を生成（整数座標系では5の倍数を2で割った位置）
                int maxGridX = Mathf.FloorToInt((dungeonSize.x - blockSize.x) / gridSize);
                int maxGridY = Mathf.FloorToInt((dungeonSize.y - blockSize.y) / gridSize);
                
                if (maxGridX < 0 || maxGridY < 0) break;
                
                int gridX = UnityEngine.Random.Range(0, maxGridX + 1);
                int gridY = UnityEngine.Random.Range(0, maxGridY + 1);
                
                // 2.5グリッドに対応する座標計算（整数座標では5/2の倍数）
                Vector2Int position = new Vector2Int(
                    Mathf.RoundToInt(gridX * gridSize), 
                    Mathf.RoundToInt(gridY * gridSize)
                );
                
                if (CanPlaceBlock(position, blockSize))
                {
                    return position;
                }
            }
            
            return new Vector2Int(-1, -1);
        }
        
        private bool CanPlaceBlock(Vector2Int position, Vector2Int blockSize)
        {
            if (position.x < 0 || position.y < 0 || 
                position.x + blockSize.x > dungeonSize.x || 
                position.y + blockSize.y > dungeonSize.y)
            {
                return false;
            }
            
            for (int x = position.x; x < position.x + blockSize.x; x++)
            {
                for (int y = position.y; y < position.y + blockSize.y; y++)
                {
                    if (dungeonGrid.IsOccupied(x, y))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        
        private void PlaceBlock(DungeonBlockConfiguration config, Vector2Int position)
        {
            GameObject blockObject = DungeonBlockFactory.CreateBlockFromPrefab(config, transform, position, cellSize);
            
            var dungeonBlock = blockObject.GetComponent<DungeonBlock>();
            if (dungeonBlock == null)
            {
                dungeonBlock = blockObject.AddComponent<DungeonBlock>();
                dungeonBlock.InitializeFromConfiguration(config, cellSize);
            }
            
            dungeonBlock.SetGridPosition(position);
            
            dungeonGrid.OccupyArea(position, config.size, dungeonBlock);
            
            activeDungeonBlocks.Add(dungeonBlock);
            placedBlocks[position] = dungeonBlock;
            
            if (logGenerationProcess)
            {
                Debug.Log($"Placed {config.GetDisplayName()} block at {position}");
            }
        }
        
        private void EstablishConnections(DungeonBlock newBlock, Vector2Int position)
        {
            if (newBlock == null) return;
            
            // 4方向の隣接ブロックをチェック
            Vector2Int[] directions = {
                new Vector2Int(0, 1),   // North
                new Vector2Int(0, -1),  // South  
                new Vector2Int(1, 0),   // East
                new Vector2Int(-1, 0)   // West
            };
            
            Direction[] blockDirections = { Direction.North, Direction.South, Direction.East, Direction.West };
            
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighborPos = position + directions[i] * Mathf.RoundToInt(2.5f);
                
                if (placedBlocks.ContainsKey(neighborPos))
                {
                    var neighborBlock = placedBlocks[neighborPos];
                    if (neighborBlock != null)
                    {
                        // 2つのブロック間でConnector連結を検証
                        ValidateConnection(newBlock, neighborBlock, blockDirections[i]);
                    }
                }
            }
        }
        
        private void ValidateConnection(DungeonBlock block1, DungeonBlock block2, Direction directionFrom1To2)
        {
            if (block1 == null || block2 == null) return;
            
            // block1から見た方向のConnectorを取得
            var connectors1 = block1.GetConnectors(directionFrom1To2);
            
            // block2から見た逆方向のConnectorを取得
            Direction oppositeDirection = GetOppositeDirection(directionFrom1To2);
            var connectors2 = block2.GetConnectors(oppositeDirection);
            
            bool canConnect = connectors1 != null && connectors1.Length > 0 && 
                             connectors2 != null && connectors2.Length > 0;
            
            if (logGenerationProcess)
            {
                string connectionStatus = canConnect ? "Connected" : "No connection possible";
                Debug.Log($"Connection check: {block1.BlockType} -> {block2.BlockType} ({directionFrom1To2}): {connectionStatus}");
            }
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
        
        private GameObject CreateDefaultBlock(DungeonBlockData blockData)
        {
            var blockObject = new GameObject($"DungeonBlock_{blockData.blockType}_{blockData.size.x}x{blockData.size.y}");
            
            var meshRenderer = blockObject.AddComponent<MeshRenderer>();
            var meshFilter = blockObject.AddComponent<MeshFilter>();
            var boxCollider = blockObject.AddComponent<BoxCollider>();
            
            var mesh = CreateBlockMesh(blockData.size);
            meshFilter.mesh = mesh;
            
            var material = new Material(Shader.Find("Standard"));
            material.color = GetBlockTypeColor(blockData.blockType);
            meshRenderer.material = material;
            
            Vector3 size = new Vector3(blockData.size.x * cellSize, 0.1f, blockData.size.y * cellSize);
            boxCollider.size = size;
            boxCollider.center = new Vector3(size.x * 0.5f - cellSize * 0.5f, 0, size.z * 0.5f - cellSize * 0.5f);
            
            return blockObject;
        }
        
        private Mesh CreateBlockMesh(Vector2Int size)
        {
            var mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(size.x * cellSize, 0, 0);
            vertices[2] = new Vector3(size.x * cellSize, 0, size.y * cellSize);
            vertices[3] = new Vector3(0, 0, size.y * cellSize);
            
            int[] triangles = { 0, 2, 1, 0, 3, 2 };
            Vector2[] uv = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Color GetBlockTypeColor(DungeonBlockType blockType)
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
        
        public DungeonBlock GetBlockAt(Vector2Int gridPosition)
        {
            return placedBlocks.TryGetValue(gridPosition, out DungeonBlock block) ? block : null;
        }
        
        public List<DungeonBlock> GetAllBlocks()
        {
            return new List<DungeonBlock>(activeDungeonBlocks);
        }
        
        public List<DungeonBlock> GetBlocksByType(DungeonBlockType blockType)
        {
            return activeDungeonBlocks.Where(block => block.BlockType == blockType).ToList();
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = Color.white;
            Vector3 dungeonWorldSize = new Vector3(dungeonSize.x * cellSize, 0, dungeonSize.y * cellSize);
            Gizmos.DrawWireCube(transform.position + dungeonWorldSize * 0.5f, dungeonWorldSize);
        }
    }
    
    [System.Serializable]
    public class DungeonBlockData
    {
        public GameObject prefab;
        public DungeonBlockType blockType = DungeonBlockType.Room;
        public Vector2Int size = new Vector2Int(5, 5);  // デフォルトサイズを設定
        public float spawnWeight = 1f;
        public int maxInstances = -1;
    }
    
    public class DungeonGrid
    {
        private DungeonBlock[,] grid;
        private bool[,] occupied;
        private int width, height;
        
        public int Width => width;
        public int Height => height;
        
        public DungeonGrid(int width, int height)
        {
            this.width = width;
            this.height = height;
            grid = new DungeonBlock[width, height];
            occupied = new bool[width, height];
        }
        
        public bool IsOccupied(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return true;
            return occupied[x, y];
        }
        
        public void OccupyArea(Vector2Int position, Vector2Int size, DungeonBlock block)
        {
            for (int x = position.x; x < position.x + size.x && x < width; x++)
            {
                for (int y = position.y; y < position.y + size.y && y < height; y++)
                {
                    occupied[x, y] = true;
                    grid[x, y] = block;
                }
            }
        }
        
        public DungeonBlock GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return null;
            return grid[x, y];
        }
    }
}