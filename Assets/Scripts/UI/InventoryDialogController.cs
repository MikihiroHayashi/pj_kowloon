using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private ItemDetailPopup itemDetailPopup;
        
        
        private List<ItemSlotUI> toolSlots = new List<ItemSlotUI>();
        private List<ItemSlotUI> materialSlots = new List<ItemSlotUI>();
        private EnhancedResourceManager resourceManager;
        private bool isOpen = false;
        private ItemSlotUI currentSelectedSlot = null;
        private Coroutine focusCoroutine = null;

        public bool IsOpen => isOpen;

        private void Update()
        {
            // インベントリが開いている時の入力処理
            // [入力管理の役割分担]
            // - InventoryDialogController: Bボタン/Escapeでの閉じる処理
            // - UIManager: Tabキー/Viewボタンでのトグル処理
            // - ItemDetailPopup: Enter/Space/Aでの使用、Delete/Backspace/Yでの破棄
            if (isOpen)
            {
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    // Bボタン / Escapeキーでインベントリを閉じる
                    if (inputManager.IsCancelPressed())
                    {
                        // 詳細ポップアップが開いている場合は先に閉じる
                        if (itemDetailPopup != null && itemDetailPopup.gameObject.activeSelf)
                        {
                            itemDetailPopup.Hide();
                        }
                        else
                        {
                            CloseInventory();
                        }
                    }
                }
            }
        }
        
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

            if (itemDetailPopup == null)
                itemDetailPopup = GetComponentInChildren<ItemDetailPopup>(true);

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
                    slotUI.OnSlotHoverEnter += (slot) => OnSlotHoverEnter(slot, true);
                    slotUI.OnSlotHoverExit += OnSlotHoverExit;
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
                    slotUI.OnSlotHoverEnter += (slot) => OnSlotHoverEnter(slot, false);
                    slotUI.OnSlotHoverExit += OnSlotHoverExit;
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
            var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
            quantityText.text = "";
            quantityText.fontSize = 14;
            quantityText.color = Color.white;
            quantityText.alignment = TextAlignmentOptions.BottomRight;
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

            SelectSlot(slotUI);

            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                ShowItemDetailPopup(slotUI, true);
            }
        }

        private void OnMaterialSlotClicked(ItemSlotUI slotUI)
        {
            Debug.Log($"Material slot {slotUI.SlotIndex} clicked");

            SelectSlot(slotUI);

            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                ShowItemDetailPopup(slotUI, false);
            }
        }

        private void ShowItemDetailPopup(ItemSlotUI slotUI, bool isToolSlot)
        {
            if (itemDetailPopup != null && slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                itemDetailPopup.Show(slotUI.CurrentSlot, slotUI, isToolSlot);
            }
            else
            {
                Debug.LogWarning("[InventoryDialogController] Cannot show item detail popup - popup or slot is null");
            }
        }

        private void OnSlotHoverEnter(ItemSlotUI slotUI, bool isToolSlot)
        {
            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                ShowItemDetailPopup(slotUI, isToolSlot);
            }
        }

        private void OnSlotHoverExit(ItemSlotUI slotUI)
        {
            if (itemDetailPopup != null)
            {
                itemDetailPopup.Hide();
            }
        }

        private void SelectSlot(ItemSlotUI slotUI)
        {
            // 前回選択されたスロットの選択状態を解除
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetSelected(false);
            }

            // 新しいスロットを選択
            currentSelectedSlot = slotUI;
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetSelected(true);
                Debug.Log($"[InventoryDialogController] Selected slot {currentSelectedSlot.SlotIndex}");
            }
        }

        public void ClearSelection()
        {
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetSelected(false);
                currentSelectedSlot = null;
                Debug.Log("[InventoryDialogController] Cleared slot selection");
            }
        }

        public void ClearAllFocus()
        {
            // 全てのツールスロットのフォーカスを解除
            foreach (var slot in toolSlots)
            {
                if (slot != null)
                    slot.SetFocused(false);
            }

            // 全てのマテリアルスロットのフォーカスを解除
            foreach (var slot in materialSlots)
            {
                if (slot != null)
                    slot.SetFocused(false);
            }

            Debug.Log("[InventoryDialogController] Cleared all slot focus");
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
                Debug.Log("[InventoryDialogController] OpenInventory called");

                inventoryPanel.SetActive(true);
                isOpen = true;

                // InputManagerにインベントリが開いたことを通知（後方互換性）
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    #pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    inputManager.SetInventoryOpen(true);
                    #pragma warning restore CS0618
                }

                // UIContextManagerへの通知はUIManagerが行うのでここでは不要

                // 既存のコルーチンを停止
                if (focusCoroutine != null)
                {
                    StopCoroutine(focusCoroutine);
                }

                // コントローラー用: 1フレーム遅延してフォーカスを設定
                // UIがアクティブになるまで待つ必要がある
                focusCoroutine = StartCoroutine(SetInitialFocusDelayed());

                Debug.Log("[InventoryDialogController] Inventory opened");
            }
        }

        private System.Collections.IEnumerator SetInitialFocusDelayed()
        {
            // 1フレーム待機
            yield return null;

            Debug.Log("[InventoryDialogController] SetInitialFocusDelayed executing");
            SetInitialFocus();
            focusCoroutine = null;
        }

        private void SetInitialFocus()
        {
            // EventSystemの存在確認
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("[InventoryDialogController] EventSystem not found! Cannot set focus.");
                return;
            }

            Debug.Log($"[InventoryDialogController] Setting initial focus. Tool slots: {toolSlots.Count}, Material slots: {materialSlots.Count}");

            // 最初の空でないツールスロットにフォーカス
            foreach (var slot in toolSlots)
            {
                if (slot != null && slot.CurrentSlot != null && !slot.CurrentSlot.IsEmpty)
                {
                    Debug.Log($"[InventoryDialogController] Attempting to focus tool slot {slot.SlotIndex}, GameObject: {slot.gameObject.name}, Active: {slot.gameObject.activeInHierarchy}");
                    eventSystem.SetSelectedGameObject(slot.gameObject);
                    Debug.Log($"[InventoryDialogController] EventSystem.currentSelectedGameObject: {eventSystem.currentSelectedGameObject?.name}");
                    return;
                }
            }

            // ツールスロットが空の場合、最初のマテリアルスロットにフォーカス
            foreach (var slot in materialSlots)
            {
                if (slot != null && slot.CurrentSlot != null && !slot.CurrentSlot.IsEmpty)
                {
                    Debug.Log($"[InventoryDialogController] Attempting to focus material slot {slot.SlotIndex}, GameObject: {slot.gameObject.name}, Active: {slot.gameObject.activeInHierarchy}");
                    eventSystem.SetSelectedGameObject(slot.gameObject);
                    Debug.Log($"[InventoryDialogController] EventSystem.currentSelectedGameObject: {eventSystem.currentSelectedGameObject?.name}");
                    return;
                }
            }

            // すべて空の場合、最初のツールスロットにフォーカス
            if (toolSlots.Count > 0 && toolSlots[0] != null)
            {
                Debug.Log($"[InventoryDialogController] All slots empty, focusing first tool slot. GameObject: {toolSlots[0].gameObject.name}, Active: {toolSlots[0].gameObject.activeInHierarchy}");
                eventSystem.SetSelectedGameObject(toolSlots[0].gameObject);
                Debug.Log($"[InventoryDialogController] EventSystem.currentSelectedGameObject: {eventSystem.currentSelectedGameObject?.name}");
            }
            else
            {
                Debug.LogWarning("[InventoryDialogController] No slots available for focus!");
            }
        }

        public void CloseInventory()
        {
            if (inventoryPanel != null)
            {
                // 実行中のコルーチンを停止
                if (focusCoroutine != null)
                {
                    StopCoroutine(focusCoroutine);
                    focusCoroutine = null;
                }

                // フォーカスと選択状態をクリア
                ClearAllFocus();
                ClearSelection();

                // 詳細ポップアップを閉じる
                if (itemDetailPopup != null)
                {
                    itemDetailPopup.Hide();
                }

                inventoryPanel.SetActive(false);
                isOpen = false;

                // InputManagerにインベントリが閉じたことを通知（後方互換性）
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    #pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    inputManager.SetInventoryOpen(false);
                    #pragma warning restore CS0618
                }

                // UIContextManagerへの通知はUIManagerが行うのでここでは不要

                Debug.Log("[InventoryDialogController] Inventory closed");
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