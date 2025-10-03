using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Player;

namespace KowloonBreak.Managers
{
    public class ConsumableManager : MonoBehaviour
    {
        public static ConsumableManager Instance { get; private set; }

        private PlayerStats playerStats;
        private EnhancedResourceManager resourceManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            playerStats = FindObjectOfType<PlayerStats>();
            resourceManager = EnhancedResourceManager.Instance;

            if (playerStats == null)
            {
                Debug.LogWarning("[ConsumableManager] PlayerStatsが見つかりません");
            }

            if (resourceManager == null)
            {
                Debug.LogWarning("[ConsumableManager] EnhancedResourceManagerが見つかりません");
            }
        }

        public bool UseConsumableItem(ItemData itemData)
        {
            if (itemData == null || !itemData.IsConsumable())
            {
                Debug.LogWarning("[ConsumableManager] アイテムがnullまたは消耗品ではありません");
                return false;
            }

            if (itemData.consumableEffect == null || !itemData.consumableEffect.HasAnyEffect)
            {
                Debug.LogWarning("[ConsumableManager] 消耗品に効果が設定されていません");
                return false;
            }

            if (playerStats == null)
            {
                Debug.LogError("[ConsumableManager] PlayerStatsが利用できません");
                return false;
            }

            return ApplyConsumableEffects(itemData);
        }

        private bool ApplyConsumableEffects(ItemData itemData)
        {
            var effect = itemData.consumableEffect;
            bool effectApplied = false;

            // 体力回復効果
            if (effect.HasHealthEffect)
            {
                float currentHealth = playerStats.Health;
                float maxHealth = playerStats.MaxHealth;

                if (currentHealth < maxHealth)
                {
                    playerStats.Heal(effect.healthRestore);
                    Debug.Log($"[ConsumableManager] 体力を{effect.healthRestore}回復しました");
                    effectApplied = true;
                }
                else
                {
                    Debug.Log("[ConsumableManager] 体力は既に満タンです");
                }
            }

            // 感染治療効果
            if (effect.HasInfectionEffect)
            {
                if (playerStats.IsInfected || playerStats.InfectionLevel > 0)
                {
                    playerStats.TreatInfection(effect.infectionTreatment);
                    Debug.Log($"[ConsumableManager] 感染を{effect.infectionTreatment}治療しました");
                    effectApplied = true;
                }
                else
                {
                    Debug.Log("[ConsumableManager] 感染していません");
                }
            }

            // スタミナ回復効果
            if (effect.HasStaminaEffect)
            {
                float currentStamina = playerStats.Stamina;
                float maxStamina = playerStats.MaxStamina;

                if (currentStamina < maxStamina)
                {
                    playerStats.RegenerateStamina(effect.staminaRestore);
                    Debug.Log($"[ConsumableManager] スタミナを{effect.staminaRestore}回復しました");
                    effectApplied = true;
                }
                else
                {
                    Debug.Log("[ConsumableManager] スタミナは既に満タンです");
                }
            }

            // 使用メッセージを表示
            if (effectApplied && !string.IsNullOrEmpty(effect.useMessage))
            {
                Debug.Log($"[ConsumableManager] {effect.useMessage}");
                // TODO: UIに使用メッセージを表示
            }

            return effectApplied;
        }

        public bool CanUseConsumableItem(ItemData itemData)
        {
            if (itemData == null || !itemData.IsConsumable() || playerStats == null)
                return false;

            var effect = itemData.consumableEffect;
            if (effect == null || !effect.HasAnyEffect)
                return false;

            // 体力回復アイテムの場合、体力が満タンでないか確認
            if (effect.HasHealthEffect && playerStats.Health < playerStats.MaxHealth)
                return true;

            // 感染治療アイテムの場合、感染しているか確認
            if (effect.HasInfectionEffect && (playerStats.IsInfected || playerStats.InfectionLevel > 0))
                return true;

            // スタミナ回復アイテムの場合、スタミナが満タンでないか確認
            if (effect.HasStaminaEffect && playerStats.Stamina < playerStats.MaxStamina)
                return true;

            return false;
        }
    }
}