using UnityEngine;
using System.Collections.Generic;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Managers;

namespace KowloonBreak.Player
{
    /// <summary>
    /// 統合ツール使用システム
    /// 全てのツール使用（採掘、攻撃、破壊等）を統一的に処理
    /// </summary>
    public class ToolInteractionSystem : MonoBehaviour
    {
        [Header("Tool Interaction Settings")]
        [SerializeField] private LayerMask destructibleLayers = -1;
        
        [Header("Range Settings")]
        [SerializeField] private float defaultAttackRange = 2f;
        [SerializeField] private Vector3 miningBoxSize = new Vector3(2f, 2f, 3f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showGizmos = true;
        
        // 参照
        private Transform playerTransform;
        private Transform toolUsagePoint;
        
        // イベント
        public System.Action<IDestructible, ToolType> OnTargetInteracted;
        public System.Action<ToolType, bool> OnToolUsageResult;
        
        // アニメーション連携用
        private PendingAction pendingAction;
        
        private struct PendingAction
        {
            public IDestructible[] targets;
            public InventorySlot tool;
            public ToolInteractionType interactionType;
            public Vector3 position;
            
            public bool IsValid => targets != null && tool != null && !tool.IsEmpty;
            
            public static PendingAction Empty => new PendingAction();
        }
        
        private enum ToolInteractionType
        {
            Attack,     // 球体範囲攻撃
            Mining,     // 箱型範囲採掘
            Generic     // 汎用破壊
        }
        
        private void Awake()
        {
            playerTransform = transform;
        }
        
        private void Start()
        {
            // ツール使用ポイントを取得
            SetupToolUsagePoint();
        }
        
        private void SetupToolUsagePoint()
        {
            // EnhancedPlayerControllerからtoolUsagePointを取得
            var playerController = GetComponent<EnhancedPlayerController>();
            if (playerController != null)
            {
                toolUsagePoint = playerController.ToolUsagePoint;
            }
            
            if (toolUsagePoint == null)
            {
                toolUsagePoint = playerTransform;
            }
        }
        
        /// <summary>
        /// ツール使用の準備（アニメーション開始前）
        /// </summary>
        public bool PrepareToolAction(InventorySlot tool)
        {
            if (tool == null || tool.IsEmpty)
            {
                LogDebug("No valid tool for interaction");
                return false;
            }
            
            var interactionType = GetInteractionType(tool.ItemData.toolType);
            var targets = FindTargetsForTool(tool, interactionType);
            
            if (targets.Length == 0)
            {
                LogDebug($"No valid targets found for {tool.ItemData.toolType}");
                OnToolUsageResult?.Invoke(tool.ItemData.toolType, false);
                return false;
            }
            
            // アニメーション用に準備
            pendingAction = new PendingAction
            {
                targets = targets,
                tool = tool,
                interactionType = interactionType,
                position = toolUsagePoint.position
            };
            
            LogDebug($"Prepared {interactionType} action with {targets.Length} targets using {tool.ItemData.toolType}");
            return true;
        }
        
        /// <summary>
        /// ツール使用の実行（アニメーションイベントから呼び出し）
        /// </summary>
        public void ExecuteToolAction()
        {
            if (!pendingAction.IsValid)
            {
                LogDebug("No valid pending action to execute");
                return;
            }
            
            bool success = ProcessTargets(pendingAction.targets, pendingAction.tool, pendingAction.interactionType);
            
            OnToolUsageResult?.Invoke(pendingAction.tool.ItemData.toolType, success);
            
            // 保留状態をクリア
            ClearPendingAction();
        }
        
        /// <summary>
        /// 保留中のアクションをクリア
        /// </summary>
        public void ClearPendingAction()
        {
            if (pendingAction.IsValid)
            {
                LogDebug("Clearing pending tool action");
                pendingAction = PendingAction.Empty;
            }
        }
        
        private ToolInteractionType GetInteractionType(ToolType toolType)
        {
            return toolType switch
            {
                ToolType.Pickaxe => ToolInteractionType.Mining,
                ToolType.IronPipe => ToolInteractionType.Attack,
                _ => ToolInteractionType.Generic
            };
        }
        
        private IDestructible[] FindTargetsForTool(InventorySlot tool, ToolInteractionType interactionType)
        {
            return interactionType switch
            {
                ToolInteractionType.Mining => FindMiningTargets(),
                ToolInteractionType.Attack => FindAttackTargets(tool.ItemData.attackRange),
                ToolInteractionType.Generic => FindGenericTargets(tool.ItemData.attackRange),
                _ => new IDestructible[0]
            };
        }
        
        private IDestructible[] FindMiningTargets()
        {
            // 箱型検索で採掘可能オブジェクトを検索
            Vector3 boxCenter = playerTransform.position + Vector3.up * 1.5f + playerTransform.forward * (miningBoxSize.z / 2f);
            Collider[] colliders = Physics.OverlapBox(boxCenter, miningBoxSize / 2f, playerTransform.rotation, destructibleLayers);
            
            return ExtractDestructiblesFromColliders(colliders, "Mining");
        }
        
        private IDestructible[] FindAttackTargets(float range)
        {
            // EnhancedPlayerControllerの球体検索を継承
            if (range <= 0) range = defaultAttackRange;
            
            Collider[] colliders = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);
            
            return ExtractDestructiblesFromColliders(colliders, "Attack");
        }
        
        private IDestructible[] FindGenericTargets(float range)
        {
            // 汎用的な球体検索
            if (range <= 0) range = defaultAttackRange;
            
            Collider[] colliders = Physics.OverlapSphere(toolUsagePoint.position, range, destructibleLayers);
            
            return ExtractDestructiblesFromColliders(colliders, "Generic");
        }
        
        private IDestructible[] ExtractDestructiblesFromColliders(Collider[] colliders, string searchType)
        {
            var targets = new List<IDestructible>();
            
            LogDebug($"{searchType} search found {colliders.Length} colliders");
            
            foreach (var collider in colliders)
            {
                // プレイヤー自身をスキップ
                if (collider.transform.root == playerTransform.root)
                    continue;
                
                // IDestructibleコンポーネントを検索
                var destructible = collider.GetComponent<IDestructible>();
                if (destructible == null)
                {
                    destructible = collider.GetComponentInParent<IDestructible>();
                }
                
                if (destructible != null && !targets.Contains(destructible))
                {
                    targets.Add(destructible);
                    LogDebug($"{searchType}: Added target {collider.name}");
                }
            }
            
            LogDebug($"{searchType}: Final target count: {targets.Count}");
            return targets.ToArray();
        }
        
        private bool ProcessTargets(IDestructible[] targets, InventorySlot tool, ToolInteractionType interactionType)
        {
            if (targets.Length == 0) return false;
            
            bool success = false;
            var toolData = tool.ItemData;
            
            // ステルス状態チェック（EnhancedPlayerControllerから取得）
            bool isStealthAttack = IsInStealthMode();
            
            foreach (var target in targets)
            {
                if (target.CanBeDestroyedBy(toolData.toolType))
                {
                    // ダメージ計算
                    float damage = toolData.attackDamage;
                    if (isStealthAttack && interactionType == ToolInteractionType.Attack)
                    {
                        damage *= 3f; // ステルス攻撃ボーナス
                    }
                    
                    // ダメージ適用（攻撃者情報も考慮）
                    ApplyDamageToTarget(target, damage, toolData.toolType);
                    
                    // イベント発火
                    OnTargetInteracted?.Invoke(target, toolData.toolType);
                    
                    success = true;
                    
                    LogDebug($"Successfully interacted with {target} using {toolData.toolType} (Damage: {damage})");
                }
                else
                {
                    LogDebug($"Cannot destroy {target} with {toolData.toolType}");
                }
            }
            
            // ツール耐久度の消費（何かしらヒットした場合のみ）
            if (success)
            {
                bool toolStillUsable = tool.UseDurability(1);
                if (!toolStillUsable)
                {
                    LogDebug($"Tool {toolData.itemName} broke!");
                }
                
                // ステルス攻撃イベント発火
                if (isStealthAttack && interactionType == ToolInteractionType.Attack)
                {
                    NotifyStealthAttack(toolData.attackDamage * 3f);
                }
            }
            
            return success;
        }
        
        private void ApplyDamageToTarget(IDestructible target, float damage, ToolType toolType)
        {
            // 敵の場合は攻撃者情報を渡す
            if (target is KowloonBreak.Enemies.EnemyBase enemy)
            {
                enemy.TakeDamage(damage, toolType, transform);
            }
            else
            {
                target.TakeDamage(damage, toolType);
            }
        }
        
        private bool IsInStealthMode()
        {
            var playerController = GetComponent<EnhancedPlayerController>();
            return playerController != null && playerController.IsInStealthMode();
        }
        
        private void NotifyStealthAttack(float damage)
        {
            var playerController = GetComponent<EnhancedPlayerController>();
            if (playerController != null)
            {
                // ステルス攻撃イベントを発火
                playerController.TriggerStealthAttackEvent(damage);
                LogDebug($"Stealth attack executed with {damage} damage");
            }
        }
        
        private void LogDebug(string message)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[ToolInteractionSystem] {message}");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || playerTransform == null) return;
            
            // 採掘範囲の可視化
            Vector3 boxCenter = playerTransform.position + Vector3.up * 1.5f + playerTransform.forward * (miningBoxSize.z / 2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, playerTransform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, miningBoxSize);
            Gizmos.matrix = Matrix4x4.identity;
            
            // 攻撃範囲の可視化
            if (toolUsagePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(toolUsagePoint.position, defaultAttackRange);
            }
        }
    }
}