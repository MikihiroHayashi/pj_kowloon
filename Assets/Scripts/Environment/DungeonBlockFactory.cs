using UnityEngine;

namespace KowloonBreak.Environment
{
    /// <summary>
    /// 道路の方向タイプ（旧RoadSystemから移動）
    /// </summary>
    [System.Serializable]
    public enum RoadDirection
    {
        Horizontal,     // 水平
        Vertical,       // 垂直
        CornerNE,       // 北東コーナー
        CornerNW,       // 北西コーナー
        CornerSE,       // 南東コーナー
        CornerSW,       // 南西コーナー
        Cross,          // 十字路
        TJunctionN,     // T字路（北向き）
        TJunctionS,     // T字路（南向き）
        TJunctionE,     // T字路（東向き）
        TJunctionW,     // T字路（西向き）
        EndCap,         // 終端
        Single          // 単体
    }

    /// <summary>
    /// ダンジョンブロック生成用のファクトリークラス
    /// 通常ブロックと道路ブロックの両方を生成
    /// RoadSystemの機能も統合済み
    /// </summary>
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
        
        public static GameObject CreateBlockFromPrefab(DungeonBlockConfiguration config, Transform parent, Vector2Int gridPosition, float cellSize, GridMapData gridMapData = null)
        {
            GameObject blockObject;
            
            // 道路の場合は特別処理
            if (config.blockType == DungeonBlockType.Road && gridMapData != null)
            {
                blockObject = CreateRoadBlock(config, parent, gridPosition, cellSize, gridMapData);
            }
            else if (config?.prefab == null)
            {
                blockObject = CreateDefaultBlock(config, cellSize);
                blockObject.transform.SetParent(parent);
            }
            else
            {
                blockObject = Object.Instantiate(config.prefab, parent);
            }
            
            // 共通の名前設定
            blockObject.name = $"Block_{config.blockType}_{config.size.x}x{config.size.y}_{gridPosition.x}_{gridPosition.y}";
            
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
        
        /// <summary>
        /// 道路ブロックを生成（統合版）
        /// </summary>
        private static GameObject CreateRoadBlock(DungeonBlockConfiguration config, Transform parent, Vector2Int gridPosition, float cellSize, GridMapData gridMapData)
        {
            try
            {
                var roadDirection = DetectRoadType(gridMapData, gridPosition.x, gridPosition.y);
                var roadPrefab = config.GetRoadPrefab(roadDirection);
                
                if (roadPrefab != null)
                {
                    var blockObject = Object.Instantiate(roadPrefab, parent);
                    UnityEngine.Debug.Log($"Created road at ({gridPosition.x}, {gridPosition.y}): {roadDirection}");
                    return blockObject;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"No prefab found for road direction {roadDirection} at ({gridPosition.x}, {gridPosition.y})");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error creating road at ({gridPosition.x}, {gridPosition.y}): {ex.Message}");
            }
            
            // フォールバック：デフォルトブロック
            return CreateDefaultBlock(config, cellSize);
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
        
        #region Road System Integration
        
        /// <summary>
        /// 指定位置の道路タイプを検出（RoadSystem.DetectRoadTypeを統合）
        /// </summary>
        private static RoadDirection DetectRoadType(GridMapData gridData, int x, int y)
        {
            if (gridData == null)
            {
                UnityEngine.Debug.LogWarning("GridData is null in DetectRoadType");
                return RoadDirection.Single;
            }
            
            bool north = IsRoadAt(gridData, x, y + 1);
            bool south = IsRoadAt(gridData, x, y - 1);
            bool east = IsRoadAt(gridData, x + 1, y);
            bool west = IsRoadAt(gridData, x - 1, y);
            
            int connections = (north ? 1 : 0) + (south ? 1 : 0) + 
                             (east ? 1 : 0) + (west ? 1 : 0);
            
            return connections switch
            {
                0 => RoadDirection.Single,
                1 => GetEndCapDirection(north, south, east, west),
                2 => GetTwoConnectionType(north, south, east, west),
                3 => GetTJunctionType(north, south, east, west),
                4 => RoadDirection.Cross,
                _ => RoadDirection.Horizontal
            };
        }
        
        private static RoadDirection GetEndCapDirection(bool n, bool s, bool e, bool w)
        {
            if (n) return RoadDirection.Vertical;  // 北に接続 → 縦道
            if (s) return RoadDirection.Vertical;  // 南に接続 → 縦道
            if (e) return RoadDirection.Horizontal; // 東に接続 → 横道
            if (w) return RoadDirection.Horizontal; // 西に接続 → 横道
            return RoadDirection.EndCap;
        }
        
        private static RoadDirection GetTwoConnectionType(bool n, bool s, bool e, bool w)
        {
            if (n && s) return RoadDirection.Vertical;
            if (e && w) return RoadDirection.Horizontal;
            if (n && e) return RoadDirection.CornerNE;
            if (n && w) return RoadDirection.CornerNW;
            if (s && e) return RoadDirection.CornerSE;
            if (s && w) return RoadDirection.CornerSW;
            return RoadDirection.Horizontal;
        }
        
        private static RoadDirection GetTJunctionType(bool n, bool s, bool e, bool w)
        {
            if (!n) return RoadDirection.TJunctionS; // 北がない = 南向きT字路
            if (!s) return RoadDirection.TJunctionN; // 南がない = 北向きT字路
            if (!e) return RoadDirection.TJunctionW; // 東がない = 西向きT字路
            if (!w) return RoadDirection.TJunctionE; // 西がない = 東向きT字路
            return RoadDirection.Cross;
        }
        
        private static bool IsRoadAt(GridMapData gridData, int x, int y)
        {
            if (!gridData.IsValidPosition(x, y)) return false;
            var cell = gridData.GetCell(x, y);
            return cell.isOccupied && cell.blockType == DungeonBlockType.Road;
        }
        
        // GetRoadPrefab method removed - now integrated into DungeonBlockConfiguration.GetRoadPrefab()
        
        #endregion
        
        public static DungeonBlockConfiguration[] GetDefaultConfigurations()
        {
            var configurations = new DungeonBlockConfiguration[12]; // 道路用に5つ追加
            
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
            
            // Road 1x1 (単体道路)
            configurations[6] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[6].prefab = null;
            configurations[6].blockType = DungeonBlockType.Road;
            configurations[6].size = new Vector2Int(1, 1);
            configurations[6].spawnWeight = 5f;
            configurations[6].maxInstances = -1;
            configurations[6].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(1, 1));
            
            // Road 2x2 (現在使用中)
            configurations[7] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[7].prefab = null;
            configurations[7].blockType = DungeonBlockType.Road;
            configurations[7].size = new Vector2Int(2, 2);
            configurations[7].spawnWeight = 10f;
            configurations[7].maxInstances = -1;
            configurations[7].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(2, 2));
            
            // Road 1x5 (短い横道)
            configurations[8] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[8].prefab = null;
            configurations[8].blockType = DungeonBlockType.Road;
            configurations[8].size = new Vector2Int(1, 5);
            configurations[8].spawnWeight = 8f;
            configurations[8].maxInstances = -1;
            configurations[8].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(1, 5));
            
            // Road 5x1 (短い縦道)
            configurations[9] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[9].prefab = null;
            configurations[9].blockType = DungeonBlockType.Road;
            configurations[9].size = new Vector2Int(5, 1);
            configurations[9].spawnWeight = 8f;
            configurations[9].maxInstances = -1;
            configurations[9].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(5, 1));
            
            // Road 2x10 (長い横道)
            configurations[10] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[10].prefab = null;
            configurations[10].blockType = DungeonBlockType.Road;
            configurations[10].size = new Vector2Int(2, 10);
            configurations[10].spawnWeight = 6f;
            configurations[10].maxInstances = -1;
            configurations[10].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(2, 10));
            
            // Road 10x2 (長い縦道)
            configurations[11] = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            configurations[11].prefab = null;
            configurations[11].blockType = DungeonBlockType.Road;
            configurations[11].size = new Vector2Int(10, 2);
            configurations[11].spawnWeight = 6f;
            configurations[11].maxInstances = -1;
            configurations[11].debugColor = DungeonBlockConfiguration.GetRoadColorBySize(new Vector2Int(10, 2));
            
            return configurations;
        }
    }
}