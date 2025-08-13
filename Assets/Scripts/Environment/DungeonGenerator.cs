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
        
        [Header("Block Prefabs")]
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
            
            if (blockPrefabs == null || blockPrefabs.Length == 0)
            {
                CreateDefaultBlockPrefabs();
            }
            
            Debug.Log("Dungeon Generator Initialized");
        }
        
        private void CreateDefaultBlockPrefabs()
        {
            blockPrefabs = new DungeonBlockData[]
            {
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Room,
                    size = new Vector2Int(5, 5),
                    spawnWeight = 30f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Room,
                    size = new Vector2Int(5, 10),
                    spawnWeight = 20f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Room,
                    size = new Vector2Int(10, 10),
                    spawnWeight = 15f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Corridor,
                    size = new Vector2Int(5, 5),
                    spawnWeight = 25f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Junction,
                    size = new Vector2Int(5, 5),
                    spawnWeight = 8f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Special,
                    size = new Vector2Int(10, 10),
                    spawnWeight = 2f,
                    maxInstances = 5
                }
            };
        }
        
        public void GenerateDungeon()
        {
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
                
                var blockData = GetRandomWeightedBlock(availableBlocks);
                if (blockData == null) continue;
                
                Vector2Int position = GetRandomValidPosition(blockData.size);
                if (position.x == -1) continue;
                
                if (CanPlaceBlock(position, blockData.size))
                {
                    PlaceBlock(blockData, position);
                    placedCount++;
                }
            }
        }
        
        private List<DungeonBlockData> GetWeightedBlockList()
        {
            var weightedList = new List<DungeonBlockData>();
            
            foreach (var blockData in blockPrefabs)
            {
                if (blockData.prefab != null)
                {
                    int weight = Mathf.RoundToInt(blockData.spawnWeight);
                    for (int i = 0; i < weight; i++)
                    {
                        weightedList.Add(blockData);
                    }
                }
            }
            
            return weightedList;
        }
        
        private DungeonBlockData GetRandomWeightedBlock(List<DungeonBlockData> weightedList)
        {
            if (weightedList.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, weightedList.Count);
            return weightedList[randomIndex];
        }
        
        private Vector2Int GetRandomValidPosition(Vector2Int blockSize)
        {
            int maxAttempts = 100;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                int x = UnityEngine.Random.Range(0, dungeonSize.x - blockSize.x + 1);
                int y = UnityEngine.Random.Range(0, dungeonSize.y - blockSize.y + 1);
                Vector2Int position = new Vector2Int(x, y);
                
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
        
        private void PlaceBlock(DungeonBlockData blockData, Vector2Int position)
        {
            GameObject blockObject;
            
            if (blockData.prefab != null)
            {
                blockObject = Instantiate(blockData.prefab, transform);
            }
            else
            {
                blockObject = CreateDefaultBlock(blockData);
            }
            
            var dungeonBlock = blockObject.GetComponent<DungeonBlock>();
            if (dungeonBlock == null)
            {
                dungeonBlock = blockObject.AddComponent<DungeonBlock>();
            }
            
            dungeonBlock.SetPosition(position, cellSize);
            dungeonBlock.GridPosition = position;
            
            dungeonGrid.OccupyArea(position, blockData.size, dungeonBlock);
            
            activeDungeonBlocks.Add(dungeonBlock);
            placedBlocks[position] = dungeonBlock;
            
            if (logGenerationProcess)
            {
                Debug.Log($"Placed {blockData.blockType} block at {position} with size {blockData.size}");
            }
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