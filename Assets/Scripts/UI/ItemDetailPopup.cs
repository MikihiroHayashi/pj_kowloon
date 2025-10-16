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

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // デフォルト参照設定
            if (popupPanel == null)
                popupPanel = transform.Find("PopupPanel")?.gameObject;

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

            // 初期状態は非表示
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        private void Start()
        {
            // UIManager経由でTargetSelectionDialogのイベントを購読
            if (UIManager.Instance != null && UIManager.Instance.TargetSelectionDialog != null)
            {
                UIManager.Instance.TargetSelectionDialog.OnTargetSelected += OnTargetSelected;
                UIManager.Instance.TargetSelectionDialog.OnCancelled += OnTargetSelectionCancelled;
            }
        }

        private void OnDestroy()
        {
            // イベント購読解除
            if (UIManager.Instance != null && UIManager.Instance.TargetSelectionDialog != null)
            {
                UIManager.Instance.TargetSelectionDialog.OnTargetSelected -= OnTargetSelected;
                UIManager.Instance.TargetSelectionDialog.OnCancelled -= OnTargetSelectionCancelled;
            }
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

            UpdatePopupContent();
            UpdatePosition();

            if (popupPanel != null)
                popupPanel.SetActive(true);

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
                bool canUse = itemData.IsConsumable();
                useButton.gameObject.SetActive(canUse);

                if (canUse)
                {
                    // 使用ボタンのテキストを更新
                    var useButtonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (useButtonText != null)
                    {
                        useButtonText.text = "使用 [E]";
                    }

                    // 効果説明を追加
                    if (itemDescriptionText != null && itemData.consumableEffect != null)
                    {
                        string effectText = GetConsumableEffectText(itemData.consumableEffect);
                        if (!string.IsNullOrEmpty(effectText))
                        {
                            itemDescriptionText.text += "\n\n<color=#88FF88>効果:</color>\n" + effectText;
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
                effectText += $"・体力 +{effect.healthRestore}\n";
            }

            if (effect.HasStaminaEffect)
            {
                effectText += $"・スタミナ +{effect.staminaRestore}\n";
            }

            if (effect.HasInfectionEffect)
            {
                effectText += $"・感染治療 -{effect.infectionTreatment}\n";
            }

            return effectText.TrimEnd('\n');
        }

        private void OnUseButtonClicked()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;

            ItemData itemData = currentSlot.ItemData;

            if (!itemData.IsConsumable())
            {
                Debug.LogWarning("[ItemDetailPopup] This item is not consumable");
                return;
            }

            // 回復アイテムの場合はターゲット選択ダイアログを表示
            if (itemData.consumableEffect != null &&
                (itemData.consumableEffect.HasHealthEffect ||
                 itemData.consumableEffect.HasStaminaEffect ||
                 itemData.consumableEffect.HasInfectionEffect))
            {
                // ItemDetailPopupを一時的に非表示
                if (popupPanel != null)
                {
                    popupPanel.SetActive(false);
                }

                // ターゲット選択ダイアログを表示（UIManager経由）
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowTargetSelection(currentSlot);
                }
                return;
            }

            // ターゲット選択が不要な場合、またはダイアログがない場合は直接使用
            UseItemOnTarget(null);
        }

        /// <summary>
        /// ターゲットが選択された時の処理
        /// </summary>
        private void OnTargetSelected(object target)
        {
            UseItemOnTarget(target);

            // ItemDetailPopupを再表示
            if (popupPanel != null && currentSlot != null && !currentSlot.IsEmpty)
            {
                popupPanel.SetActive(true);
                // フォーカスを元のスロットに戻す
                if (sourceSlotUI != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(sourceSlotUI.gameObject);
                }
            }
        }

        /// <summary>
        /// ターゲット選択がキャンセルされた時の処理
        /// </summary>
        private void OnTargetSelectionCancelled()
        {
            // ItemDetailPopupを再表示
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                // フォーカスを元のスロットに戻す
                if (sourceSlotUI != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(sourceSlotUI.gameObject);
                }
            }
        }

        /// <summary>
        /// アイテムを指定ターゲットに使用
        /// </summary>
        private void UseItemOnTarget(object target)
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;

            ItemData itemData = currentSlot.ItemData;
            bool success = false;

            // ターゲットがプレイヤーの場合
            if (target is Player.EnhancedPlayerController player)
            {
                success = UseItemOnPlayer(player);
            }
            // ターゲットがコンパニオンの場合
            else if (target is Characters.CompanionAI companion)
            {
                success = UseItemOnCompanion(companion);
            }
            // ターゲット指定なし（プレイヤー自身に使用）
            else
            {
                // ConsumableManagerを通じて使用（従来の処理）
                success = currentSlot.UseConsumable();
            }

            if (success)
            {
                Debug.Log($"[ItemDetailPopup] Used {itemData.itemName}");

                // スロットが空になった場合はポップアップを閉じる
                if (currentSlot.IsEmpty)
                {
                    Hide();
                }
                else
                {
                    // 内容を更新
                    UpdatePopupContent();
                }
            }
            else
            {
                Debug.LogWarning($"[ItemDetailPopup] Failed to use {itemData.itemName}");
            }
        }

        /// <summary>
        /// プレイヤーにアイテムを使用
        /// </summary>
        private bool UseItemOnPlayer(Player.EnhancedPlayerController player)
        {
            if (player == null || currentSlot == null) return false;

            ItemData itemData = currentSlot.ItemData;
            ConsumableEffect effect = itemData.consumableEffect;

            if (effect == null) return false;

            // 体力回復
            if (effect.HasHealthEffect)
            {
                player.Heal(effect.healthRestore);
                Debug.Log($"[ItemDetailPopup] Player healed {effect.healthRestore} HP");
            }

            // スタミナ回復（現在のシステムに応じて実装）
            if (effect.HasStaminaEffect)
            {
                // プレイヤーにスタミナ回復メソッドがあれば呼び出し
                player.RestoreFullStamina(); // 仮実装
                Debug.Log($"[ItemDetailPopup] Player restored stamina");
            }

            // 感染治療
            if (effect.HasInfectionEffect)
            {
                player.TreatInfection(effect.infectionTreatment);
                Debug.Log($"[ItemDetailPopup] Player treated infection -{effect.infectionTreatment}");
            }

            // アイテムを消費
            currentSlot.RemoveItem(1);

            // UI通知
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{itemData.itemName}を使用しました", NotificationType.Success);
            }

            return true;
        }

        /// <summary>
        /// コンパニオンにアイテムを使用
        /// </summary>
        private bool UseItemOnCompanion(Characters.CompanionAI companion)
        {
            if (companion == null || currentSlot == null) return false;

            ItemData itemData = currentSlot.ItemData;
            ConsumableEffect effect = itemData.consumableEffect;

            if (effect == null) return false;

            // 体力回復
            if (effect.HasHealthEffect)
            {
                companion.Heal(effect.healthRestore);
                Debug.Log($"[ItemDetailPopup] Companion healed {effect.healthRestore} HP");
            }

            // スタミナ回復（コンパニオンにスタミナシステムがあれば）
            if (effect.HasStaminaEffect)
            {
                // コンパニオン用のスタミナ回復処理（実装に応じて調整）
                Debug.Log($"[ItemDetailPopup] Companion stamina restored");
            }

            // 感染治療（CompanionCharacterから取得）
            if (effect.HasInfectionEffect)
            {
                var companionCharacter = companion.GetComponent<Characters.CompanionCharacter>();
                if (companionCharacter != null && companionCharacter.Infection != null)
                {
                    companionCharacter.Infection.TreatInfection(effect.infectionTreatment);
                    Debug.Log($"[ItemDetailPopup] Companion treated infection -{effect.infectionTreatment}");
                }
            }

            // アイテムを消費
            currentSlot.RemoveItem(1);

            // UI通知
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{companion.name}に{itemData.itemName}を使用しました", NotificationType.Success);
            }

            return true;
        }

        private void OnDiscardButtonClicked()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;

            ItemData itemData = currentSlot.ItemData;

            // 確認ダイアログを表示するのが理想的だが、今回はシンプルに1個破棄
            bool success = DiscardItem();

            if (success)
            {
                Debug.Log($"[ItemDetailPopup] Discarded 1x {itemData.itemName}");

                // スロットが空になった場合はポップアップを閉じる
                if (currentSlot.IsEmpty)
                {
                    Hide();
                }
                else
                {
                    // 内容を更新
                    UpdatePopupContent();
                }
            }
        }

        private bool DiscardItem()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return false;

            ItemData itemData = currentSlot.ItemData;

            // スロットから直接アイテムを削除
            bool success = currentSlot.RemoveItem(1);

            if (success)
            {
                Debug.Log($"[ItemDetailPopup] Discarded 1x {itemData.itemName}");
            }
            else
            {
                Debug.LogWarning($"[ItemDetailPopup] Failed to discard {itemData.itemName}");
            }

            return success;
        }

        private void Update()
        {
            // InputManagerを使用した入力処理
            if (popupPanel != null && popupPanel.activeSelf)
            {
                var inputManager = KowloonBreak.Core.InputManager.Instance;
                if (inputManager == null) return;

                // 使用ボタン (A / E)
                if (inputManager.IsUseItemPressed() && currentSlot != null && currentSlot.ItemData.IsConsumable())
                {
                    OnUseButtonClicked();
                }

                // 破棄ボタン (Y / Q)
                if (inputManager.IsDiscardItemPressed())
                {
                    OnDiscardButtonClicked();
                }

                // キャンセルはInventoryDialogControllerで処理
            }
        }
    }
}
