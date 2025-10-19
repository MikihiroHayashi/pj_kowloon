using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.UI
{
    public class ItemDetailPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI durabilityText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button closeButton;

        [Header("Action Icons")]
        [SerializeField] private Image useButtonIcon;
        [SerializeField] private Image discardButtonIcon;

        [Header("Position Settings")]
        [SerializeField] private Vector2 offset = new Vector2(20f, 0f); // スロットの右側に表示するオフセット

        private InventorySlot currentSlot;
        private ItemSlotUI sourceSlotUI;
        private bool isToolSlot;
        private RectTransform rectTransform;
        
        // Detail-only: Raise events for actions; actual handling is delegated to inventory
        public System.Action<InventorySlot> OnUseRequested;
        public System.Action<InventorySlot> OnDiscardRequested;

        public bool IsVisible => popupPanel != null && popupPanel.activeSelf;
        public InventorySlot BoundSlot => currentSlot;
        public ItemSlotUI SourceSlot => sourceSlotUI;


        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // デフォルト参照設定
            if (popupPanel == null)
                popupPanel = transform.Find("PopupPanel")?.gameObject;
            // フォールバック: パネルが見つからない場合は自身をパネルとして扱う
            if (popupPanel == null)
                popupPanel = this.gameObject;

            if (itemIcon == null)
                itemIcon = transform.Find("PopupPanel/ItemIcon")?.GetComponent<Image>();

            if (itemNameText == null)
                itemNameText = transform.Find("PopupPanel/ItemName")?.GetComponent<TextMeshProUGUI>();

            if (itemDescriptionText == null)
                itemDescriptionText = transform.Find("PopupPanel/Description")?.GetComponent<TextMeshProUGUI>();

            if (durabilityText == null)
                durabilityText = transform.Find("PopupPanel/DurabilityText")?.GetComponent<TextMeshProUGUI>();

            if (durabilityBar == null)
                durabilityBar = transform.Find("PopupPanel/DurabilityBar")?.GetComponent<Image>();

            if (quantityText == null)
                quantityText = transform.Find("PopupPanel/QuantityText")?.GetComponent<TextMeshProUGUI>();

            if (useButton == null)
                useButton = transform.Find("PopupPanel/UseButton")?.GetComponent<Button>();

            if (discardButton == null)
                discardButton = transform.Find("PopupPanel/DiscardButton")?.GetComponent<Button>();

            if (closeButton == null)
                closeButton = transform.Find("PopupPanel/CloseButton")?.GetComponent<Button>();

            if (useButtonIcon == null && useButton != null)
                useButtonIcon = useButton.transform.Find("Icon")?.GetComponent<Image>();

            if (discardButtonIcon == null && discardButton != null)
                discardButtonIcon = discardButton.transform.Find("Icon")?.GetComponent<Image>();

            // ボタンイベント設定
            if (useButton != null)
                useButton.onClick.AddListener(OnUseButtonClicked);

            if (discardButton != null)
                discardButton.onClick.AddListener(OnDiscardButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // 初期状態は非表示（自身をパネルとして扱う場合も統一）
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        private void Start()
        {
            // Detail-only: no subscription here; InventoryDialogController handles dialog events
        }

        private void OnDestroy()
        {
            // Detail-only: nothing to unsubscribe here
        }

        public void Show(InventorySlot slot, ItemSlotUI slotUI, bool isTool)
        {
            if (slot == null || slot.IsEmpty)
            {
                Debug.LogWarning("[ItemDetailPopup] Cannot show popup for empty slot");
                return;
            }

            currentSlot = slot;
            sourceSlotUI = slotUI;
            isToolSlot = isTool;
            // reset any internal flags (none)

            // 先に表示してから内容更新（例外で途中中断しても可視状態を担保）
            if (popupPanel != null)
                popupPanel.SetActive(true);

            try
            {
                UpdatePopupContent();
                UpdatePosition();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemDetailPopup] Failed to update content/position: {ex.Message}\n{ex.StackTrace}");
            }

            Debug.Log($"[ItemDetailPopup] Showing details for {slot.ItemData.itemName}");
        }

        private void UpdatePosition()
        {
            if (sourceSlotUI == null || rectTransform == null) return;

            RectTransform slotRect = sourceSlotUI.GetComponent<RectTransform>();
            if (slotRect == null) return;

            // スロットの右側に配置
            Vector3[] slotCorners = new Vector3[4];
            slotRect.GetWorldCorners(slotCorners);

            // 右上のコーナーを基準にオフセットを追加
            Vector3 targetPosition = slotCorners[2]; // 右上
            targetPosition.x += offset.x;
            targetPosition.y += offset.y;

            // ワールド座標をローカル座標に変換
            if (rectTransform.parent != null)
            {
                RectTransform parentRect = rectTransform.parent as RectTransform;
                if (parentRect != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect,
                        RectTransformUtility.WorldToScreenPoint(null, targetPosition),
                        null,
                        out localPoint
                    );

                    rectTransform.anchoredPosition = localPoint;
                }
            }
        }

        public void Hide()
        {
            // 既に非表示なら処理しない（ログ抑制）
            if (popupPanel != null && !popupPanel.activeSelf)
            {
                return;
            }

            if (popupPanel != null)
                popupPanel.SetActive(false);

            currentSlot = null;
            sourceSlotUI = null;

            Debug.Log("[ItemDetailPopup] Popup hidden");
        }

        private void UpdatePopupContent()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;

            ItemData itemData = currentSlot.ItemData;

            // アイコン
            if (itemIcon != null)
            {
                itemIcon.sprite = itemData.icon;
                itemIcon.color = Color.white;
            }

            // アイテム名
            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
            }

            // 説明
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = itemData.description;
            }

            // 個数
            if (quantityText != null)
            {
                quantityText.text = $"個数: {currentSlot.Quantity}";
            }

            // 耐久値（道具の場合のみ）
            if (itemData.IsTool())
            {
                if (durabilityText != null)
                {
                    durabilityText.gameObject.SetActive(true);
                    float percentage = currentSlot.GetDurabilityPercentage() * 100f;
                    durabilityText.text = $"耐久値: {currentSlot.Durability}/{itemData.durability} ({percentage:F0}%)";
                }

                if (durabilityBar != null)
                {
                    durabilityBar.gameObject.SetActive(true);
                    durabilityBar.fillAmount = currentSlot.GetDurabilityPercentage();

                    // 耐久度に応じて色を変更
                    float durabilityPercentage = currentSlot.GetDurabilityPercentage();
                    if (durabilityPercentage > 0.5f)
                        durabilityBar.color = Color.green;
                    else if (durabilityPercentage > 0.25f)
                        durabilityBar.color = Color.yellow;
                    else
                        durabilityBar.color = Color.red;
                }
            }
            else
            {
                if (durabilityText != null)
                    durabilityText.gameObject.SetActive(false);

                if (durabilityBar != null)
                    durabilityBar.gameObject.SetActive(false);
            }

            // 使用ボタン（消耗品の場合のみ表示）
            if (useButton != null)
            {
                bool isConsumable = itemData.IsConsumable();
                useButton.gameObject.SetActive(isConsumable);

                if (isConsumable)
                {
                    // 使用ボタンのテキストを更新
                    var useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (useButtonText != null)
                    {
                        useButtonText.text = "使用 [E]";
                    }

                    // 使用可能可否をConsumableManagerに問い合わせて活性/非活性を制御
                    bool interactable = true;
                    var cm = KowloonBreak.Managers.ConsumableManager.Instance;
                    if (cm != null)
                    {
                        interactable = cm.CanUseConsumableItem(itemData);
                        // クールダウン中は非活性
                        if (cm.IsOnCooldown(itemData))
                        {
                            interactable = false;
                        }
                    }
                    useButton.interactable = interactable;

                    // 効果説明を追加
                    if (itemDescriptionText != null && itemData.consumableEffect != null)
                    {
                        string effectText = GetConsumableEffectText(itemData.consumableEffect);
                        if (!string.IsNullOrEmpty(effectText))
                        {
                            itemDescriptionText.text += "<color=#88FF88>効果:</color>" + effectText;
                        }
                        // クールダウン表示
                        if (cm != null && itemData.cooldownSeconds > 0f)
                        {
                            float remain = cm.GetRemainingCooldown(itemData);
                            if (remain > 0f)
                            {
                                itemDescriptionText.text += $"\n<color=#FFCC66>クールダウン中: {remain:F1}s</color>";
                            }
                            else
                            {
                                itemDescriptionText.text += $"\n<color=#CCCCCC>クールダウン: {itemData.cooldownSeconds:F1}s</color>";
                            }
                        }
                    }
                }
            }

            // 捨てるボタン
            if (discardButton != null)
            {
                var discardButtonText = discardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (discardButtonText != null)
                {
                    discardButtonText.text = "捨てる [Q]";
                }
            }
        }

        private string GetConsumableEffectText(ConsumableEffect effect)
        {
            if (effect == null) return "";

            string effectText = "";

            if (effect.HasHealthEffect)
            {
                effectText += $"・体力 +{effect.healthRestore}";
            }

            if (effect.HasStaminaEffect)
            {
                effectText += $"・スタミナ +{effect.staminaRestore}";
            }

            if (effect.HasInfectionEffect)
            {
                effectText += $"・感染治療 -{effect.infectionTreatment}";
            }

            return effectText.TrimEnd();
            }

        private void OnUseButtonClicked()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;
            OnUseRequested?.Invoke(currentSlot);
        }

        /// <summary>
        /// ターゲットが選択された時の処理
        /// </summary>
        // Removed: Target selection handling is delegated to InventoryDialogController

        /// <summary>
        /// ターゲット選択がキャンセルされた時の処理
        /// </summary>
        // Removed: Cancellation is handled by InventoryDialogController

        /// <summary>
        /// アイテムを指定ターゲットに使用
        /// </summary>
        // Removed: Item use logic is delegated to InventoryDialogController

        /// <summary>
        /// プレイヤーにアイテムを使用
        /// </summary>
        // Removed

        /// <summary>
        /// コンパニオンにアイテムを使用
        /// </summary>
        // Removed

        private void OnDiscardButtonClicked()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;
            OnDiscardRequested?.Invoke(currentSlot);
        }

        // Removed: discard handling is delegated

        // Removed: input handled by Inventory/Focus flow

        // Ensure subscription to TargetSelectionDialog events at runtime
        // Removed

    }
}
