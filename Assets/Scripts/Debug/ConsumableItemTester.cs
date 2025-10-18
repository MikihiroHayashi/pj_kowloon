using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Debugging
{
    public class ConsumableItemTester : MonoBehaviour
    {
        [Header("デバッグ用ダメージ")]
        [SerializeField] private KeyCode damageKey = KeyCode.H;
        [SerializeField] private KeyCode infectKey = KeyCode.I;
        [SerializeField] private float damageAmount = 25f;

        private EnhancedResourceManager resourceManager;

        private void Start()
        {
            resourceManager = EnhancedResourceManager.Instance;

            if (resourceManager == null)
            {
                Debug.LogError("[ConsumableItemTester] EnhancedResourceManagerが見つかりません");
                enabled = false;
            }
        }

        private void Update()
        {
            if (resourceManager == null) return;

            // デバッグ用ダメージのみ残す
            if (Input.GetKeyDown(damageKey))
            {
                ApplyTestDamage();
            }

            if (Input.GetKeyDown(infectKey))
            {
                ApplyTestInfection();
            }
        }


        private void ApplyTestDamage()
        {
            var player = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
                Debug.Log($"[ConsumableItemTester] テスト用ダメージ {damageAmount} を与えました");
            }
        }

        private void ApplyTestInfection()
        {
            var player = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
            if (player != null)
            {
                player.SetInfectionStatus(true);
                Debug.Log("[ConsumableItemTester] テスト用感染を適用しました");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== 回復アイテム情報 ===");
            GUILayout.Label($"ワクチン: {GetItemCount("ワクチン")} 個");
            GUILayout.Label($"缶詰: {GetItemCount("缶詰")} 個");
            GUILayout.Label($"包帯: {GetItemCount("包帯")} 個");
            GUILayout.Label("");
            GUILayout.Label("=== デバッグ ===");
            GUILayout.Label($"Hキー: ダメージ ({damageAmount})");
            GUILayout.Label("Iキー: 感染付与");

            // インベントリ経由での使用を促すメッセージ
            GUILayout.Label("");
            GUILayout.Label("※ アイテム使用はインベントリから行ってください");

            // プレイヤー状態表示
            var player = FindObjectOfType<KowloonBreak.Player.EnhancedPlayerController>();
            if (player != null)
            {
                GUILayout.Label("");
                GUILayout.Label($"体力: {player.Health:F1}/{player.MaxHealth}");
                GUILayout.Label($"感染: {(player.IsInfected ? "感染中" : "健康")} ({player.InfectionLevel:F1}%)");
            }
            GUILayout.EndArea();
        }

        private int GetItemCount(string itemName)
        {
            if (resourceManager == null) return 0;
            return resourceManager.GetItemCount(itemName);
        }
    }
}
