using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KowloonBreak.Core;

namespace KowloonBreak.UI
{
    public class ItemSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Animator slotAnimator;
        
        [Header("Visual Settings")]
        [SerializeField] private Color emptyColor = Color.gray;
        [SerializeField] private Sprite emptySlotSprite;
        
        private InventorySlot currentSlot;
        private bool isSelected = false;
        private bool isFocused = false;
        private int slotIndex = -1;
        
        public InventorySlot CurrentSlot => currentSlot;
        public bool IsSelected => isSelected;
        public bool IsFocused => isFocused;
        public int SlotIndex => slotIndex;

        public System.Action<ItemSlotUI> OnSlotClicked;
        public System.Action<ItemSlotUI> OnSlotHoverEnter;
        public System.Action<ItemSlotUI> OnSlotHoverExit;
        
        private void Awake()
        {
            // デフォルトの参照を設定
            if (itemIcon == null)
                itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
            
            if (quantityText == null)
                quantityText = transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();
            
            if (durabilityBar == null)
                durabilityBar = transform.Find("DurabilityBar")?.GetComponent<Image>();
            
            if (slotBackground == null)
                slotBackground = GetComponent<Image>();
            
            if (selectionFrame == null)
                selectionFrame = transform.Find("SelectionFrame")?.GetComponent<Image>();

            if (slotAnimator == null)
                slotAnimator = GetComponent<Animator>();

            // ボタンイベントを設定
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }

            // フォーカスイベントを設定
            SetupFocusEvents();
        }
        
        private void Start()
        {
            UpdateVisuals();

            // 初期状態のアニメーション設定
            UpdateFocusAnimation();
            UpdateSelectionAnimation();
        }
        
        public void Initialize(int index)
        {
            slotIndex = index;
            UpdateVisuals();
        }
        
        public void SetSlot(InventorySlot slot)
        {
            if (currentSlot != null)
            {
                currentSlot.OnSlotChanged -= OnSlotChanged;
            }
            
            currentSlot = slot;
            
            if (currentSlot != null)
            {
                currentSlot.OnSlotChanged += OnSlotChanged;
            }
            
            UpdateVisuals();
        }
        
        private void OnSlotChanged(InventorySlot slot)
        {
            UpdateVisuals();
        }
        
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;

            isSelected = selected;
            UpdateSelectionVisuals();
            UpdateSelectionAnimation();
        }

        public void SetFocused(bool focused)
        {
            if (isFocused == focused) return;

            isFocused = focused;
            UpdateFocusAnimation();
        }

        private void SetupFocusEvents()
        {
            var eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = gameObject.AddComponent<EventTrigger>();

            // フォーカス取得イベント（マウスホバー）
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((data) =>
            {
                SetFocused(true);
                OnSlotHoverEnter?.Invoke(this);
            });
            eventTrigger.triggers.Add(pointerEnter);

            // フォーカス喪失イベント（マウス離脱）
            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((data) =>
            {
                SetFocused(false);
                OnSlotHoverExit?.Invoke(this);
            });
            eventTrigger.triggers.Add(pointerExit);

            // フォーカス取得イベント（キーボード/コントローラー選択）
            var select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            select.callback.AddListener((data) =>
            {
                Debug.Log($"[ItemSlotUI] Select event triggered on slot {slotIndex}");
                SetFocused(true);
                OnSlotHoverEnter?.Invoke(this);
            });
            eventTrigger.triggers.Add(select);

            // フォーカス喪失イベント（キーボード/コントローラー選択解除）
            var deselect = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
            deselect.callback.AddListener((data) =>
            {
                Debug.Log($"[ItemSlotUI] Deselect event triggered on slot {slotIndex}");
                SetFocused(false);
                OnSlotHoverExit?.Invoke(this);
            });
            eventTrigger.triggers.Add(deselect);
        }

        private void UpdateFocusAnimation()
        {
            if (slotAnimator != null)
            {
                slotAnimator.SetBool("Focus", isFocused);
            }
        }

        private void UpdateSelectionAnimation()
        {
            if (slotAnimator != null)
            {
                slotAnimator.SetBool("Selected", isSelected);
            }
        }
        
        private void UpdateVisuals()
        {
            if (currentSlot == null || currentSlot.IsEmpty)
            {
                ShowEmptySlot();
            }
            else
            {
                ShowItemSlot();
            }
            
            UpdateSelectionVisuals();
        }
        
        private void ShowEmptySlot()
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = emptySlotSprite;
                itemIcon.color = emptyColor;
            }
            
            if (quantityText != null)
            {
                quantityText.transform.parent.gameObject.SetActive(false);
            }
            
            if (durabilityBar != null)
            {
                durabilityBar.transform.parent.gameObject.SetActive(false);
            }
        }
        
        private void ShowItemSlot()
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = currentSlot.ItemData.icon;
                itemIcon.color = Color.white;
            }
            
            if (quantityText != null)
            {
                if (currentSlot.Quantity > 1)
                {
                    quantityText.text = currentSlot.Quantity.ToString();
                    quantityText.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.transform.parent.gameObject.SetActive(false);
                }
            }
            
            if (durabilityBar != null)
            {
                if (currentSlot.ItemData.IsTool())
                {
                    durabilityBar.transform.parent.gameObject.SetActive(true);
                    float durabilityPercentage = currentSlot.GetDurabilityPercentage();
                    durabilityBar.fillAmount = durabilityPercentage;
                    
                    // 耐久度に応じて色を変更
                    if (durabilityPercentage > 0.5f)
                        durabilityBar.color = Color.green;
                    else if (durabilityPercentage > 0.25f)
                        durabilityBar.color = Color.yellow;
                    else
                        durabilityBar.color = Color.red;
                }
                else
                {
                    durabilityBar.transform.parent.gameObject.SetActive(false);
                }
            }
        }
        
        private void UpdateSelectionVisuals()
        {
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(isSelected);
            }

            // カラー変更はアニメーションで制御するため削除
        }
        
        private void OnClick()
        {
            OnSlotClicked?.Invoke(this);
        }
        
        public void ShowTooltip()
        {
            if (currentSlot == null || currentSlot.IsEmpty) return;
            
            // ツールチップ表示の実装
            string tooltipText = GetTooltipText();
            
            // ツールチップマネージャーがある場合
            // TooltipManager.Instance?.ShowTooltip(tooltipText, transform.position);
            
            Debug.Log($"Tooltip: {tooltipText}");
        }
        
        public void HideTooltip()
        {
            // ツールチップ非表示の実装
            // TooltipManager.Instance?.HideTooltip();
        }
        
        private string GetTooltipText()
        {
            if (currentSlot == null || currentSlot.IsEmpty)
                return "";
            
            var item = currentSlot.ItemData;
            var text = $"<b>{item.itemName}</b>\n";
            text += $"{item.description}\n";
            text += $"Quantity: {currentSlot.Quantity}";
            
            if (item.IsTool())
            {
                text += $"\nDurability: {currentSlot.Durability}/{item.durability}";
                text += $"\nDamage: {item.attackDamage}";
                text += $"\nRange: {item.attackRange}";
            }
            else
            {
                text += $"\nValue: {item.value}";
            }
            
            return text;
        }
        
        public void PlaySelectSound()
        {
            // 選択音の再生
            // AudioManager.Instance?.PlaySFX("ui_select");
        }
        
        public void PlayClickSound()
        {
            // クリック音の再生
            // AudioManager.Instance?.PlaySFX("ui_click");
        }
        
        private void OnDestroy()
        {
            if (currentSlot != null)
            {
                currentSlot.OnSlotChanged -= OnSlotChanged;
            }
        }
    }
}