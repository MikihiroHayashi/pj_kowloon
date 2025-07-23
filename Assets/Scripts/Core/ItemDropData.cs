using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class ItemDropData
    {
        [Header("Item Reference")]
        public ItemData itemData;           // ScriptableObjectの参照
        
        [Header("Drop Settings")]
        public int minAmount = 1;           // 最小ドロップ数
        public int maxAmount = 1;           // 最大ドロップ数
        [Range(0f, 1f)]
        public float dropChance = 1f;       // ドロップ確率
        
        [Header("Override Settings (オプション)")]
        public bool overrideWeight = false; // 重みをオーバーライドするか
        [Range(0.1f, 10f)]
        public float customWeight = 1f;     // カスタム重み
        
        public bool IsValid()
        {
            return itemData != null && 
                   !string.IsNullOrEmpty(itemData.itemName) && 
                   dropChance > 0f && 
                   maxAmount > 0;
        }
        
        public float GetEffectiveWeight()
        {
            if (overrideWeight)
                return customWeight;
            
            return itemData != null ? itemData.dropWeight : 1f;
        }
        
        public GameObject GetPrefab()
        {
            return itemData?.droppedItemPrefab;
        }
        
        public string GetItemName()
        {
            return itemData?.itemName ?? "Unknown Item";
        }
    }
}