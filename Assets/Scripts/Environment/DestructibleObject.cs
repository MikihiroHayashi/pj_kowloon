using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Environment
{
    public abstract class DestructibleObject : MonoBehaviour, IDestructible
    {
        [Header("Destructible Settings")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected ToolType[] allowedTools;
        [SerializeField] protected bool destroyOnZeroHealth = true;
        [SerializeField] protected float respawnTime = 600f; // 10分 (ゲーム時間24時間)
        
        [Header("Drop Settings")]
        [SerializeField] protected DropItem[] dropItems;
        [SerializeField] protected float dropRadius = 2f;
        [SerializeField] protected float dropForce = 5f;
        
        [Header("Visual Effects")]
        [SerializeField] protected GameObject destroyEffect;
        [SerializeField] protected AudioClip destroySound;
        [SerializeField] protected AudioClip hitSound;
        
        protected bool isDestroyed = false;
        protected float respawnTimer = 0f;
        protected Vector3 originalPosition;
        protected Quaternion originalRotation;
        protected AudioSource audioSource;
        
        public bool IsDestroyed => isDestroyed;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float RespawnTime => respawnTime;
        
        public event Action<DestructibleObject> OnDestroyed;
        public event Action<DestructibleObject> OnRespawned;
        public event Action<DestructibleObject, float> OnDamaged;
        
        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        protected virtual void Update()
        {
            if (isDestroyed && respawnTime > 0)
            {
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnTime)
                {
                    Respawn();
                }
            }
        }
        
        public virtual bool CanBeDestroyedBy(ToolType toolType)
        {
            if (isDestroyed) return false;
            
            if (allowedTools == null || allowedTools.Length == 0)
                return true;
                
            foreach (var tool in allowedTools)
            {
                if (tool == toolType)
                    return true;
            }
            
            return false;
        }
        
        public virtual void TakeDamage(float damage, ToolType toolType)
        {
            if (isDestroyed || !CanBeDestroyedBy(toolType))
                return;
                
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            OnDamaged?.Invoke(this, damage);
            
            // ヒット音再生
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // ヒットエフェクト
            OnHit(damage, toolType);
            
            if (currentHealth <= 0)
            {
                DestroyObject();
            }
        }
        
        protected virtual void OnHit(float damage, ToolType toolType)
        {
            // 派生クラスでオーバーライド可能
        }
        
        protected virtual void DestroyObject()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            
            // 破壊エフェクト
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, transform.rotation);
            }
            
            // 破壊音再生
            if (destroySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(destroySound);
            }
            
            // アイテムドロップ
            DropItems();
            
            // オブジェクトを非表示にする
            if (destroyOnZeroHealth)
            {
                gameObject.SetActive(false);
            }
            
            OnDestroyed?.Invoke(this);
            
            // リスポーンタイマーリセット
            respawnTimer = 0f;
            
            Debug.Log($"{gameObject.name} was destroyed");
        }
        
        protected virtual void DropItems()
        {
            if (dropItems == null || dropItems.Length == 0) return;
            
            foreach (var dropItem in dropItems)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= dropItem.dropChance)
                {
                    int dropAmount = UnityEngine.Random.Range(dropItem.minAmount, dropItem.maxAmount + 1);
                    
                    for (int i = 0; i < dropAmount; i++)
                    {
                        CreateDroppedItem(dropItem.itemName);
                    }
                }
            }
        }
        
        protected virtual void CreateDroppedItem(string itemName)
        {
            // DroppedItemプレハブが存在する場合
            GameObject droppedItemPrefab = Resources.Load<GameObject>("Prefabs/DroppedItem");
            if (droppedItemPrefab != null)
            {
                Vector3 dropPosition = transform.position + UnityEngine.Random.insideUnitSphere * dropRadius;
                dropPosition.y = transform.position.y + 0.5f;
                
                GameObject droppedItem = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);
                DroppedItem droppedItemComponent = droppedItem.GetComponent<DroppedItem>();
                
                if (droppedItemComponent != null)
                {
                    droppedItemComponent.Initialize(itemName, 1);
                    
                    // ドロップ時の物理的な動きを追加
                    Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
                        randomDirection.y = Mathf.Abs(randomDirection.y) + 0.5f;
                        rb.AddForce(randomDirection * dropForce, ForceMode.Impulse);
                    }
                }
            }
            else
            {
                // プレハブが存在しない場合は直接インベントリに追加
                var resourceManager = EnhancedResourceManager.Instance;
                if (resourceManager != null)
                {
                    resourceManager.AddItem(itemName, 1);
                }
            }
        }
        
        public virtual void Respawn()
        {
            if (!isDestroyed) return;
            
            isDestroyed = false;
            currentHealth = maxHealth;
            respawnTimer = 0f;
            
            // 元の位置に戻す
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            
            // オブジェクトを再表示
            gameObject.SetActive(true);
            
            OnRespawned?.Invoke(this);
            
            Debug.Log($"{gameObject.name} respawned");
        }
        
        public virtual void SetRespawnTime(float time)
        {
            respawnTime = time;
        }
        
        public virtual float GetRespawnProgress()
        {
            if (!isDestroyed || respawnTime <= 0) return 0f;
            return respawnTimer / respawnTime;
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            // ドロップ範囲を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, dropRadius);
        }
    }
    
    [Serializable]
    public class DropItem
    {
        public string itemName;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 1f)]
        public float dropChance = 1f;
    }
}