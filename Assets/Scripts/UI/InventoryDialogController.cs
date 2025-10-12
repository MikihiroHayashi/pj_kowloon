using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class InventoryDialogController : MonoBehaviour, IFocusableUI
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
        private InventoryInputHandler inventoryInputHandler;
        private bool isInputEnabled = true;

        public bool IsOpen => isOpen;

        // IFocusableUI実装
        public bool IsVisible => isOpen && inventoryPanel != null && inventoryPanel.activeSelf;
        public int Priority => 4; // インベントリは中優先度
        public string UIName => "InventoryDialog";

        private void Update()
        {
            // 入力処理はInventoryInputHandlerに移譲
            // （Update()での直接処理は廃止）
        }
        
        private void Awake()
        {
            Debug.Log($"[InventoryDialogController] Awake called on {gameObject.name}");

            // デフォルトの参照を設定
            if (inventoryPanel == null)
                inventoryPanel = transform.Find("InventoryPanel")?.gameObject;

            Debug.Log($"[InventoryDialogController] inventoryPanel found: {inventoryPanel?.name ?? "NULL"}");

            if (toolSlotsGrid == null)
                toolSlotsGrid = transform.Find("InventoryPanel/ToolSlots")?.GetComponent<GridLayoutGroup>();

            Debug.Log($"[InventoryDialogController] toolSlotsGrid found: {(toolSlotsGrid != null ? "Yes" : "NULL")}");

            if (materialSlotsGrid == null)
                materialSlotsGrid = transform.Find("InventoryPanel/MaterialSlots")?.GetComponent<GridLayoutGroup>();

            Debug.Log($"[InventoryDialogController] materialSlotsGrid found: {(materialSlotsGrid != null ? "Yes" : "NULL")}");

            if (closeButton == null)
                closeButton = transform.Find("InventoryPanel/CloseButton")?.GetComponent<Button>();

            if (itemDetailPopup == null)
                itemDetailPopup = GetComponentInChildren<ItemDetailPopup>(true);

            // クローズボタンのイベント設定
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }

            // InventoryInputHandlerを作成してInputManagerに登録
            inventoryInputHandler = new InventoryInputHandler(this);
            var inputManager = KowloonBreak.Core.InputManager.Instance;
            if (inputManager != null)
            {
                inputManager.RegisterInputHandler("Inventory", inventoryInputHandler);
            }

            Debug.Log("[InventoryDialogController] Awake completed");
        }
        
        private void Start()
        {
            resourceManager = EnhancedResourceManager.Instance;

            if (resourceManager != null)
            {
                // イベント監視
                resourceManager.OnToolSlotChanged += OnToolSlotChanged;
                resourceManager.OnMaterialSlotChanged += OnMaterialSlotChanged;
            }

            // Awake()で登録できなかった場合のリトライ
            if (inventoryInputHandler == null)
            {
                inventoryInputHandler = new InventoryInputHandler(this);
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    inputManager.RegisterInputHandler("Inventory", inventoryInputHandler);
                }
            }

            // UIFocusManagerに登録
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
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
            if (toolSlotsGrid == null)
            {
                Debug.LogWarning("[InventoryDialogController] toolSlotsGrid is null!");
                return;
            }

            toolSlots.Clear();

            // シーンに既に配置されているスロットを取得
            ItemSlotUI[] existingSlots = toolSlotsGrid.GetComponentsInChildren<ItemSlotUI>(true);
            Debug.Log($"[InventoryDialogController] CreateToolSlots: Found {existingSlots.Length} existing tool slots, need {resourceManager.ToolSlots} slots");

            // 既存スロットがある場合は再利用
            if (existingSlots.Length > 0)
            {
                int slotsToUse = Mathf.Min(existingSlots.Length, resourceManager.ToolSlots);

                for (int i = 0; i < slotsToUse; i++)
                {
                    ItemSlotUI slotUI = existingSlots[i];
                    if (slotUI != null)
                    {
                        // イベントの重複登録を防ぐため、まず削除
                        slotUI.OnSlotClicked -= OnToolSlotClicked;
                        slotUI.OnSlotHoverExit -= OnSlotHoverExit;

                        slotUI.Initialize(i);
                        slotUI.OnSlotClicked += OnToolSlotClicked;
                        slotUI.OnSlotHoverEnter += (slot) => OnSlotHoverEnter(slot, true);
                        slotUI.OnSlotHoverExit += OnSlotHoverExit;
                        toolSlots.Add(slotUI);
                        slotUI.gameObject.SetActive(true);
                    }
                }

                // 余分なスロットは非アクティブ化
                for (int i = slotsToUse; i < existingSlots.Length; i++)
                {
                    existingSlots[i].gameObject.SetActive(false);
                }

                // 足りない場合は新規作成
                for (int i = existingSlots.Length; i < resourceManager.ToolSlots; i++)
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
            else
            {
                // 既存スロットがない場合は新規作成
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
        }
        
        private void CreateMaterialSlots()
        {
            if (materialSlotsGrid == null)
            {
                Debug.LogWarning("[InventoryDialogController] materialSlotsGrid is null!");
                return;
            }

            materialSlots.Clear();

            // シーンに既に配置されているスロットを取得
            ItemSlotUI[] existingSlots = materialSlotsGrid.GetComponentsInChildren<ItemSlotUI>(true);
            Debug.Log($"[InventoryDialogController] CreateMaterialSlots: Found {existingSlots.Length} existing material slots, need {resourceManager.MaterialSlots} slots");

            // 既存スロットがある場合は再利用
            if (existingSlots.Length > 0)
            {
                int slotsToUse = Mathf.Min(existingSlots.Length, resourceManager.MaterialSlots);

                for (int i = 0; i < slotsToUse; i++)
                {
                    ItemSlotUI slotUI = existingSlots[i];
                    if (slotUI != null)
                    {
                        // イベントの重複登録を防ぐため、まず削除
                        slotUI.OnSlotClicked -= OnMaterialSlotClicked;
                        slotUI.OnSlotHoverExit -= OnSlotHoverExit;

                        slotUI.Initialize(i);
                        slotUI.OnSlotClicked += OnMaterialSlotClicked;
                        slotUI.OnSlotHoverEnter += (slot) => OnSlotHoverEnter(slot, false);
                        slotUI.OnSlotHoverExit += OnSlotHoverExit;
                        materialSlots.Add(slotUI);
                        slotUI.gameObject.SetActive(true);
                    }
                }

                // 余分なスロットは非アクティブ化
                for (int i = slotsToUse; i < existingSlots.Length; i++)
                {
                    existingSlots[i].gameObject.SetActive(false);
                }

                // 足りない場合は新規作成
                for (int i = existingSlots.Length; i < resourceManager.MaterialSlots; i++)
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
            else
            {
                // 既存スロットがない場合は新規作成
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
            SelectSlot(slotUI);

            if (slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                ShowItemDetailPopup(slotUI, true);
            }
        }

        private void OnMaterialSlotClicked(ItemSlotUI slotUI)
        {
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
            }
        }

        public void ClearSelection()
        {
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.SetSelected(false);
                currentSelectedSlot = null;
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
            Debug.Log($"[InventoryDialogController] OpenInventory called. GameObject active: {gameObject.activeInHierarchy}, inventoryPanel: {inventoryPanel?.name}, inventoryPanel active: {inventoryPanel?.activeSelf}");

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                isOpen = true;

                Debug.Log($"[InventoryDialogController] inventoryPanel activated. toolSlots count: {toolSlots.Count}, materialSlots count: {materialSlots.Count}");

                // 初回起動時にスロットが初期化されていない場合は初期化
                if (toolSlots.Count == 0 || materialSlots.Count == 0)
                {
                    Debug.Log("[InventoryDialogController] Initializing slots...");
                    InitializeSlots();
                    Debug.Log($"[InventoryDialogController] Slots initialized. toolSlots: {toolSlots.Count}, materialSlots: {materialSlots.Count}");
                }

                // スロット情報を更新
                Debug.Log("[InventoryDialogController] Updating all slots...");
                UpdateAllSlots();

                // InputManagerの入力ハンドラーをInventoryに切り替え
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    inputManager.SwitchInputHandler("Inventory");

                    // 後方互換性のため残す
                    #pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    inputManager.SetInventoryOpen(true);
                    #pragma warning restore CS0618
                }

                // 既存のコルーチンを停止
                if (focusCoroutine != null)
                {
                    StopCoroutine(focusCoroutine);
                }

                // UIFocusManagerにプッシュ（他のUIを自動的に無効化）
                if (UIFocusManager.Instance != null)
                {
                    UIFocusManager.Instance.PushUI(this);
                }

                // レイアウト更新とフォーカス設定は次フレームで
                focusCoroutine = StartCoroutine(PostOpenInventorySetup());
            }
        }

        private System.Collections.IEnumerator PostOpenInventorySetup()
        {
            // 1フレーム待機してUIのレイアウトが確定するのを待つ
            yield return null;

            // フォーカスを設定
            SetInitialFocus();
            focusCoroutine = null;
        }

        private System.Collections.IEnumerator SetInitialFocusDelayed()
        {
            // 1フレーム待機
            yield return null;

            SetInitialFocus();
            focusCoroutine = null;
        }

        private void SetInitialFocus()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return;

            // 最初の空でないツールスロットにフォーカス
            foreach (var slot in toolSlots)
            {
                if (slot != null && slot.CurrentSlot != null && !slot.CurrentSlot.IsEmpty)
                {
                    eventSystem.SetSelectedGameObject(slot.gameObject);
                    return;
                }
            }

            // ツールスロットが空の場合、最初のマテリアルスロットにフォーカス
            foreach (var slot in materialSlots)
            {
                if (slot != null && slot.CurrentSlot != null && !slot.CurrentSlot.IsEmpty)
                {
                    eventSystem.SetSelectedGameObject(slot.gameObject);
                    return;
                }
            }

            // すべて空の場合、最初のツールスロットにフォーカス
            if (toolSlots.Count > 0 && toolSlots[0] != null)
            {
                eventSystem.SetSelectedGameObject(toolSlots[0].gameObject);
            }
        }

        public void CloseInventory()
        {
            if (inventoryPanel != null && isOpen)
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

                // UIFocusManagerからポップ（他のUIを自動的に有効化）
                if (UIFocusManager.Instance != null)
                {
                    UIFocusManager.Instance.PopUI(this);
                }

                inventoryPanel.SetActive(false);
                isOpen = false;

                // InputManagerの入力ハンドラーをGameplayに戻す
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager != null)
                {
                    inputManager.SwitchInputHandler("Gameplay");

                    // 後方互換性のため残す
                    #pragma warning disable CS0618 // 型またはメンバーが旧型式です
                    inputManager.SetInventoryOpen(false);
                    #pragma warning restore CS0618
                }
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

            // UIFocusManagerから登録解除
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.UnregisterUI(this);
            }
        }

        /// <summary>
        /// IFocusableUI実装: 入力の有効/無効を設定
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;

            // すべてのスロットボタンの有効/無効を制御
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

            foreach (var slot in materialSlots)
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

            // クローズボタンの有効/無効を制御
            if (closeButton != null)
            {
                closeButton.interactable = enabled;
            }
        }
    }
}