using UnityEngine;

namespace KowloonBreak.Enemies
{
    public class BasicEnemy : EnemyBase
    {
        protected override void Awake()
        {
            base.Awake();
            
            // 基本的な敵の設定
            maxHealth = 50f;
            currentHealth = maxHealth;
            attackDamage = 15f;
            moveSpeed = 2.5f;
            attackRange = 1.5f;
            detectionRange = 8f;
            attackCooldown = 1.5f;
        }
        
        protected override void PerformAttack()
        {
            base.PerformAttack();
            
            // 基本攻撃の追加処理
            // プレイヤーへのダメージ処理をここに実装
        }
    }
}