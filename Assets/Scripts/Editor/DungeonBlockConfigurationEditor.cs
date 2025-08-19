using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonBlockConfiguration))]
    public class DungeonBlockConfigurationEditor : UnityEditor.Editor
    {
        private DungeonBlockConfiguration config;
        private bool showRoadSettings = true;
        
        private void OnEnable()
        {
            config = (DungeonBlockConfiguration)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dungeon Block Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawBasicSettings();
            EditorGUILayout.Space();
            
            DrawGenerationSettings();
            EditorGUILayout.Space();
            
            DrawVisualSettings();
            EditorGUILayout.Space();
            
            // 道路タイプの場合のみ道路設定を表示
            if (config.blockType == DungeonBlockType.Road)
            {
                DrawRoadSettings();
                EditorGUILayout.Space();
            }
            
            DrawValidationInfo();
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Block Definition", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"), 
                new GUIContent("Prefab", "Base prefab for non-road blocks"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockType"), 
                new GUIContent("Block Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("size"), 
                new GUIContent("Size", "Grid size in cells"));
        }
        
        private void DrawGenerationSettings()
        {
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnWeight"), 
                new GUIContent("Spawn Weight", "Probability weight for automatic generation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxInstances"), 
                new GUIContent("Max Instances", "-1 for unlimited"));
        }
        
        private void DrawVisualSettings()
        {
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugColor"), 
                new GUIContent("Debug Color", "Color for editor visualization"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultMaterial"), 
                new GUIContent("Default Material", "Material for generated blocks"));
        }
        
        private void DrawRoadSettings()
        {
            showRoadSettings = EditorGUILayout.Foldout(showRoadSettings, "Road Prefab Settings", true);
            
            if (showRoadSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Basic Roads", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalRoadPrefab"), 
                    new GUIContent("Horizontal Road"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalRoadPrefab"), 
                    new GUIContent("Vertical Road"));
                    
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Corners", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerNEPrefab"), 
                    new GUIContent("Corner NE (North-East)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerNWPrefab"), 
                    new GUIContent("Corner NW (North-West)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerSEPrefab"), 
                    new GUIContent("Corner SE (South-East)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerSWPrefab"), 
                    new GUIContent("Corner SW (South-West)"));
                    
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Junctions", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("crossPrefab"), 
                    new GUIContent("Cross Junction"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tJunctionNPrefab"), 
                    new GUIContent("T-Junction North"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tJunctionSPrefab"), 
                    new GUIContent("T-Junction South"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tJunctionEPrefab"), 
                    new GUIContent("T-Junction East"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tJunctionWPrefab"), 
                    new GUIContent("T-Junction West"));
                    
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("End Pieces", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("endCapPrefab"), 
                    new GUIContent("End Cap"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("singleRoadPrefab"), 
                    new GUIContent("Single Road"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawValidationInfo()
        {
            EditorGUILayout.LabelField("Configuration Status", EditorStyles.boldLabel);
            
            if (config.IsValid())
            {
                EditorGUILayout.HelpBox("Configuration is valid", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Configuration has issues - check size and spawn weight", MessageType.Warning);
            }
            
            if (config.blockType == DungeonBlockType.Road)
            {
                if (config.HasValidRoadConfiguration())
                {
                    EditorGUILayout.HelpBox("Road configuration is valid", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("No road prefabs assigned - at least one road prefab is recommended", MessageType.Warning);
                }
            }
            
            EditorGUILayout.LabelField($"Display Name: {config.GetDisplayName()}");
        }
    }
}