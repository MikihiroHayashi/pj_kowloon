using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Kowloon Break/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;
        public string description;
        public ItemType itemType;
        public Sprite icon;
        public int maxStackSize = 99;
        
        [Header("Tool Specific")]
        public ToolType toolType;
        public int durability = 100;
        public float attackDamage = 1f;
        public float attackRange = 1.5f;
        
        [Header("Material Specific")]
        public MaterialType materialType;
        public float value = 1f;
        
        [Header("Drop Settings")]
        public GameObject droppedItemPrefab;  // ドロップ時に生成されるプレハブ
        public float dropWeight = 1f;        // ドロップ重み（確率計算用）
        
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(itemName))
            {
                itemName = "Unknown Item";
                description = "No description available";
                itemType = ItemType.Material;
                maxStackSize = 99;
                durability = 100;
                attackDamage = 1f;
                attackRange = 1.5f;
                value = 1f;
            }
        }
        
        public bool IsStackable()
        {
            return maxStackSize > 1;
        }
        
        public bool IsTool()
        {
            return itemType == ItemType.Tool;
        }
        
        public bool IsMaterial()
        {
            return itemType == ItemType.Material;
        }
    }
}