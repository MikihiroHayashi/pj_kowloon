using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class ToolSelectionHUDController : MonoBehaviour
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
        
        public int SelectedToolIndex => selectedToolIndex;
        public InventorySlot SelectedTool => resourceManager?.GetToolSlot(selectedToolIndex);
        
        public System.Action<int, InventorySlot> OnToolSelected;
        public System.Action<int, InventorySlot> OnToolUsed;
        
        private void Awake()
        {
            // デフォルトの参照を設定
            if (toolSlotsLayout == null)
                toolSlotsLayout = GetComponent<HorizontalLayoutGroup>();
            
            if (toolSlotsParent == null)
                toolSlotsParent = toolSlotsLayout != null ? toolSlotsLayout.transform : transform;
            
            if (toolSlotsLayout == null)
                toolSlotsLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
            
            // レイアウト設定
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
                
                // イベント監視
                resourceManager.OnToolSlotChanged += OnToolSlotChanged;
            }
        }
        
        private void Update()
        {
            HandleToolSelection();
        }
        
        private void HandleToolSelection()
        {
            // 1-8キーで道具選択
            for (int i = 0; i < displayToolCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectTool(i);
                    break;
                }
            }
            
            // 矢印キーでフォーカス移動
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                int newIndex = selectedToolIndex - 1;
                if (newIndex < 0) newIndex = displayToolCount - 1;
                SelectTool(newIndex);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                int newIndex = (selectedToolIndex + 1) % displayToolCount;
                SelectTool(newIndex);
            }
            
            // Enterキーまたはスペースキーで選択されたツールを使用
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                UseTool(selectedToolIndex);
            }
            
            // マウスホイールで道具選択
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                int direction = scroll > 0f ? -1 : 1;
                int newIndex = (selectedToolIndex + direction) % displayToolCount;
                if (newIndex < 0) newIndex = displayToolCount - 1;
                
                SelectTool(newIndex);
            }
        }
        
        private void InitializeToolSlots()
        {
            if (resourceManager == null) return;
            
            toolSlots.Clear();
            
            // シーンに配置されたスロットを取得
            ItemSlotUI[] existingSlots = toolSlotsParent.GetComponentsInChildren<ItemSlotUI>(true);
            
            // displayToolCountまでのスロットを使用
            int slotsToUse = Mathf.Min(existingSlots.Length, displayToolCount);
            
            for (int i = 0; i < slotsToUse; i++)
            {
                ItemSlotUI slotUI = existingSlots[i];
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSlotClicked += OnToolSlotClicked;
                    toolSlots.Add(slotUI);
                    
                    // スロットをアクティブにする
                    slotUI.gameObject.SetActive(true);
                }
            }
            
            // 足りない場合は警告を出す
            if (existingSlots.Length < displayToolCount)
            {
                Debug.LogWarning($"Not enough tool slots in scene. Found: {existingSlots.Length}, Required: {displayToolCount}");
            }
            
            // 余分なスロットは非アクティブにする
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
            
            Debug.Log($"Selected tool slot {selectedToolIndex}: {selectedSlot?.ItemData?.itemName ?? "Empty"}");
        }
        
        public void UseTool(int index)
        {
            if (index < 0 || index >= displayToolCount) return;
            
            var toolSlot = resourceManager?.GetToolSlot(index);
            if (toolSlot != null && !toolSlot.IsEmpty)
            {
                OnToolUsed?.Invoke(index, toolSlot);
                Debug.Log($"Used tool: {toolSlot.ItemData?.itemName ?? "Unknown"} from slot {index}");
            }
            else
            {
                Debug.Log($"No tool in slot {index} to use");
            }
        }
        
        private void UpdateSelection()
        {
            for (int i = 0; i < toolSlots.Count; i++)
            {
                toolSlots[i].SetSelected(i == selectedToolIndex);
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
        }
    }
}