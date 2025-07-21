using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class InventoryDialogController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GridLayoutGroup toolSlotsGrid;
        [SerializeField] private GridLayoutGroup materialSlotsGrid;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject slotPrefab;
        
        [Header("Settings")]
        [SerializeField] private bool closeOnEscape = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
        [SerializeField] private bool useUIManager = true;
        
        private List<ItemSlotUI> toolSlots = new List<ItemSlotUI>();
        private List<ItemSlotUI> materialSlots = new List<ItemSlotUI>();
        private EnhancedResourceManager resourceManager;
        private bool isOpen = false;
        
        public bool IsOpen => isOpen;
        
        private void Awake()
        {
            // デフォルトの参照を設定
            if (inventoryPanel == null)
                inventoryPanel = transform.Find("InventoryPanel")?.gameObject;
            
            if (toolSlotsGrid == null)
                toolSlotsGrid = transform.Find("InventoryPanel/ToolSlots")?.GetComponent<GridLayoutGroup>();
            
            if (materialSlotsGrid == null)
                materialSlotsGrid = transform.Find("InventoryPanel/MaterialSlots")?.GetComponent<GridLayoutGroup>();
            
            if (closeButton == null)
                closeButton = transform.Find("InventoryPanel/CloseButton")?.GetComponent<Button>();
            
            // クローズボタンのイベント設定
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }
        }
        
        private void Start()
        {
            resourceManager = EnhancedResourceManager.Instance;
            
            if (resourceManager != null)
            {
                InitializeSlots();
                UpdateAllSlots();
                
                // イベント監視
                resourceManager.OnToolSlotChanged += OnToolSlotChanged;
                resourceManager.OnMaterialSlotChanged += OnMaterialSlotChanged;
            }
            
            // 初期状態は非表示
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            if (!useUIManager && Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
            
            if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape) && isOpen)
            {
                CloseInventory();
            }
        }
        
        private void InitializeSlots()
        {
            if (resourceManager == null) return;
            
            // 道具スロット初期化
            CreateToolSlots();
            
            // 素材スロット初期化
            CreateMaterialSlots();
        }
        
        private void CreateToolSlots()
        {
            if (toolSlotsGrid == null) return;
            
            // 既存のスロットをクリア
            foreach (var slot in toolSlots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            toolSlots.Clear();
            
            // 新しいスロットを作成
            for (int i = 0; i < resourceManager.ToolSlots; i++)
            {
                GameObject slotObj = CreateSlotObject(toolSlotsGrid.transform);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSlotClicked += OnToolSlotClicked;
                    toolSlots.Add(slotUI);
                }
            }
        }
        
        private void CreateMaterialSlots()
        {
            if (materialSlotsGrid == null) return;
            
            // 既存のスロットをクリア
            foreach (var slot in materialSlots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            materialSlots.Clear();
            
            // 新しいスロットを作成
            for (int i = 0; i < resourceManager.MaterialSlots; i++)
            {
                GameObject slotObj = CreateSlotObject(materialSlotsGrid.transform);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Initialize(i);
                    slotUI.OnSlotClicked += OnMaterialSlotClicked;
                    materialSlots.Add(slotUI);
                }
            }
        }
        
        private GameObject CreateSlotObject(Transform parent)
        {
            GameObject slotObj;
            
            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, parent);
            }
            else
            {
                slotObj = CreateDefaultSlot(parent);
            }
            
            return slotObj;
        }
        
        private GameObject CreateDefaultSlot(Transform parent)
        {
            GameObject slotObj = new GameObject("ItemSlot");
            slotObj.transform.SetParent(parent);
            
            // Image (背景)
            Image background = slotObj.AddComponent<Image>();
            background.color = Color.gray;
            
            // Button
            Button button = slotObj.AddComponent<Button>();
            
            // ItemSlotUI
            ItemSlotUI slotUI = slotObj.AddComponent<ItemSlotUI>();
            
            // アイコン用のGameObject
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(slotObj.transform);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            
            // 数量テキスト用のGameObject
            GameObject quantityObj = new GameObject("QuantityText");
            quantityObj.transform.SetParent(slotObj.transform);
            var quantityText = quantityObj.AddComponent<Text>();
            quantityText.text = "";
            quantityText.fontSize = 14;
            quantityText.color = Color.white;
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.raycastTarget = false;
            
            // 耐久度バー用のGameObject
            GameObject durabilityObj = new GameObject("DurabilityBar");
            durabilityObj.transform.SetParent(slotObj.transform);
            Image durabilityBar = durabilityObj.AddComponent<Image>();
            durabilityBar.color = Color.green;
            durabilityBar.type = Image.Type.Filled;
            durabilityBar.raycastTarget = false;
            durabilityBar.gameObject.SetActive(false);
            
            // 選択フレーム用のGameObject
            GameObject frameObj = new GameObject("SelectionFrame");
            frameObj.transform.SetParent(slotObj.transform);
            Image frame = frameObj.AddComponent<Image>();
            frame.color = Color.yellow;
            frame.raycastTarget = false;
            frameObj.SetActive(false);
            
            // RectTransformの設定
            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            return slotObj;
        }
        
        private void UpdateAllSlots()
        {
            if (resourceManager == null) return;
            
            // 道具スロット更新
            for (int i = 0; i < toolSlots.Count; i++)
            {
                var slot = resourceManager.GetToolSlot(i);
                toolSlots[i].SetSlot(slot);
            }
            
            // 素材スロット更新
            for (int i = 0; i < materialSlots.Count; i++)
            {
                var slot = resourceManager.GetMaterialSlot(i);
                materialSlots[i].SetSlot(slot);
            }
        }
        
        private void OnToolSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < toolSlots.Count)
            {
                toolSlots[index].SetSlot(slot);
            }
        }
        
        private void OnMaterialSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < materialSlots.Count)
            {
                materialSlots[index].SetSlot(slot);
            }
        }
        
        private void OnToolSlotClicked(ItemSlotUI slotUI)
        {
            Debug.Log($"Tool slot {slotUI.SlotIndex} clicked");
            
            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                slotUI.ShowTooltip();
            }
        }
        
        private void OnMaterialSlotClicked(ItemSlotUI slotUI)
        {
            Debug.Log($"Material slot {slotUI.SlotIndex} clicked");
            
            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                slotUI.ShowTooltip();
            }
        }
        
        public void ToggleInventory()
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
        
        public void OpenInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                isOpen = true;
                
                if (useUIManager && UIManager.Instance != null)
                {
                    UIManager.Instance.OpenPanel("Inventory");
                }
                else
                {
                    // カーソルを表示
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    
                    // 時間を停止
                    Time.timeScale = 0f;
                }
                
                Debug.Log("Inventory opened");
            }
        }
        
        public void CloseInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
                isOpen = false;
                
                if (useUIManager && UIManager.Instance != null)
                {
                    UIManager.Instance.ClosePanel("Inventory");
                }
                else
                {
                    // カーソルを隠す
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    
                    // 時間を再開
                    Time.timeScale = 1f;
                }
                
                Debug.Log("Inventory closed");
            }
        }
        
        public void RefreshInventory()
        {
            UpdateAllSlots();
        }
        
        public void SetSlotPrefab(GameObject prefab)
        {
            slotPrefab = prefab;
        }
        
        public void SetToggleKey(KeyCode key)
        {
            toggleKey = key;
        }
        
        public List<ItemSlotUI> GetToolSlots()
        {
            return new List<ItemSlotUI>(toolSlots);
        }
        
        public List<ItemSlotUI> GetMaterialSlots()
        {
            return new List<ItemSlotUI>(materialSlots);
        }
        
        private void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.OnToolSlotChanged -= OnToolSlotChanged;
                resourceManager.OnMaterialSlotChanged -= OnMaterialSlotChanged;
            }
        }
    }
}