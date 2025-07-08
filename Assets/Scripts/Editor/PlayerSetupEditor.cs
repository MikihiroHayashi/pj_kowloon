using UnityEngine;
using UnityEditor;
using KowloonBreak.Player;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(PlayerSetup))]
    public class PlayerSetupEditor : UnityEditor.Editor
    {
        private PlayerSetup playerSetup;
        
        private void OnEnable()
        {
            playerSetup = (PlayerSetup)target;
        }
        
        public override void OnInspectorGUI()
        {
            // デフォルトのInspectorを表示
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Setup Controls", EditorStyles.boldLabel);
            
            // 現在のPlayerの状態を表示
            GameObject currentPlayer = playerSetup.GetPlayerObject();
            if (currentPlayer != null)
            {
                EditorGUILayout.HelpBox($"Player Object: {currentPlayer.name}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No Player object found", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // ボタンを横並びに配置
            EditorGUILayout.BeginHorizontal();
            
            // Player生成ボタン
            if (GUILayout.Button("Setup Player", GUILayout.Height(30)))
            {
                // Editorモードでの変更を記録
                Undo.RecordObject(playerSetup, "Setup Player");
                
                try
                {
                    playerSetup.SetupPlayer();
                    
                    // シーンを保存が必要な状態にマーク
                    EditorUtility.SetDirty(playerSetup);
                    
                    // シーンビューを更新
                    SceneView.RepaintAll();
                    
                    Debug.Log("Player setup completed in Editor mode!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to setup player: {e.Message}");
                }
            }
            
            // Player削除ボタン
            if (GUILayout.Button("Remove Player", GUILayout.Height(30)))
            {
                if (currentPlayer != null)
                {
                    if (EditorUtility.DisplayDialog("Remove Player", 
                        $"Are you sure you want to remove the player object '{currentPlayer.name}'?", 
                        "Yes", "No"))
                    {
                        Undo.RecordObject(playerSetup, "Remove Player");
                        
                        try
                        {
                            playerSetup.RemovePlayer();
                            
                            EditorUtility.SetDirty(playerSetup);
                            SceneView.RepaintAll();
                            
                            Debug.Log("Player removed from scene!");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Failed to remove player: {e.Message}");
                        }
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Remove Player", "No player object found to remove.", "OK");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 現在のPlayerの詳細情報
            if (currentPlayer != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Player Details", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField($"Position: {currentPlayer.transform.position}");
                EditorGUILayout.LabelField($"Rotation: {currentPlayer.transform.rotation.eulerAngles}");
                
                var controller = playerSetup.GetPlayerController();
                if (controller != null)
                {
                    EditorGUILayout.LabelField($"Controller: {controller.GetType().Name}");
                }
                
                var charController = currentPlayer.GetComponent<CharacterController>();
                if (charController != null)
                {
                    EditorGUILayout.LabelField($"Character Controller: Active");
                }
                
                EditorGUILayout.EndVertical();
            }
            
            // 警告メッセージ
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Player setup is available in Play Mode, but changes might not persist.", MessageType.Warning);
            }
        }
    }
}