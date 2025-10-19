using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        private bool isInputEnabled = true;
        // Pending state for target selection flow
        private InventorySlot pendingUseSlot = null;
        private ItemSlotUI pendingSourceSlotUI = null;
        private bool pendingIsTool = false;

        public bool IsOpen => isOpen;

        // IFocusableUI実装
        public bool IsVisible => isOpen && inventoryPanel != null && inventoryPanel.activeSelf;
        public int Priority => 4; // インベントリは中優先度
        public string UIName => "InventoryDialog";

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
            // フォールバック: 階層外にある場合に備えてシーン全体から検索
            if (itemDetailPopup == null)
                itemDetailPopup = FindObjectOfType<ItemDetailPopup>(true);
            if (itemDetailPopup == null)
                Debug.LogWarning("[InventoryDialogController] ItemDetailPopup not found in children or scene. Detail popup will be unavailable.");

            // Subscribe to detail popup actions
            if (itemDetailPopup != null)
            {
                itemDetailPopup.OnUseRequested += HandleUseRequested;
                itemDetailPopup.OnDiscardRequested += HandleDiscardRequested;
            }

            // クローズボタンのイベント設定
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }

            // resourceManagerを早期に初期化
            resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.OnToolSlotChanged -= OnToolSlotChanged;
                resourceManager.OnMaterialSlotChanged -= OnMaterialSlotChanged;
                resourceManager.OnToolSlotChanged += OnToolSlotChanged;
                resourceManager.OnMaterialSlotChanged += OnMaterialSlotChanged;
            }
        }
        
        private void Start()
        {
            // UIFocusManagerに登録
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
            }
        }

        private void Update()
        {
            if (!isOpen || !isInputEnabled) return;
            var input = KowloonBreak.Core.InputManager.Instance;
            var contextManager = KowloonBreak.Core.UIContextManager.Instance;
            if (input == null) return;
            if (contextManager != null && contextManager.CurrentContext != KowloonBreak.Core.UIContext.Inventory) return;

            // Keyboard/controller shortcuts for use/discard while detail is open
            if (input.IsUseItemPressed())
            {
                var slot = currentSelectedSlot != null ? currentSelectedSlot.CurrentSlot : null;
                if (slot != null && !slot.IsEmpty)
                {
                    HandleUseRequested(slot);
                }
            }
            if (input.IsDiscardItemPressed())
            {
                var slot = currentSelectedSlot != null ? currentSelectedSlot.CurrentSlot : null;
                if (slot != null && !slot.IsEmpty)
                {
                    HandleDiscardRequested(slot);
                }
            }

            // Auto-hide detail popup if the popup-bound slot becomes empty (independent of selection)
            if (itemDetailPopup != null && itemDetailPopup.IsVisible)
            {
                var bound = itemDetailPopup.BoundSlot;
                if (bound == null || bound.IsEmpty)
                {
                    itemDetailPopup.Hide();
                }
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
                // If the changed slot is currently selected, update or hide the detail popup
                if (currentSelectedSlot == toolSlots[index])
                {
                    if (slot == null || slot.IsEmpty)
                    {
                        itemDetailPopup?.Hide();
                    }
                    else
                    {
                        itemDetailPopup?.Show(slot, currentSelectedSlot, true);
                    }
                }
            }
        }
        
        private void OnMaterialSlotChanged(int index, InventorySlot slot)
        {
            if (index >= 0 && index < materialSlots.Count)
            {
                materialSlots[index].SetSlot(slot);
                // If the changed slot is currently selected, update or hide the detail popup
                if (currentSelectedSlot == materialSlots[index])
                {
                    if (slot == null || slot.IsEmpty)
                    {
                        itemDetailPopup?.Hide();
                    }
                    else
                    {
                        itemDetailPopup?.Show(slot, currentSelectedSlot, false);
                    }
                }
            }
        }
        
        private void OnToolSlotClicked(ItemSlotUI slotUI)
        {
            // クリックは選択のみ行い、詳細ポップアップはホバーで制御
            SelectSlot(slotUI);
        }

        private void OnMaterialSlotClicked(ItemSlotUI slotUI)
        {
            // クリックは選択のみ行い、詳細ポップアップはホバーで制御
            SelectSlot(slotUI);
        }

        private void ShowItemDetailPopup(ItemSlotUI slotUI, bool isToolSlot)
        {
            if (itemDetailPopup != null && slotUI.CurrentSlot != null && !slotUI.CurrentSlot.IsEmpty)
            {
                itemDetailPopup.Show(slotUI.CurrentSlot, slotUI, isToolSlot);
            }
        }

        private void HandleUseRequested(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;
            var itemData = slot.ItemData;
            if (itemData == null || !itemData.IsConsumable())
            {
                Debug.LogWarning("[InventoryDialog] This item is not consumable");
                return;
            }

            bool needsTarget = itemData.consumableEffect != null &&
                               (itemData.consumableEffect.HasHealthEffect ||
                                itemData.consumableEffect.HasStaminaEffect ||
                                itemData.consumableEffect.HasInfectionEffect);

            pendingUseSlot = slot;
            pendingSourceSlotUI = currentSelectedSlot;
            pendingIsTool = (pendingSourceSlotUI != null && toolSlots.Contains(pendingSourceSlotUI));

            if (needsTarget && UIManager.Instance != null)
            {
                var dialog = UIManager.Instance.TargetSelectionDialog;
                if (dialog != null)
                {
                    dialog.OnTargetSelected -= OnTargetSelectedFromDialog;
                    dialog.OnCancelled -= OnTargetSelectionCancelledFromDialog;
                    dialog.OnTargetSelected += OnTargetSelectedFromDialog;
                    dialog.OnCancelled += OnTargetSelectionCancelledFromDialog;
                }
                if (dialog != null)
                {
                    UIManager.Instance.ShowTargetSelection(slot);
                }
                else
                {
                    Debug.LogWarning("[InventoryDialog] TargetSelectionDialog is not assigned; using item on player directly.");
                    bool usedDirect = UseOnPlayer(slot);
                    AfterUseUpdate(usedDirect);
                }
                return;
            }

            bool used = UseOnPlayer(slot);
            AfterUseUpdate(used);
        }

        private void HandleDiscardRequested(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;
            var itemData = slot.ItemData;
            bool success = slot.RemoveItem(1);
            if (success)
            {
                Debug.Log($"[InventoryDialog] Discarded 1x {itemData.itemName}");
                RefreshInventory();
                if (slot.IsEmpty) itemDetailPopup?.Hide(); else itemDetailPopup?.Show(slot, currentSelectedSlot, toolSlots.Contains(currentSelectedSlot));
            }
            else
            {
                Debug.LogWarning($"[InventoryDialog] Failed to discard {itemData.itemName}");
            }
        }

        private void OnTargetSelectedFromDialog(object target)
        {
            bool used = false;
            if (pendingUseSlot == null || pendingUseSlot.IsEmpty)
            {
                used = false;
            }
            else if (target is Player.EnhancedPlayerController)
            {
                used = UseOnPlayer(pendingUseSlot);
            }
            else if (target is KowloonBreak.Characters.CompanionAI companion)
            {
                used = UseOnCompanion(pendingUseSlot, companion);
            }
            AfterUseUpdate(used);

            // 使用が完了した直後にターゲット選択ダイアログのスロットを更新（回復アニメーションを即時反映）
            var dialog = UIManager.Instance != null ? UIManager.Instance.TargetSelectionDialog : null;
            if (dialog != null && dialog.IsVisible)
            {
                // 値更新が揺れないよう次フレームで実行（UIはunscaledでTween）
                UIManager.Instance.RunNextFrame(() => dialog.RefreshSlots());
            }
        }

        private void OnTargetSelectionCancelledFromDialog()
        {
            if (pendingUseSlot != null && pendingSourceSlotUI != null)
            {
                itemDetailPopup?.Show(pendingUseSlot, pendingSourceSlotUI, pendingIsTool);
            }
            ClearPendingUse();
        }

        private void ClearPendingUse()
        {
            var dialog = UIManager.Instance != null ? UIManager.Instance.TargetSelectionDialog : null;
            if (dialog != null)
            {
                dialog.OnTargetSelected -= OnTargetSelectedFromDialog;
                dialog.OnCancelled -= OnTargetSelectionCancelledFromDialog;
            }
            pendingUseSlot = null;
            pendingSourceSlotUI = null;
            pendingIsTool = false;
        }

        private void AfterUseUpdate(bool used)
        {
            if (used)
            {
                RefreshInventory();
                if (pendingUseSlot != null)
                {
                    if (pendingUseSlot.IsEmpty) itemDetailPopup?.Hide(); else itemDetailPopup?.Show(pendingUseSlot, pendingSourceSlotUI, pendingIsTool);
                }
            }
            else
            {
                if (pendingUseSlot != null && pendingSourceSlotUI != null)
                {
                    itemDetailPopup?.Show(pendingUseSlot, pendingSourceSlotUI, pendingIsTool);
                }
            }
            // ターゲット選択ダイアログを開いたままにする要件: ダイアログが可視の間は pendingUse を維持
            var dialog = UIManager.Instance != null ? UIManager.Instance.TargetSelectionDialog : null;
            bool dialogVisible = dialog != null && dialog.IsVisible;
            if (!dialogVisible)
            {
                ClearPendingUse();
            }
        }

        private bool UseOnPlayer(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return false;
            var item = slot.ItemData;
            if (item == null || !item.IsConsumable()) return false;
            bool used = slot.UseConsumable();
            if (!used)
            {
                var erm = EnhancedResourceManager.Instance != null ? EnhancedResourceManager.Instance : FindObjectOfType<EnhancedResourceManager>();
                if (erm != null)
                {
                    used = erm.UseConsumableItem(item);
                }
            }
            if (used && UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{item.itemName}を使用しました", NotificationType.Success);
            }
            return used;
        }

        private bool UseOnCompanion(InventorySlot slot, KowloonBreak.Characters.CompanionAI companion)
        {
            if (slot == null || slot.IsEmpty || companion == null) return false;
            var item = slot.ItemData;
            var effect = item != null ? item.consumableEffect : null;
            if (item == null || effect == null) return false;

            if (effect.HasHealthEffect)
            {
                companion.Heal(effect.healthRestore);
            }
            if (effect.HasStaminaEffect)
            {
                // Optional stamina logic if available
            }
            if (effect.HasInfectionEffect)
            {
                var compChar = companion.GetComponent<Characters.CompanionCharacter>();
                if (compChar != null && compChar.Infection != null)
                {
                    compChar.Infection.TreatInfection(effect.infectionTreatment);
                }
            }
            slot.RemoveItem(1);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{companion.name}に{item.itemName}を使用しました", NotificationType.Success);
            }
            return true;
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
            // ホバーを外したら詳細ポップアップを閉じる（ホバーで表示/非表示を統一）
            itemDetailPopup?.Hide();
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
            // フォールバック: resourceManagerがnullの場合は再取得
            if (resourceManager == null)
            {
                resourceManager = EnhancedResourceManager.Instance;
                if (resourceManager != null)
                {
                    resourceManager.OnToolSlotChanged -= OnToolSlotChanged;
                    resourceManager.OnMaterialSlotChanged -= OnMaterialSlotChanged;
                    resourceManager.OnToolSlotChanged += OnToolSlotChanged;
                    resourceManager.OnMaterialSlotChanged += OnMaterialSlotChanged;
                }
            }

            isOpen = true;

            // 初回起動時にスロットが初期化されていない場合は初期化
            if (toolSlots.Count == 0 || materialSlots.Count == 0)
            {
                InitializeSlots();
            }

            // スロット情報を更新
            UpdateAllSlots();

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

        private System.Collections.IEnumerator PostOpenInventorySetup()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
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
            if (isOpen)
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

                isOpen = false;

                // GameObjectの非アクティブ化はUIManagerが担当
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

            // 有効化時にEventSystemの選択が外れていればスロットにフォーカスを戻す
            if (enabled)
            {
                var es = EventSystem.current;
                if (es != null)
                {
                    bool needsFocus = es.currentSelectedGameObject == null ||
                        (es.currentSelectedGameObject != null &&
                         !(inventoryPanel != null ? es.currentSelectedGameObject.transform.IsChildOf(inventoryPanel.transform) : es.currentSelectedGameObject.transform.IsChildOf(transform)));

                    if (needsFocus)
                    {
                        GameObject target = null;
                        if (currentSelectedSlot != null)
                        {
                            target = currentSelectedSlot.gameObject;
                        }
                        else if (toolSlots.Count > 0 && toolSlots[0] != null)
                        {
                            target = toolSlots[0].gameObject;
                        }
                        else if (materialSlots.Count > 0 && materialSlots[0] != null)
                        {
                            target = materialSlots[0].gameObject;
                        }

                        if (target != null)
                        {
                            es.SetSelectedGameObject(target);
                        }
                    }
                }
            }
        }
    }
}
