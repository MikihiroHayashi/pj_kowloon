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
        [SerializeField] protected ItemDropData[] dropItems;  // ItemDataベースのドロップシステム
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
            Debug.Log($"[DestructibleObject] {gameObject.name} - CanBeDestroyedBy({toolType})");
            Debug.Log($"[DestructibleObject] {gameObject.name} - IsDestroyed: {isDestroyed}");
            
            if (isDestroyed) 
            {
                Debug.Log($"[DestructibleObject] {gameObject.name} - Object is already destroyed");
                return false;
            }
            
            if (allowedTools == null || allowedTools.Length == 0)
            {
                Debug.Log($"[DestructibleObject] {gameObject.name} - No tool restrictions, allowing all tools");
                return true;
            }
            
            Debug.Log($"[DestructibleObject] {gameObject.name} - Allowed tools: [{string.Join(", ", allowedTools)}]");
                
            foreach (var tool in allowedTools)
            {
                if (tool == toolType)
                {
                    Debug.Log($"[DestructibleObject] {gameObject.name} - Tool {toolType} is allowed");
                    return true;
                }
            }
            
            Debug.Log($"[DestructibleObject] {gameObject.name} - Tool {toolType} is not allowed");
            return false;
        }
        
        public virtual void TakeDamage(float damage, ToolType toolType)
        {
            Debug.Log($"[DestructibleObject] {gameObject.name} - TakeDamage({damage}, {toolType})");
            Debug.Log($"[DestructibleObject] {gameObject.name} - Current health before damage: {currentHealth}/{maxHealth}");
            
            if (isDestroyed)
            {
                Debug.Log($"[DestructibleObject] {gameObject.name} - Cannot take damage, object is destroyed");
                return;
            }
            
            if (!CanBeDestroyedBy(toolType))
            {
                Debug.Log($"[DestructibleObject] {gameObject.name} - Cannot take damage, tool {toolType} not allowed");
                return;
            }
                
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            Debug.Log($"[DestructibleObject] {gameObject.name} - Health after damage: {currentHealth}/{maxHealth}");
            
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
                Debug.Log($"[DestructibleObject] {gameObject.name} - Health reached zero, destroying object");
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
            if (dropItems == null || dropItems.Length == 0)
            {
                return;
            }
            
            foreach (var dropData in dropItems)
            {
                if (!dropData.IsValid()) 
                {
                    continue;
                }
                
                if (UnityEngine.Random.Range(0f, 1f) <= dropData.dropChance)
                {
                    int dropAmount = UnityEngine.Random.Range(dropData.minAmount, dropData.maxAmount + 1);
                    
                    for (int i = 0; i < dropAmount; i++)
                    {
                        CreateDroppedItem(dropData);
                    }
                }
            }
        }
        
        protected virtual void CreateDroppedItem(ItemDropData dropData)
        {
            if (dropData?.itemData == null)
            {
                return;
            }
            
            GameObject prefab = dropData.GetPrefab();
            if (prefab == null)
            {
                Debug.LogError($"[DestructibleObject] No prefab assigned to {dropData.GetItemName()}!");
                return;
            }
            
            // ドロップ位置を設定（オブジェクトの中心から少し上）
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
            GameObject droppedItem = Instantiate(prefab, dropPosition, Quaternion.identity);
            
            DroppedItem droppedItemComponent = droppedItem.GetComponent<DroppedItem>();
            if (droppedItemComponent != null)
            {
                droppedItemComponent.Initialize(dropData.GetItemName(), 1);
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
    
}