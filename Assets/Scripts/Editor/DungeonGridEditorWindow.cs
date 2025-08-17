using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    public class DungeonGridEditorWindow : EditorWindow
    {
        [MenuItem("Kowloon Break/Dungeon Grid Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DungeonGridEditorWindow>("Dungeon Grid Editor");
            window.minSize = new Vector2(800, 700);
        }
        
        private GridMapData gridMapData;
        private DungeonBlockConfiguration[] availableConfigurations;
        private DungeonGenerator targetGenerator;
        private RoadConfiguration roadConfiguration;
        
        // UI状態
        private Vector2 scrollPosition;
        private Vector2 gridScrollPosition;
        private int selectedConfigurationIndex = 0;
        private bool isPainting = false;
        private bool isErasing = false;
        
        // グリッド表示設定
        private float gridCellDisplaySize = 10f;
        private bool showGrid = true;
        private bool showBlockTypes = true;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);
        
        // パレット設定
        private int paletteColumns = 3;
        private float paletteButtonSize = 80f;
        
        private void OnEnable()
        {
            LoadAvailableConfigurations();
            FindTargetGenerator();
        }
        
        private void OnGUI()
        {
            try
            {
                EditorGUILayout.BeginVertical();
                
                DrawHeader();
                DrawGridDataSection();
                DrawGridSettings();
                
                EditorGUILayout.Space();
                
                // ヘッダー部分の高さを計算
                float headerHeight = 140f; // ヘッダー、グリッドデータ、設定部分の概算高さ
                float bottomControlsHeight = 60f; // 下部ボタンエリアの高さ
                float availableHeight = position.height - headerHeight - bottomControlsHeight;
                
                EditorGUILayout.BeginHorizontal();
                
                // 左側：グリッド表示
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.7f));
                DrawGridEditor(availableHeight);
                EditorGUILayout.EndVertical();
                
                // 右側：パーツパレット
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.3f));
                DrawPartsPanel(availableHeight);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                DrawBottomControls();
                
                EditorGUILayout.EndVertical();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"GUI Error: {e.Message}", MessageType.Error);
            }
            
            HandleInput();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Dungeon Grid Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }
        
        private void DrawGridDataSection()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Grid Map Data:", GUILayout.Width(100));
            gridMapData = (GridMapData)EditorGUILayout.ObjectField(gridMapData, typeof(GridMapData), false);
            
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                CreateNewGridMapData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Road Config:", GUILayout.Width(100));
            roadConfiguration = (RoadConfiguration)EditorGUILayout.ObjectField(roadConfiguration, typeof(RoadConfiguration), false);
            EditorGUILayout.EndHorizontal();
            
            if (gridMapData != null)
            {
                EditorGUILayout.LabelField($"Grid Size: {gridMapData.gridSize.x} x {gridMapData.gridSize.y}");
                EditorGUILayout.LabelField($"Occupancy: {gridMapData.GetOccupancyPercentage():F1}%");
            }
        }
        
        private void DrawGridSettings()
        {
            if (gridMapData == null) return;
            
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grid Size:", GUILayout.Width(80));
            Vector2Int newSize = EditorGUILayout.Vector2IntField("", gridMapData.gridSize);
            if (newSize != gridMapData.gridSize && newSize.x > 0 && newSize.y > 0)
            {
                Undo.RecordObject(gridMapData, "Resize Grid");
                gridMapData.ResizeGrid(newSize);
                EditorUtility.SetDirty(gridMapData);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            gridCellDisplaySize = EditorGUILayout.Slider("Cell Display Size", gridCellDisplaySize, 5f, 20f);
            showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
            showBlockTypes = EditorGUILayout.Toggle("Show Types", showBlockTypes);
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawGridEditor(float availableHeight)
        {
            if (gridMapData == null)
            {
                EditorGUILayout.HelpBox("Please select or create a GridMapData asset", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField("Grid Editor", EditorStyles.boldLabel);
            
            float scrollViewHeight = availableHeight - 25f; // ラベル分を差し引く
            
            gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, 
                GUILayout.Width(position.width * 0.7f - 10), 
                GUILayout.Height(scrollViewHeight));
            
            var gridRect = GUILayoutUtility.GetRect(
                gridMapData.gridSize.x * gridCellDisplaySize + 20,
                gridMapData.gridSize.y * gridCellDisplaySize + 20
            );
            
            DrawGrid(gridRect);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawGrid(Rect gridRect)
        {
            var gridArea = new Rect(gridRect.x + 10, gridRect.y + 10, 
                gridMapData.gridSize.x * gridCellDisplaySize, 
                gridMapData.gridSize.y * gridCellDisplaySize);
            
            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;
            
            // マウス位置からグリッド座標を計算
            if (gridArea.Contains(mousePos))
            {
                Vector2 localPos = mousePos - gridArea.position;
                hoveredCell = new Vector2Int(
                    Mathf.FloorToInt(localPos.x / gridCellDisplaySize),
                    Mathf.FloorToInt((gridArea.height - localPos.y) / gridCellDisplaySize) // Y軸反転
                );
                
                if (!gridMapData.IsValidPosition(hoveredCell.x, hoveredCell.y))
                {
                    hoveredCell = new Vector2Int(-1, -1);
                }
            }
            else
            {
                hoveredCell = new Vector2Int(-1, -1);
            }
            
            // グリッドを描画
            for (int x = 0; x < gridMapData.gridSize.x; x++)
            {
                for (int y = 0; y < gridMapData.gridSize.y; y++)
                {
                    DrawGridCell(x, y, gridArea);
                }
            }
            
            // グリッドライン
            if (showGrid)
            {
                DrawGridLines(gridArea);
            }
            
            // ホバー表示
            if (hoveredCell.x >= 0 && hoveredCell.y >= 0)
            {
                DrawHoverCell(hoveredCell, gridArea);
            }
        }
        
        private void DrawGridCell(int x, int y, Rect gridArea)
        {
            var cell = gridMapData.GetCell(x, y);
            
            float cellX = gridArea.x + x * gridCellDisplaySize;
            float cellY = gridArea.y + (gridMapData.gridSize.y - 1 - y) * gridCellDisplaySize; // Y軸反転
            var cellRect = new Rect(cellX, cellY, gridCellDisplaySize, gridCellDisplaySize);
            
            Color cellColor = GetBlockTypeColor(cell.blockType);
            if (!cell.isOccupied)
            {
                cellColor = Color.white;
            }
            
            EditorGUI.DrawRect(cellRect, cellColor);
            
            // ブロックタイプ表示
            if (showBlockTypes && cell.isOccupied)
            {
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.black;
                
                string typeText = cell.blockType.ToString().Substring(0, 1); // 最初の文字
                EditorGUI.LabelField(cellRect, typeText, style);
            }
        }
        
        private void DrawGridLines(Rect gridArea)
        {
            Handles.color = Color.gray;
            
            // 縦線
            for (int x = 0; x <= gridMapData.gridSize.x; x++)
            {
                float lineX = gridArea.x + x * gridCellDisplaySize;
                Handles.DrawLine(
                    new Vector3(lineX, gridArea.y),
                    new Vector3(lineX, gridArea.y + gridArea.height)
                );
            }
            
            // 横線
            for (int y = 0; y <= gridMapData.gridSize.y; y++)
            {
                float lineY = gridArea.y + y * gridCellDisplaySize;
                Handles.DrawLine(
                    new Vector3(gridArea.x, lineY),
                    new Vector3(gridArea.x + gridArea.width, lineY)
                );
            }
        }
        
        private void DrawHoverCell(Vector2Int cell, Rect gridArea)
        {
            float cellX = gridArea.x + cell.x * gridCellDisplaySize;
            float cellY = gridArea.y + (gridMapData.gridSize.y - 1 - cell.y) * gridCellDisplaySize;
            var cellRect = new Rect(cellX, cellY, gridCellDisplaySize, gridCellDisplaySize);
            
            Handles.color = Color.yellow;
            Handles.DrawWireCube(cellRect.center, new Vector3(gridCellDisplaySize, gridCellDisplaySize, 0));
        }
        
        private void DrawPartsPanel(float availableHeight)
        {
            EditorGUILayout.LabelField("Parts Palette", EditorStyles.boldLabel);
            
            if (availableConfigurations == null || availableConfigurations.Length == 0)
            {
                EditorGUILayout.HelpBox("No block configurations found", MessageType.Warning);
                if (GUILayout.Button("Reload Configurations"))
                {
                    LoadAvailableConfigurations();
                }
                return;
            }
            
            float scrollViewHeight = availableHeight - 100f; // ラベルと選択情報表示エリア分を差し引く
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
                GUILayout.Height(scrollViewHeight));
            
            // ツールボタン
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Are you sure you want to clear the entire grid?", "Yes", "No"))
                {
                    Undo.RecordObject(gridMapData, "Clear Grid");
                    gridMapData.ClearGrid();
                    EditorUtility.SetDirty(gridMapData);
                }
            }
            
            EditorGUILayout.Space();
            
            // パーツパレット
            EditorGUILayout.LabelField("Block Types", EditorStyles.boldLabel);
            
            int columns = paletteColumns;
            int currentColumn = 0;
            bool horizontalStarted = false;
            
            for (int i = 0; i < availableConfigurations.Length; i++)
            {
                var config = availableConfigurations[i];
                if (config == null) continue;
                
                if (currentColumn == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    horizontalStarted = true;
                }
                
                bool isSelected = (selectedConfigurationIndex == i);
                GUI.backgroundColor = isSelected ? Color.yellow : Color.white;
                
                string buttonText = $"{config.blockType}\n{config.size.x}x{config.size.y}";
                
                if (GUILayout.Button(buttonText, GUILayout.Width(paletteButtonSize), GUILayout.Height(paletteButtonSize)))
                {
                    selectedConfigurationIndex = i;
                }
                
                currentColumn++;
                if (currentColumn >= columns)
                {
                    currentColumn = 0;
                    EditorGUILayout.EndHorizontal();
                    horizontalStarted = false;
                }
            }
            
            if (horizontalStarted)
            {
                EditorGUILayout.EndHorizontal();
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndScrollView();
            
            // 選択中のパーツ情報
            if (selectedConfigurationIndex >= 0 && selectedConfigurationIndex < availableConfigurations.Length)
            {
                var selectedConfig = availableConfigurations[selectedConfigurationIndex];
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Selected:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {selectedConfig.blockType}");
                EditorGUILayout.LabelField($"Size: {selectedConfig.size.x} x {selectedConfig.size.y}");
            }
        }
        
        private void DrawBottomControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate 3D Dungeon", GUILayout.Height(30)))
            {
                Generate3DDungeon();
            }
            
            GUI.backgroundColor = Color.blue;
            if (GUILayout.Button("Load from Generator", GUILayout.Height(30)))
            {
                LoadFromGenerator();
            }
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Save Grid Data", GUILayout.Height(30)))
            {
                SaveGridData();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private void HandleInput()
        {
            Event currentEvent = Event.current;
            
            if (hoveredCell.x >= 0 && hoveredCell.y >= 0 && gridMapData != null)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (currentEvent.button == 0) // 左クリック
                    {
                        isPainting = true;
                        PaintCell(hoveredCell);
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 1) // 右クリック
                    {
                        isErasing = true;
                        EraseCell(hoveredCell);
                        currentEvent.Use();
                    }
                }
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    if (isPainting)
                    {
                        PaintCell(hoveredCell);
                        currentEvent.Use();
                    }
                    else if (isErasing)
                    {
                        EraseCell(hoveredCell);
                        currentEvent.Use();
                    }
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    isPainting = false;
                    isErasing = false;
                }
            }
            
            if (currentEvent.type == EventType.MouseUp)
            {
                isPainting = false;
                isErasing = false;
            }
        }
        
        private void PaintCell(Vector2Int cell)
        {
            if (selectedConfigurationIndex < 0 || selectedConfigurationIndex >= availableConfigurations.Length)
                return;
                
            var config = availableConfigurations[selectedConfigurationIndex];
            
            if (gridMapData.CanPlaceBlock(cell, config.size))
            {
                Undo.RecordObject(gridMapData, "Paint Block");
                gridMapData.FillArea(cell, config.size, config.blockType, selectedConfigurationIndex);
                EditorUtility.SetDirty(gridMapData);
                Repaint();
            }
        }
        
        private void EraseCell(Vector2Int cell)
        {
            var currentCell = gridMapData.GetCell(cell.x, cell.y);
            if (currentCell.isOccupied)
            {
                Undo.RecordObject(gridMapData, "Erase Block");
                gridMapData.ClearArea(cell, currentCell.configurationSize);
                EditorUtility.SetDirty(gridMapData);
                Repaint();
            }
        }
        
        private Color GetBlockTypeColor(DungeonBlockType blockType)
        {
            return DungeonBlockConfiguration.GetDefaultColor(blockType);
        }
        
        private void LoadAvailableConfigurations()
        {
            // ScriptableObjectとして保存されたDungeonBlockConfigurationを検索
            string[] guids = AssetDatabase.FindAssets("t:DungeonBlockConfiguration");
            var configList = new List<DungeonBlockConfiguration>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<DungeonBlockConfiguration>(path);
                if (config != null)
                {
                    configList.Add(config);
                }
            }
            
            // デフォルト設定を追加（見つからない場合）
            if (configList.Count == 0)
            {
                availableConfigurations = DungeonBlockFactory.GetDefaultConfigurations();
            }
            else
            {
                availableConfigurations = configList.ToArray();
            }
        }
        
        private void FindTargetGenerator()
        {
            targetGenerator = FindObjectOfType<DungeonGenerator>();
        }
        
        private void CreateNewGridMapData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Grid Map Data",
                "NewGridMapData",
                "asset",
                "Choose location for new Grid Map Data"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var newGridData = CreateInstance<GridMapData>();
                newGridData.InitializeGrid();
                
                AssetDatabase.CreateAsset(newGridData, path);
                AssetDatabase.SaveAssets();
                
                gridMapData = newGridData;
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newGridData;
            }
        }
        
        private void Generate3DDungeon()
        {
            if (gridMapData == null)
            {
                EditorUtility.DisplayDialog("Error", "No grid data selected", "OK");
                return;
            }
            
            if (targetGenerator == null)
            {
                targetGenerator = FindObjectOfType<DungeonGenerator>();
            }
            
            if (targetGenerator == null)
            {
                EditorUtility.DisplayDialog("Error", "No DungeonGenerator found in scene", "OK");
                return;
            }
            
            GenerateDungeonFromGridData();
        }
        
        private void GenerateDungeonFromGridData()
        {
            Undo.RecordObject(targetGenerator, "Generate 3D Dungeon from Grid");
            
            // 既存のダンジョンをクリア
            ClearExistingDungeon();
            
            int blocksCreated = 0;
            
            // グリッドデータから3Dダンジョンを生成
            for (int x = 0; x < gridMapData.gridSize.x; x++)
            {
                for (int y = 0; y < gridMapData.gridSize.y; y++)
                {
                    var cell = gridMapData.GetCell(x, y);
                    if (cell.isOccupied && cell.configurationIndex >= 0)
                    {
                        // ブロックの左下角のセルでのみ生成（重複防止）
                        if (IsBlockOrigin(x, y, cell))
                        {
                            var config = availableConfigurations[cell.configurationIndex];
                            Debug.Log($"Creating block at ({x}, {y}): {config.blockType} {config.size.x}x{config.size.y}");
                            CreateBlockFromCell(new Vector2Int(x, y), cell);
                            blocksCreated++;
                        }
                    }
                }
            }
            
            Debug.Log($"3D Dungeon generated successfully! Created {blocksCreated} blocks from grid data.");
        }
        
        private bool IsBlockOrigin(int x, int y, GridMapData.GridCell cell)
        {
            if (!cell.isOccupied) return false;
            
            // 左と下の両方向をチェックして、真の原点かどうかを判定
            bool isLeftEdge = (x == 0) || !gridMapData.IsValidPosition(x - 1, y) || 
                             !gridMapData.GetCell(x - 1, y).isOccupied || 
                             gridMapData.GetCell(x - 1, y).configurationIndex != cell.configurationIndex;
                             
            bool isBottomEdge = (y == 0) || !gridMapData.IsValidPosition(x, y - 1) || 
                               !gridMapData.GetCell(x, y - 1).isOccupied || 
                               gridMapData.GetCell(x, y - 1).configurationIndex != cell.configurationIndex;
            
            // 左端かつ下端の場合のみ原点とする
            return isLeftEdge && isBottomEdge;
        }
        
        private void CreateBlockFromCell(Vector2Int gridPos, GridMapData.GridCell cell)
        {
            if (cell.configurationIndex < 0 || cell.configurationIndex >= availableConfigurations.Length)
                return;
                
            var config = availableConfigurations[cell.configurationIndex];
            GameObject blockObject;
            
            // 道の場合は特別処理
            if (config.blockType == DungeonBlockType.Road)
            {
                blockObject = CreateRoadBlock(gridPos, cell, config);
            }
            else
            {
                blockObject = DungeonBlockFactory.CreateBlockFromPrefab(
                    config, 
                    targetGenerator.transform, 
                    gridPos, 
                    gridMapData.cellSize
                );
            }
            
            var dungeonBlock = blockObject.GetComponent<DungeonBlock>();
            if (dungeonBlock == null)
            {
                dungeonBlock = blockObject.AddComponent<DungeonBlock>();
                dungeonBlock.InitializeFromConfiguration(config, gridMapData.cellSize);
            }
            
            dungeonBlock.SetGridPosition(gridPos);
            
            Undo.RegisterCreatedObjectUndo(blockObject, "Create Dungeon Block from Grid");
        }
        
        private GameObject CreateRoadBlock(Vector2Int gridPos, GridMapData.GridCell cell, DungeonBlockConfiguration config)
        {
            GameObject blockObject;
            
            if (roadConfiguration != null)
            {
                var roadSystem = new RoadSystem(gridMapData, roadConfiguration);
                var roadDirection = roadSystem.DetectRoadType(gridPos.x, gridPos.y);
                var roadPrefab = roadSystem.GetRoadPrefab(roadDirection);
                
                if (roadPrefab != null)
                {
                    blockObject = PrefabUtility.InstantiatePrefab(roadPrefab) as GameObject;
                    blockObject.transform.SetParent(targetGenerator.transform);
                    blockObject.name = $"Road_{roadDirection}_{gridPos.x}_{gridPos.y}";
                    
                    Debug.Log($"Created road at ({gridPos.x}, {gridPos.y}): {roadDirection}");
                }
                else
                {
                    // フォールバック：デフォルト道モデル
                    blockObject = DungeonBlockFactory.CreateBlockFromPrefab(
                        config, targetGenerator.transform, gridPos, gridMapData.cellSize);
                    blockObject.name = $"Road_Default_{gridPos.x}_{gridPos.y}";
                    
                    Debug.LogWarning($"No prefab found for road direction {roadDirection}, using default block");
                }
            }
            else
            {
                // RoadConfigurationが設定されていない場合のフォールバック
                blockObject = DungeonBlockFactory.CreateBlockFromPrefab(
                    config, targetGenerator.transform, gridPos, gridMapData.cellSize);
                blockObject.name = $"Road_NoConfig_{gridPos.x}_{gridPos.y}";
                
                Debug.LogWarning("RoadConfiguration not set, using default block for road");
            }
            
            // ワールド位置設定
            Vector3 worldPos = new Vector3(
                gridPos.x * gridMapData.cellSize, 0, gridPos.y * gridMapData.cellSize);
            blockObject.transform.position = worldPos;
            
            return blockObject;
        }
        
        private void ClearExistingDungeon()
        {
            var children = new List<Transform>();
            for (int i = 0; i < targetGenerator.transform.childCount; i++)
            {
                children.Add(targetGenerator.transform.GetChild(i));
            }
            
            foreach (var child in children)
            {
                if (child != null && (child.name.Contains("DungeonBlock") || child.name.Contains("PrefabBlock")))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }
        
        private void LoadFromGenerator()
        {
            if (targetGenerator == null)
            {
                EditorUtility.DisplayDialog("Error", "No DungeonGenerator found", "OK");
                return;
            }
            
            if (gridMapData == null)
            {
                EditorUtility.DisplayDialog("Error", "No grid data selected", "OK");
                return;
            }
            
            // 既存の3DダンジョンからグリッドデータをリバースEngineer
            Undo.RecordObject(gridMapData, "Load from Generator");
            gridMapData.ClearGrid();
            
            var allBlocks = targetGenerator.GetAllBlocks();
            foreach (var block in allBlocks)
            {
                Vector2Int gridPos = block.GridPosition;
                Vector2Int blockSize = block.BlockSize;
                
                // 対応するconfigurationIndexを検索
                int configIndex = FindConfigurationIndex(block.BlockType, blockSize);
                
                gridMapData.FillArea(gridPos, blockSize, block.BlockType, configIndex);
            }
            
            EditorUtility.SetDirty(gridMapData);
            Repaint();
            
            Debug.Log("Grid data loaded from existing 3D dungeon");
        }
        
        private int FindConfigurationIndex(DungeonBlockType blockType, Vector2Int size)
        {
            for (int i = 0; i < availableConfigurations.Length; i++)
            {
                var config = availableConfigurations[i];
                if (config.blockType == blockType && config.size == size)
                {
                    return i;
                }
            }
            return -1;
        }
        
        private void SaveGridData()
        {
            if (gridMapData != null)
            {
                EditorUtility.SetDirty(gridMapData);
                AssetDatabase.SaveAssets();
                Debug.Log("Grid data saved successfully");
            }
        }
    }
}