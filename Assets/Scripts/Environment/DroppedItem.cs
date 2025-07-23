using System.Collections;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Environment
{
    public class DroppedItem : MonoBehaviour
    {
        [Header("Dropped Item Settings")]
        [SerializeField] private string itemName;
        [SerializeField] private int quantity = 1;
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private float autoPickupDelay = 1f;
        [SerializeField] private float despawnTime = 300f; // 5分後に消滅
        
        
        
        private bool canPickup = false;
        private float spawnTime;
        private Collider itemCollider;
        private Renderer itemRenderer;
        private Rigidbody rb;
        
        public string ItemName => itemName;
        public int Quantity => quantity;
        
        private void Awake()
        {
            
            // Rigidbodyの確認（必須）
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"[DroppedItem] Rigidbody component missing on {gameObject.name}! Please add Rigidbody to the prefab.");
                return;
            }
            
            // 物理コライダーの確認（必須）
            itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
            {
                Debug.LogError($"[DroppedItem] Collider component missing on {gameObject.name}! Please add Collider to the prefab.");
                return;
            }
            
            // コライダーがトリガーでないことを確認
            if (itemCollider.isTrigger)
            {
                Debug.LogError($"[DroppedItem] Main collider should not be a trigger on {gameObject.name}! Please set isTrigger = false.");
            }
            
            // ピックアップ用のトリガーコライダーを検索（Model子オブジェクトと共存可能）
            ItemPickupTrigger pickupTrigger = GetComponentInChildren<ItemPickupTrigger>();
            if (pickupTrigger == null)
            {
                Debug.LogError($"[DroppedItem] ItemPickupTrigger component missing in children of {gameObject.name}! Please add ItemPickupTrigger component to a child object (can be on Model object).");
                return;
            }
            else
            {
                Debug.Log($"[DroppedItem] Found ItemPickupTrigger on: {pickupTrigger.name}");
            }
            
            // 遅延してSetParentItemを呼び出す
            StartCoroutine(SetupPickupTrigger(pickupTrigger));
            
            // Rendererの確認（子オブジェクトから検索）
            itemRenderer = GetComponentInChildren<Renderer>();
            
            if (itemRenderer == null)
            {
                Debug.LogError($"[DroppedItem] Renderer component missing in children of {gameObject.name}! Please add a child object with Renderer component (Model).");
            }
            else
            {
                Debug.Log($"[DroppedItem] Found Renderer in child object: {itemRenderer.name}");
            }
        }
        
        private void Start()
        {
            spawnTime = Time.time;
            
            // ItemPickupTriggerの再確認と設定
            ItemPickupTrigger pickupTrigger = GetComponentInChildren<ItemPickupTrigger>();
            if (pickupTrigger != null)
            {
                pickupTrigger.SetParentItem(this);
                Debug.Log($"[DroppedItem] Start() - Set parent for ItemPickupTrigger on {pickupTrigger.name}");
            }
            
            // 少し遅れてから拾得可能にする
            StartCoroutine(EnablePickupAfterDelay());
        }
        
        private void Update()
        {
            // 自動消滅チェック
            if (Time.time - spawnTime > despawnTime)
            {
                Despawn();
            }
        }
        
        
        private IEnumerator EnablePickupAfterDelay()
        {
            yield return new WaitForSeconds(autoPickupDelay);
            canPickup = true;
        }
        
        
        
        public void Initialize(string itemName, int quantity)
        {
            // 安全な文字列処理
            this.itemName = string.IsNullOrEmpty(itemName) ? "不明なアイテム" : itemName;
            this.quantity = Mathf.Max(1, quantity);
            
            Debug.Log($"[DroppedItem] Initialize called: '{this.itemName}' x{this.quantity}");
        }
        
        
        
        public void OnPlayerTriggerEnter(Collider playerCollider)
        {
            Debug.Log($"[DroppedItem] Trigger entered by: {playerCollider.name}, Tag: {playerCollider.tag}, CanPickup: {canPickup}");
            
            if (!canPickup) 
            {
                Debug.Log($"[DroppedItem] Cannot pickup yet. Item: {itemName}");
                return;
            }
            
            // プレイヤーのタグをチェック
            if (playerCollider.CompareTag("Player"))
            {
                Debug.Log($"[DroppedItem] Player detected, attempting pickup of {itemName}");
                TryPickup();
            }
            else
            {
                Debug.Log($"[DroppedItem] Non-player object detected: {playerCollider.name}");
            }
        }
        
        private void TryPickup()
        {
            Debug.Log($"[DroppedItem] TryPickup called for {itemName} x{quantity}");
            
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) 
            {
                Debug.LogError("[DroppedItem] EnhancedResourceManager.Instance is null!");
                return;
            }
            
            Debug.Log($"[DroppedItem] ResourceManager found, attempting to add {itemName} x{quantity}");
            
            // インベントリに追加を試行
            if (resourceManager.AddItem(itemName, quantity))
            {
                Debug.Log($"[DroppedItem] Successfully added {itemName} x{quantity} to inventory");
                // 拾得成功
                OnPickedUp();
            }
            else
            {
                // インベントリが満杯の場合の処理
                Debug.LogWarning($"[DroppedItem] Failed to add {itemName} x{quantity} to inventory - possibly full");
            }
        }
        
        private void OnPickedUp()
        {
            Debug.Log($"[DroppedItem] Successfully picked up {quantity} {itemName}");
            
            // オブジェクトを削除
            Destroy(gameObject);
        }
        
        private void Despawn()
        {
            Debug.Log($"Dropped item {itemName} despawned");
            Destroy(gameObject);
        }
        
        public void SetPickupRange(float range)
        {
            pickupRange = range;
        }
        
        public void SetDespawnTime(float time)
        {
            despawnTime = time;
        }
        
        private IEnumerator SetupPickupTrigger(ItemPickupTrigger pickupTrigger)
        {
            // 複数フレーム待機してからSetParentItemを呼び出す
            yield return new WaitForEndOfFrame();
            yield return null;
            
            if (pickupTrigger != null)
            {
                pickupTrigger.SetParentItem(this);
                Debug.Log($"[DroppedItem] SetupPickupTrigger completed for {itemName}");
            }
            else
            {
                Debug.LogError("[DroppedItem] PickupTrigger is null in SetupPickupTrigger!");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // ピックアップ範囲を表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}