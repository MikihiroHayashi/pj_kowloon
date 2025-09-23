using System;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Characters;
using KowloonBreak.Core;

namespace KowloonBreak.UI
{
    public class InfectedInteractionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject interactionPanel;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Button vaccineButton;
        [SerializeField] private Button amputateButton;
        [SerializeField] private Button carryButton;
        [SerializeField] private Button cancelButton;

        [Header("Button Texts")]
        [SerializeField] private Text vaccineButtonText;
        [SerializeField] private Text amputateButtonText;
        [SerializeField] private Text carryButtonText;

        private CompanionCharacter currentInfectedCharacter;
        private bool isUIActive = false;

        public event Action<CompanionCharacter, InfectionTreatmentType> OnTreatmentSelected;
        public event Action<CompanionCharacter> OnCarrySelected;

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            SetupButtonListeners();
            HideUI();
        }

        private void InitializeUI()
        {
            if (interactionPanel == null)
                Debug.LogError("Interaction Panel is not assigned!");

            if (vaccineButtonText != null)
                vaccineButtonText.text = "ワクチンを打つ";

            if (amputateButtonText != null)
                amputateButtonText.text = "腕を切断する";

            if (carryButtonText != null)
                carryButtonText.text = "背負う";
        }

        private void SetupButtonListeners()
        {
            if (vaccineButton != null)
            {
                vaccineButton.onClick.AddListener(() => SelectTreatment(InfectionTreatmentType.Vaccine));
            }

            if (amputateButton != null)
            {
                amputateButton.onClick.AddListener(() => SelectTreatment(InfectionTreatmentType.Amputation));
            }

            if (carryButton != null)
            {
                carryButton.onClick.AddListener(SelectCarry);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HideUI);
            }
        }

        public void ShowInteractionUI(CompanionCharacter infectedCharacter)
        {
            if (infectedCharacter == null || !infectedCharacter.Infection.IsInfected)
            {
                Debug.LogWarning("Cannot show interaction UI for non-infected character");
                return;
            }

            currentInfectedCharacter = infectedCharacter;
            UpdateUI();
            interactionPanel.SetActive(true);
            isUIActive = true;

            // カーソルを有効にしてUIを操作できるようにする
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"Showing infection interaction UI for {infectedCharacter.Name}");
        }

        public void HideUI()
        {
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }

            isUIActive = false;
            currentInfectedCharacter = null;

            // カーソルをゲームモードに戻す
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Hiding infection interaction UI");
        }

        private void UpdateUI()
        {
            if (currentInfectedCharacter == null) return;

            // キャラクター名を表示
            if (characterNameText != null)
            {
                characterNameText.text = currentInfectedCharacter.Name + " (感染状態)";
            }

            // 各ボタンの有効/無効を設定
            UpdateButtonAvailability();
        }

        private void UpdateButtonAvailability()
        {
            var inventoryManager = Managers.EnhancedResourceManager.Instance;
            if (inventoryManager == null)
            {
                Debug.LogWarning("ResourceManager not found - cannot check item availability");
                return;
            }

            // ワクチンボタンの有効性
            bool hasVaccine = inventoryManager.HasEnoughResources(ResourceType.Medicine, 1);
            if (vaccineButton != null)
            {
                vaccineButton.interactable = hasVaccine;
                if (vaccineButtonText != null)
                {
                    vaccineButtonText.text = hasVaccine ? "ワクチンを打つ" : "ワクチンを打つ (ワクチンなし)";
                }
            }

            // 腕切断ボタンの有効性（のこぎり、包帯、酒が必要）
            bool hasSaw = inventoryManager.HasEnoughResources(ResourceType.Materials, 1); // のこぎりを材料として扱う
            bool hasBandage = inventoryManager.HasEnoughResources(ResourceType.Medicine, 1); // 包帯を薬として扱う
            bool hasAlcohol = inventoryManager.HasEnoughResources(ResourceType.Food, 1); // 酒を食料として扱う
            bool canAmputate = hasSaw && hasBandage && hasAlcohol;

            if (amputateButton != null)
            {
                amputateButton.interactable = canAmputate;
                if (amputateButtonText != null)
                {
                    if (canAmputate)
                    {
                        amputateButtonText.text = "腕を切断する";
                    }
                    else
                    {
                        amputateButtonText.text = "腕を切断する (道具不足)";
                    }
                }
            }

            // 背負うボタンは常に有効
            if (carryButton != null)
            {
                carryButton.interactable = true;
            }
        }

        private void SelectTreatment(InfectionTreatmentType treatmentType)
        {
            if (currentInfectedCharacter == null) return;

            OnTreatmentSelected?.Invoke(currentInfectedCharacter, treatmentType);
            HideUI();
        }

        private void SelectCarry()
        {
            if (currentInfectedCharacter == null) return;

            OnCarrySelected?.Invoke(currentInfectedCharacter);
            HideUI();
        }

        private void Update()
        {
            // ESCキーでUIを閉じる
            if (isUIActive && Input.GetKeyDown(KeyCode.Escape))
            {
                HideUI();
            }
        }

        private void OnDestroy()
        {
            // イベントリスナーをクリーンアップ
            if (vaccineButton != null)
                vaccineButton.onClick.RemoveAllListeners();

            if (amputateButton != null)
                amputateButton.onClick.RemoveAllListeners();

            if (carryButton != null)
                carryButton.onClick.RemoveAllListeners();

            if (cancelButton != null)
                cancelButton.onClick.RemoveAllListeners();
        }
    }

    public enum InfectionTreatmentType
    {
        Vaccine,
        Amputation
    }
}