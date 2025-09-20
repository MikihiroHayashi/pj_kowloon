using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;

namespace KowloonBreak.Editor
{
    public class DungeonAssetCreationMenu
    {
        [MenuItem("Assets/Create/Kowloon Break/New Dungeon Piece Library")]
        public static void CreateDungeonPieceLibrary()
        {
            CreateAssetWithDialog<DungeonPieceLibrary>("New Dungeon Piece Library", "asset", (library) =>
            {
                library.AddCategory("Buildings", Color.blue);
                library.AddCategory("Roads", Color.yellow);
                library.AddCategory("Special", Color.green);
                library.AddCategory("Decorations", Color.magenta);
            });
        }

        [MenuItem("Assets/Create/Kowloon Break/New Road Prefab Set")]
        public static void CreateRoadPrefabSet()
        {
            CreateAssetWithDialog<DungeonRoadPrefabSet>("New Road Prefab Set", "asset", (roadSet) =>
            {
                EditorUtility.DisplayDialog("Road Prefab Set Created", 
                    "Don't forget to assign the road prefabs in the inspector!\n\n" +
                    "You can use the 'Auto Configure' button in the inspector to automatically " +
                    "assign prefabs based on naming conventions.", "OK");
            });
        }


        private static void CreateAssetWithDialog<T>(string defaultName, string extension, System.Action<T> setupAction = null) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanel($"Create {typeof(T).Name}", "Assets", defaultName, extension);
            if (!string.IsNullOrEmpty(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
                
                T asset = ScriptableObject.CreateInstance<T>();
                setupAction?.Invoke(asset);
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                
                Debug.Log($"Created {typeof(T).Name} at {path}");
            }
        }

        [MenuItem("Kowloon Break/Setup Default Assets")]
        public static void SetupDefaultAssets()
        {
            bool result = EditorUtility.DisplayDialog("Setup Default Assets",
                "This will create default Dungeon Piece Library and Road Prefab Set in the ScriptableObject folder.\n\n" +
                "Continue?", "Yes", "Cancel");
                
            if (!result) return;

            CreateDefaultDungeonPieceLibrary();
            CreateDefaultRoadPrefabSet();
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "Default assets have been created in Assets/ScriptableObject/\n\n" +
                "Don't forget to assign the actual prefabs in the Road Prefab Set!", "OK");
        }

        private static void CreateDefaultDungeonPieceLibrary()
        {
            string folderPath = "Assets/ScriptableObject";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObject");
            }

            string assetPath = $"{folderPath}/DefaultDungeonPieceLibrary.asset";
            
            var library = ScriptableObject.CreateInstance<DungeonPieceLibrary>();
            
            library.AddCategory("Buildings", new Color(0.3f, 0.5f, 0.9f));
            library.AddCategory("Roads", new Color(0.9f, 0.9f, 0.3f));
            library.AddCategory("Special", new Color(0.3f, 0.9f, 0.5f));
            library.AddCategory("Decorations", new Color(0.9f, 0.3f, 0.9f));

            // 既存のPrefabを検索して割り当て
            GameObject buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefab/Dungeon/Dungeon_5x5.prefab");
            GameObject largeBuildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefab/Dungeon/Dungeon_10x10.prefab");
            
            Debug.Log($"=== Prefab Assignment Check ===");
            Debug.Log($"buildingPrefab: {(buildingPrefab != null ? buildingPrefab.name : "NULL")}");
            Debug.Log($"largeBuildingPrefab: {(largeBuildingPrefab != null ? largeBuildingPrefab.name : "NULL")}");
            Debug.Log($"=== End Prefab Assignment Check ===");

            var buildingPieces = new[]
            {
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Small Building 1x1", 
                    type = PieceType.Building, 
                    size = new Vector2Int(1, 1),
                    prefab = buildingPrefab,
                    displayColor = new Color(0.7f, 0.7f, 0.9f),
                    compatibleLevels = new[] { LevelType.Residential, LevelType.Commercial }
                },
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Medium Building 2x2", 
                    type = PieceType.Building, 
                    size = new Vector2Int(2, 2),
                    prefab = largeBuildingPrefab,
                    displayColor = new Color(0.6f, 0.6f, 0.8f),
                    compatibleLevels = new[] { LevelType.Commercial, LevelType.Industrial }
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
                }
            };

            var roadPieces = new[]
            {
                new DungeonPieceTemplate 
                { 
                    id = System.Guid.NewGuid().ToString(),
                    name = "Road Start Point", 
                    type = PieceType.RoadStart, 
                    size = new Vector2Int(1, 1),
                    displayColor = new Color(0.9f, 0.9f, 0.3f),
                    isRoadStartPoint = true,
                    blocksPaths = false
                }
            };

            var specialPieces = new[]
            {
                new DungeonPieceTemplate
                {
                    id = System.Guid.NewGuid().ToString(),
                    name = "Player Spawn",
                    type = PieceType.SpawnPoint,
                    size = new Vector2Int(1, 1),
                    displayColor = new Color(0.3f, 0.9f, 0.3f),
                    blocksPaths = false
                },
                new DungeonPieceTemplate
                {
                    id = System.Guid.NewGuid().ToString(),
                    name = "Exit Point",
                    type = PieceType.ExitPoint,
                    size = new Vector2Int(1, 1),
                    displayColor = new Color(0.9f, 0.3f, 0.3f),
                    blocksPaths = false
                },
                new DungeonPieceTemplate
                {
                    id = System.Guid.NewGuid().ToString(),
                    name = "Blank Space",
                    type = PieceType.Blank,
                    size = new Vector2Int(1, 1),
                    displayColor = new Color(0.7f, 0.7f, 0.7f),
                    blocksPaths = false,
                    prefab = null // プレファブなし（空きマス用）
                }
            };

            foreach (var piece in buildingPieces)
            {
                library.AddPieceToCategory(0, piece);
            }
            foreach (var piece in roadPieces)
            {
                library.AddPieceToCategory(1, piece);
            }
            foreach (var piece in specialPieces)
            {
                library.AddPieceToCategory(2, piece);
            }

            AssetDatabase.CreateAsset(library, assetPath);
            Debug.Log($"Created Default Dungeon Piece Library at {assetPath}");
        }

        private static void CreateDefaultRoadPrefabSet()
        {
            string folderPath = "Assets/ScriptableObject";
            string assetPath = $"{folderPath}/DefaultRoadPrefabSet.asset";
            
            var roadSet = ScriptableObject.CreateInstance<DungeonRoadPrefabSet>();
            
            AssetDatabase.CreateAsset(roadSet, assetPath);
            Debug.Log($"Created Default Road Prefab Set at {assetPath}");
        }
    }
}