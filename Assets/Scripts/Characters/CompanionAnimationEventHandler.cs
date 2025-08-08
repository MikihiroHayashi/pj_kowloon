using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Characters
{
    /// <summary>
    /// コンパニオンのアニメーションイベントを処理するクラス
    /// アニメーションファイルからイベントを受け取り、適切な処理を実行
    /// </summary>
    public class CompanionAnimationEventHandler : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private bool enableCombatEvents = true;
        
        private CompanionAI companionAI;
        private CompanionCharacter companionCharacter;
        
        private void Awake()
        {
            // 親オブジェクトまたは同じオブジェクトからCompanionAIを取得
            companionAI = GetComponentInParent<CompanionAI>();
            if (companionAI == null)
            {
                companionAI = GetComponent<CompanionAI>();
            }
            
            // CompanionCharacterも取得
            companionCharacter = GetComponentInParent<CompanionCharacter>();
            if (companionCharacter == null)
            {
                companionCharacter = GetComponent<CompanionCharacter>();
            }
            
            if (companionAI == null)
            {
                Debug.LogError($"[CompanionAnimationEventHandler] CompanionAI not found on {gameObject.name}!");
            }
            
            if (companionCharacter == null)
            {
                Debug.LogError($"[CompanionAnimationEventHandler] CompanionCharacter not found on {gameObject.name}!");
            }
        }
        
        /// <summary>
        /// 攻撃のタイミング（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnAttackHit()
        {
            if (!enableCombatEvents || companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnAttackHit - {gameObject.name} attack animation event triggered");
            
            // プレイヤーシステムと同等のExecuteToolUsageEffectを呼び出し
            companionAI.ExecuteToolUsageEffect();
        }
        
        /// <summary>
        /// 攻撃アニメーション終了時
        /// </summary>
        public void OnAttackEnd()
        {
            if (!enableCombatEvents || companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnAttackEnd - {gameObject.name} attack animation ended");
            
            // 攻撃終了後の処理があれば実装
            // 例：クールタイムの設定、次の行動の決定など
        }
        
        /// <summary>
        /// 足音イベント（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnFootstep()
        {
            if (companionAI == null) return;
            
            // 足音再生処理（必要に応じて実装）
            Debug.Log($"[CompanionAnimationEventHandler] OnFootstep - {gameObject.name} footstep");
        }
        
        /// <summary>
        /// ダッジロール開始時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnDodgeStart()
        {
            if (companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnDodgeStart - {gameObject.name} dodge started");
            
            // ダッジロール中は一時的に無敵状態にする（必要に応じて実装）
        }
        
        /// <summary>
        /// ダッジロール終了時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnDodgeEnd()
        {
            if (companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnDodgeEnd - {gameObject.name} dodge ended");
            
            // ダッジロール終了後の処理
        }
        
        /// <summary>
        /// 特殊アクション開始時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnSpecialActionStart()
        {
            if (companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnSpecialActionStart - {gameObject.name} special action started");
            
            // 特殊アクション開始処理（キャラクターの役割に応じた処理）
            HandleSpecialActionByRole();
        }
        
        /// <summary>
        /// 特殊アクション終了時（アニメーションイベントから呼ばれる）
        /// </summary>
        public void OnSpecialActionEnd()
        {
            if (companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] OnSpecialActionEnd - {gameObject.name} special action ended");
        }
        
        /// <summary>
        /// 役割に応じた特殊アクション処理
        /// </summary>
        private void HandleSpecialActionByRole()
        {
            if (companionCharacter == null) return;
            
            switch (companionCharacter.Role)
            {
                case CharacterRole.Fighter:
                    // 戦闘特化：強力な攻撃やコンボ
                    Debug.Log($"{gameObject.name} executing fighter special attack");
                    break;
                    
                case CharacterRole.Scout:
                    // 探索特化：索敵や隠密行動
                    Debug.Log($"{gameObject.name} executing scout special ability");
                    break;
                    
                case CharacterRole.Medic:
                    // 治療特化：回復や支援
                    Debug.Log($"{gameObject.name} executing medic healing");
                    break;
                    
                case CharacterRole.Engineer:
                    // 建設特化：修理や強化
                    Debug.Log($"{gameObject.name} executing engineer repair");
                    break;
                    
                case CharacterRole.Negotiator:
                    // 交渉特化：敵の動きを制限
                    Debug.Log($"{gameObject.name} executing negotiator ability");
                    break;
            }
        }
        
        /// <summary>
        /// カスタムアニメーションイベント（汎用）
        /// </summary>
        /// <param name="eventName">イベント名</param>
        public void OnCustomEvent(string eventName)
        {
            if (companionAI == null) return;
            
            Debug.Log($"[CompanionAnimationEventHandler] Custom event triggered on {gameObject.name}: {eventName}");
            
            // カスタムイベントの処理
            HandleCustomEvent(eventName);
        }
        
        /// <summary>
        /// カスタムイベントの処理
        /// </summary>
        /// <param name="eventName">イベント名</param>
        private void HandleCustomEvent(string eventName)
        {
            switch (eventName.ToLower())
            {
                case "damage":
                case "hit":
                    OnAttackHit();
                    break;
                    
                case "attackend":
                case "combatend":
                    OnAttackEnd();
                    break;
                    
                case "footstep":
                case "step":
                    OnFootstep();
                    break;
                    
                case "dodgestart":
                    OnDodgeStart();
                    break;
                    
                case "dodgeend":
                    OnDodgeEnd();
                    break;
                    
                case "special":
                case "ability":
                    OnSpecialActionStart();
                    break;
                    
                default:
                    Debug.LogWarning($"[CompanionAnimationEventHandler] Unknown custom event: {eventName}");
                    break;
            }
        }
    }
}