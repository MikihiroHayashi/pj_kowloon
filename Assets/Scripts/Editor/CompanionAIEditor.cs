using UnityEngine;
using UnityEditor;
using KowloonBreak.Characters;
using KowloonBreak.Core;

namespace KowloonBreak.Editor
{
    [CustomEditor(typeof(CompanionAI))]
    public class CompanionAIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクターを表示
            DrawDefaultInspector();

            // 区切り線
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialogue Test Controls", EditorStyles.boldLabel);

            CompanionAI companionAI = (CompanionAI)target;

            // ボタンレイアウト用のスタイル
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 30
            };

            EditorGUILayout.BeginVertical("box");

            // Debug Dialogue Data ボタン
            if (GUILayout.Button("Debug Dialogue Data", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    companionAI.DebugDialogueData();
                }
                else
                {
                    Debug.LogWarning("This function can only be used in Play Mode.");
                }
            }

            EditorGUILayout.Space(5);

            // Test Normal Dialogue ボタン
            if (GUILayout.Button("Test Normal Dialogue", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    companionAI.TestNormalDialogue();
                    Debug.Log("Normal dialogue test executed.");
                }
                else
                {
                    Debug.LogWarning("This function can only be used in Play Mode.");
                }
            }

            EditorGUILayout.Space(5);

            // Test Infection Dialogue ボタン
            if (GUILayout.Button("Test Infection Dialogue", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    companionAI.TestInfectionDialogue();
                    Debug.Log("Infection dialogue test executed.");
                }
                else
                {
                    Debug.LogWarning("This function can only be used in Play Mode.");
                }
            }

            EditorGUILayout.Space(5);

            // Test Force Infection and Dialogue ボタン
            if (GUILayout.Button("Test Force Infection and Dialogue", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    companionAI.TestForceInfectionAndDialogue();
                    Debug.Log("Force infection and dialogue test executed.");
                }
                else
                {
                    Debug.LogWarning("This function can only be used in Play Mode.");
                }
            }

            EditorGUILayout.EndVertical();

            // 注意事項
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("These test buttons only work in Play Mode. Make sure the scene is running before using them.", MessageType.Info);

            // Play Mode時のリアルタイム情報表示
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");

                // 感染状態情報
                var companionCharacter = companionAI.GetComponent<CompanionCharacter>();
                if (companionCharacter != null)
                {
                    EditorGUILayout.LabelField($"Infection Status: {companionCharacter.Infection.IsInfected}");
                    EditorGUILayout.LabelField($"Infection Level: {companionCharacter.Infection.CurrentInfection:F1}/{companionCharacter.Infection.MaxInfectionValue}");

                    if (companionCharacter.Infection.IsInfected)
                    {
                        EditorGUILayout.LabelField("Status: INFECTED", EditorStyles.boldLabel);
                    }
                }

                // ダイアログシステム状態
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Dialogue System Enabled: {GetPrivateField(companionAI, "enableDialogueSystem")}");

                var dialogueData = GetPrivateField(companionAI, "dialogueData");
                EditorGUILayout.LabelField($"Dialogue Data: {(dialogueData != null ? ((ScriptableObject)dialogueData).name : "None")}");

                EditorGUILayout.EndVertical();

                // 自動更新のためにRepaintを要求
                if (Event.current.type == EventType.Layout)
                {
                    Repaint();
                }
            }
        }

        /// <summary>
        /// プライベートフィールドの値を取得するヘルパーメソッド
        /// </summary>
        private object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj);
        }
    }
}