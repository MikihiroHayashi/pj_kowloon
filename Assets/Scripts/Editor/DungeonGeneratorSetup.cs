using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    public class DungeonGeneratorSetup
    {
        [MenuItem("Kowloon Break/Setup Dungeon Generator")]
        public static void SetupDungeonGeneratorInScene()
        {
            // 既存のDungeonGeneratorを検索
            DungeonGenerator existingGenerator = Object.FindObjectOfType<DungeonGenerator>();
            
            if (existingGenerator != null)
            {
                bool replace = EditorUtility.DisplayDialog("DungeonGenerator Found", 
                    "DungeonGenerator already exists in the scene. Replace it?", 
                    "Yes", "No");
                    
                if (replace)
                {
                    Object.DestroyImmediate(existingGenerator.gameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("Setup Cancelled", 
                        "DungeonGenerator setup cancelled.", "OK");
                    return;
                }
            }

            // 新しいDungeonGeneratorを作成
            GameObject generatorObject = new GameObject("Dungeon Generator");
            DungeonGenerator generator = generatorObject.AddComponent<DungeonGenerator>();

            // 親オブジェクトを作成
            GameObject dungeonParent = new GameObject("Generated Dungeon");
            
            // DungeonGeneratorに親オブジェクトを設定
            var serializedObject = new SerializedObject(generator);
            var dungeonParentProperty = serializedObject.FindProperty("dungeonParent");
            dungeonParentProperty.objectReferenceValue = dungeonParent.transform;
            serializedObject.ApplyModifiedProperties();

            // 選択状態にする
            Selection.activeGameObject = generatorObject;
            EditorGUIUtility.PingObject(generatorObject);

            EditorUtility.DisplayDialog("Setup Complete", 
                "DungeonGenerator has been added to the scene!\n\n" +
                "Next steps:\n" +
                "1. Assign Road Prefab Set in the inspector\n" +
                "2. Configure NavMesh settings if needed\n" +
                "3. Use the Dungeon Editor to create layouts", "OK");

            Debug.Log("DungeonGenerator setup completed in scene");
        }

        [MenuItem("Kowloon Break/Quick Setup (Full)")]
        public static void QuickSetupAll()
        {
            // 1. デフォルトアセット作成
            DungeonAssetCreationMenu.SetupDefaultAssets();
            
            // 2. DungeonGenerator設置
            SetupDungeonGeneratorInScene();
            
            // 3. Road Prefab Setを自動設定
            AutoConfigureRoadPrefabs();

            EditorUtility.DisplayDialog("Quick Setup Complete", 
                "Complete dungeon system setup finished!\n\n" +
                "You can now:\n" +
                "• Open Dungeon Editor\n" +
                "• Place pieces and road start points\n" +
                "• Generate roads and 3D dungeons", "OK");
        }

        private static void AutoConfigureRoadPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:DungeonRoadPrefabSet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                DungeonRoadPrefabSet roadSet = AssetDatabase.LoadAssetAtPath<DungeonRoadPrefabSet>(path);
                
                if (roadSet != null)
                {
                    roadSet.AutoConfigureFromExistingPrefabs();
                    EditorUtility.SetDirty(roadSet);
                    AssetDatabase.SaveAssets();
                    
                    // DungeonGeneratorに設定
                    DungeonGenerator generator = Object.FindObjectOfType<DungeonGenerator>();
                    if (generator != null)
                    {
                        var serializedObject = new SerializedObject(generator);
                        var roadPrefabsProperty = serializedObject.FindProperty("roadPrefabs");
                        roadPrefabsProperty.objectReferenceValue = roadSet;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        [MenuItem("Kowloon Break/Setup Dungeon Generator", true)]
        public static bool ValidateSetupDungeonGenerator()
        {
            // シーンが開いている場合のみメニューを有効化
            return !EditorApplication.isPlaying;
        }

        [MenuItem("Kowloon Break/Quick Setup (Full)", true)]
        public static bool ValidateQuickSetup()
        {
            return !EditorApplication.isPlaying;
        }
    }
}