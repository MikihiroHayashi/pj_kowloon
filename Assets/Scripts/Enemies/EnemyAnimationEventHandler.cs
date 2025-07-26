using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Enemies
{
    /// <summary>
    /// エネミーのアニメーションイベントを処理するクラス
    /// アニメーションファイルからイベントを受け取り、適切な処理を実行
    /// </summary>
    public class EnemyAnimationEventHandler : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField] private bool enableAttackEvents = true;
        
        private EnemyBase enemyBase;
        
        private void Awake()
        {
            enemyBase = GetComponentInParent<EnemyBase>();
            if (enemyBase == null)
            {
                enemyBase = GetComponent<EnemyBase>();
            }
            
            if (enemyBase == null)
            {
                Debug.LogError("[EnemyAnimationEventHandler] EnemyBase not found!");
            }
        }
        
        /// <summary>
        /// 攻撃のダメージ判定タイミング（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnAttackHit()
        {
            if (!enableAttackEvents || enemyBase == null) return;
            
            Debug.Log("[EnemyAnimationEventHandler] OnAttackHit - Animation event triggered");
            enemyBase.ExecuteAttackDamage();
        }
        
        /// <summary>
        /// 攻撃開始時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnAttackStart()
        {
            if (!enableAttackEvents || enemyBase == null) return;
            
            Debug.Log("[EnemyAnimationEventHandler] OnAttackStart - Animation event triggered");
            // 攻撃開始時の処理（音響効果など）
        }
        
        /// <summary>
        /// 攻撃終了時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnAttackEnd()
        {
            if (!enableAttackEvents || enemyBase == null) return;
            
            Debug.Log("[EnemyAnimationEventHandler] OnAttackEnd - Animation event triggered");
            enemyBase.OnAttackAnimationEnd();
        }
        
        /// <summary>
        /// 足音イベント（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnFootstep()
        {
            if (enemyBase == null) return;
            
            // 足音再生処理
            enemyBase.PlayFootstepFromAnimation();
        }
        
        /// <summary>
        /// 死亡アニメーション完了時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnDeathComplete()
        {
            if (enemyBase == null) return;
            
            Debug.Log("[EnemyAnimationEventHandler] OnDeathComplete - Animation event triggered");
            enemyBase.OnDeathAnimationComplete();
        }
        
        /// <summary>
        /// カスタムアニメーションイベント（汎用）
        /// </summary>
        /// <param name="eventName">イベント名</param>
        public void OnCustomEvent(string eventName)
        {
            if (enemyBase == null) return;
            
            Debug.Log($"[EnemyAnimationEventHandler] Custom event triggered: {eventName}");
            enemyBase.HandleCustomAnimationEvent(eventName);
        }
    }
}