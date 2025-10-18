using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class ToolSelectionHUDController : MonoBehaviour, IFocusableUI
    {
        [Header("UI References")]
        [SerializeField] private HorizontalLayoutGroup toolSlotsLayout;
        [SerializeField] private Transform toolSlotsParent;

        [Header("Settings")]
        [SerializeField] private int displayToolCount = 8;
        [SerializeField] private float slotSize = 64f;
        [SerializeField] private float spacing = 8f;

        [Header("Visual Settings")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color emptyColor = Color.gray;

        private List<ItemSlotUI> toolSlots = new List<ItemSlotUI>();
        private EnhancedResourceManager resourceManager;
        private int selectedToolIndex = 0;
        private bool isInputEnabled = true; // input enable flag

        [Header("Input Handling")]
        [Tooltip("If true, HUD handles prev/next inputs; otherwise player handles.")]
        [SerializeField] private bool handlePrevNextInput = false;

        public int SelectedToolIndex => selectedToolIndex;
        public InventorySlot SelectedTool => resourceManager?.GetToolSlot(selectedToolIndex);

        public System.Action<int, InventorySlot> OnToolSelected;
        public System.Action<int, InventorySlot> OnToolUsed;

        // IFocusableUI
        public bool IsVisible => gameObject.activeInHierarchy;
        public int Priority => 5;
        public string UIName => "ToolSelectionHUD";

        private void Awake()
        {
            if (toolSlotsLayout == null)
                toolSlotsLayout = GetComponent<HorizontalLayoutGroup>();

            if (toolSlotsParent == null)
                toolSlotsParent = toolSlotsLayout != null ? toolSlotsLayout.transform : transform;

            if (toolSlotsLayout == null)
                toolSlotsLayout = gameObject.AddComponent<HorizontalLayoutGroup>();

            toolSlotsLayout.spacing = spacing;
            toolSlotsLayout.childAlignment = TextAnchor.MiddleCenter;
            toolSlotsLayout.childControlWidth = false;
            toolSlotsLayout.childControlHeight = false;
            toolSlotsLayout.childForceExpandWidth = false;
            toolSlotsLayout.childForceExpandHeight = false;
        }

        private void Start()
        {
            resourceManager = EnhancedResourceManager.Instance;

            if (resourceManager != null)
            {
                InitializeToolSlots();
                UpdateAllSlots();
                UpdateSelection();
                resourceManager.OnToolSlotChanged += OnToolSlotChanged;
            }

            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
                if (UIFocusManager.Instance.GetActiveUI() == null)
                {
                    UIFocusManager.Instance.PushUI(this);
                }
            }
        }

        private void Update()
        {
            HandleToolSelection();
        }

        private void HandleToolSelection()
        {
            var inputManager = KowloonBreak.Core.InputManager.Instance;
            bool inventoryOpen = inputManager != null && inputManager.IsInventoryOpen();
            if (inventoryOpen) return;

            if (handlePrevNextInput && inputManager != null)
            {
                int sel = inputManager.GetToolSelectionInput();
                if (sel >= 0 && sel < displayToolCount)
                {
                    SelectTool(sel);
                    return;
                }

                if (inputManager.IsToolPreviousPressed())
                {
                    int newIndex = selectedToolIndex - 1;
                    if (newIndex < 0) newIndex = displayToolCount - 1;
                    SelectTool(newIndex);
                    return;
                }
                if (inputManager.IsToolNextPressed())
                {
                    int newIndex = (selectedToolIndex + 1) % displayToolCount;
                    SelectTool(newIndex);
                    return;
                }
            }

            if (handlePrevNextInput)
            {
                // 1-8 direct selection
                for (int i = 0; i < displayToolCount; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                    {
                        SelectTool(i);
                        break;
                    }
                }

                // Q/E or Arrow keys
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    int newIndex = selectedToolIndex - 1;
                    if (newIndex < 0) newIndex = displayToolCount - 1;
                    SelectTool(newIndex);
                    return;
                }
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    int newIndex = (selectedToolIndex + 1) % displayToolCount;
                    SelectTool(newIndex);
                    return;
                }

                // Mouse wheel
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0f)
                {
                    int direction = scroll > 0f ? -1 : 1;
                    int newIndex = (selectedToolIndex + direction) % displayToolCount;
                    if (newIndex < 0) newIndex = displayToolCount - 1;
                    SelectTool(newIndex);
                }
            }

            // Confirm selection
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                UseTool(selectedToolIndex);
            }
        }

        private void InitializeToolSlots()
        {
            if (resourceManager == null) return;

            toolSlots.Clear();
            ItemSlotUI[] existingSlots = toolSlotsParent.GetComponentsInChildren<ItemSlotUI>(true);
            int slotsToUse = Mathf.Min(existingSlots.Length, displayToolCount);

            for (int i = 0; i < slotsToUse; i++)
            {
                ItemSlotUI slotUI = existingSlots[i];
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSlotClicked += OnToolSlotClicked;
                    toolSlots.Add(slotUI);
                    slotUI.gameObject.SetActive(true);
                }
            }

            for (int i = slotsToUse; i < existingSlots.Length; i++)
            {
                existingSlots[i].gameObject.SetActive(false);
            }
        }

        private void UpdateAllSlots()
        {
            if (resourceManager == null) return;

            for (int i = 0; i < toolSlots.Count; i++)
            {
                var slot = resourceManager.GetToolSlot(i);
                toolSlots[i].SetSlot(slot);
            }
        }

        private void OnToolSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < toolSlots.Count)
            {
                toolSlots[index].SetSlot(slot);
            }
        }

        private void OnToolSlotClicked(ItemSlotUI slotUI)
        {
            SelectTool(slotUI.SlotIndex);
        }

        public void SelectTool(int index)
        {
            if (index < 0 || index >= displayToolCount) return;

            selectedToolIndex = index;
            UpdateSelection();

            var selectedSlot = resourceManager?.GetToolSlot(selectedToolIndex);
            OnToolSelected?.Invoke(selectedToolIndex, selectedSlot);
        }

        public void UseTool(int index)
        {
            if (index < 0 || index >= displayToolCount) return;

            var toolSlot = resourceManager?.GetToolSlot(index);
            if (toolSlot != null && !toolSlot.IsEmpty)
            {
                OnToolUsed?.Invoke(index, toolSlot);
            }
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < toolSlots.Count; i++)
            {
                bool isSelected = (i == selectedToolIndex);
                toolSlots[i].SetSelected(isSelected);
                toolSlots[i].SetFocused(isSelected);
            }
        }

        public InventorySlot GetSelectedTool()
        {
            return resourceManager?.GetToolSlot(selectedToolIndex);
        }

        public bool HasSelectedTool()
        {
            var selectedSlot = GetSelectedTool();
            return selectedSlot != null && !selectedSlot.IsEmpty;
        }

        public void SetSlotSize(float size)
        {
            slotSize = size;

            foreach (var slot in toolSlots)
            {
                if (slot != null)
                {
                    RectTransform rectTransform = slot.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(size, size);
                }
            }
        }

        public void SetSpacing(float newSpacing)
        {
            spacing = newSpacing;
            if (toolSlotsLayout != null)
            {
                toolSlotsLayout.spacing = newSpacing;
            }
        }

        public void SetDisplayToolCount(int count)
        {
            displayToolCount = Mathf.Clamp(count, 1, 8);
            if (toolSlotsParent != null)
            {
                InitializeToolSlots();
                UpdateAllSlots();
                UpdateSelection();
            }
        }

        public void RefreshDisplay()
        {
            UpdateAllSlots();
            UpdateSelection();
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.OnToolSlotChanged -= OnToolSlotChanged;
            }

            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.UnregisterUI(this);
            }
        }

        /// <summary>
        /// IFocusableUI: enable/disable input
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;

            foreach (var slot in toolSlots)
            {
                if (slot != null)
                {
                    var button = slot.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = enabled;
                    }
                }
            }
        }
    }
}
