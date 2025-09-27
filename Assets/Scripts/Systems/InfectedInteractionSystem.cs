using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Characters;
using KowloonBreak.UI;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Systems
{
    public class InfectedInteractionSystem : MonoBehaviour
    {
        public static InfectedInteractionSystem Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private InfectedInteractionUI interactionUI;

        private CompanionCharacter selectedCharacter;

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
            InitializeSystem();
        }

        private void InitializeSystem()
        {
            // UIが設定されていない場合はUIManagerから取得
            if (interactionUI == null)
            {
                if (UI.UIManager.Instance != null)
                {
                    interactionUI = UI.UIManager.Instance.InfectedInteractionUI;
                }

                // UIManagerから取得できない場合は従来の検索
                if (interactionUI == null)
                {
                    interactionUI = FindObjectOfType<InfectedInteractionUI>();
                }
            }

            // UIイベントをバインド
            if (interactionUI != null)
            {
                interactionUI.OnTreatmentSelected += HandleTreatmentSelected;
                interactionUI.OnCarrySelected += HandleCarrySelected;
            }
        }

        // Note: インタラクション処理はUIManagerで統合管理

        private void HandleTreatmentSelected(CompanionCharacter character, InfectionTreatmentType treatmentType)
        {
            switch (treatmentType)
            {
                case InfectionTreatmentType.Vaccine:
                    ApplyVaccineTreatment(character);
                    break;
                case InfectionTreatmentType.Amputation:
                    ApplyAmputationTreatment(character);
                    break;
            }
        }

        private void ApplyVaccineTreatment(CompanionCharacter character)
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager != null && resourceManager.ConsumeResources(ResourceType.Medicine, 1))
            {
                character.Infection.CureWithVaccine();
                Debug.Log($"Applied vaccine treatment to {character.Name}");

                // アニメーションリセット
                ResetCharacterAnimation(character);

                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification($"{character.Name}をワクチンで治療しました", UI.NotificationType.Success);
                }
            }
            else
            {
                Debug.LogWarning("Not enough medicine for vaccine treatment");
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification("ワクチンが不足しています", UI.NotificationType.Warning);
                }
            }
        }

        private void ApplyAmputationTreatment(CompanionCharacter character)
        {
            var resourceManager = EnhancedResourceManager.Instance;

            // 必要なアイテムを消費（のこぎり、包帯、酒）
            bool hasAllItems = resourceManager.ConsumeResources(ResourceType.Materials, 1) &&
                              resourceManager.ConsumeResources(ResourceType.Medicine, 1) &&
                              resourceManager.ConsumeResources(ResourceType.Food, 1);

            if (hasAllItems)
            {
                character.Infection.CureWithAmputation();
                Debug.Log($"Applied amputation treatment to {character.Name}");

                // アニメーションリセット
                ResetCharacterAnimation(character);

                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification($"{character.Name}の腕を切断しました - 走れなくなります", UI.NotificationType.Warning);
                }
            }
            else
            {
                Debug.LogWarning("Not enough items for amputation treatment");
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification("必要な道具が不足しています", UI.NotificationType.Warning);
                }
            }
        }

        private void HandleCarrySelected(CompanionCharacter character)
        {
            // CarrySystemを使用して背負い開始
            var carrySystem = CarrySystem.Instance;
            if (carrySystem == null)
            {
                carrySystem = FindObjectOfType<CarrySystem>();
            }

            if (carrySystem != null)
            {
                bool success = carrySystem.StartCarrying(character);
                if (!success)
                {
                    Debug.LogWarning($"Failed to start carrying {character.Name}");
                    if (UI.UIManager.Instance != null)
                    {
                        UI.UIManager.Instance.ShowNotification("背負うことができませんでした", UI.NotificationType.Warning);
                    }
                }
            }
            else
            {
                Debug.LogError("CarrySystem not found!");
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification("背負いシステムが見つかりません", UI.NotificationType.Warning);
                }
            }
        }

        private void ResetCharacterAnimation(CompanionCharacter character)
        {
            // 感染アニメーションをリセットして通常状態に戻す
            var animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger("Infection");
                animator.SetTrigger("Reset");
            }
        }

        private void OnDestroy()
        {
            if (interactionUI != null)
            {
                interactionUI.OnTreatmentSelected -= HandleTreatmentSelected;
                interactionUI.OnCarrySelected -= HandleCarrySelected;
            }
        }

    }
}