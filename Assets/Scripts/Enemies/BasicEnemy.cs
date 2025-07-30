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
            
            // パトロール設定
            enablePatrol = true;
            lostPlayerReturnDelay = 3f; // 基本敵は早めに復帰
            
            // 速度設定（基本敵の特性：Move Speedがベース）
            patrolSpeedMultiplier = 0.7f;   // パトロール時：Move Speed × 0.7 = 1.75
            chaseSpeedMultiplier = 1.5f;    // 追跡時：Move Speed × 1.5 = 3.75  
            returnSpeedMultiplier = 1.0f;   // 復帰時：Move Speed × 1.0 = 2.5
        }
        
        protected override void PerformAttack()
        {
            base.PerformAttack();
            
            // 基本攻撃の追加処理
        }
        
        protected override void OnPatrolPointReached(int pointIndex)
        {
            base.OnPatrolPointReached(pointIndex);
            
            // パトロールポイント到着時の特別な処理
            // 例：周囲を見回す、特定の音を再生するなど
        }
        
        protected override void OnStateChanged(EnemyState from, EnemyState to)
        {
            base.OnStateChanged(from, to);
            
            // 基本敵固有の状態変更処理
            switch (to)
            {
                case EnemyState.Chase:
                    // 追跡開始時の処理（音やエフェクトなど）
                    break;
                case EnemyState.Return:
                    // 復帰開始時の処理
                    break;
            }
        }
    }
}