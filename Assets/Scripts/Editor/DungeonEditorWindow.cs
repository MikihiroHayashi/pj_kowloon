using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    public class DungeonEditorWindow : EditorWindow
    {
        private DungeonLayout currentLayout;
        private DungeonPiece[,] grid;
        private DungeonRoadPrefabSet roadPrefabSet;
        private DungeonPieceLibrary pieceLibrary;
        private List<DungeonPieceTemplate> currentPieceTemplates;

        private Vector2 scrollPosition;
        private Vector2 gridScrollPosition;
        private int selectedPieceIndex = 0;
        private int selectedCategoryIndex = 0;
        private bool isDragging = false;
        private bool showGrid = true;
        private float gridCellSize = 30f;
        private bool needsRefreshPieceTemplates = false;

        private const float PALETTE_WIDTH = 200f;
        private const float PROPERTIES_WIDTH = 250f;

        [MenuItem("Kowloon Break/Dungeon Editor")]
        public static void ShowWindow()
        {
            GetWindow<DungeonEditorWindow>("Dungeon Editor");
        }

        private void OnEnable()
        {
            InitializeEditor();
        }

        private void InitializeEditor()
        {
            try
            {
                Debug.Log("Initializing Dungeon Editor...");
                
                if (currentLayout == null)
                {
                    currentLayout = ScriptableObject.CreateInstance<DungeonLayout>();
                    currentLayout.gridSize = new Vector2Int(20, 20);
                    currentLayout.cellSize = 5f;
                    currentLayout.layoutName = "New Dungeon Layout";
                    Debug.Log("Created new DungeonLayout");
                }

                InitializeGrid();
                LoadPieceTemplates();
                LoadRoadPrefabs();
                
                Debug.Log("Dungeon Editor initialization completed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize Dungeon Editor: {e.Message}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
            }
        }

        private void InitializeGrid()
        {
            grid = new DungeonPiece[currentLayout.gridSize.x, currentLayout.gridSize.y];

            foreach (var piece in currentLayout.pieces)
            {
                PlacePieceOnGrid(piece);
            }
        }

        private void LoadPieceTemplates()
        {
            LoadPieceLibrary();
            RefreshPieceTemplates();
        }

        private void LoadPieceLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:DungeonPieceLibrary");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                pieceLibrary = AssetDatabase.LoadAssetAtPath<DungeonPieceLibrary>(path);
            }

            if (pieceLibrary == null)
            {
                CreateDefaultPieceLibrary();
            }
        }

        private void CreateDefaultPieceLibrary()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObject"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObject");
            }

            pieceLibrary = ScriptableObject.CreateInstance<DungeonPieceLibrary>();

            pieceLibrary.AddCategory("Buildings", new Color(0.3f, 0.5f, 0.9f));
            pieceLibrary.AddCategory("Roads", new Color(0.9f, 0.9f, 0.3f));
            pieceLibrary.AddCategory("Special", new Color(0.3f, 0.9f, 0.5f));
            pieceLibrary.AddCategory("Decorations", new Color(0.9f, 0.3f, 0.9f));

            // 既存のPrefabを検索して割り当て
            GameObject buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefab/Dungeon/Dungeon_5x5.prefab");
            GameObject largeBuildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefab/Dungeon/Dungeon_10x10.prefab");
            
            Debug.Log($"=== DungeonEditorWindow Prefab Assignment ===");
            Debug.Log($"buildingPrefab: {(buildingPrefab != null ? buildingPrefab.name : "NULL")}");
            Debug.Log($"largeBuildingPrefab: {(largeBuildingPrefab != null ? largeBuildingPrefab.name : "NULL")}");
            Debug.Log($"=== End DungeonEditorWindow Prefab Assignment ===");

            var defaultPieces = new[]
            {
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Small Building 1x1", 
                    type = PieceType.Building, 
                    size = new Vector2Int(1, 1),
                    prefab = buildingPrefab,
                    displayColor = new Color(0.7f, 0.7f, 0.9f)
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Medium Building 2x2", 
                    type = PieceType.Building, 
                    size = new Vector2Int(2, 2),
                    prefab = largeBuildingPrefab,
                    displayColor = new Color(0.6f, 0.6f, 0.8f)
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Long Building 1x2", 
                    type = PieceType.Building, 
                    size = new Vector2Int(1, 2),
                    prefab = buildingPrefab,
                    displayColor = new Color(0.8f, 0.6f, 0.7f),
                    canRotate = true
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Road Start Point", 
                    type = PieceType.RoadStart, 
                    size = new Vector2Int(1, 1),
                    isRoadStartPoint = true,
                    blocksPaths = false,
                    displayColor = new Color(0.9f, 0.9f, 0.3f)
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Player Spawn", 
                    type = PieceType.SpawnPoint, 
                    size = new Vector2Int(1, 1),
                    blocksPaths = false,
                    displayColor = new Color(0.3f, 0.9f, 0.3f)
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Exit Point", 
                    type = PieceType.ExitPoint, 
                    size = new Vector2Int(1, 1),
                    blocksPaths = false,
                    displayColor = new Color(0.9f, 0.3f, 0.3f)
                }
            };

            // Buildings カテゴリ (index 0)
            for (int i = 0; i < 3; i++)
            {
                pieceLibrary.AddPieceToCategory(0, defaultPieces[i]);
            }
            
            // Roads カテゴリ (index 1)
            pieceLibrary.AddPieceToCategory(1, defaultPieces[3]);
            
            // Special カテゴリ (index 2)
            for (int i = 4; i < defaultPieces.Length; i++)
            {
                pieceLibrary.AddPieceToCategory(2, defaultPieces[i]);
            }

            AssetDatabase.CreateAsset(pieceLibrary, "Assets/ScriptableObject/DefaultDungeonPieceLibrary.asset");
            AssetDatabase.SaveAssets();
        }

        private void LoadRoadPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:DungeonRoadPrefabSet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                roadPrefabSet = AssetDatabase.LoadAssetAtPath<DungeonRoadPrefabSet>(path);
            }
        }

        private void RefreshPieceTemplates()
        {
            currentPieceTemplates = new List<DungeonPieceTemplate>();

            if (pieceLibrary != null)
            {
                Debug.Log($"=== RefreshPieceTemplates Debug ===");
                Debug.Log($"Selected Category Index: {selectedCategoryIndex}");
                Debug.Log($"Total Categories: {pieceLibrary.Categories.Count}");
                
                // selectedCategoryIndex が 0 の場合は "All" を表示
                if (selectedCategoryIndex == 0)
                {
                    currentPieceTemplates.AddRange(pieceLibrary.GetAllPieces());
                    Debug.Log($"Showing ALL pieces: {currentPieceTemplates.Count}");
                }
                else if (selectedCategoryIndex > 0 && selectedCategoryIndex <= pieceLibrary.Categories.Count)
                {
                    // インデックスを1つずらす（0="All", 1=Categories[0], 2=Categories[1]...)
                    int categoryIndex = selectedCategoryIndex - 1;
                    currentPieceTemplates.AddRange(pieceLibrary.Categories[categoryIndex].pieces);
                    Debug.Log($"Showing category '{pieceLibrary.Categories[categoryIndex].name}' pieces: {currentPieceTemplates.Count}");
                }
                
                // デバッグ: 各ピースの情報を表示
                foreach (var piece in currentPieceTemplates)
                {
                    Debug.Log($"  - {piece.name} ({piece.type})");
                }
                Debug.Log($"=== End RefreshPieceTemplates Debug ===");
            }

            selectedPieceIndex = Mathf.Clamp(selectedPieceIndex, 0, Mathf.Max(0, currentPieceTemplates.Count - 1));
        }

        private void OnGUI()
        {
            // null参照チェックを追加
            if (currentLayout == null)
            {
                EditorGUILayout.HelpBox("Initializing editor... Please wait.", MessageType.Info);
                InitializeEditor();
                return;
            }

            try
            {
                try
                {
                    DrawToolbar();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawToolbar: {e.Message}");
                    throw;
                }

                EditorGUILayout.BeginHorizontal();
                try
                {
                    try
                    {
                        DrawPiecesPalette();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error in DrawPiecesPalette: {e.Message}");
                        throw;
                    }

                    try
                    {
                        DrawGridEditor();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error in DrawGridEditor: {e.Message}");
                        throw;
                    }

                    try
                    {
                        DrawProperties();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error in DrawProperties: {e.Message}");
                        throw;
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                try
                {
                    HandleEvents();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in HandleEvents: {e.Message}");
                    throw;
                }

                // 遅延実行: GUIの更新が完了してから実行
                if (needsRefreshPieceTemplates)
                {
                    needsRefreshPieceTemplates = false;
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            RefreshPieceTemplates();
                            Repaint();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error in delayed RefreshPieceTemplates: {e.Message}");
                        }
                    };
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"=== GUI Error in DungeonEditorWindow ===");
                Debug.LogError($"Error Message: {e.Message}");
                Debug.LogError($"Exception Type: {e.GetType().Name}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
                
                // 重要な変数の状態をログ出力
                Debug.LogError($"Debug Info - currentLayout: {(currentLayout != null ? "OK" : "NULL")}");
                Debug.LogError($"Debug Info - pieceLibrary: {(pieceLibrary != null ? "OK" : "NULL")}");
                Debug.LogError($"Debug Info - currentPieceTemplates: {(currentPieceTemplates != null ? $"OK (Count: {currentPieceTemplates.Count})" : "NULL")}");
                Debug.LogError($"Debug Info - grid: {(grid != null ? $"OK (Size: {grid.GetLength(0)}x{grid.GetLength(1)})" : "NULL")}");
                Debug.LogError($"Debug Info - roadPrefabSet: {(roadPrefabSet != null ? "OK" : "NULL")}");
                
                // 内部例外があるかチェック
                if (e.InnerException != null)
                {
                    Debug.LogError($"Inner Exception: {e.InnerException.Message}");
                }
                
                Debug.LogError($"=== End Error Report ===");
                
                // 強制的にGUIレイアウトをリセット
                GUIUtility.ExitGUI();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            try
            {
                if (GUILayout.Button("New", EditorStyles.toolbarButton))
                {
                    CreateNewLayout();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveLayout();
                }

                if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                {
                    LoadLayout();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Generate Roads", EditorStyles.toolbarButton))
                {
                    GenerateRoads();
                }

                if (GUILayout.Button("Generate 3D", EditorStyles.toolbarButton))
                {
                    Generate3DDungeon();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPiecesPalette()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PALETTE_WIDTH));
            try
            {
                EditorGUILayout.LabelField("Pieces Palette", EditorStyles.boldLabel);

                // *** 修正2: ボタン配置をtry-finallyで保護 ***
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("Refresh", EditorStyles.miniButton))
                    {
                        // *** 修正3: LoadPieceTemplates呼び出し時のエラーハンドリング ***
                        try
                        {
                            LoadPieceTemplates();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error loading piece templates: {e.Message}");
                        }
                    }
                    if (GUILayout.Button("Edit Library", EditorStyles.miniButton))
                    {
                        if (pieceLibrary != null)
                        {
                            Selection.activeObject = pieceLibrary;
                            EditorGUIUtility.PingObject(pieceLibrary);
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                if (pieceLibrary != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        EditorGUILayout.LabelField("Category:", GUILayout.Width(60));

                        string[] categoryNames = new string[pieceLibrary.Categories.Count + 1];
                        categoryNames[0] = "All";
                        for (int i = 0; i < pieceLibrary.Categories.Count; i++)
                        {
                            categoryNames[i + 1] = pieceLibrary.Categories[i].name;
                        }

                        int newCategoryIndex = EditorGUILayout.Popup(selectedCategoryIndex, categoryNames);
                        if (newCategoryIndex != selectedCategoryIndex)
                        {
                            selectedCategoryIndex = newCategoryIndex;
                            // GUI更新中のため、次のフレームで更新をスケジュール
                            needsRefreshPieceTemplates = true;
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                try
                {
                    if (currentPieceTemplates != null)
                    {
                        for (int i = 0; i < currentPieceTemplates.Count; i++)
                        {
                            var piece = currentPieceTemplates[i];
                            bool isSelected = selectedPieceIndex == i;

                            Color originalColor = GUI.backgroundColor;
                            if (isSelected)
                            {
                                GUI.backgroundColor = Color.cyan;
                            }
                            else
                            {
                                GUI.backgroundColor = piece.displayColor * 0.8f + Color.white * 0.2f;
                            }

                            GUILayout.BeginHorizontal();
                            try
                            {
                                if (piece.icon != null)
                                {
                                    GUILayout.Box(piece.icon.texture, GUILayout.Width(40), GUILayout.Height(40));
                                }
                                else
                                {
                                    GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
                                }

                                if (GUILayout.Button($"{piece.name}\n{piece.size.x}x{piece.size.y}\n{piece.type}",
                                    GUILayout.Height(40), GUILayout.ExpandWidth(true)))
                                {
                                    selectedPieceIndex = i;
                                }
                            }
                            finally
                            {
                                GUILayout.EndHorizontal();
                                GUI.backgroundColor = originalColor;
                            }
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawGridEditor()
        {
            if (currentLayout == null)
            {
                EditorGUILayout.HelpBox("Layout not initialized", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical();
            try
            {
                EditorGUILayout.LabelField($"Grid Editor - {currentLayout.layoutName ?? "Unnamed"}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                try
                {
                    showGrid = EditorGUILayout.Toggle("Show Grid", showGrid);
                    gridCellSize = EditorGUILayout.Slider("Cell Size", gridCellSize, 10f, 50f);
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition);
                try
                {
                    Rect gridRect = DrawGrid();
                    if (Event.current != null)
                    {
                        HandleGridInput(gridRect);
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private Rect DrawGrid()
        {
            float gridWidth = currentLayout.gridSize.x * gridCellSize;
            float gridHeight = currentLayout.gridSize.y * gridCellSize;

            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

            if (showGrid)
            {
                Handles.color = Color.gray;

                for (int x = 0; x <= currentLayout.gridSize.x; x++)
                {
                    float xPos = gridRect.x + x * gridCellSize;
                    Handles.DrawLine(new Vector3(xPos, gridRect.y), new Vector3(xPos, gridRect.y + gridHeight));
                }

                for (int y = 0; y <= currentLayout.gridSize.y; y++)
                {
                    float yPos = gridRect.y + y * gridCellSize;
                    Handles.DrawLine(new Vector3(gridRect.x, yPos), new Vector3(gridRect.x + gridWidth, yPos));
                }
            }

            DrawPiecesOnGrid(gridRect);

            return gridRect;
        }

        private void DrawPiecesOnGrid(Rect gridRect)
        {
            // 道路パスを描画
            DrawRoadPaths(gridRect);

            // ピースを描画
            foreach (var piece in currentLayout.pieces)
            {
                Rect pieceRect = new Rect(
                    gridRect.x + piece.gridPosition.x * gridCellSize,
                    gridRect.y + piece.gridPosition.y * gridCellSize,
                    piece.size.x * gridCellSize,
                    piece.size.y * gridCellSize
                );

                Color pieceColor = GetPieceColor(piece.type);
                EditorGUI.DrawRect(pieceRect, pieceColor);

                GUI.Label(pieceRect, piece.type.ToString(), EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawRoadPaths(Rect gridRect)
        {
            if (currentLayout.roadPaths == null || currentLayout.roadPaths.Count == 0)
                return;

            Color roadColor = new Color(0.8f, 0.6f, 0.2f, 0.7f);

            foreach (var roadPath in currentLayout.roadPaths)
            {
                if (roadPath.pathPoints == null || roadPath.pathPoints.Count == 0)
                    continue;

                for (int i = 0; i < roadPath.pathPoints.Count; i++)
                {
                    var point = roadPath.pathPoints[i];

                    Rect roadRect = new Rect(
                        gridRect.x + point.x * gridCellSize,
                        gridRect.y + point.y * gridCellSize,
                        gridCellSize,
                        gridCellSize
                    );

                    EditorGUI.DrawRect(roadRect, roadColor);

                    if (i < roadPath.pathPoints.Count - 1)
                    {
                        var nextPoint = roadPath.pathPoints[i + 1];
                        DrawRoadConnection(gridRect, point, nextPoint);
                    }
                }
            }
        }

        private void DrawRoadConnection(Rect gridRect, Vector2Int from, Vector2Int to)
        {
            Vector3 fromPos = new Vector3(
                gridRect.x + (from.x + 0.5f) * gridCellSize,
                gridRect.y + (from.y + 0.5f) * gridCellSize,
                0
            );

            Vector3 toPos = new Vector3(
                gridRect.x + (to.x + 0.5f) * gridCellSize,
                gridRect.y + (to.y + 0.5f) * gridCellSize,
                0
            );

            Handles.color = new Color(0.8f, 0.4f, 0.1f, 0.8f);
            Handles.DrawLine(fromPos, toPos);
        }

        private Color GetPieceColor(PieceType type)
        {
            switch (type)
            {
                case PieceType.Building: return new Color(0.7f, 0.7f, 0.9f, 0.8f);
                case PieceType.RoadStart: return new Color(0.9f, 0.9f, 0.3f, 0.8f);
                case PieceType.SpawnPoint: return new Color(0.3f, 0.9f, 0.3f, 0.8f);
                case PieceType.ExitPoint: return new Color(0.9f, 0.3f, 0.3f, 0.8f);
                default: return new Color(0.8f, 0.8f, 0.8f, 0.8f);
            }
        }

        private void HandleGridInput(Rect gridRect)
        {
            Event currentEvent = Event.current;

            // *** 修正6: イベント処理の安全性を向上 ***
            if (currentEvent == null) return;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (gridRect.Contains(currentEvent.mousePosition))
                {
                    Vector2Int gridPos = ScreenToGridPosition(currentEvent.mousePosition, gridRect);

                    try
                    {
                        if (currentEvent.control)
                        {
                            RemovePieceAt(gridPos);
                        }
                        else
                        {
                            PlacePieceAt(gridPos);
                        }

                        currentEvent.Use();
                        Repaint();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error handling grid input: {e.Message}");
                    }
                }
            }
        }

        private Vector2Int ScreenToGridPosition(Vector2 screenPos, Rect gridRect)
        {
            return new Vector2Int(
                Mathf.FloorToInt((screenPos.x - gridRect.x) / gridCellSize),
                Mathf.FloorToInt((screenPos.y - gridRect.y) / gridCellSize)
            );
        }

        private void PlacePieceAt(Vector2Int gridPos)
        {
            if (currentPieceTemplates == null || selectedPieceIndex < 0 || selectedPieceIndex >= currentPieceTemplates.Count)
                return;

            var template = currentPieceTemplates[selectedPieceIndex];

            if (!currentLayout.CanPlacePiece(gridPos, template.size))
                return;

            var newPiece = new DungeonPiece
            {
                id = System.Guid.NewGuid().ToString(),
                type = template.type,
                size = template.size,
                gridPosition = gridPos,
                rotation = 0f,
                prefab = template.prefab,
                isRoadStartPoint = template.isRoadStartPoint
            };

            currentLayout.pieces.Add(newPiece);
            PlacePieceOnGrid(newPiece);
        }

        private void RemovePieceAt(Vector2Int gridPos)
        {
            var pieceToRemove = GetPieceAt(gridPos);
            if (pieceToRemove != null)
            {
                currentLayout.pieces.Remove(pieceToRemove);
                RemovePieceFromGrid(pieceToRemove);
            }
        }

        private DungeonPiece GetPieceAt(Vector2Int gridPos)
        {
            if (!GridUtility.IsValidGridPosition(gridPos, currentLayout.gridSize))
                return null;

            return grid[gridPos.x, gridPos.y];
        }

        private void PlacePieceOnGrid(DungeonPiece piece)
        {
            for (int x = 0; x < piece.size.x; x++)
            {
                for (int y = 0; y < piece.size.y; y++)
                {
                    Vector2Int pos = piece.gridPosition + new Vector2Int(x, y);
                    if (GridUtility.IsValidGridPosition(pos, currentLayout.gridSize))
                    {
                        grid[pos.x, pos.y] = piece;
                    }
                }
            }
        }

        private void RemovePieceFromGrid(DungeonPiece piece)
        {
            for (int x = 0; x < piece.size.x; x++)
            {
                for (int y = 0; y < piece.size.y; y++)
                {
                    Vector2Int pos = piece.gridPosition + new Vector2Int(x, y);
                    if (GridUtility.IsValidGridPosition(pos, currentLayout.gridSize))
                    {
                        grid[pos.x, pos.y] = null;
                    }
                }
            }
        }

        private void DrawProperties()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PROPERTIES_WIDTH));
            try
            {
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

                currentLayout.layoutName = EditorGUILayout.TextField("Layout Name", currentLayout.layoutName);
                currentLayout.levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", currentLayout.levelType);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
                currentLayout.gridSize = EditorGUILayout.Vector2IntField("Grid Size", currentLayout.gridSize);
                currentLayout.cellSize = EditorGUILayout.FloatField("Cell Size (Unity Units)", currentLayout.cellSize);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Asset References", EditorStyles.boldLabel);

                var newPieceLibrary = (DungeonPieceLibrary)EditorGUILayout.ObjectField("Piece Library", pieceLibrary, typeof(DungeonPieceLibrary), false);
                if (newPieceLibrary != pieceLibrary)
                {
                    pieceLibrary = newPieceLibrary;
                    // GUI更新中のため、次のフレームで更新をスケジュール
                    needsRefreshPieceTemplates = true;
                }

                roadPrefabSet = (DungeonRoadPrefabSet)EditorGUILayout.ObjectField("Road Prefab Set", roadPrefabSet, typeof(DungeonRoadPrefabSet), false);

                EditorGUILayout.Space();

                if (GUILayout.Button("Create New Piece Library"))
                {
                    CreateNewPieceLibrary();
                }

                if (GUILayout.Button("Create New Road Prefab Set"))
                {
                    CreateNewRoadPrefabSet();
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Pieces Count: {currentLayout.pieces.Count}");
                EditorGUILayout.LabelField($"Road Paths: {currentLayout.roadPaths.Count}");
                EditorGUILayout.LabelField($"Available Templates: {(currentPieceTemplates?.Count ?? 0)}");
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void CreateNewPieceLibrary()
        {
            string path = EditorUtility.SaveFilePanel("Create New Piece Library", "Assets", "NewDungeonPieceLibrary", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                var newLibrary = ScriptableObject.CreateInstance<DungeonPieceLibrary>();
                newLibrary.AddCategory("Buildings", Color.blue);
                newLibrary.AddCategory("Roads", Color.yellow);
                newLibrary.AddCategory("Special", Color.green);

                AssetDatabase.CreateAsset(newLibrary, path);
                AssetDatabase.SaveAssets();

                pieceLibrary = newLibrary;
                RefreshPieceTemplates();

                Selection.activeObject = newLibrary;
                EditorGUIUtility.PingObject(newLibrary);
            }
        }

        private void CreateNewRoadPrefabSet()
        {
            string path = EditorUtility.SaveFilePanel("Create New Road Prefab Set", "Assets", "NewRoadPrefabSet", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                var newRoadSet = ScriptableObject.CreateInstance<DungeonRoadPrefabSet>();

                AssetDatabase.CreateAsset(newRoadSet, path);
                AssetDatabase.SaveAssets();

                roadPrefabSet = newRoadSet;

                Selection.activeObject = newRoadSet;
                EditorGUIUtility.PingObject(newRoadSet);
            }
        }

        private void HandleEvents()
        {
            Event currentEvent = Event.current;

            if (currentEvent != null && currentEvent.type == EventType.KeyDown)
            {
                switch (currentEvent.keyCode)
                {
                    case KeyCode.Delete:
                        currentEvent.Use();
                        Repaint();
                        break;
                }
            }
        }

        private void CreateNewLayout()
        {
            currentLayout = ScriptableObject.CreateInstance<DungeonLayout>();
            currentLayout.gridSize = new Vector2Int(20, 20);
            currentLayout.cellSize = 5f;
            currentLayout.layoutName = "New Dungeon Layout";
            InitializeGrid();
            Repaint();
        }

        private void SaveLayout()
        {
            string path = EditorUtility.SaveFilePanel("Save Dungeon Layout", "Assets", currentLayout.layoutName, "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                AssetDatabase.CreateAsset(currentLayout, path);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Save Complete", "Dungeon layout saved successfully!", "OK");
            }
        }

        private void LoadLayout()
        {
            string path = EditorUtility.OpenFilePanel("Load Dungeon Layout", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                var loadedLayout = AssetDatabase.LoadAssetAtPath<DungeonLayout>(path);
                if (loadedLayout != null)
                {
                    currentLayout = loadedLayout;
                    InitializeGrid();
                    Repaint();
                }
            }
        }

        private void GenerateRoads()
        {
            if (roadPrefabSet == null)
            {
                EditorUtility.DisplayDialog("Error", "Road Prefab Set is not assigned!", "OK");
                return;
            }

            var pathfinder = new RoadPathfinder(currentLayout, roadPrefabSet);
            currentLayout.roadPaths = pathfinder.GenerateRoadPaths();
            
            EditorUtility.DisplayDialog("Roads Generated", $"Generated {currentLayout.roadPaths.Count} road paths", "OK");
            Repaint();
        }

        private void Generate3DDungeon()
        {
            DungeonGenerator generator = Object.FindObjectOfType<DungeonGenerator>();

            if (generator == null)
            {
                bool createGenerator = EditorUtility.DisplayDialog("DungeonGenerator Not Found",
                    "DungeonGenerator is not found in the scene!\n\n" +
                    "Would you like to create one automatically?", "Yes", "Cancel");

                if (createGenerator)
                {
                    CreateDungeonGeneratorInScene();
                    generator = Object.FindObjectOfType<DungeonGenerator>();
                }
                else
                {
                    return;
                }
            }

            if (generator != null)
            {
                generator.GenerateDungeon(currentLayout);
                EditorUtility.DisplayDialog("Generation Complete", "3D Dungeon generated successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to create DungeonGenerator!", "OK");
            }
        }

        private void CreateDungeonGeneratorInScene()
        {
            GameObject generatorObject = new GameObject("Dungeon Generator");
            DungeonGenerator generator = generatorObject.AddComponent<DungeonGenerator>();

            GameObject dungeonParent = new GameObject("Generated Dungeon");

            var serializedObject = new SerializedObject(generator);
            var dungeonParentProperty = serializedObject.FindProperty("dungeonParent");
            if (dungeonParentProperty != null)
            {
                dungeonParentProperty.objectReferenceValue = dungeonParent.transform;
                serializedObject.ApplyModifiedProperties();
            }

            string[] guids = AssetDatabase.FindAssets("t:DungeonRoadPrefabSet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                DungeonRoadPrefabSet roadSet = AssetDatabase.LoadAssetAtPath<DungeonRoadPrefabSet>(path);

                if (roadSet != null)
                {
                    var roadPrefabsProperty = serializedObject.FindProperty("roadPrefabs");
                    if (roadPrefabsProperty != null)
                    {
                        roadPrefabsProperty.objectReferenceValue = roadSet;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            Selection.activeGameObject = generatorObject;
            EditorGUIUtility.PingObject(generatorObject);

            Debug.Log("DungeonGenerator created in scene automatically");
        }
    }
}