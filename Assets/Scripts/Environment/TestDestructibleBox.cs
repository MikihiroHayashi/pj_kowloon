using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Environment
{
    public class TestDestructibleBox : DestructibleObject
    {
        [Header("References")]
        [SerializeField] private Renderer modelRenderer;
        [SerializeField] private Collider objectCollider;
        
        protected override void Awake()
        {
            base.Awake();
            
            // デフォルト設定
            maxHealth = 50f;
            currentHealth = maxHealth;
            
            // 許可されるツール
            allowedTools = new ToolType[] { ToolType.Pickaxe, ToolType.IronPipe };
            
            // ドロップの物理設定
            dropRadius = 1.5f;
            dropForce = 3f;
            
            // リスポーン設定
            respawnTime = 30f;
            
            Debug.Log($"[TestDestructibleBox] {gameObject.name} initialized");
            Debug.Log($"[TestDestructibleBox] {gameObject.name} - Layer: {gameObject.layer}");
            Debug.Log($"[TestDestructibleBox] {gameObject.name} - Health: {currentHealth}/{maxHealth}");
            Debug.Log($"[TestDestructibleBox] {gameObject.name} - Allowed tools: [{string.Join(", ", allowedTools)}]");
            
            // ドロップアイテム設定の確認
            if (dropItems == null || dropItems.Length == 0)
            {
                Debug.LogError($"[TestDestructibleBox] {gameObject.name} - No drop items configured! Please set dropItems in inspector.");
            }
            
            ValidateReferences();
        }
        
        private void ValidateReferences()
        {
            // 参照チェック
            if (modelRenderer == null)
            {
                Debug.LogError("[TestDestructibleBox] Model Renderer is not assigned in the inspector!");
            }
            
            if (objectCollider == null)
            {
                Debug.LogError("[TestDestructibleBox] Object Collider is not assigned in the inspector!");
            }
        }
        
        
        protected override void OnHit(float damage, ToolType toolType)
        {
            base.OnHit(damage, toolType);
            
            Debug.Log($"[TestDestructibleBox] OnHit called - Health: {currentHealth}/{maxHealth} with {toolType}");
            
            // ダメージエフェクトを開始
            if (modelRenderer != null && modelRenderer.material != null)
            {
                Debug.Log($"[TestDestructibleBox] Starting damage effect on {modelRenderer.name}");
                Debug.Log($"[TestDestructibleBox] Material has '_Damage_Amount' property: {modelRenderer.material.HasProperty("_Damage_Amount")}");
                StartCoroutine(DamageEffect());
            }
            else
            {
                Debug.LogError($"[TestDestructibleBox] Cannot start damage effect - modelRenderer: {modelRenderer != null}, material: {modelRenderer?.material != null}");
            }
        }
        
        private System.Collections.IEnumerator DamageEffect()
        {
            Debug.Log("[TestDestructibleBox] DamageEffect started");
            
            // _Damage_Amountを1に設定
            modelRenderer.material.SetFloat("_Damage_Amount", 1f);
            Debug.Log("[TestDestructibleBox] Set _Damage_Amount to 1.0");
            
            // 0.1秒待機
            yield return new WaitForSeconds(0.1f);
            
            // _Damage_Amountを0に戻す
            if (modelRenderer != null && modelRenderer.material != null)
            {
                modelRenderer.material.SetFloat("_Damage_Amount", 0f);
                Debug.Log("[TestDestructibleBox] Reset _Damage_Amount to 0.0");
            }
            
            Debug.Log("[TestDestructibleBox] DamageEffect completed");
        }
        
        protected override void DestroyObject()
        {
            Debug.Log("Test Box is being destroyed!");
            
            base.DestroyObject();
        }
        
        public override void Respawn()
        {
            base.Respawn();
            
            // リスポーン時に_Damage_Amountをリセット
            if (modelRenderer != null && modelRenderer.material != null)
            {
                modelRenderer.material.SetFloat("_Damage_Amount", 0f);
            }
            
            Debug.Log("Test Box respawned!");
        }
    }
}