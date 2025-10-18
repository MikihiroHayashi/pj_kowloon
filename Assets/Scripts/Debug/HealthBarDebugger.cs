using UnityEngine;
using KowloonBreak.Player;

namespace KowloonBreak.Debugging
{
    public class HealthBarDebugger : MonoBehaviour
    {
        private EnhancedPlayerController player;
        
        private void Start()
        {
            player = FindObjectOfType<EnhancedPlayerController>();
            UnityEngine.Debug.Log($"[HealthBarDebugger] EnhancedPlayerController found: {player != null}");
        }
        
        private void Update()
        {
            // Xキーでダメージ
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (player != null)
                {
                    UnityEngine.Debug.Log("[HealthBarDebugger] Manual damage test - pressing X");
                    player.TakeDamage(10f);
                }
            }
            
            // Hキーで回復
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (player != null)
                {
                    UnityEngine.Debug.Log("[HealthBarDebugger] Manual heal test - pressing H");
                    player.Heal(10f);
                }
            }
        }
        
        private void OnGUI()
        {
            if (player == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Health: {player.Health:F1}/{player.MaxHealth:F1}");
            GUILayout.Label($"Health %: {player.HealthPercentage:F2}");
            GUILayout.Label($"Is Alive: {player.IsAlive}");
            
            if (GUILayout.Button("Damage -10"))
            {
                UnityEngine.Debug.Log("[HealthBarDebugger] Manual damage button clicked");
                player.TakeDamage(10f);
            }
            
            if (GUILayout.Button("Heal +10"))
            {
                UnityEngine.Debug.Log("[HealthBarDebugger] Manual heal button clicked");
                player.Heal(10f);
            }
            
            GUILayout.EndArea();
        }
    }
}
