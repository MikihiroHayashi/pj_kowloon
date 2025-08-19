using UnityEngine;
using UnityEditor;
using KowloonBreak.Environment;
using System.Collections.Generic;

namespace KowloonBreak.Editor
{
    /// <summary>
    /// Configurationアセットの検証用エディタースクリプト
    /// </summary>
    public class ConfigurationAssetVerifier : EditorWindow
    {
        [MenuItem("Kowloon Break/Verify Configuration Assets")]
        public static void ShowWindow()
        {
            GetWindow<ConfigurationAssetVerifier>("Configuration Verifier");
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Configuration Asset Verification", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Verify All Configuration Assets"))
            {
                VerifyConfigurationAssets();
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("List Asset Database Results"))
            {
                ListAssetDatabaseResults();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Test Default Configuration Creation"))
            {
                TestDefaultConfigurationCreation();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            // ここに結果表示エリアを設ける場合
            EditorGUILayout.EndScrollView();
        }

        private void VerifyConfigurationAssets()
        {
            Debug.Log("=== Configuration Asset Verification ===");

            // 直接パスでロードを試行
            string[] assetPaths = new string[]
            {
                "Assets/Configurations/BlockConfig_Road_2x2.asset",
                "Assets/Configurations/BlockConfig_Room_10x10.asset",
                "Assets/Configurations/BlockConfig_Room_10x5.asset",
                "Assets/Configurations/BlockConfig_Room_5x5.asset"
            };

            foreach (string path in assetPaths)
            {
                Debug.Log($"Checking asset at path: {path}");
                
                var asset = AssetDatabase.LoadAssetAtPath<DungeonBlockConfiguration>(path);
                if (asset != null)
                {
                    Debug.Log($"✓ Successfully loaded: {asset.name}");
                    Debug.Log($"  - Block Type: {asset.blockType}");
                    Debug.Log($"  - Size: {asset.size}");
                    Debug.Log($"  - Prefab: {(asset.prefab != null ? asset.prefab.name : "NULL")}");
                    Debug.Log($"  - Debug Color: {asset.debugColor}");
                }
                else
                {
                    Debug.LogError($"✗ Failed to load asset at: {path}");
                    
                    // アセットの存在確認
                    if (System.IO.File.Exists(path))
                    {
                        Debug.Log($"  - File exists on disk");
                    }
                    else
                    {
                        Debug.LogError($"  - File does not exist on disk");
                    }
                }
            }
        }

        private void ListAssetDatabaseResults()
        {
            Debug.Log("=== AssetDatabase Search Results ===");
            
            // GUIDで検索
            string[] guids = AssetDatabase.FindAssets("t:DungeonBlockConfiguration");
            Debug.Log($"Found {guids.Length} GUIDs for DungeonBlockConfiguration");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"GUID: {guid} -> Path: {path}");
                
                var asset = AssetDatabase.LoadAssetAtPath<DungeonBlockConfiguration>(path);
                if (asset != null)
                {
                    Debug.Log($"  ✓ Asset loaded: {asset.name} ({asset.blockType} {asset.size})");
                }
                else
                {
                    Debug.LogError($"  ✗ Failed to load asset at path: {path}");
                }
            }
            
            // 名前で検索も試行
            Debug.Log("--- Searching by name patterns ---");
            string[] namePatterns = { "BlockConfig", "Room", "Road" };
            
            foreach (string pattern in namePatterns)
            {
                string[] nameGuids = AssetDatabase.FindAssets(pattern);
                Debug.Log($"Pattern '{pattern}' found {nameGuids.Length} results");
                
                foreach (string guid in nameGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Configuration"))
                    {
                        Debug.Log($"  Relevant: {path}");
                    }
                }
            }
        }
        
        private void TestDefaultConfigurationCreation()
        {
            Debug.Log("=== Testing Default Configuration Creation ===");
            
            try
            {
                var defaultConfigs = DungeonBlockFactory.GetDefaultConfigurations();
                Debug.Log($"Successfully created {defaultConfigs.Length} default configurations");
                
                for (int i = 0; i < defaultConfigs.Length; i++)
                {
                    var config = defaultConfigs[i];
                    Debug.Log($"Default[{i}]: {config.blockType} {config.size} - Weight: {config.spawnWeight}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create default configurations: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}