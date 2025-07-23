using UnityEngine;

namespace KowloonBreak.Environment
{
    public class ItemPickupTrigger : MonoBehaviour
    {
        private DroppedItem parentItem;
        
        public void SetParentItem(DroppedItem parent)
        {
            parentItem = parent;
            Debug.Log($"[ItemPickupTrigger] Parent item set: {parent?.ItemName}");
        }
        
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[ItemPickupTrigger] OnTriggerEnter - Object: {other.name}, Tag: {other.tag}");
            
            if (parentItem != null)
            {
                parentItem.OnPlayerTriggerEnter(other);
            }
            else
            {
                Debug.LogError($"[ItemPickupTrigger] parentItem is null! GameObject: {gameObject.name}, Parent: {transform.parent?.name ?? "None"}");
                
                // 親からDroppedItemを検索してみる
                DroppedItem foundParent = GetComponentInParent<DroppedItem>();
                if (foundParent != null)
                {
                    Debug.Log($"[ItemPickupTrigger] Auto-recovered: Found DroppedItem in parent: {foundParent.ItemName}");
                    SetParentItem(foundParent);
                    foundParent.OnPlayerTriggerEnter(other);
                }
                else
                {
                    // 最後の手段：ルートから検索
                    DroppedItem rootParent = transform.root.GetComponent<DroppedItem>();
                    if (rootParent != null)
                    {
                        Debug.Log($"[ItemPickupTrigger] Auto-recovered from root: {rootParent.ItemName}");
                        SetParentItem(rootParent);
                        rootParent.OnPlayerTriggerEnter(other);
                    }
                    else
                    {
                        Debug.LogError("[ItemPickupTrigger] Could not find DroppedItem anywhere in hierarchy!");
                    }
                }
            }
        }
    }
}