using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Player;

namespace KowloonBreak.Managers
{
    public class ConsumableManager : MonoBehaviour
    {
        public static ConsumableManager Instance { get; private set; }

        private EnhancedPlayerController playerController;
        private EnhancedResourceManager resourceManager;

        // Per-item cooldown tracking (Time.time when cooldown ends)
        private readonly System.Collections.Generic.Dictionary<ItemData, float> cooldownUntil = new System.Collections.Generic.Dictionary<ItemData, float>();

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
            playerController = FindObjectOfType<EnhancedPlayerController>();
            resourceManager = EnhancedResourceManager.Instance;

            if (playerController == null)
            {
                Debug.LogWarning("[ConsumableManager] EnhancedPlayerControllerが見つかりません");
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

            if (playerController == null)
            {
                Debug.LogError("[ConsumableManager] EnhancedPlayerControllerが利用できません");
                return false;
            }

            // Cooldown check
            if (IsOnCooldown(itemData))
            {
                Debug.Log($"[ConsumableManager] {itemData.itemName} はクールダウン中です。残り {GetRemainingCooldown(itemData):F1}s");
                return false;
            }

            return ApplyConsumableEffects(itemData);
        }

        private bool ApplyConsumableEffects(ItemData itemData)
        {
            var effect = itemData.consumableEffect;
            bool effectApplied = false;

            // Early handling: vaccine (full cure) should reset infection state completely
            if (effect.HasInfectionEffect && effect.effectType == ConsumableType.InfectionCure)
            {
                if (playerController.IsInfected || playerController.InfectionLevel > 0)
                {
                    playerController.SetInfectionStatus(false);
                    playerController.TriggerUseItemAnimation();
                    Debug.Log("[ConsumableManager] Vaccine used: infection fully cured");

                    // Apply cooldown and message then return early to avoid partial-treat block below
                    if (itemData.cooldownSeconds > 0f)
                    {
                        cooldownUntil[itemData] = Time.time + itemData.cooldownSeconds;
                    }
                    if (!string.IsNullOrEmpty(effect.useMessage))
                    {
                        Debug.Log($"[ConsumableManager] {effect.useMessage}");
                        if (KowloonBreak.UI.UIManager.Instance != null)
                        {
                            KowloonBreak.UI.UIManager.Instance.ShowNotification(effect.useMessage, KowloonBreak.UI.NotificationType.Success);
                        }
                    }
                    return true;
                }
            }

            // 体力回復効果
            if (effect.HasHealthEffect)
            {
                float currentHealth = playerController.Health;
                float maxHealth = playerController.MaxHealth;

                if (currentHealth < maxHealth)
                {
                    playerController.Heal(effect.healthRestore);
                    // 回復アニメーション
                    playerController.TriggerHealAnimation();
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
                if (playerController.IsInfected || playerController.InfectionLevel > 0)
                {
                    playerController.TreatInfection(effect.infectionTreatment);
                    // 使用アニメーション（回復系と同一で良ければTriggerHealAnimationでも可）
                    playerController.TriggerUseItemAnimation();
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
                float currentStamina = playerController.CurrentStamina;
                float maxStamina = playerController.MaxStamina;

                if (currentStamina < maxStamina)
                {
                    // EnhancedPlayerControllerに回復APIを委譲
                    playerController.RegenerateStamina(effect.staminaRestore);
                    // 使用アニメーション
                    playerController.TriggerUseItemAnimation();
                    Debug.Log($"[ConsumableManager] スタミナを{effect.staminaRestore}回復しました");
                    effectApplied = true;
                }
                else
                {
                    Debug.Log("[ConsumableManager] スタミナは既に満タンです");
                }
            }

            if (effectApplied)
            {
                // Set cooldown if configured
                if (itemData.cooldownSeconds > 0f)
                {
                    cooldownUntil[itemData] = Time.time + itemData.cooldownSeconds;
                }

                // 使用メッセージを表示
                if (!string.IsNullOrEmpty(effect.useMessage))
                {
                    Debug.Log($"[ConsumableManager] {effect.useMessage}");
                    if (KowloonBreak.UI.UIManager.Instance != null)
                    {
                        KowloonBreak.UI.UIManager.Instance.ShowNotification(effect.useMessage, KowloonBreak.UI.NotificationType.Success);
                    }
                }
            }

            return effectApplied;
        }

        public bool CanUseConsumableItem(ItemData itemData)
        {
            if (itemData == null || !itemData.IsConsumable() || playerController == null)
                return false;

            var effect = itemData.consumableEffect;
            if (effect == null || !effect.HasAnyEffect)
                return false;

            // Cooldown gate
            if (IsOnCooldown(itemData))
                return false;

            // 体力回復アイテムの場合、体力が満タンでないか確認
            if (effect.HasHealthEffect && playerController.Health < playerController.MaxHealth)
                return true;

            // 感染治療アイテムの場合、感染しているか確認
            if (effect.HasInfectionEffect && (playerController.IsInfected || playerController.InfectionLevel > 0))
                return true;

            // スタミナ回復アイテムの場合、スタミナが満タンでないか確認
            if (effect.HasStaminaEffect && playerController.CurrentStamina < playerController.MaxStamina)
                return true;

            return false;
        }

        public bool IsOnCooldown(ItemData itemData)
        {
            if (itemData == null || itemData.cooldownSeconds <= 0f) return false;
            if (!cooldownUntil.TryGetValue(itemData, out var until)) return false;
            return Time.time < until;
        }

        public float GetRemainingCooldown(ItemData itemData)
        {
            if (itemData == null || itemData.cooldownSeconds <= 0f) return 0f;
            if (!cooldownUntil.TryGetValue(itemData, out var until)) return 0f;
            return Mathf.Max(0f, until - Time.time);
        }
    }
}
