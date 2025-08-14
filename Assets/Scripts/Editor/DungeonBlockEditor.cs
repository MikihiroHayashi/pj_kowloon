using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonBlock))]
    public class DungeonBlockEditor : UnityEditor.Editor
    {
        private DungeonBlock block;
        private bool showConnections = true;
        private bool showSpawnPoints = true;
        private bool showEnvironment = true;
        
        private void OnEnable()
        {
            block = (DungeonBlock)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Kowloon Break - Dungeon Block", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawBlockConfiguration();
            EditorGUILayout.Space();
            
            DrawConnectionPoints();
            EditorGUILayout.Space();
            
            DrawSpawnPoints();
            EditorGUILayout.Space();
            
            DrawEnvironmentObjects();
            EditorGUILayout.Space();
            
            DrawBlockInfo();
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void DrawBlockConfiguration()
        {
            EditorGUILayout.LabelField("Block Configuration", EditorStyles.boldLabel);
            
            var configurationProp = serializedObject.FindProperty("configuration");
            
            if (configurationProp != null)
            {
                EditorGUILayout.PropertyField(configurationProp, new GUIContent("Configuration"));
                
                if (configurationProp.objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("Using new Configuration system. Legacy settings below are for fallback only.", MessageType.Info);
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("Create Configuration Asset"))
                    {
                        CreateConfigurationAsset();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No Configuration assigned. Using legacy settings below.", MessageType.Warning);
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("Create Configuration from Legacy Settings"))
                    {
                        CreateConfigurationFromLegacy();
                    }
                }
                
                EditorGUILayout.Space();
            }
            
            // Legacy settings
            EditorGUILayout.LabelField("Legacy Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockSize"), new GUIContent("Block Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockType"), new GUIContent("Block Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"), new GUIContent("Cell Size"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Size 5x5"))
            {
                serializedObject.FindProperty("blockSize").vector2IntValue = new Vector2Int(5, 5);
            }
            if (GUILayout.Button("Set Size 5x10"))
            {
                serializedObject.FindProperty("blockSize").vector2IntValue = new Vector2Int(5, 10);
            }
            if (GUILayout.Button("Set Size 10x10"))
            {
                serializedObject.FindProperty("blockSize").vector2IntValue = new Vector2Int(10, 10);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawConnectionPoints()
        {
            showConnections = EditorGUILayout.Foldout(showConnections, "Connection Points", true);
            
            if (showConnections)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("northConnectors"), new GUIContent("North Connectors"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("southConnectors"), new GUIContent("South Connectors"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("eastConnectors"), new GUIContent("East Connectors"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("westConnectors"), new GUIContent("West Connectors"), true);
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Auto-Generate Connection Points"))
                {
                    AutoGenerateConnectionPoints();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawSpawnPoints()
        {
            showSpawnPoints = EditorGUILayout.Foldout(showSpawnPoints, "Spawn Points", true);
            
            if (showSpawnPoints)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enemySpawnPoints"), new GUIContent("Enemy Spawn Points"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("itemSpawnPoints"), new GUIContent("Item Spawn Points"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawnPoint"), new GUIContent("Player Spawn Point"));
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Auto-Generate Enemy Spawns"))
                {
                    AutoGenerateEnemySpawns();
                }
                if (GUILayout.Button("Auto-Generate Item Spawns"))
                {
                    AutoGenerateItemSpawns();
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Create Player Spawn Point"))
                {
                    CreatePlayerSpawnPoint();
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawEnvironmentObjects()
        {
            showEnvironment = EditorGUILayout.Foldout(showEnvironment, "Environment Objects", true);
            
            if (showEnvironment)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("decorativeObjects"), new GUIContent("Decorative Objects"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacles"), new GUIContent("Obstacles"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactableObjects"), new GUIContent("Interactable Objects"), true);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Enable All"))
                {
                    block.EnableDecorations(true);
                    block.EnableObstacles(true);
                    block.EnableInteractables(true);
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Disable All"))
                {
                    block.EnableDecorations(false);
                    block.EnableObstacles(false);
                    block.EnableInteractables(false);
                }
                
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawBlockInfo()
        {
            EditorGUILayout.LabelField("Block Info", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector2IntField("Grid Position", block.GridPosition);
            EditorGUILayout.Toggle("Is Occupied", block.IsOccupied);
            EditorGUILayout.Vector3Field("World Size", block.WorldSize);
            EditorGUI.EndDisabledGroup();
        }
        
        private void AutoGenerateConnectionPoints()
        {
            Undo.RecordObject(target, "Auto-Generate Connection Points");
            
            var blockSize = block.BlockSize;
            var cellSize = block.CellSize;
            
            CreateConnectorArray("northConnectors", new Vector3(blockSize.x * cellSize * 0.5f, 0, blockSize.y * cellSize));
            CreateConnectorArray("southConnectors", new Vector3(blockSize.x * cellSize * 0.5f, 0, 0));
            CreateConnectorArray("eastConnectors", new Vector3(blockSize.x * cellSize, 0, blockSize.y * cellSize * 0.5f));
            CreateConnectorArray("westConnectors", new Vector3(0, 0, blockSize.y * cellSize * 0.5f));
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CreateConnectorArray(string propertyName, Vector3 position)
        {
            var connectorsProperty = serializedObject.FindProperty(propertyName);
            connectorsProperty.arraySize = 1;
            
            GameObject connector = new GameObject($"{propertyName.Replace("Connectors", "")}Connector");
            connector.transform.SetParent(block.transform);
            connector.transform.localPosition = position;
            
            connectorsProperty.GetArrayElementAtIndex(0).objectReferenceValue = connector.transform;
        }
        
        private void AutoGenerateEnemySpawns()
        {
            Undo.RecordObject(target, "Auto-Generate Enemy Spawns");
            
            var spawns = serializedObject.FindProperty("enemySpawnPoints");
            var blockSize = block.BlockSize;
            var cellSize = block.CellSize;
            
            int spawnCount = Mathf.Max(1, (blockSize.x * blockSize.y) / 25);
            spawns.arraySize = spawnCount;
            
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    UnityEngine.Random.Range(cellSize, blockSize.x * cellSize - cellSize),
                    0.1f,
                    UnityEngine.Random.Range(cellSize, blockSize.y * cellSize - cellSize)
                );
                
                GameObject spawn = new GameObject($"EnemySpawn_{i + 1}");
                spawn.transform.SetParent(block.transform);
                spawn.transform.localPosition = randomPos;
                
                spawns.GetArrayElementAtIndex(i).objectReferenceValue = spawn.transform;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AutoGenerateItemSpawns()
        {
            Undo.RecordObject(target, "Auto-Generate Item Spawns");
            
            var spawns = serializedObject.FindProperty("itemSpawnPoints");
            var blockSize = block.BlockSize;
            var cellSize = block.CellSize;
            
            int spawnCount = Mathf.Max(1, (blockSize.x * blockSize.y) / 30);
            spawns.arraySize = spawnCount;
            
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    UnityEngine.Random.Range(cellSize * 0.5f, blockSize.x * cellSize - cellSize * 0.5f),
                    0.1f,
                    UnityEngine.Random.Range(cellSize * 0.5f, blockSize.y * cellSize - cellSize * 0.5f)
                );
                
                GameObject spawn = new GameObject($"ItemSpawn_{i + 1}");
                spawn.transform.SetParent(block.transform);
                spawn.transform.localPosition = randomPos;
                
                spawns.GetArrayElementAtIndex(i).objectReferenceValue = spawn.transform;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CreatePlayerSpawnPoint()
        {
            Undo.RecordObject(target, "Create Player Spawn Point");
            
            var blockSize = block.BlockSize;
            var cellSize = block.CellSize;
            
            Vector3 centerPos = new Vector3(
                blockSize.x * cellSize * 0.5f,
                0.1f,
                blockSize.y * cellSize * 0.5f
            );
            
            GameObject playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.SetParent(block.transform);
            playerSpawn.transform.localPosition = centerPos;
            
            serializedObject.FindProperty("playerSpawnPoint").objectReferenceValue = playerSpawn.transform;
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CreateConfigurationFromLegacy()
        {
            Undo.RecordObject(target, "Create Configuration from Legacy Settings");
            
            // Configurationsフォルダを作成
            string configDir = "Assets/Configurations";
            if (!System.IO.Directory.Exists(configDir))
            {
                System.IO.Directory.CreateDirectory(configDir);
                AssetDatabase.Refresh();
            }
            
            // ScriptableObjectとしてConfigurationを作成
            var config = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
            config.blockType = block.BlockType;
            config.size = block.BlockSize;
            config.spawnWeight = 10f; // デフォルト値
            config.maxInstances = -1; // デフォルト値
            config.debugColor = DungeonBlockConfiguration.GetDefaultColor(block.BlockType);
            
            // アセットとして保存
            string assetPath = $"{configDir}/BlockConfig_{block.BlockType}_{block.BlockSize.x}x{block.BlockSize.y}.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            
            // Configurationプロパティにアサイン
            var configProp = serializedObject.FindProperty("configuration");
            if (configProp != null)
            {
                configProp.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
            }
            
            Debug.Log($"Created configuration asset: {assetPath}");
        }
        
        private void CreateConfigurationAsset()
        {
            var configProp = serializedObject.FindProperty("configuration");
            if (configProp?.objectReferenceValue != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(configProp.objectReferenceValue);
                Selection.activeObject = configProp.objectReferenceValue;
                EditorGUIUtility.PingObject(configProp.objectReferenceValue);
                Debug.Log($"Configuration asset selected: {assetPath}");
            }
        }
        
        private void OnSceneGUI()
        {
            if (block == null) return;
            
            // ブロックのワイヤーフレーム表示
            Handles.color = Color.cyan;
            Vector3 worldSize = block.WorldSize;
            Vector3 center = block.transform.position + new Vector3(worldSize.x * 0.5f, 0, worldSize.z * 0.5f);
            
            Handles.DrawWireCube(center, worldSize);
            
            // 選択時の詳細ラベル表示
            var style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            
            // 背景付きラベル
            var labelContent = $"{block.BlockType}\n{block.BlockSize.x}x{block.BlockSize.y}\nPos: {block.GridPosition}";
            Vector3 labelPos = center + Vector3.up * 2.5f;
            
            // 背景描画
            Handles.color = new Color(0, 0, 0, 0.7f);
            Vector2 labelSize = style.CalcSize(new GUIContent(labelContent));
            Vector3 screenPos = HandleUtility.WorldToGUIPoint(labelPos);
            
            Handles.BeginGUI();
            GUI.Box(new Rect(screenPos.x - labelSize.x * 0.5f - 5, screenPos.y - labelSize.y * 0.5f - 5, 
                           labelSize.x + 10, labelSize.y + 10), "");
            Handles.EndGUI();
            
            // ラベル表示
            Handles.color = Color.white;
            Handles.Label(labelPos, labelContent, style);
            
            DrawConnectionGizmos();
            DrawSpawnPointGizmos();
        }
        
        private void DrawConnectionGizmos()
        {
            DrawConnectorGizmos(block.GetConnectors(Direction.North), Color.blue);
            DrawConnectorGizmos(block.GetConnectors(Direction.South), Color.red);
            DrawConnectorGizmos(block.GetConnectors(Direction.East), Color.green);
            DrawConnectorGizmos(block.GetConnectors(Direction.West), Color.magenta);
        }
        
        private void DrawConnectorGizmos(Transform[] connectors, Color color)
        {
            if (connectors == null) return;
            
            Handles.color = color;
            foreach (var connector in connectors)
            {
                if (connector != null)
                {
                    Handles.SphereHandleCap(0, connector.position, Quaternion.identity, 0.3f, EventType.Repaint);
                }
            }
        }
        
        private void DrawSpawnPointGizmos()
        {
            if (block.GetPlayerSpawnPoint() != null)
            {
                Handles.color = Color.cyan;
                Handles.SphereHandleCap(0, block.GetPlayerSpawnPoint().position, Quaternion.identity, 0.5f, EventType.Repaint);
            }
        }
    }
}