using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonGenerator))]
    public class DungeonGeneratorEditor : UnityEditor.Editor
    {
        private DungeonGenerator generator;
        private bool showStats = true;
        
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
            
            DrawMainControls();
            EditorGUILayout.Space();
            
            DrawStats();
            EditorGUILayout.Space();
            
            DrawDefaultProperties();
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void DrawMainControls()
        {
            EditorGUILayout.LabelField("Main Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Open Grid Editor", GUILayout.Height(35)))
            {
                try 
                {
                    DungeonGridEditorWindow.ShowWindow();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to open Grid Editor: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", 
                        $"Failed to open Grid Editor: {ex.Message}\nCheck Console for details.", 
                        "OK");
                }
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All Blocks", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Clear Dungeon", 
                    "Are you sure you want to clear all dungeon blocks?", 
                    "Yes", "No"))
                {
                    ClearAllBlocks();
                }
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Use the Grid Editor for visual dungeon design and 3D generation.\n" +
                "This inspector shows generated blocks and basic statistics.\n\n" +
                "First Time Setup:\n" +
                "1. Click 'Open Grid Editor'\n" +
                "2. Click 'Create Config Assets' if you see configuration warnings\n" +
                "3. Design your dungeon in the grid\n" +
                "4. Click 'Generate 3D Dungeon'", 
                MessageType.Info);
        }
        
        private void DrawStats()
        {
            showStats = EditorGUILayout.Foldout(showStats, "Dungeon Statistics", true);
            
            if (showStats)
            {
                EditorGUI.indentLevel++;
                
                var stats = generator.GetStats();
                var allBlocks = generator.GetAllBlocks();
                
                EditorGUILayout.LabelField($"Total Blocks: {stats.totalBlocks}");
                
                if (stats.totalBlocks > 0)
                {
                    EditorGUILayout.LabelField($"├ Rooms: {stats.roomCount}");
                    EditorGUILayout.LabelField($"├ Corridors: {stats.corridorCount}");
                    EditorGUILayout.LabelField($"├ Junctions: {stats.junctionCount}");
                    EditorGUILayout.LabelField($"├ Special Rooms: {stats.specialCount}");
                    EditorGUILayout.LabelField($"└ Roads: {stats.roadCount}");
                    
                    float coverage = (float)stats.totalBlocks / (generator.DungeonSize.x * generator.DungeonSize.y) * 100f;
                    EditorGUILayout.LabelField($"Grid Coverage: {coverage:F1}%");
                }
                else
                {
                    EditorGUILayout.LabelField("No blocks generated yet");
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawDefaultProperties()
        {
            EditorGUILayout.LabelField("Display Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dungeonSize"), 
                new GUIContent("Dungeon Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"), 
                new GUIContent("Cell Size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugGizmos"), 
                new GUIContent("Show Debug Gizmos"));
        }
        
        private void ClearAllBlocks()
        {
            Undo.RecordObject(generator, "Clear All Dungeon Blocks");
            
            // 子オブジェクトからブロックを削除
            var children = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < generator.transform.childCount; i++)
            {
                children.Add(generator.transform.GetChild(i));
            }
            
            foreach (var child in children)
            {
                if (child != null && child.name.Contains("Block_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
            
            // 内部リストもクリア
            generator.ClearAllBlocks();
            
            Debug.Log("All dungeon blocks cleared");
            SceneView.RepaintAll();
        }
        
        private void OnSceneGUI()
        {
            if (generator == null || !generator.ShowDebugGizmos) return;
            
            Handles.color = Color.cyan;
            Vector3 dungeonWorldSize = new Vector3(
                generator.DungeonSize.x * generator.CellSize, 
                0, 
                generator.DungeonSize.y * generator.CellSize
            );
            
            Vector3 center = generator.transform.position + dungeonWorldSize * 0.5f;
            Handles.DrawWireCube(center, dungeonWorldSize);
            
            Handles.Label(center + Vector3.up * 5f, 
                $"Dungeon Area\n{generator.DungeonSize.x} x {generator.DungeonSize.y}\n{generator.GetStats().totalBlocks} blocks");
        }
    }
}