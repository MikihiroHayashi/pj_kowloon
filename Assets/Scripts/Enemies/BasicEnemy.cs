using UnityEngine;

namespace KowloonBreak.Enemies
{
    public class BasicEnemy : EnemyBase
    {
        protected override void Awake()
        {
            base.Awake();

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