using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Core;

namespace KowloonBreak.UI
{
    public class ItemSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Text quantityText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image selectionFrame;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color emptyColor = Color.gray;
        [SerializeField] private Sprite emptySlotSprite;
        
        private InventorySlot currentSlot;
        private bool isSelected = false;
        private int slotIndex = -1;
        
        public InventorySlot CurrentSlot => currentSlot;
        public bool IsSelected => isSelected;
        public int SlotIndex => slotIndex;
        
        public System.Action<ItemSlotUI> OnSlotClicked;
        
        private void Awake()
        {
            // デフォルトの参照を設定
            if (itemIcon == null)
                itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
            
            if (quantityText == null)
                quantityText = transform.Find("QuantityText")?.GetComponent<Text>();
            
            if (durabilityBar == null)
                durabilityBar = transform.Find("DurabilityBar")?.GetComponent<Image>();
            
            if (slotBackground == null)
                slotBackground = GetComponent<Image>();
            
            if (selectionFrame == null)
                selectionFrame = transform.Find("SelectionFrame")?.GetComponent<Image>();
            
            // ボタンイベントを設定
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }
        
        private void Start()
        {
            UpdateVisuals();
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
            isSelected = selected;
            UpdateSelectionVisuals();
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
                quantityText.text = "";
            }
            
            if (durabilityBar != null)
            {
                durabilityBar.gameObject.SetActive(false);
            }
        }
        
        private void ShowItemSlot()
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = currentSlot.ItemData.icon;
                itemIcon.color = normalColor;
            }
            
            if (quantityText != null)
            {
                if (currentSlot.Quantity > 1)
                {
                    quantityText.text = currentSlot.Quantity.ToString();
                }
                else
                {
                    quantityText.text = "";
                }
            }
            
            if (durabilityBar != null)
            {
                if (currentSlot.ItemData.IsTool())
                {
                    durabilityBar.gameObject.SetActive(true);
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
                    durabilityBar.gameObject.SetActive(false);
                }
            }
        }
        
        private void UpdateSelectionVisuals()
        {
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(isSelected);
            }
            
            if (slotBackground != null)
            {
                slotBackground.color = isSelected ? selectedColor : normalColor;
            }
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