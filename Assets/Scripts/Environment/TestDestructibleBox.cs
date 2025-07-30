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
            
            
            // ドロップアイテム設定の確認
            
            ValidateReferences();
        }
        
        private void ValidateReferences()
        {
        }
        
        
        protected override void OnHit(float damage, ToolType toolType)
        {
            base.OnHit(damage, toolType);
            
            
            // ダメージエフェクトを開始
            if (modelRenderer != null && modelRenderer.material != null)
            {
                StartCoroutine(DamageEffect());
            }
        }
        
        private System.Collections.IEnumerator DamageEffect()
        {
            // _Damage_Amountを1に設定
            modelRenderer.material.SetFloat("_Damage_Amount", 1f);
            
            // 0.1秒待機
            yield return new WaitForSeconds(0.1f);
            
            // _Damage_Amountを0に戻す
            if (modelRenderer != null && modelRenderer.material != null)
            {
                modelRenderer.material.SetFloat("_Damage_Amount", 0f);
            }
        }
        
        protected override void DestroyObject()
        {
            
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
            
        }
    }
}