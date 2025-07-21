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
        [SerializeField] private GameObject toolSlotPrefab;
        
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
        
        private void Awake()
        {
            // デフォルトの参照を設定
            if (toolSlotsLayout == null)
                toolSlotsLayout = GetComponent<HorizontalLayoutGroup>();
            
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
            
            // 既存のスロットをクリア
            foreach (var slot in toolSlots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            toolSlots.Clear();
            
            // 新しいスロットを作成
            for (int i = 0; i < displayToolCount; i++)
            {
                GameObject slotObj = CreateToolSlotObject(i);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSlotClicked += OnToolSlotClicked;
                    toolSlots.Add(slotUI);
                }
            }
        }
        
        private GameObject CreateToolSlotObject(int index)
        {
            GameObject slotObj;
            
            if (toolSlotPrefab != null)
            {
                slotObj = Instantiate(toolSlotPrefab, toolSlotsLayout.transform);
            }
            else
            {
                slotObj = CreateDefaultToolSlot(index);
            }
            
            // サイズ設定
            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
            
            return slotObj;
        }
        
        private GameObject CreateDefaultToolSlot(int index)
        {
            GameObject slotObj = new GameObject($"ToolSlot_{index}");
            slotObj.transform.SetParent(toolSlotsLayout.transform);
            
            // Image (背景)
            Image background = slotObj.AddComponent<Image>();
            background.color = normalColor;
            
            // Button
            Button button = slotObj.AddComponent<Button>();
            
            // ItemSlotUI
            ItemSlotUI slotUI = slotObj.AddComponent<ItemSlotUI>();
            
            // アイコン用のGameObject
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(slotObj.transform);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            
            // 数量テキスト (道具スロットでは通常非表示)
            GameObject quantityObj = new GameObject("QuantityText");
            quantityObj.transform.SetParent(slotObj.transform);
            var quantityText = quantityObj.AddComponent<Text>();
            quantityText.text = "";
            quantityText.fontSize = 12;
            quantityText.color = Color.white;
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.raycastTarget = false;
            
            // 耐久度バー
            GameObject durabilityObj = new GameObject("DurabilityBar");
            durabilityObj.transform.SetParent(slotObj.transform);
            Image durabilityBar = durabilityObj.AddComponent<Image>();
            durabilityBar.color = Color.green;
            durabilityBar.type = Image.Type.Filled;
            durabilityBar.raycastTarget = false;
            
            // 選択フレーム
            GameObject frameObj = new GameObject("SelectionFrame");
            frameObj.transform.SetParent(slotObj.transform);
            Image frame = frameObj.AddComponent<Image>();
            frame.color = selectedColor;
            frame.raycastTarget = false;
            frameObj.SetActive(false);
            
            // キー番号表示
            GameObject keyNumberObj = new GameObject("KeyNumber");
            keyNumberObj.transform.SetParent(slotObj.transform);
            var keyNumberText = keyNumberObj.AddComponent<Text>();
            keyNumberText.text = (index + 1).ToString();
            keyNumberText.fontSize = 10;
            keyNumberText.color = Color.white;
            keyNumberText.alignment = TextAnchor.UpperLeft;
            keyNumberText.raycastTarget = false;
            
            // RectTransformの設定
            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 子オブジェクトの配置
            SetupChildTransforms(slotObj);
            
            return slotObj;
        }
        
        private void SetupChildTransforms(GameObject slotObj)
        {
            // アイコンの配置
            Transform iconTransform = slotObj.transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
            }
            
            // 数量テキストの配置
            Transform quantityTransform = slotObj.transform.Find("QuantityText");
            if (quantityTransform != null)
            {
                RectTransform quantityRect = quantityTransform.GetComponent<RectTransform>();
                quantityRect.anchorMin = new Vector2(0.6f, 0f);
                quantityRect.anchorMax = new Vector2(1f, 0.4f);
                quantityRect.offsetMin = Vector2.zero;
                quantityRect.offsetMax = Vector2.zero;
            }
            
            // 耐久度バーの配置
            Transform durabilityTransform = slotObj.transform.Find("DurabilityBar");
            if (durabilityTransform != null)
            {
                RectTransform durabilityRect = durabilityTransform.GetComponent<RectTransform>();
                durabilityRect.anchorMin = new Vector2(0f, 0f);
                durabilityRect.anchorMax = new Vector2(1f, 0.1f);
                durabilityRect.offsetMin = Vector2.zero;
                durabilityRect.offsetMax = Vector2.zero;
            }
            
            // 選択フレームの配置
            Transform frameTransform = slotObj.transform.Find("SelectionFrame");
            if (frameTransform != null)
            {
                RectTransform frameRect = frameTransform.GetComponent<RectTransform>();
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = Vector2.zero;
                frameRect.offsetMax = Vector2.zero;
            }
            
            // キー番号の配置
            Transform keyNumberTransform = slotObj.transform.Find("KeyNumber");
            if (keyNumberTransform != null)
            {
                RectTransform keyNumberRect = keyNumberTransform.GetComponent<RectTransform>();
                keyNumberRect.anchorMin = new Vector2(0f, 0.8f);
                keyNumberRect.anchorMax = new Vector2(0.4f, 1f);
                keyNumberRect.offsetMin = Vector2.zero;
                keyNumberRect.offsetMax = Vector2.zero;
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
            InitializeToolSlots();
            UpdateAllSlots();
            UpdateSelection();
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