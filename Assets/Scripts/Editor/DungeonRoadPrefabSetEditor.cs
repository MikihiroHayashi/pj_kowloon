using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(DungeonRoadPrefabSet))]
    public class DungeonRoadPrefabSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DungeonRoadPrefabSet roadSet = (DungeonRoadPrefabSet)target;

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Auto Configure from Existing Prefabs", GUILayout.Height(30)))
            {
                roadSet.AutoConfigureFromExistingPrefabs();
                EditorUtility.DisplayDialog("Auto Configuration", 
                    "Attempted to automatically assign road prefabs based on naming conventions.\n\n" +
                    "Please verify the assignments below.", "OK");
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Auto Configure will search for prefabs with 'Dungeon_Road' in their name and assign them based on naming patterns:\n\n" +
                "• 'horizontal' → Straight Horizontal\n" +
                "• 'vertical' → Straight Vertical\n" +
                "• 'cross' → Cross\n" +
                "• 'cornerNE', 'cornerNW', etc. → Corner variants\n" +
                "• 'tJunctionN', 'tJunctionE', etc. → T-Junction variants\n" +
                "• 'endCapN', 'endCapE', etc. → End Cap variants",
                MessageType.Info);

            EditorGUILayout.Space();

            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (!roadSet.IsConfigured())
            {
                EditorGUILayout.HelpBox(
                    "Road Prefab Set is not fully configured! Please assign at least the basic road prefabs (Straight Horizontal, Straight Vertical, and Cross).",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Road Prefab Set is properly configured!", MessageType.Info);
            }
        }
    }
}