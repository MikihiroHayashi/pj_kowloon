using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;

namespace KowloonBreak.Characters
{
    /// <summary>
    /// コンパニオン用の簡易MiningSystem
    /// プレイヤーのMiningSystemと同等の機能を提供
    /// </summary>
    public class CompanionMiningSystem : MonoBehaviour
    {
        [Header("Mining Settings")]
        [SerializeField] private float miningRange = 3f;
        [SerializeField] private Vector3 miningBoxSize = new Vector3(2f, 2f, 3f);
        [SerializeField] private LayerMask mineableLayerMask = -1;
        
        [Header("Tool Settings")]
        [SerializeField] private ToolType defaultToolType = ToolType.IronPipe;
        [SerializeField] private float defaultDamage = 15f;
        
        private Transform companionTransform;
        private IDestructible pendingTarget;
        private ToolType pendingToolType;
        private float pendingDamage;
        
        private void Awake()
        {
            companionTransform = transform;
        }
        
        /// <summary>
        /// 現在のターゲットに対して攻撃準備
        /// </summary>
        public bool TryAttackWithTool(GameObject target)
        {
            if (target == null) 
            {
                Debug.LogWarning("[CompanionMiningSystem] TryAttackWithTool: target is null");
                return false;
            }
            
            Debug.Log($"[CompanionMiningSystem] TryAttackWithTool: attempting to attack {target.name}");
            
            // IDestructibleコンポーネントを探す（自身から親に向かって検索）
            var destructible = target.GetComponent<IDestructible>();
            if (destructible == null)
            {
                Debug.Log($"[CompanionMiningSystem] TryAttackWithTool: {target.name} does not have IDestructible, checking parent objects");
                destructible = target.GetComponentInParent<IDestructible>();
            }
            
            if (destructible == null) 
            {
                Debug.LogWarning($"[CompanionMiningSystem] TryAttackWithTool: {target.name} and its parents do not have IDestructible component");
                return false;
            }
            
            // 実際のターゲットオブジェクトを特定
            string targetName = destructible is MonoBehaviour mono ? mono.gameObject.name : target.name;
            Debug.Log($"[CompanionMiningSystem] TryAttackWithTool: found IDestructible on {targetName}, preparing attack");
            
            // 攻撃準備
            PrepareAttack(destructible, defaultToolType, defaultDamage);
            return true;
        }
        
        /// <summary>
        /// 範囲内の採掘可能オブジェクトを攻撃準備
        /// </summary>
        public bool TryAttackInRange()
        {
            var targets = FindMineableTargets();
            if (targets.Length > 0)
            {
                var closestTarget = FindClosestTarget(targets);
                if (closestTarget != null)
                {
                    PrepareAttack(closestTarget, defaultToolType, defaultDamage);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// アニメーション用に攻撃を準備
        /// </summary>
        private void PrepareAttack(IDestructible target, ToolType toolType, float damage)
        {
            pendingTarget = target;
            pendingToolType = toolType;
            pendingDamage = damage;
            
            Debug.Log($"[CompanionMiningSystem] Prepared attack: {target} with {toolType}, damage: {damage}");
        }
        
        /// <summary>
        /// アニメーションイベントから呼び出される実際のダメージ実行
        /// </summary>
        public void ExecuteMiningDamage()
        {
            if (pendingTarget == null)
            {
                Debug.Log("[CompanionMiningSystem] No pending attack to execute - target may have been destroyed or cleared");
                return;
            }
            
            // ターゲットが既に破壊されているかチェック
            if (pendingTarget.IsDestroyed)
            {
                Debug.Log("[CompanionMiningSystem] Pending target is already destroyed, skipping attack");
                pendingTarget = null;
                return;
            }
            
            // MonoBehaviourとして有効性をチェック（オブジェクトが削除されている場合）
            if (pendingTarget is MonoBehaviour targetMono && targetMono == null)
            {
                Debug.Log("[CompanionMiningSystem] Pending target GameObject has been destroyed, skipping attack");
                pendingTarget = null;
                return;
            }
            
            Debug.Log("[CompanionMiningSystem] Executing attack damage from animation event");
            AttemptAttack(pendingTarget, pendingToolType, pendingDamage);
            
            // クリア
            pendingTarget = null;
        }
        
        /// <summary>
        /// 保留状態をクリア
        /// </summary>
        public void ClearPendingAttack()
        {
            if (pendingTarget != null)
            {
                Debug.Log("[CompanionMiningSystem] Clearing pending attack");
                pendingTarget = null;
            }
        }
        
        /// <summary>
        /// 実際の攻撃処理
        /// </summary>
        private void AttemptAttack(IDestructible target, ToolType toolType, float damage)
        {
            Debug.Log($"[CompanionMiningSystem] Attempting to attack with tool: {toolType}");
            Debug.Log($"[CompanionMiningSystem] Tool damage: {damage}");
            
            // ターゲットがそのツールで破壊可能かチェック
            bool canDestroy = target.CanBeDestroyedBy(toolType);
            Debug.Log($"[CompanionMiningSystem] Can destroy target with {toolType}: {canDestroy}");
            
            if (canDestroy)
            {
                Debug.Log($"[CompanionMiningSystem] Dealing {damage} damage to target");
                
                // ダメージを与える
                target.TakeDamage(damage, toolType);
                
                Debug.Log($"[CompanionMiningSystem] Attack successful: Dealt {damage} damage to {target} with {toolType}");
            }
            else
            {
                Debug.Log($"[CompanionMiningSystem] Cannot attack this object with {toolType}");
            }
        }
        
        /// <summary>
        /// 範囲内の採掘可能ターゲットを検索
        /// </summary>
        private IDestructible[] FindMineableTargets()
        {
            Vector3 boxCenter = companionTransform.position + companionTransform.forward * (miningRange / 2f);
            Collider[] colliders = Physics.OverlapBox(boxCenter, miningBoxSize / 2f, companionTransform.rotation, mineableLayerMask);
            
            var targets = new System.Collections.Generic.List<IDestructible>();
            
            foreach (var collider in colliders)
            {
                if (collider.transform == companionTransform) continue;
                
                var destructible = collider.GetComponent<IDestructible>();
                if (destructible == null)
                {
                    destructible = collider.GetComponentInParent<IDestructible>();
                }
                
                if (destructible != null)
                {
                    targets.Add(destructible);
                }
            }
            
            return targets.ToArray();
        }
        
        /// <summary>
        /// 最も近いターゲットを取得
        /// </summary>
        private IDestructible FindClosestTarget(IDestructible[] targets)
        {
            if (targets.Length == 0) return null;
            
            IDestructible closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var target in targets)
            {
                if (target is MonoBehaviour targetMono)
                {
                    float distance = Vector3.Distance(companionTransform.position, targetMono.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = target;
                    }
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// デフォルトツールタイプを設定
        /// </summary>
        public void SetDefaultTool(ToolType toolType, float damage)
        {
            defaultToolType = toolType;
            defaultDamage = damage;
        }
    }
}