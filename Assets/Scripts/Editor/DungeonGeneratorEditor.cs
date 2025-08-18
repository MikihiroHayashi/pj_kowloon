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
            if (GUILayout.Button("Generate Dungeon (Runtime)", GUILayout.Height(30)))
            {
                GenerateUsingRuntimeSystem();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Dungeon", GUILayout.Height(30)))
            {
                ClearDungeonInEditor();
                SceneView.RepaintAll();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Primary Grid Editor Controls
            EditorGUILayout.LabelField("Primary Editor", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Open Grid Editor (Recommended)", GUILayout.Height(30)))
            {
                DungeonGridEditorWindow.ShowWindow();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Use Grid Editor for visual dungeon design and generation.", MessageType.Info);
        }
        
        // Generation settings removed - managed by DungeonGridEditorWindow
        
        // Block configurations settings removed - managed by DungeonGridEditorWindow
        
        // Helper methods removed - functionality moved to DungeonGridEditorWindow
        
        private void CreateDefaultConfigurations()
        {
            // Simplified method - just ensures runtime system has defaults
            Undo.RecordObject(target, "Create Default Block Configurations");
            
            var method = typeof(DungeonGenerator).GetMethod("CreateDefaultBlockConfigurations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(generator, null);
                serializedObject.Update();
                EditorUtility.SetDirty(generator);
                Debug.Log("Default configurations created for runtime system!");
            }
            else
            {
                Debug.LogWarning("Could not create default configurations. Use Grid Editor for dungeon creation.");
            }
        }
        
        // AddCommonBlockSizes methods removed - configuration handled by DungeonGridEditorWindow
        
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
        
        private void GenerateUsingRuntimeSystem()
        {
            if (generator == null) return;
            
            Undo.RecordObject(generator, "Generate Dungeon");
            
            try
            {
                // Runtime system configuration check
                if (generator.GetType().GetField("blockConfigurations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(generator) == null)
                {
                    Debug.LogWarning("Block configurations not initialized, creating defaults...");
                    CreateDefaultConfigurations();
                }
                
                ClearDungeonInEditor();
                generator.GenerateDungeon();
                Debug.Log("Dungeon generated using runtime system!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Runtime generation failed: {ex.Message}");
                Debug.LogError($"Please use Grid Editor for reliable dungeon creation.");
                
                // Open Grid Editor as recommended solution
                DungeonGridEditorWindow.ShowWindow();
            }
        }
        
        // Legacy generation method removed - use DungeonGridEditorWindow for dungeon creation
        
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
                if (child != null && (child.name.Contains("DungeonBlock") || child.name.Contains("PrefabBlock")))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }
        
        // Legacy helper methods removed - use DungeonGridEditorWindow for dungeon creation
        
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