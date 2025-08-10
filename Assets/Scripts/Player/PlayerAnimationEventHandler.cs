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
            // EnhancedPlayerControllerのInitializePlayerで設定されるため、
            // Awake時点ではnullが正常な状態
        }

        private void Start()
        {
            // 1フレーム遅らせてから確認（EnhancedPlayerControllerの初期化完了を待つ）
            StartCoroutine(LateInitialization());
        }

        private System.Collections.IEnumerator LateInitialization()
        {
            // 1フレーム待機
            yield return null;
            
            // PlayerControllerの確認
            if (playerController == null)
            {
                Debug.LogError("[PlayerAnimationEventHandler] PlayerController still null after late initialization. EnhancedPlayerController may not be present or SetPlayerController() was not called.");
                
                // 最後の手段として直接検索を試す
                var controller = GetComponentInParent<EnhancedPlayerController>();
                if (controller == null)
                {
                    controller = GetComponent<EnhancedPlayerController>();
                }
                
                if (controller != null)
                {
                    Debug.LogWarning("[PlayerAnimationEventHandler] Found EnhancedPlayerController via fallback search. Setting reference manually.");
                    playerController = controller;
                }
                else
                {
                    Debug.LogError("[PlayerAnimationEventHandler] Could not find EnhancedPlayerController even with fallback search!");
                }
            }
            // PlayerController正常設定時は特にログ出力不要
        }

        /// <summary>
        /// EnhancedPlayerControllerから呼び出されてプレイヤー参照を設定
        /// </summary>
        public void SetPlayerController(EnhancedPlayerController controller)
        {
            playerController = controller;
        }
        
        /// <summary>
        /// 攻撃のタイミング（アニメーションイベントから呼ばれる - Attack用）
        /// </summary>
        public void OnAttackHit()
        {
            if (!enableToolUsageEvents || playerController == null) 
            {
                if (playerController == null)
                    Debug.LogWarning("[PlayerAnimationEventHandler] OnAttackHit called but playerController is null!");
                return;
            }
            
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
            if (playerController == null) 
            {
                Debug.LogWarning("[PlayerAnimationEventHandler] OnFootstep called but playerController is null!");
                return;
            }
            
            // 足音再生処理
            playerController.PlayFootstepFromAnimation();
        }
        
        /// <summary>
        /// ダッジの無敵時間開始（アニメーションイベントから呼ばれる）
        /// </summary>
        public void StartDodgeInvincibility()
        {
            if (playerController == null) return;
            
            playerController.SetInvincible(true);
        }
        
        /// <summary>
        /// ダッジの無敵時間終了（アニメーションイベントから呼ばれる）
        /// </summary>
        public void EndDodgeInvincibility()
        {
            if (playerController == null) return;
            
            playerController.SetInvincible(false);
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