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
        private Vector2Int hoveredBlockOrigin = new Vector2Int(-1, -1);
        private Vector2Int hoveredBlockSize = Vector2Int.one;
        
        // パレット設定
        private int paletteColumns = 3;
        private float paletteButtonSize = 80f;
        
        private void OnEnable()
        {
            Debug.Log("[DungeonGridEditor] OnEnable called");
            
            LoadAvailableConfigurations();
            FindTargetGenerator();
            
            // GridMapDataが未設定の場合、デフォルトを作成
            if (gridMapData == null)
            {
                Debug.Log("[DungeonGridEditor] GridMapData is null, creating default");
                CreateDefaultGridMapData();
            }
            else
            {
                Debug.Log($"[DungeonGridEditor] GridMapData exists: {gridMapData.name} - Size: {gridMapData.gridSize}");
            }
            
            // 既存のGridMapDataでもnullチェック
            if (gridMapData != null && gridMapData.gridSize.x <= 0)
            {
                Debug.Log("[DungeonGridEditor] GridMapData has invalid size, reinitializing");
                gridMapData.InitializeGrid();
            }
            
            Debug.Log("[DungeonGridEditor] OnEnable completed");
        }
        
        private void CreateDefaultGridMapData()
        {
            gridMapData = CreateInstance<GridMapData>();
            gridMapData.gridSize = new Vector2Int(50, 50); // デフォルトサイズ設定
            gridMapData.InitializeGrid();
            gridMapData.name = "TempGridMapData";
            
            Debug.Log("Created temporary GridMapData with size 50x50");
        }
        
        private void OnGUI()
        {
            // GUILayoutGroupの不整合を防ぐため、シンプルな構造に変更
            EditorGUILayout.BeginVertical();
            
            try
            {
                // ヘッダー部分
                DrawHeader();
                DrawGridDataSection();
                DrawGridSettings();
                EditorGUILayout.Space();
                
                // メインエリア - 下部ボタン用のスペースを確保
                float headerHeight = 120f; // ヘッダー部分の推定高さ
                float bottomHeight = 50f;  // ボタン部分の高さ
                float availableHeight = position.height - headerHeight - bottomHeight;
                
                Rect mainRect = GUILayoutUtility.GetRect(0, Mathf.Max(availableHeight, 200f), GUILayout.ExpandWidth(true));
                DrawMainArea(mainRect);
                
                // 下部ボタン - 固定スペースで確実に表示
                GUILayout.Space(5f);
                DrawBottomControls();
            }
            catch (ExitGUIException)
            {
                // ExitGUIExceptionは特別なUnity例外で、再スローする必要がある
                throw;
            }
            catch (System.Exception e)
            {
                // 他のエラー時もGUI構造を維持
                EditorGUILayout.HelpBox($"GUI Error: {e.Message}\nPlease check console for details.", MessageType.Error);
                Debug.LogError($"DungeonGridEditorWindow GUI Error: {e}");
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
            
            // Input処理はGUIの外で実行
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp)
            {
                HandleInput();
            }
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
            var newGridMapData = (GridMapData)EditorGUILayout.ObjectField(gridMapData, typeof(GridMapData), false);
            
            if (newGridMapData != gridMapData)
            {
                Debug.Log($"[DungeonGridEditor] GridMapData changed from {(gridMapData?.name ?? "null")} to {(newGridMapData?.name ?? "null")}");
                gridMapData = newGridMapData;
                if (gridMapData == null)
                {
                    CreateDefaultGridMapData();
                }
            }
            
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                CreateNewGridMapData();
            }
            
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                if (gridMapData != null)
                {
                    Debug.Log("[DungeonGridEditor] Resetting grid data");
                    Undo.RecordObject(gridMapData, "Reset Grid");
                    gridMapData.ClearGrid();
                    EditorUtility.SetDirty(gridMapData);
                }
            }
            
            // デバッグボタンを追加
            if (GUILayout.Button("Debug", GUILayout.Width(50)))
            {
                LogDebugInfo();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 状態表示
            if (gridMapData == null)
            {
                EditorGUILayout.HelpBox("No Grid Map Data selected. A temporary grid will be used for editing.", MessageType.Warning);
            }
            else if (gridMapData.name == "TempGridMapData")
            {
                EditorGUILayout.HelpBox("Using temporary grid data. Click 'New' to create a saved asset.", MessageType.Info);
            }
            
            if (gridMapData != null)
            {
                EditorGUILayout.LabelField($"Grid Size: {gridMapData.gridSize.x} x {gridMapData.gridSize.y}");
                EditorGUILayout.LabelField($"Occupancy: {gridMapData.GetOccupancyPercentage():F1}%");
                EditorGUILayout.LabelField($"Available Configs: {(availableConfigurations?.Length ?? 0)}");
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
            if (gridMapData == null)
            {
                EditorGUI.HelpBox(gridRect, "GridMapData is null. Please create or select a GridMapData asset.", MessageType.Error);
                return;
            }
            
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
                    hoveredBlockOrigin = new Vector2Int(-1, -1);
                }
                else
                {
                    CalculateHoveredBlock();
                }
            }
            else
            {
                hoveredCell = new Vector2Int(-1, -1);
                hoveredBlockOrigin = new Vector2Int(-1, -1);
            }
            
            // Handlesの描画準備
            Handles.BeginGUI();
            
            // グリッドを描画
            for (int x = 0; x < gridMapData.gridSize.x; x++)
            {
                for (int y = 0; y < gridMapData.gridSize.y; y++)
                {
                    DrawGridCell(x, y, gridArea);
                }
            }
            
            Handles.EndGUI();
            
            // グリッドライン
            if (showGrid)
            {
                DrawGridLines(gridArea);
            }
            
            // ホバー表示（ブロック単位）
            if (hoveredBlockOrigin.x >= 0 && hoveredBlockOrigin.y >= 0)
            {
                DrawHoverBlock(hoveredBlockOrigin, hoveredBlockSize, gridArea);
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
            
            // EditorGUI.DrawRectをHandles.DrawSolidRectangleWithOutlineに変更
            Handles.DrawSolidRectangleWithOutline(cellRect, cellColor, Color.gray);
            
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
            Handles.BeginGUI();
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // 半透明のグレー
            
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
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// ホバー中のセルが属するブロックを計算
        /// </summary>
        private void CalculateHoveredBlock()
        {
            var cell = gridMapData.GetCell(hoveredCell.x, hoveredCell.y);
            
            if (cell.isOccupied)
            {
                // 既存ブロックの原点を検索
                hoveredBlockOrigin = FindBlockOrigin(hoveredCell, cell);
                hoveredBlockSize = cell.configurationSize;
            }
            else
            {
                // 新規ブロックを配置する場合
                if (selectedConfigurationIndex >= 0 && selectedConfigurationIndex < availableConfigurations.Length)
                {
                    var config = availableConfigurations[selectedConfigurationIndex];
                    hoveredBlockOrigin = hoveredCell;
                    hoveredBlockSize = config.size;
                    
                    // 配置可能かチェック
                    if (!gridMapData.CanPlaceBlock(hoveredBlockOrigin, hoveredBlockSize))
                    {
                        hoveredBlockOrigin = new Vector2Int(-1, -1);
                    }
                }
                else
                {
                    hoveredBlockOrigin = new Vector2Int(-1, -1);
                }
            }
        }
        
        /// <summary>
        /// 指定セルが属するブロックの原点を検索（統合版）
        /// </summary>
        private Vector2Int FindBlockOrigin(Vector2Int cellPos, GridMapData.GridCell cell)
        {
            if (!cell.isOccupied) return cellPos;
            
            // ブロックの左下角を検索（最大探索範囲を制限）
            int startX = Mathf.Max(0, cellPos.x - cell.configurationSize.x + 1);
            int startY = Mathf.Max(0, cellPos.y - cell.configurationSize.y + 1);
            
            for (int x = startX; x <= cellPos.x; x++)
            {
                for (int y = startY; y <= cellPos.y; y++)
                {
                    if (gridMapData.IsValidPosition(x, y))
                    {
                        var checkCell = gridMapData.GetCell(x, y);
                        if (checkCell.isOccupied && 
                            checkCell.configurationIndex == cell.configurationIndex &&
                            IsBlockOriginOptimized(x, y, checkCell))
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            return cellPos; // フォールバック
        }
        
        /// <summary>
        /// ブロック原点判定（最適化版）
        /// </summary>
        private bool IsBlockOriginOptimized(int x, int y, GridMapData.GridCell cell)
        {
            if (!cell.isOccupied) return false;
            
            // 左端かつ下端のチェック（最適化）
            bool isLeftEdge = (x == 0) || !IsSameBlock(x - 1, y, cell.configurationIndex);
            bool isBottomEdge = (y == 0) || !IsSameBlock(x, y - 1, cell.configurationIndex);
            
            return isLeftEdge && isBottomEdge;
        }
        
        /// <summary>
        /// 同じブロックかチェック（ヘルパー）
        /// </summary>
        private bool IsSameBlock(int x, int y, int configurationIndex)
        {
            if (!gridMapData.IsValidPosition(x, y)) return false;
            var checkCell = gridMapData.GetCell(x, y);
            return checkCell.isOccupied && checkCell.configurationIndex == configurationIndex;
        }
        
        /// <summary>
        /// ブロック単位のホバー表示
        /// </summary>
        private void DrawHoverBlock(Vector2Int origin, Vector2Int size, Rect gridArea)
        {
            if (size.x <= 0 || size.y <= 0) return;
            
            float startX = gridArea.x + origin.x * gridCellDisplaySize;
            float startY = gridArea.y + (gridMapData.gridSize.y - origin.y - size.y) * gridCellDisplaySize;
            float width = size.x * gridCellDisplaySize;
            float height = size.y * gridCellDisplaySize;
            
            var blockRect = new Rect(startX, startY, width, height);
            
            Handles.BeginGUI();
            
            // ブロック全体をハイライト
            Handles.color = new Color(1f, 1f, 0f, 0.3f); // 半透明の黄色
            Handles.DrawSolidRectangleWithOutline(blockRect, new Color(1f, 1f, 0f, 0.2f), Color.yellow);
            
            Handles.EndGUI();
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
                
                string buttonText = GetBlockDisplayName(config);
                
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
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Create Config Assets", GUILayout.Height(30)))
            {
                CreateConfigurationAssets();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // 簡単な操作説明
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Help:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Left Click: Place selected block");
            EditorGUILayout.LabelField("• Right Click: Remove block");
            EditorGUILayout.LabelField("• Create Config Assets: Generate default configuration files");
        }
        
        private void CreateConfigurationAssets()
        {
            if (EditorUtility.DisplayDialog("Create Configuration Assets", 
                "This will create default DungeonBlockConfiguration assets in the Configurations folder. Continue?", 
                "Yes", "No"))
            {
                try
                {
                    string configPath = "Assets/Configurations";
                    if (!System.IO.Directory.Exists(configPath))
                    {
                        System.IO.Directory.CreateDirectory(configPath);
                        AssetDatabase.Refresh();
                    }
                    
                    var defaultConfigs = DungeonBlockFactory.GetDefaultConfigurations();
                    int createdCount = 0;
                    
                    foreach (var config in defaultConfigs)
                    {
                        string fileName = $"Config_{config.blockType}_{config.size.x}x{config.size.y}.asset";
                        string assetPath = $"{configPath}/{fileName}";
                        
                        // 既存ファイルをスキップ
                        if (AssetDatabase.LoadAssetAtPath<DungeonBlockConfiguration>(assetPath) == null)
                        {
                            AssetDatabase.CreateAsset(config, assetPath);
                            createdCount++;
                        }
                    }
                    
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // 再読み込み
                    LoadAvailableConfigurations();
                    
                    EditorUtility.DisplayDialog("Success", 
                        $"Created {createdCount} configuration assets in {configPath}", 
                        "OK");
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        $"Failed to create configuration assets: {ex.Message}", 
                        "OK");
                }
            }
        }
        
        /// <summary>
        /// メインエリアを描画（IMGUI以外の方法で安全に処理）
        /// </summary>
        private void DrawMainArea(Rect mainRect)
        {
            // 左側：グリッド表示エリア
            Rect gridRect = new Rect(mainRect.x, mainRect.y, mainRect.width * 0.7f, mainRect.height);
            
            // 右側：パーツパレットエリア
            Rect paletteRect = new Rect(mainRect.x + mainRect.width * 0.7f, mainRect.y, mainRect.width * 0.3f, mainRect.height);
            
            // グリッドエリアの描画
            GUILayout.BeginArea(gridRect);
            EditorGUILayout.BeginVertical();
            DrawGridEditor(gridRect.height - 30f);
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
            
            // パレットエリアの描画
            GUILayout.BeginArea(paletteRect);
            EditorGUILayout.BeginVertical();
            DrawPartsPanel(paletteRect.height - 30f);
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
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
                        PaintBlock(hoveredCell);
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 1) // 右クリック
                    {
                        isErasing = true;
                        EraseBlock(hoveredCell);
                        currentEvent.Use();
                    }
                }
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    if (isPainting)
                    {
                        PaintBlock(hoveredCell);
                        currentEvent.Use();
                    }
                    else if (isErasing)
                    {
                        EraseBlock(hoveredCell);
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
        
        private void PaintBlock(Vector2Int cell)
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
        
        private void EraseBlock(Vector2Int cell)
        {
            var currentCell = gridMapData.GetCell(cell.x, cell.y);
            if (currentCell.isOccupied)
            {
                // ブロック全体を消去するため、原点を検索
                Vector2Int blockOrigin = FindBlockOrigin(cell, currentCell);
                
                Undo.RecordObject(gridMapData, "Erase Block");
                gridMapData.ClearArea(blockOrigin, currentCell.configurationSize);
                EditorUtility.SetDirty(gridMapData);
                Repaint();
            }
        }
        
        // Legacy method for compatibility - use EraseBlock instead
        private void EraseCell(Vector2Int cell)
        {
            EraseBlock(cell);
        }
        
        private Color GetBlockTypeColor(DungeonBlockType blockType)
        {
            return DungeonBlockConfiguration.GetDefaultColor(blockType);
        }
        
        private void LoadAvailableConfigurations()
        {
            try
            {
                Debug.Log("[DungeonGridEditor] Starting configuration loading...");
                
                // ScriptableObjectとして保存されたDungeonBlockConfigurationを検索
                string[] guids = AssetDatabase.FindAssets("t:DungeonBlockConfiguration");
                Debug.Log($"[DungeonGridEditor] Found {guids.Length} GUID(s) for DungeonBlockConfiguration");
                
                var configList = new List<DungeonBlockConfiguration>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.Log($"[DungeonGridEditor] Attempting to load: {path}");
                    
                    var config = AssetDatabase.LoadAssetAtPath<DungeonBlockConfiguration>(path);
                    if (config != null)
                    {
                        Debug.Log($"[DungeonGridEditor] Successfully loaded: {config.name} - {config.blockType} {config.size}");
                        configList.Add(config);
                    }
                    else
                    {
                        Debug.LogWarning($"[DungeonGridEditor] Failed to load asset at path: {path}");
                    }
                }
                
                // デフォルト設定を使用（アセットがない場合、または追加で使用）
                var defaultConfigs = DungeonBlockFactory.GetDefaultConfigurations();
                Debug.Log($"[DungeonGridEditor] Created {defaultConfigs.Length} default configurations");
                
                if (configList.Count == 0)
                {
                    // アセットが見つからない場合はデフォルトのみ使用
                    availableConfigurations = defaultConfigs;
                    Debug.Log($"[DungeonGridEditor] No assets found. Using {defaultConfigs.Length} default configurations only.");
                }
                else
                {
                    // 見つかったアセット + デフォルト設定を組み合わせ
                    var allConfigs = new List<DungeonBlockConfiguration>(configList);
                    allConfigs.AddRange(defaultConfigs);
                    availableConfigurations = allConfigs.ToArray();
                    Debug.Log($"[DungeonGridEditor] Combined: {configList.Count} assets + {defaultConfigs.Length} defaults = {availableConfigurations.Length} total");
                }
                
                // 選択インデックスをリセット
                selectedConfigurationIndex = Mathf.Clamp(selectedConfigurationIndex, 0, availableConfigurations.Length - 1);
                
                Debug.Log($"[DungeonGridEditor] Configuration loading completed. Selected index: {selectedConfigurationIndex}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DungeonGridEditor] Error loading configurations: {ex.Message}\n{ex.StackTrace}");
                // フォールバック：最小限のデフォルト設定
                availableConfigurations = CreateMinimalDefaultConfigurations();
                Debug.Log($"[DungeonGridEditor] Using fallback: {availableConfigurations.Length} minimal configurations");
            }
        }
        
        private DungeonBlockConfiguration[] CreateMinimalDefaultConfigurations()
        {
            var configs = new List<DungeonBlockConfiguration>();
            
            // 基本的なRoom設定のみ作成
            var roomConfig = CreateInstance<DungeonBlockConfiguration>();
            roomConfig.name = "Default Room";
            roomConfig.blockType = DungeonBlockType.Room;
            roomConfig.size = new Vector2Int(5, 5);
            roomConfig.debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Room);
            configs.Add(roomConfig);
            
            // 基本的なRoad設定のみ作成  
            var roadConfig = CreateInstance<DungeonBlockConfiguration>();
            roadConfig.name = "Default Road";
            roadConfig.blockType = DungeonBlockType.Road;
            roadConfig.size = new Vector2Int(2, 2);
            roadConfig.debugColor = DungeonBlockConfiguration.GetDefaultColor(DungeonBlockType.Road);
            configs.Add(roadConfig);
            
            return configs.ToArray();
        }
        
        private void FindTargetGenerator()
        {
            // まずFindObjectOfTypeで検索
            targetGenerator = FindObjectOfType<DungeonGenerator>();
            
            if (targetGenerator == null)
            {
                Debug.Log("[DungeonGridEditor] No DungeonGenerator found with FindObjectOfType");
                
                // 全てのGameObjectを検索してDungeonGeneratorコンポーネントを探す
                var allObjects = Resources.FindObjectsOfTypeAll<DungeonGenerator>();
                Debug.Log($"[DungeonGridEditor] FindObjectsOfTypeAll found {allObjects.Length} DungeonGenerators");
                
                foreach (var generator in allObjects)
                {
                    if (generator != null && generator.gameObject.scene.IsValid())
                    {
                        targetGenerator = generator;
                        Debug.Log($"[DungeonGridEditor] Using DungeonGenerator from scene: {generator.name}");
                        break;
                    }
                }
                
                if (targetGenerator == null)
                {
                    Debug.Log("[DungeonGridEditor] Still no valid DungeonGenerator found");
                }
            }
            else
            {
                Debug.Log($"[DungeonGridEditor] Found DungeonGenerator: {targetGenerator.name}");
                Debug.Log($"[DungeonGridEditor] Generator has {targetGenerator.GetAllBlocks().Count} blocks");
                var stats = targetGenerator.GetStats();
                Debug.Log($"[DungeonGridEditor] Generator stats - Total: {stats.totalBlocks}, Rooms: {stats.roomCount}, Roads: {stats.roadCount}");
            }
        }
        
        private void CreateDungeonGenerator()
        {
            GameObject generatorObject = new GameObject("DungeonGenerator");
            targetGenerator = generatorObject.AddComponent<DungeonGenerator>();
            
            // デフォルト設定を適用
            // DungeonGeneratorのpublicフィールドは直接設定できないので、リフレクションまたは別の方法を使用
            
            // Undoシステムに登録
            Undo.RegisterCreatedObjectUndo(generatorObject, "Create DungeonGenerator");
            
            // オブジェクトを選択
            Selection.activeObject = generatorObject;
            
            Debug.Log($"[DungeonGridEditor] Created new DungeonGenerator: {generatorObject.name}");
            EditorUtility.DisplayDialog("DungeonGenerator Created", 
                "A new DungeonGenerator has been created in the scene.", "OK");
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
                Debug.Log("[DungeonGridEditor] Target generator is null, searching for one...");
                FindTargetGenerator();
            }
            
            if (targetGenerator == null)
            {
                bool createNew = EditorUtility.DisplayDialog("No DungeonGenerator Found", 
                    "No DungeonGenerator found in the scene. Would you like to create one?", 
                    "Create", "Cancel");
                    
                if (createNew)
                {
                    CreateDungeonGenerator();
                }
                else
                {
                    return;
                }
            }
            
            GenerateDungeonFromGridData();
        }
        
        private void GenerateDungeonFromGridData()
        {
            Undo.RecordObject(targetGenerator, "Generate 3D Dungeon from Grid");
            
            // 既存のダンジョンをクリア
            ClearExistingDungeon();
            targetGenerator.ClearAllBlocks();
            
            int blocksCreated = 0;
            
            // グリッドデータから3Dダンジョンを生成
            for (int x = 0; x < gridMapData.gridSize.x; x++)
            {
                for (int y = 0; y < gridMapData.gridSize.y; y++)
                {
                    var cell = gridMapData.GetCell(x, y);
                    if (cell.isOccupied && cell.configurationIndex >= 0 && cell.configurationIndex < availableConfigurations.Length)
                    {
                        // ブロックの左下角のセルでのみ生成（重複防止）
                        if (IsBlockOrigin(x, y, cell))
                        {
                            var config = availableConfigurations[cell.configurationIndex];
                            Debug.Log($"Creating block at ({x}, {y}): {config.blockType} {config.size.x}x{config.size.y}");
                            
                            var createdBlock = CreateBlockFromCell(new Vector2Int(x, y), cell);
                            if (createdBlock != null)
                            {
                                // DungeonGeneratorに生成されたブロックを登録
                                var dungeonBlock = createdBlock.GetComponent<DungeonBlock>();
                                if (dungeonBlock != null)
                                {
                                    targetGenerator.RegisterBlock(dungeonBlock);
                                }
                                blocksCreated++;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"3D Dungeon generated successfully! Created {blocksCreated} blocks from grid data.");
            EditorUtility.SetDirty(targetGenerator);
        }
        
        /// <summary>
        /// ブロック原点判定（既存メソッド、後方互換性のため保持）
        /// </summary>
        private bool IsBlockOrigin(int x, int y, GridMapData.GridCell cell)
        {
            return IsBlockOriginOptimized(x, y, cell);
        }
        
        private GameObject CreateBlockFromCell(Vector2Int gridPos, GridMapData.GridCell cell)
        {
            if (cell.configurationIndex < 0 || cell.configurationIndex >= availableConfigurations.Length)
            {
                Debug.LogError($"Invalid configuration index {cell.configurationIndex} at ({gridPos.x}, {gridPos.y})");
                return null;
            }
                
            var config = availableConfigurations[cell.configurationIndex];
            
            try
            {
                // 統一されたFactoryを使用（道路対応を含む）
                GameObject blockObject = DungeonBlockFactory.CreateBlockFromPrefab(
                    config, 
                    targetGenerator.transform, 
                    gridPos, 
                    gridMapData.cellSize,
                    gridMapData    // 道路生成用
                );
                
                if (blockObject != null)
                {
                    Undo.RegisterCreatedObjectUndo(blockObject, "Create Dungeon Block from Grid");
                    
                    // ワールド位置を正確に設定
                    Vector3 worldPos = new Vector3(
                        gridPos.x * gridMapData.cellSize,
                        0,
                        gridPos.y * gridMapData.cellSize
                    );
                    blockObject.transform.position = worldPos;
                    
                    Debug.Log($"Successfully created block {config.blockType} at world position {worldPos}");
                }
                
                return blockObject;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create block at ({gridPos.x}, {gridPos.y}): {ex.Message}");
                return null;
            }
        }
        
        // CreateRoadBlock method removed - road generation unified in DungeonBlockFactory
        
        private void ClearExistingDungeon()
        {
            if (targetGenerator == null) return;
            
            var children = new List<Transform>();
            for (int i = 0; i < targetGenerator.transform.childCount; i++)
            {
                children.Add(targetGenerator.transform.GetChild(i));
            }
            
            int clearedCount = 0;
            foreach (var child in children)
            {
                if (child != null && (child.name.Contains("DungeonBlock") || child.name.Contains("PrefabBlock") || child.name.Contains("Block_")))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    clearedCount++;
                }
            }
            
            Debug.Log($"Cleared {clearedCount} existing dungeon blocks");
        }
        
        private void LoadFromGenerator()
        {
            if (targetGenerator == null)
            {
                Debug.Log("[DungeonGridEditor] Target generator is null, searching for one...");
                FindTargetGenerator();
            }
            
            if (targetGenerator == null)
            {
                EditorUtility.DisplayDialog("Error", "No DungeonGenerator found in scene. Please create one first.", "OK");
                return;
            }
            
            if (gridMapData == null)
            {
                EditorUtility.DisplayDialog("Error", "No grid data selected. Please create or select a GridMapData asset.", "OK");
                return;
            }
            
            // 既存の3DダンジョンからグリッドデータをリバースEngineer
            Undo.RecordObject(gridMapData, "Load from Generator");
            gridMapData.ClearGrid();
            
            var allBlocks = targetGenerator.GetAllBlocks();
            int loadedBlocks = 0;
            
            foreach (var block in allBlocks)
            {
                if (block == null) continue;
                
                Vector2Int gridPos = block.GridPosition;
                Vector2Int blockSize = block.BlockSize;
                
                // グリッド範囲チェック
                if (gridPos.x >= 0 && gridPos.y >= 0 && 
                    gridPos.x + blockSize.x <= gridMapData.gridSize.x && 
                    gridPos.y + blockSize.y <= gridMapData.gridSize.y)
                {
                    // 対応するconfigurationIndexを検索
                    int configIndex = FindConfigurationIndex(block.BlockType, blockSize);
                    
                    gridMapData.FillArea(gridPos, blockSize, block.BlockType, configIndex);
                    loadedBlocks++;
                }
                else
                {
                    Debug.LogWarning($"Block {block.name} at {gridPos} is outside grid bounds, skipping");
                }
            }
            
            EditorUtility.SetDirty(gridMapData);
            Repaint();
            
            Debug.Log($"Grid data loaded from existing 3D dungeon. Loaded {loadedBlocks} blocks.");
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
        
        /// <summary>
        /// ブロックの表示名を取得（道路の種類を区別）
        /// </summary>
        private string GetBlockDisplayName(DungeonBlockConfiguration config)
        {
            if (config.blockType == DungeonBlockType.Road)
            {
                return GetRoadDisplayName(config.size);
            }
            
            return $"{config.blockType}\n{config.size.x}x{config.size.y}";
        }
        
        /// <summary>
        /// 道路の表示名を取得
        /// </summary>
        private string GetRoadDisplayName(Vector2Int size)
        {
            if (size.x == 1 && size.y == 1)
                return "Road\n1x1\n(単体)";
            if (size.x == 2 && size.y == 2)
                return "Road\n2x2\n(広場)";
            if (size.x == 1 && size.y == 5)
                return "Road\n1x5\n(短横)";
            if (size.x == 5 && size.y == 1)
                return "Road\n5x1\n(短縦)";
            if (size.x == 2 && size.y == 10)
                return "Road\n2x10\n(長横)";
            if (size.x == 10 && size.y == 2)
                return "Road\n10x2\n(長縦)";
                
            return $"Road\n{size.x}x{size.y}";
        }
        
        /// <summary>
        /// デバッグ情報を出力
        /// </summary>
        private void LogDebugInfo()
        {
            Debug.Log("=== Dungeon Grid Editor Debug Info ===");
            
            // GridMapData情報
            if (gridMapData != null)
            {
                Debug.Log($"GridMapData: {gridMapData.name}");
                Debug.Log($"Grid Size: {gridMapData.gridSize.x} x {gridMapData.gridSize.y}");
                Debug.Log($"Cell Size: {gridMapData.cellSize}");
                Debug.Log($"Occupancy: {gridMapData.GetOccupancyPercentage():F1}%");
                
                // 数セルの状態をチェック
                for (int x = 0; x < Mathf.Min(3, gridMapData.gridSize.x); x++)
                {
                    for (int y = 0; y < Mathf.Min(3, gridMapData.gridSize.y); y++)
                    {
                        var cell = gridMapData.GetCell(x, y);
                        Debug.Log($"Cell[{x},{y}]: Occupied={cell.isOccupied}, Type={cell.blockType}, Size={cell.configurationSize}, ConfigIndex={cell.configurationIndex}");
                    }
                }
            }
            else
            {
                Debug.Log("GridMapData: NULL");
            }
            
            // Configuration情報
            if (availableConfigurations != null)
            {
                Debug.Log($"Available Configurations: {availableConfigurations.Length}");
                for (int i = 0; i < availableConfigurations.Length; i++)
                {
                    var config = availableConfigurations[i];
                    Debug.Log($"Config[{i}]: {config?.name ?? "NULL"} - Type: {config?.blockType}, Size: {config?.size}");
                }
                Debug.Log($"Selected Configuration Index: {selectedConfigurationIndex}");
            }
            else
            {
                Debug.Log("Available Configurations: NULL");
            }
            
            // UI状態
            Debug.Log($"Grid Cell Display Size: {gridCellDisplaySize}");
            Debug.Log($"Show Grid: {showGrid}");
            Debug.Log($"Show Block Types: {showBlockTypes}");
            Debug.Log($"Hovered Cell: {hoveredCell}");
            Debug.Log($"Hovered Block Origin: {hoveredBlockOrigin}");
            
            // Target Generator
            if (targetGenerator != null)
            {
                Debug.Log($"Target Generator: {targetGenerator.name}");
                Debug.Log($"Generator Blocks: {targetGenerator.GetAllBlocks().Count}");
            }
            else
            {
                Debug.Log("Target Generator: NULL");
            }
            
            Debug.Log("=== End Debug Info ===");
        }
    }
}