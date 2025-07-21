using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity;
        [SerializeField] private int durability;
        
        public ItemData ItemData => itemData;
        public int Quantity => quantity;
        public int Durability => durability;
        public bool IsEmpty => itemData == null || quantity <= 0;
        public bool IsFull => itemData != null && quantity >= itemData.maxStackSize;
        
        public event Action<InventorySlot> OnSlotChanged;
        
        public InventorySlot()
        {
            itemData = null;
            quantity = 0;
            durability = 0;
        }
        
        public InventorySlot(ItemData item, int count = 1, int itemDurability = -1)
        {
            itemData = item;
            quantity = count;
            durability = itemDurability >= 0 ? itemDurability : item.durability;
        }
        
        public bool CanAddItem(ItemData item, int count = 1)
        {
            if (IsEmpty) return true;
            
            if (itemData == item && item.IsStackable())
            {
                return quantity + count <= itemData.maxStackSize;
            }
            
            return false;
        }
        
        public int AddItem(ItemData item, int count = 1, int itemDurability = -1)
        {
            if (IsEmpty)
            {
                itemData = item;
                quantity = Mathf.Min(count, item.maxStackSize);
                durability = itemDurability >= 0 ? itemDurability : item.durability;
                
                OnSlotChanged?.Invoke(this);
                return count - quantity;
            }
            
            if (itemData == item && item.IsStackable())
            {
                int canAdd = Mathf.Min(count, itemData.maxStackSize - quantity);
                quantity += canAdd;
                
                OnSlotChanged?.Invoke(this);
                return count - canAdd;
            }
            
            return count;
        }
        
        public bool RemoveItem(int count = 1)
        {
            if (IsEmpty || count <= 0) return false;
            
            quantity -= count;
            
            if (quantity <= 0)
            {
                Clear();
            }
            else
            {
                OnSlotChanged?.Invoke(this);
            }
            
            return true;
        }
        
        public void Clear()
        {
            itemData = null;
            quantity = 0;
            durability = 0;
            OnSlotChanged?.Invoke(this);
        }
        
        public void SetItem(ItemData item, int count = 1, int itemDurability = -1)
        {
            itemData = item;
            quantity = count;
            durability = itemDurability >= 0 ? itemDurability : (item?.durability ?? 0);
            OnSlotChanged?.Invoke(this);
        }
        
        public bool UseDurability(int damage = 1)
        {
            if (itemData == null || !itemData.IsTool()) return false;
            
            durability -= damage;
            
            if (durability <= 0)
            {
                Clear();
                return false;
            }
            
            OnSlotChanged?.Invoke(this);
            return true;
        }
        
        public float GetDurabilityPercentage()
        {
            if (itemData == null || !itemData.IsTool()) return 1f;
            return (float)durability / itemData.durability;
        }
        
        public InventorySlot Clone()
        {
            return new InventorySlot(itemData, quantity, durability);
        }
    }
}