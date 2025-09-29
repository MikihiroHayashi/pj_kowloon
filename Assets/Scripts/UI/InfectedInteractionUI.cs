using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KowloonBreak.Characters;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class InfectedInteractionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject interactionPanel;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Button vaccineButton;
        [SerializeField] private Button amputateButton;
        [SerializeField] private Button carryButton;
        [SerializeField] private Button cancelButton;

        [Header("Button Texts")]
        [SerializeField] private TextMeshProUGUI vaccineButtonText;
        [SerializeField] private TextMeshProUGUI amputateButtonText;
        [SerializeField] private TextMeshProUGUI carryButtonText;

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

            // 感染者専用カメラに切り替え
            SwitchToInfectedCamera();

            // 最初のボタンにフォーカスを設定
            SetDefaultFocus();

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

            // 元のカメラに戻す
            ReturnToPreviousCamera();

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

        private void SetDefaultFocus()
        {
            // 利用可能な最初のボタンにフォーカスを設定
            Button firstAvailableButton = null;

            if (vaccineButton != null && vaccineButton.interactable)
                firstAvailableButton = vaccineButton;
            else if (amputateButton != null && amputateButton.interactable)
                firstAvailableButton = amputateButton;
            else if (carryButton != null && carryButton.interactable)
                firstAvailableButton = carryButton;
            else if (cancelButton != null && cancelButton.interactable)
                firstAvailableButton = cancelButton;

            if (firstAvailableButton != null)
            {
                EventSystem.current.SetSelectedGameObject(firstAvailableButton.gameObject);
            }
        }

        private void Update()
        {
            // UIがアクティブな間、選択されたオブジェクトがない場合は再設定
            if (isUIActive && EventSystem.current.currentSelectedGameObject == null)
            {
                SetDefaultFocus();
            }
        }

        private void SwitchToInfectedCamera()
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                Debug.LogWarning("[InfectedInteractionUI] CameraManager not found - cannot switch to infected camera");
                return;
            }

            if (currentInfectedCharacter != null)
            {
                // 感染者の位置をターゲットとして感染者カメラに切り替え
                cameraManager.SwitchToInfectedCamera(currentInfectedCharacter.transform);
            }
            else
            {
                // 感染者キャラクターがない場合は通常の感染者カメラに切り替え
                cameraManager.SwitchToCamera(Managers.GameCameraType.Infected);
            }
        }

        private void ReturnToPreviousCamera()
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager == null)
            {
                Debug.LogWarning("[InfectedInteractionUI] CameraManager not found - cannot return to previous camera");
                return;
            }

            cameraManager.ReturnToPreviousCamera();
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