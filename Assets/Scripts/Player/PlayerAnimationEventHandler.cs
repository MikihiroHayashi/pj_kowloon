using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Player
{
    /// <summary>
    /// プレイヤーのアニメーションイベントを処理するクラス
    /// アニメーションファイルからイベントを受け取り、適切な処理を実行
    /// </summary>
    public class PlayerAnimationEventHandler : MonoBehaviour
    {
        [Header("Tool Usage Settings")]
        [SerializeField] private bool enableToolUsageEvents = true;
        
        private EnhancedPlayerController playerController;
        
        private void Awake()
        {
            playerController = GetComponentInParent<EnhancedPlayerController>();
            if (playerController == null)
            {
                playerController = GetComponent<EnhancedPlayerController>();
            }
            
            if (playerController == null)
            {
                Debug.LogError("[PlayerAnimationEventHandler] EnhancedPlayerController not found!");
            }
        }
        
        /// <summary>
        /// 攻撃のタイミング（アニメーションイベントから呼ばれる - Attack用）
        /// </summary>
        public void OnAttackHit()
        {
            if (!enableToolUsageEvents || playerController == null) return;
            
            Debug.Log("[PlayerAnimationEventHandler] OnAttackHit - Attack animation event triggered");
            playerController.ExecuteToolUsageEffect();
        }
        
        /// <summary>
        /// 掘削のタイミング（アニメーションイベントから呼ばれる - Dig用）
        /// </summary>
        public void OnDigHit()
        {
            if (!enableToolUsageEvents || playerController == null) return;
            
            Debug.Log("[PlayerAnimationEventHandler] OnDigHit - Dig animation event triggered");
            playerController.ExecuteToolUsageEffect();
        }
        
        /// <summary>
        /// 攻撃アニメーション終了時（Attack用）
        /// </summary>
        public void OnAttackEnd()
        {
            if (!enableToolUsageEvents || playerController == null) return;
            
            Debug.Log("[PlayerAnimationEventHandler] OnAttackEnd - Attack animation ended");
            playerController.OnToolUsageAnimationEnd();
        }
        
        /// <summary>
        /// 掘削アニメーション終了時（Dig用）
        /// </summary>
        public void OnDigEnd()
        {
            if (!enableToolUsageEvents || playerController == null) return;
            
            Debug.Log("[PlayerAnimationEventHandler] OnDigEnd - Dig animation ended");
            playerController.OnToolUsageAnimationEnd();
        }
        
        /// <summary>
        /// 足音イベント（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnFootstep()
        {
            if (playerController == null) return;
            
            // 足音再生処理
            playerController.PlayFootstepFromAnimation();
        }
        
        /// <summary>
        /// カスタムアニメーションイベント（汎用）
        /// </summary>
        /// <param name="eventName">イベント名</param>
        public void OnCustomEvent(string eventName)
        {
            if (playerController == null) return;
            
            Debug.Log($"[PlayerAnimationEventHandler] Custom event triggered: {eventName}");
            playerController.HandleCustomAnimationEvent(eventName);
        }
    }
}