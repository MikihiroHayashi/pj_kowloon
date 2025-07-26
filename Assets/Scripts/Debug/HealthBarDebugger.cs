using UnityEngine;
using KowloonBreak.Player;

namespace KowloonBreak.Debugging
{
    public class HealthBarDebugger : MonoBehaviour
    {
        private PlayerStats playerStats;
        
        private void Start()
        {
            playerStats = FindObjectOfType<PlayerStats>();
            Debug.Log($"[HealthBarDebugger] PlayerStats found: {playerStats != null}");
        }
        
        private void Update()
        {
            // Xキーでダメージ
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (playerStats != null)
                {
                    Debug.Log("[HealthBarDebugger] Manual damage test - pressing X");
                    playerStats.TakeDamage(10f);
                }
            }
            
            // Hキーで回復
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (playerStats != null)
                {
                    Debug.Log("[HealthBarDebugger] Manual heal test - pressing H");
                    playerStats.Heal(10f);
                }
            }
        }
        
        private void OnGUI()
        {
            if (playerStats == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Health: {playerStats.Health:F1}/{playerStats.MaxHealth:F1}");
            GUILayout.Label($"Health %: {playerStats.HealthPercentage:F2}");
            GUILayout.Label($"Is Alive: {playerStats.IsAlive}");
            
            if (GUILayout.Button("Damage -10"))
            {
                Debug.Log("[HealthBarDebugger] Manual damage button clicked");
                playerStats.TakeDamage(10f);
            }
            
            if (GUILayout.Button("Heal +10"))
            {
                Debug.Log("[HealthBarDebugger] Manual heal button clicked");
                playerStats.Heal(10f);
            }
            
            GUILayout.EndArea();
        }
    }
}