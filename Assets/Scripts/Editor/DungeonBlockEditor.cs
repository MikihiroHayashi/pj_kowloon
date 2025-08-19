using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonBlock))]
    public class DungeonBlockEditor : UnityEditor.Editor
    {
        private DungeonBlock block;
        private bool showInfo = true;
        private bool showConfiguration = true;
        
        private void OnEnable()
        {
            block = (DungeonBlock)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dungeon Block (Simplified)", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawBlockInfo();
            EditorGUILayout.Space();
            
            DrawConfiguration();
            EditorGUILayout.Space();
            
            DrawActions();
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void DrawBlockInfo()
        {
            showInfo = EditorGUILayout.Foldout(showInfo, "Block Information", true);
            
            if (showInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"Block Type: {block.BlockType}");
                EditorGUILayout.LabelField($"Block Size: {block.BlockSize.x} x {block.BlockSize.y}");
                EditorGUILayout.LabelField($"Grid Position: ({block.GridPosition.x}, {block.GridPosition.y})");
                EditorGUILayout.LabelField($"World Position: {block.transform.position}");
                EditorGUILayout.LabelField($"Cell Size: {block.CellSize}");
                
                Vector3 worldSize = block.WorldSize;
                EditorGUILayout.LabelField($"World Size: {worldSize.x} x {worldSize.z}");
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawConfiguration()
        {
            showConfiguration = EditorGUILayout.Foldout(showConfiguration, "Configuration", true);
            
            if (showConfiguration)
            {
                EditorGUI.indentLevel++;
                
                var configProperty = serializedObject.FindProperty("configuration");
                EditorGUILayout.PropertyField(configProperty, new GUIContent("Block Configuration"));
                
                if (block.Configuration != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Configuration Details:", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Display Name", block.Configuration.GetDisplayName());
                    EditorGUILayout.FloatField("Spawn Weight", block.Configuration.spawnWeight);
                    EditorGUILayout.IntField("Max Instances", block.Configuration.maxInstances);
                    EditorGUILayout.ColorField("Debug Color", block.Configuration.debugColor);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("No configuration assigned. Create one below.", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Configuration"))
            {
                CreateConfiguration();
            }
            
            if (GUILayout.Button("Update Position"))
            {
                UpdatePosition();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Focus Camera"))
            {
                FocusCamera();
            }
            
            if (GUILayout.Button("Open Grid Editor"))
            {
                DungeonGridEditorWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateConfiguration()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Dungeon Block Configuration",
                $"DungeonBlockConfig_{block.BlockType}",
                "asset",
                "Choose location for new configuration"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var config = ScriptableObject.CreateInstance<DungeonBlockConfiguration>();
                config.blockType = block.BlockType;
                config.size = block.BlockSize;
                config.spawnWeight = 1f;
                config.maxInstances = -1;
                config.debugColor = DungeonBlockConfiguration.GetDefaultColor(block.BlockType);
                
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                
                // 作成したConfigurationを割り当て
                serializedObject.FindProperty("configuration").objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = config;
                
                Debug.Log($"Created configuration: {path}");
            }
        }
        
        private void UpdatePosition()
        {
            Undo.RecordObject(block, "Update Block Position");
            block.SetGridPosition(block.GridPosition);
            EditorUtility.SetDirty(block);
            Debug.Log($"Updated position for block at grid ({block.GridPosition.x}, {block.GridPosition.y})");
        }
        
        private void FocusCamera()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Frame(new Bounds(block.transform.position, block.WorldSize), false);
            }
        }
        
        private void OnSceneGUI()
        {
            if (block == null) return;
            
            // シンプルなワイヤーフレーム表示
            Handles.color = block.Configuration?.debugColor ?? Color.white;
            Vector3 size = block.WorldSize;
            Vector3 center = block.transform.position + new Vector3(size.x * 0.5f, 0, size.z * 0.5f);
            
            Handles.DrawWireCube(center, size);
            
            // ブロック情報ラベル
            Handles.Label(center + Vector3.up * 3f, 
                $"{block.BlockType} Block\n" +
                $"Size: {block.BlockSize.x}x{block.BlockSize.y}\n" +
                $"Grid: ({block.GridPosition.x}, {block.GridPosition.y})");
        }
    }
}