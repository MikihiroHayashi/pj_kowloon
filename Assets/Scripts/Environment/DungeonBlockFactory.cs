using UnityEngine;

namespace KowloonBreak.Environment
{
    public static class DungeonBlockFactory
    {
        public static GameObject CreateDefaultBlock(DungeonBlockConfiguration config, float cellSize, string namePrefix = "DungeonBlock")
        {
            if (config == null)
            {
                Debug.LogError("Cannot create block: configuration is null");
                return null;
            }
            
            config.ValidateAndFix();
            
            var blockObject = new GameObject($"{namePrefix}_{config.blockType}_{config.size.x}x{config.size.y}");
            
            // メッシュコンポーネント追加
            var meshRenderer = blockObject.AddComponent<MeshRenderer>();
            var meshFilter = blockObject.AddComponent<MeshFilter>();
            var boxCollider = blockObject.AddComponent<BoxCollider>();
            
            // メッシュ生成
            var mesh = CreateBlockMesh(config.size, cellSize);
            meshFilter.mesh = mesh;
            
            // マテリアル設定
            Material material;
            if (config.defaultMaterial != null)
            {
                material = config.defaultMaterial;
            }
            else
            {
                material = new Material(Shader.Find("Standard"));
                material.color = config.debugColor;
            }
            meshRenderer.material = material;
            
            // コライダー設定
            Vector3 blockSize = config.GetWorldSize(cellSize);
            boxCollider.size = new Vector3(blockSize.x, 0.1f, blockSize.z);
            boxCollider.center = new Vector3(blockSize.x * 0.5f - cellSize * 0.5f, 0, blockSize.z * 0.5f - cellSize * 0.5f);
            
            // DungeonBlockコンポーネント追加・設定
            var dungeonBlock = blockObject.AddComponent<DungeonBlock>();
            dungeonBlock.InitializeFromConfiguration(config, cellSize);
            
            return blockObject;
        }
        
        public static GameObject CreateBlockFromPrefab(DungeonBlockConfiguration config, Transform parent, Vector2Int gridPosition, float cellSize)
        {
            if (config?.prefab == null)
            {
                return CreateDefaultBlock(config, cellSize);
            }
            
            var blockObject = Object.Instantiate(config.prefab, parent);
            blockObject.name = $"PrefabBlock_{config.blockType}_{config.size.x}x{config.size.y}_{gridPosition.x}_{gridPosition.y}";
            
            // DungeonBlockコンポーネント確認・設定
            var dungeonBlock = blockObject.GetComponent<DungeonBlock>();
            if (dungeonBlock == null)
            {
                dungeonBlock = blockObject.AddComponent<DungeonBlock>();
            }
            
            dungeonBlock.InitializeFromConfiguration(config, cellSize);
            dungeonBlock.SetGridPosition(gridPosition);
            
            // ワールド位置設定
            Vector3 worldPos = config.GetWorldPosition(gridPosition, cellSize);
            blockObject.transform.position = worldPos;
            
            return blockObject;
        }
        
        private static Mesh CreateBlockMesh(Vector2Int size, float cellSize)
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
            
            mesh.name = $"DungeonBlockMesh_{size.x}x{size.y}";
            
            return mesh;
        }
        
        public static DungeonBlockConfiguration[] GetDefaultConfigurations()
        {
            var configurations = new DungeonBlockConfiguration[6];
            
            // Room 5x5
            configurations[0] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[0].prefab = null;
            configurations[0].blockType = DungeonBlockType.Room;
            configurations[0].size = new Vector2Int(5, 5);
            configurations[0].spawnWeight = 30f;
            configurations[0].maxInstances = -1;
            configurations[0].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Room);
            
            // Room 5x10
            configurations[1] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[1].prefab = null;
            configurations[1].blockType = DungeonBlockType.Room;
            configurations[1].size = new Vector2Int(5, 10);
            configurations[1].spawnWeight = 25f;
            configurations[1].maxInstances = -1;
            configurations[1].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Room);
            
            // Room 10x10
            configurations[2] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[2].prefab = null;
            configurations[2].blockType = DungeonBlockType.Room;
            configurations[2].size = new Vector2Int(10, 10);
            configurations[2].spawnWeight = 20f;
            configurations[2].maxInstances = -1;
            configurations[2].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Room);
            
            // Corridor
            configurations[3] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[3].prefab = null;
            configurations[3].blockType = DungeonBlockType.Corridor;
            configurations[3].size = new Vector2Int(5, 5);
            configurations[3].spawnWeight = 20f;
            configurations[3].maxInstances = -1;
            configurations[3].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Corridor);
            
            // Junction
            configurations[4] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[4].prefab = null;
            configurations[4].blockType = DungeonBlockType.Junction;
            configurations[4].size = new Vector2Int(5, 5);
            configurations[4].spawnWeight = 10f;
            configurations[4].maxInstances = -1;
            configurations[4].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Junction);
            
            // Special
            configurations[5] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[5].prefab = null;
            configurations[5].blockType = DungeonBlockType.Special;
            configurations[5].size = new Vector2Int(10, 10);
            configurations[5].spawnWeight = 5f;
            configurations[5].maxInstances = 5;
            configurations[5].debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Special);
            
            return configurations;
        }
    }
}