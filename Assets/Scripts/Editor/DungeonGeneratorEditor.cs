using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonGenerator))]
    public class DungeonGeneratorEditor : UnityEditor.Editor
    {
        private DungeonGenerator generator;
        private bool showGenerationSettings = true;
        private bool showBlockPrefabs = true;
        private bool showDebugSettings = true;
        private bool showGeneratedInfo = true;
        
        private void OnEnable()
        {
            generator = (DungeonGenerator)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Kowloon Break - Dungeon Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawGenerationControls();
            EditorGUILayout.Space();
            
            DrawGenerationSettings();
            EditorGUILayout.Space();
            
            DrawBlockPrefabsSettings();
            EditorGUILayout.Space();
            
            DrawDebugSettings();
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                DrawGeneratedInfo();
            }
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void DrawGenerationControls()
        {
            EditorGUILayout.LabelField("Generation Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Dungeon", GUILayout.Height(30)))
            {
                GenerateDungeonInEditor();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Preview in Scene", GUILayout.Height(30)))
            {
                SceneView.RepaintAll();
                Selection.activeGameObject = generator.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
            {
                ClearDungeonInEditor();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawGenerationSettings()
        {
            showGenerationSettings = EditorGUILayout.Foldout(showGenerationSettings, "Generation Settings", true);
            
            if (showGenerationSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dungeonSize"), new GUIContent("Dungeon Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"), new GUIContent("Cell Size"));
                
                EditorGUILayout.Space();
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useRandomSeed"), new GUIContent("Use Random Seed"));
                
                if (!serializedObject.FindProperty("useRandomSeed").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("generationSeed"), new GUIContent("Generation Seed"));
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Block Density Settings", EditorStyles.miniBoldLabel);
                
                var roomDensity = serializedObject.FindProperty("roomDensity");
                var corridorDensity = serializedObject.FindProperty("corridorDensity");
                var junctionDensity = serializedObject.FindProperty("junctionDensity");
                var specialRoomDensity = serializedObject.FindProperty("specialRoomDensity");
                
                EditorGUILayout.Slider(roomDensity, 0f, 1f, "Room Density");
                EditorGUILayout.Slider(corridorDensity, 0f, 1f, "Corridor Density");
                EditorGUILayout.Slider(junctionDensity, 0f, 1f, "Junction Density");
                EditorGUILayout.Slider(specialRoomDensity, 0f, 1f, "Special Room Density");
                
                float totalDensity = roomDensity.floatValue + corridorDensity.floatValue + 
                                   junctionDensity.floatValue + specialRoomDensity.floatValue;
                
                if (totalDensity > 1f)
                {
                    EditorGUILayout.HelpBox($"Total density is {totalDensity:F2}. Consider reducing values for better performance.", 
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Total density: {totalDensity:F2}", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawBlockPrefabsSettings()
        {
            showBlockPrefabs = EditorGUILayout.Foldout(showBlockPrefabs, "Block Prefabs", true);
            
            if (showBlockPrefabs)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty blockPrefabs = serializedObject.FindProperty("blockPrefabs");
                
                EditorGUILayout.PropertyField(blockPrefabs, new GUIContent("Block Prefabs"), true);
                
                if (blockPrefabs.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No block prefabs assigned. Default blocks will be generated automatically.", 
                        MessageType.Info);
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Add Common Block Sizes"))
                {
                    AddCommonBlockSizes(blockPrefabs);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void AddCommonBlockSizes(SerializedProperty blockPrefabs)
        {
            Undo.RecordObject(target, "Add Common Block Sizes");
            
            // 既存の要素をクリア
            blockPrefabs.ClearArray();
            
            // 直接DungeonGeneratorのフィールドにアクセスして設定
            ForceSetBlockPrefabs();
        }
        
        private void ForceSetBlockPrefabs()
        {
            // DungeonGeneratorに直接アクセスして配列を設定
            var dungeonGenerator = target as DungeonGenerator;
            
            var blockDataArray = new DungeonBlockData[]
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
                    blockType = DungeonBlockType.Corridor,
                    size = new Vector2Int(5, 10),
                    spawnWeight = 20f,
                    maxInstances = -1
                },
                new DungeonBlockData
                {
                    prefab = null,
                    blockType = DungeonBlockType.Special,
                    size = new Vector2Int(10, 10),
                    spawnWeight = 5f,
                    maxInstances = 3
                }
            };
            
            // privateフィールドにアクセスするためにリフレクションを使用
            var fieldInfo = typeof(DungeonGenerator).GetField("blockPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(dungeonGenerator, blockDataArray);
                EditorUtility.SetDirty(dungeonGenerator);
                serializedObject.Update();
                
                Debug.Log("=== Force set block prefabs using reflection ===");
                for (int i = 0; i < blockDataArray.Length; i++)
                {
                    var data = blockDataArray[i];
                    Debug.Log($"Set block {i}: {data.blockType} {data.size} weight:{data.spawnWeight} max:{data.maxInstances}");
                }
            }
            else
            {
                Debug.LogError("Could not find blockPrefabs field - falling back to SerializedProperty method");
                AddCommonBlockSizesSerializedProperty();
            }
        }
        
        private void AddCommonBlockSizesSerializedProperty()
        {
            var blockPrefabs = serializedObject.FindProperty("blockPrefabs");
            
            var commonSizes = new Vector2Int[]
            {
                new Vector2Int(5, 5),
                new Vector2Int(5, 10), 
                new Vector2Int(10, 10)
            };
            
            var commonTypes = new DungeonBlockType[]
            {
                DungeonBlockType.Room,
                DungeonBlockType.Corridor,
                DungeonBlockType.Special
            };
            
            var commonWeights = new float[]
            {
                30f,  // Room
                20f,  // Corridor
                5f    // Special
            };
            
            var maxInstances = new int[]
            {
                -1,   // Room (無制限)
                -1,   // Corridor (無制限)
                3     // Special (3個まで)
            };
            
            blockPrefabs.arraySize = commonSizes.Length;
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            
            for (int i = 0; i < commonSizes.Length; i++)
            {
                var element = blockPrefabs.GetArrayElementAtIndex(i);
                
                // 各プロパティを個別に設定
                var prefabProp = element.FindPropertyRelative("prefab");
                var blockTypeProp = element.FindPropertyRelative("blockType");
                var sizeProp = element.FindPropertyRelative("size");
                var weightProp = element.FindPropertyRelative("spawnWeight");
                var maxInstancesProp = element.FindPropertyRelative("maxInstances");
                
                prefabProp.objectReferenceValue = null;
                blockTypeProp.enumValueIndex = (int)commonTypes[i];
                
                // Vector2Intを確実に設定するため、x,yを個別に設定
                var sizeXProp = sizeProp.FindPropertyRelative("x");
                var sizeYProp = sizeProp.FindPropertyRelative("y");
                sizeXProp.intValue = commonSizes[i].x;
                sizeYProp.intValue = commonSizes[i].y;
                
                weightProp.floatValue = commonWeights[i];
                maxInstancesProp.intValue = maxInstances[i];
                
                Debug.Log($"Setting block {i}: {commonTypes[i]} {commonSizes[i]} weight:{commonWeights[i]} max:{maxInstances[i]}");
            }
            
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
            
            // 設定が正しく反映されたか確認
            Debug.Log("=== Verification after setting ===");
            for (int i = 0; i < blockPrefabs.arraySize; i++)
            {
                var element = blockPrefabs.GetArrayElementAtIndex(i);
                var blockType = (DungeonBlockType)element.FindPropertyRelative("blockType").enumValueIndex;
                var size = element.FindPropertyRelative("size").vector2IntValue;
                var weight = element.FindPropertyRelative("spawnWeight").floatValue;
                var maxInst = element.FindPropertyRelative("maxInstances").intValue;
                Debug.Log($"Verified block {i}: {blockType} {size} weight:{weight} max:{maxInst}");
            }
        }
        
        private void DrawDebugSettings()
        {
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
            
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugGizmos"), new GUIContent("Show Debug Gizmos"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("logGenerationProcess"), new GUIContent("Log Generation Process"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawGeneratedInfo()
        {
            showGeneratedInfo = EditorGUILayout.Foldout(showGeneratedInfo, "Generated Dungeon Info", true);
            
            if (showGeneratedInfo && generator.Grid != null)
            {
                EditorGUI.indentLevel++;
                
                var blocks = generator.GetAllBlocks();
                EditorGUILayout.LabelField($"Total Blocks: {blocks.Count}");
                
                var roomCount = generator.GetBlocksByType(DungeonBlockType.Room).Count;
                var corridorCount = generator.GetBlocksByType(DungeonBlockType.Corridor).Count;
                var junctionCount = generator.GetBlocksByType(DungeonBlockType.Junction).Count;
                var specialCount = generator.GetBlocksByType(DungeonBlockType.Special).Count;
                
                EditorGUILayout.LabelField($"Rooms: {roomCount}");
                EditorGUILayout.LabelField($"Corridors: {corridorCount}");
                EditorGUILayout.LabelField($"Junctions: {junctionCount}");
                EditorGUILayout.LabelField($"Special Rooms: {specialCount}");
                
                float coverage = (float)blocks.Count / (generator.DungeonSize.x * generator.DungeonSize.y) * 100f;
                EditorGUILayout.LabelField($"Grid Coverage: {coverage:F1}%");
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void GenerateDungeonInEditor()
        {
            if (generator == null) return;
            
            Undo.RecordObject(generator, "Generate Dungeon");
            
            ClearDungeonInEditor();
            
            var dungeonSize = generator.DungeonSize;
            var cellSize = generator.CellSize;
            
            var useRandomSeedProp = serializedObject.FindProperty("useRandomSeed");
            var generationSeedProp = serializedObject.FindProperty("generationSeed");
            
            int seed = useRandomSeedProp.boolValue ? 
                System.DateTime.Now.Millisecond : generationSeedProp.intValue;
            
            UnityEngine.Random.InitState(seed);
            
            var blockPrefabsProperty = serializedObject.FindProperty("blockPrefabs");
            if (blockPrefabsProperty.arraySize == 0)
            {
                EditorUtility.DisplayDialog("Dungeon Generator", 
                    "Block Prefabsが設定されていません。'Add Common Block Sizes'ボタンで基本設定を追加してください。", "OK");
                return;
            }
            
            // 重複チェック用のグリッドとインスタンスカウンターを初期化
            var occupiedGrid = new bool[dungeonSize.x, dungeonSize.y];
            blockInstanceCounts.Clear(); // インスタンスカウンターをリセット
            int placedCount = 0;
            
            // 各ブロックタイプの目標配置数を計算
            var roomDensity = serializedObject.FindProperty("roomDensity").floatValue;
            var corridorDensity = serializedObject.FindProperty("corridorDensity").floatValue;
            var junctionDensity = serializedObject.FindProperty("junctionDensity").floatValue;
            var specialRoomDensity = serializedObject.FindProperty("specialRoomDensity").floatValue;
            
            int totalGridCells = dungeonSize.x * dungeonSize.y;
            int targetRooms = Mathf.RoundToInt(totalGridCells * roomDensity / 25f); // 5x5の平均サイズで概算
            int targetCorridors = Mathf.RoundToInt(totalGridCells * corridorDensity / 25f);
            int targetJunctions = Mathf.RoundToInt(totalGridCells * junctionDensity / 25f);
            int targetSpecials = Mathf.RoundToInt(totalGridCells * specialRoomDensity / 100f); // 10x10の平均サイズで概算
            
            int totalTargetBlocks = targetRooms + targetCorridors + targetJunctions + targetSpecials;
            int maxAttempts = totalTargetBlocks * 20;
            int attemptCount = 0;
            
            Debug.Log($"Target blocks: Rooms={targetRooms}, Corridors={targetCorridors}, Junctions={targetJunctions}, Specials={targetSpecials}");
            
            while (placedCount < totalTargetBlocks && attemptCount < maxAttempts)
            {
                attemptCount++;
                
                var blockData = GetRandomBlockData(blockPrefabsProperty);
                if (blockData == null) continue;
                
                Vector2Int position = GetRandomValidPositionNoOverlap(blockData.Value.size, occupiedGrid, dungeonSize);
                if (position.x == -1) continue;
                
                // GetRandomValidPositionNoOverlap内で既にチェック済みなので、直接配置
                CreateBlockInEditor(blockData.Value, position, occupiedGrid);
                MarkAreaAsOccupied(position, blockData.Value.size, occupiedGrid);
                
                // インスタンスカウンターを更新
                int blockIndex = blockData.Value.blockIndex;
                blockInstanceCounts[blockIndex] = blockInstanceCounts.GetValueOrDefault(blockIndex, 0) + 1;
                placedCount++;
                
                if (serializedObject.FindProperty("logGenerationProcess").boolValue)
                {
                    int currentCount = blockInstanceCounts[blockIndex];
                    Debug.Log($"Placed {blockData.Value.blockType} at ({position.x}, {position.y}) size {blockData.Value.size} (Instance {currentCount})");
                }
            }
            
            float coverage = (float)GetOccupiedCellCount(occupiedGrid) / totalGridCells * 100f;
            
            // 統計レポート
            string report = $"Dungeon generated with seed: {seed}. Blocks placed: {placedCount}, Grid coverage: {coverage:F1}%\n";
            report += "Block Instance Counts:\n";
            
            for (int i = 0; i < blockPrefabsProperty.arraySize; i++)
            {
                var element = blockPrefabsProperty.GetArrayElementAtIndex(i);
                var blockType = (DungeonBlockType)element.FindPropertyRelative("blockType").enumValueIndex;
                var size = element.FindPropertyRelative("size").vector2IntValue;
                var maxInstances = element.FindPropertyRelative("maxInstances").intValue;
                int currentCount = blockInstanceCounts.GetValueOrDefault(i, 0);
                
                string maxText = maxInstances > 0 ? $"/{maxInstances}" : "/∞";
                report += $"  {blockType} ({size.x}x{size.y}): {currentCount}{maxText}\n";
            }
            
            Debug.Log(report);
            EditorUtility.SetDirty(generator);
        }
        
        private void ClearDungeonInEditor()
        {
            if (generator == null) return;
            
            var children = new List<Transform>();
            for (int i = 0; i < generator.transform.childCount; i++)
            {
                children.Add(generator.transform.GetChild(i));
            }
            
            foreach (var child in children)
            {
                if (child != null && child.name.Contains("DungeonBlock"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }
        
        private Dictionary<int, int> blockInstanceCounts = new Dictionary<int, int>();
        
        private (GameObject prefab, DungeonBlockType blockType, Vector2Int size, float spawnWeight, int blockIndex)? GetRandomBlockData(SerializedProperty blockPrefabs)
        {
            if (blockPrefabs.arraySize == 0) return null;
            
            var availableBlocks = new List<(GameObject, DungeonBlockType, Vector2Int, float, int)>();
            
            for (int i = 0; i < blockPrefabs.arraySize; i++)
            {
                var element = blockPrefabs.GetArrayElementAtIndex(i);
                var prefab = element.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
                var blockType = (DungeonBlockType)element.FindPropertyRelative("blockType").enumValueIndex;
                var size = element.FindPropertyRelative("size").vector2IntValue;
                var weight = element.FindPropertyRelative("spawnWeight").floatValue;
                var maxInstances = element.FindPropertyRelative("maxInstances").intValue;
                
                if (size.x <= 0 || size.y <= 0) 
                {
                    Debug.LogWarning($"Invalid block size ({size.x}, {size.y}) for {blockType} - skipping");
                    continue; // 無効なサイズをスキップ
                }
                
                // Max Instancesチェック
                int currentCount = blockInstanceCounts.GetValueOrDefault(i, 0);
                if (maxInstances > 0 && currentCount >= maxInstances)
                {
                    if (serializedObject.FindProperty("logGenerationProcess").boolValue)
                    {
                        Debug.Log($"Skipping {blockType} - Max instances ({maxInstances}) reached");
                    }
                    continue; // 上限に達したブロックはスキップ
                }
                
                availableBlocks.Add((prefab, blockType, size, weight, i));
            }
            
            if (availableBlocks.Count == 0) 
            {
                if (serializedObject.FindProperty("logGenerationProcess").boolValue)
                {
                    Debug.LogWarning("No available blocks for generation - all may have reached max instances or have invalid sizes");
                }
                return null;
            }
            
            // 重み付きランダム選択
            var weightedList = new List<(GameObject, DungeonBlockType, Vector2Int, float, int)>();
            foreach (var block in availableBlocks)
            {
                int weightInt = Mathf.RoundToInt(block.Item4);
                for (int j = 0; j < weightInt; j++)
                {
                    weightedList.Add(block);
                }
            }
            
            if (weightedList.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, weightedList.Count);
            return weightedList[randomIndex];
        }
        
        private Vector2Int GetRandomValidPosition(Vector2Int blockSize, bool[,] occupiedGrid, Vector2Int dungeonSize)
        {
            int maxAttempts = 100;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                int x = UnityEngine.Random.Range(0, dungeonSize.x - blockSize.x + 1);
                int y = UnityEngine.Random.Range(0, dungeonSize.y - blockSize.y + 1);
                Vector2Int position = new Vector2Int(x, y);
                
                if (CanPlaceBlock(position, blockSize, occupiedGrid, dungeonSize))
                {
                    return position;
                }
            }
            
            return new Vector2Int(-1, -1);
        }
        
        private bool CanPlaceBlock(Vector2Int position, Vector2Int blockSize, bool[,] occupiedGrid, Vector2Int dungeonSize)
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
                    if (occupiedGrid[x, y])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private Vector2Int GetRandomValidPositionNoOverlap(Vector2Int blockSize, bool[,] occupiedGrid, Vector2Int dungeonSize)
        {
            int maxX = dungeonSize.x - blockSize.x;
            int maxY = dungeonSize.y - blockSize.y;
            
            if (maxX < 0 || maxY < 0) return new Vector2Int(-1, -1);
            
            // 大きなダンジョンの場合はランダムサンプリング、小さい場合は全探索
            int totalPossiblePositions = (maxX + 1) * (maxY + 1);
            int maxSamples = Mathf.Min(totalPossiblePositions, 100);
            
            if (totalPossiblePositions <= 100)
            {
                // 小さいダンジョン：全位置をチェック
                var validPositions = new List<Vector2Int>();
                
                for (int x = 0; x <= maxX; x++)
                {
                    for (int y = 0; y <= maxY; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsAreaCompletelyFree(pos, blockSize, occupiedGrid, dungeonSize))
                        {
                            validPositions.Add(pos);
                        }
                    }
                }
                
                if (validPositions.Count == 0) return new Vector2Int(-1, -1);
                return validPositions[UnityEngine.Random.Range(0, validPositions.Count)];
            }
            else
            {
                // 大きなダンジョン：ランダムサンプリング
                for (int attempt = 0; attempt < maxSamples; attempt++)
                {
                    int x = UnityEngine.Random.Range(0, maxX + 1);
                    int y = UnityEngine.Random.Range(0, maxY + 1);
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    if (IsAreaCompletelyFree(pos, blockSize, occupiedGrid, dungeonSize))
                    {
                        return pos;
                    }
                }
                
                return new Vector2Int(-1, -1);
            }
        }
        
        private bool IsAreaCompletelyFree(Vector2Int position, Vector2Int blockSize, bool[,] occupiedGrid, Vector2Int dungeonSize)
        {
            // 境界チェック
            if (position.x < 0 || position.y < 0 || 
                position.x + blockSize.x > dungeonSize.x || 
                position.y + blockSize.y > dungeonSize.y)
            {
                return false;
            }
            
            // 指定されたエリアが完全に空いているかチェック
            for (int x = position.x; x < position.x + blockSize.x; x++)
            {
                for (int y = position.y; y < position.y + blockSize.y; y++)
                {
                    if (occupiedGrid[x, y])
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private void MarkAreaAsOccupied(Vector2Int position, Vector2Int blockSize, bool[,] occupiedGrid)
        {
            for (int x = position.x; x < position.x + blockSize.x; x++)
            {
                for (int y = position.y; y < position.y + blockSize.y; y++)
                {
                    occupiedGrid[x, y] = true;
                }
            }
        }
        
        private int GetOccupiedCellCount(bool[,] occupiedGrid)
        {
            int count = 0;
            for (int x = 0; x < occupiedGrid.GetLength(0); x++)
            {
                for (int y = 0; y < occupiedGrid.GetLength(1); y++)
                {
                    if (occupiedGrid[x, y]) count++;
                }
            }
            return count;
        }
        
        private void CreateBlockInEditor((GameObject prefab, DungeonBlockType blockType, Vector2Int size, float spawnWeight, int blockIndex) blockData, 
            Vector2Int position, bool[,] occupiedGrid)
        {
            GameObject blockObject;
            
            if (blockData.prefab != null)
            {
                blockObject = PrefabUtility.InstantiatePrefab(blockData.prefab) as GameObject;
                blockObject.transform.SetParent(generator.transform);
            }
            else
            {
                blockObject = CreateDefaultBlockInEditor(blockData.blockType, blockData.size);
            }
            
            blockObject.name = $"DungeonBlock_{blockData.blockType}_{blockData.size.x}x{blockData.size.y}_{position.x}_{position.y}";
            
            var dungeonBlock = blockObject.GetComponent<DungeonBlock>();
            if (dungeonBlock == null)
            {
                dungeonBlock = blockObject.AddComponent<DungeonBlock>();
            }
            
            // プリファブの原点は左下角 (0,0,0) に設定されている想定
            Vector3 worldPos = new Vector3(position.x * generator.CellSize, 0, position.y * generator.CellSize);
            blockObject.transform.position = worldPos;
            
            // DungeonBlockコンポーネントに情報を設定
            dungeonBlock.GridPosition = position;
            dungeonBlock.IsOccupied = true;
            
            Undo.RegisterCreatedObjectUndo(blockObject, "Create Dungeon Block");
        }
        
        private GameObject CreateDefaultBlockInEditor(DungeonBlockType blockType, Vector2Int size)
        {
            var blockObject = new GameObject($"DefaultBlock_{blockType}_{size.x}x{size.y}");
            blockObject.transform.SetParent(generator.transform);
            
            var meshRenderer = blockObject.AddComponent<MeshRenderer>();
            var meshFilter = blockObject.AddComponent<MeshFilter>();
            var boxCollider = blockObject.AddComponent<BoxCollider>();
            
            var mesh = CreateBlockMesh(size);
            meshFilter.mesh = mesh;
            
            var material = new Material(Shader.Find("Standard"));
            material.color = GetBlockTypeColor(blockType);
            meshRenderer.material = material;
            
            Vector3 blockSize = new Vector3(size.x * generator.CellSize, 0.1f, size.y * generator.CellSize);
            boxCollider.size = blockSize;
            boxCollider.center = new Vector3(blockSize.x * 0.5f - generator.CellSize * 0.5f, 0, blockSize.z * 0.5f - generator.CellSize * 0.5f);
            
            return blockObject;
        }
        
        private Mesh CreateBlockMesh(Vector2Int size)
        {
            var mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(size.x * generator.CellSize, 0, 0);
            vertices[2] = new Vector3(size.x * generator.CellSize, 0, size.y * generator.CellSize);
            vertices[3] = new Vector3(0, 0, size.y * generator.CellSize);
            
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
        
        private void OnSceneGUI()
        {
            if (generator == null) return;
            
            Handles.color = Color.cyan;
            Vector3 dungeonWorldSize = new Vector3(
                generator.DungeonSize.x * generator.CellSize, 
                0, 
                generator.DungeonSize.y * generator.CellSize
            );
            
            Vector3 center = generator.transform.position + dungeonWorldSize * 0.5f;
            Handles.DrawWireCube(center, dungeonWorldSize);
            
            Handles.Label(center + Vector3.up * 5f, $"Dungeon Area\n{generator.DungeonSize.x} x {generator.DungeonSize.y}");
        }
    }
}