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
        
        private void OnSceneGUI()
        {
            if (block == null) return;
            
            Handles.color = Color.yellow;
            Vector3 worldSize = block.WorldSize;
            Vector3 center = block.transform.position + worldSize * 0.5f;
            
            Handles.DrawWireCube(center, worldSize);
            
            var style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;
            
            Handles.Label(center + Vector3.up * 2f, 
                $"{block.BlockType}\n{block.BlockSize.x}x{block.BlockSize.y}", style);
            
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