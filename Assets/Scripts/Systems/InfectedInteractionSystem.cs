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

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask characterLayerMask = -1;

        [Header("UI References")]
        [SerializeField] private InfectedInteractionUI interactionUI;

        private Transform playerTransform;
        private List<CompanionCharacter> nearbyInfectedCharacters = new List<CompanionCharacter>();
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
            // プレイヤーを見つける
            FindPlayer();

            // UIが設定されていない場合は検索
            if (interactionUI == null)
            {
                interactionUI = FindObjectOfType<InfectedInteractionUI>();
            }

            // UIイベントをバインド
            if (interactionUI != null)
            {
                interactionUI.OnTreatmentSelected += HandleTreatmentSelected;
                interactionUI.OnCarrySelected += HandleCarrySelected;
            }
        }

        private void FindPlayer()
        {
            // プレイヤーを検索
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                // 代替検索
                var playerController = FindObjectOfType<Player.EnhancedPlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                }
            }
        }

        private void Update()
        {
            if (playerTransform == null) return;

            UpdateNearbyInfectedCharacters();
            HandleInteractionInput();
        }

        private void UpdateNearbyInfectedCharacters()
        {
            nearbyInfectedCharacters.Clear();

            // 周囲のキャラクターを検索
            CompanionCharacter[] allCharacters = FindObjectsOfType<CompanionCharacter>();

            foreach (var character in allCharacters)
            {
                if (character.Infection.IsInfected)
                {
                    float distance = Vector3.Distance(playerTransform.position, character.transform.position);
                    if (distance <= interactionRange)
                    {
                        nearbyInfectedCharacters.Add(character);
                    }
                }
            }
        }

        private void HandleInteractionInput()
        {
            // Eキーでインタラクション
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (nearbyInfectedCharacters.Count > 0)
                {
                    // 最も近い感染者を選択
                    CompanionCharacter closestCharacter = GetClosestInfectedCharacter();
                    if (closestCharacter != null)
                    {
                        ShowInteractionUI(closestCharacter);
                    }
                }
            }
        }

        private CompanionCharacter GetClosestInfectedCharacter()
        {
            if (nearbyInfectedCharacters.Count == 0) return null;

            CompanionCharacter closest = null;
            float closestDistance = float.MaxValue;

            foreach (var character in nearbyInfectedCharacters)
            {
                float distance = Vector3.Distance(playerTransform.position, character.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = character;
                }
            }

            return closest;
        }

        private void ShowInteractionUI(CompanionCharacter character)
        {
            if (interactionUI != null)
            {
                selectedCharacter = character;
                interactionUI.ShowInteractionUI(character);
            }
            else
            {
                Debug.LogError("InfectedInteractionUI not found!");
            }
        }

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

        public bool HasNearbyInfectedCharacters()
        {
            return nearbyInfectedCharacters.Count > 0;
        }

        public List<CompanionCharacter> GetNearbyInfectedCharacters()
        {
            return new List<CompanionCharacter>(nearbyInfectedCharacters);
        }

        private void OnDestroy()
        {
            if (interactionUI != null)
            {
                interactionUI.OnTreatmentSelected -= HandleTreatmentSelected;
                interactionUI.OnCarrySelected -= HandleCarrySelected;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(playerTransform.position, interactionRange);
            }
        }
    }
}