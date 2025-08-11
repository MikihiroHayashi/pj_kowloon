using UnityEngine;
using System.Collections.Generic;
using KowloonBreak.Environment;
using KowloonBreak.Managers;

namespace KowloonBreak.Core
{
    /// <summary>
    /// PlayerとCompanionで共通のツール使用システム基底クラス
    /// 採掘、攻撃、破壊等のツール使用を統一的に処理
    /// </summary>
    public abstract class BaseToolInteractionSystem : MonoBehaviour
    {
        [Header("Tool Interaction Settings")]
        [SerializeField] protected LayerMask destructibleLayers = -1;
        
        [Header("Range Settings")]
        [SerializeField] protected float defaultAttackRange = 2f;
        [SerializeField] protected Vector3 miningBoxSize = new Vector3(2f, 2f, 3f);
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = true;
        [SerializeField] protected bool showGizmos = true;
        
        // 参照
        protected Transform characterTransform;
        protected Transform toolUsagePoint;
        
        // イベント
        public System.Action<IDestructible, ToolType> OnTargetInteracted;
        public System.Action<ToolType, bool> OnToolUsageResult;
        
        // アニメーション連携用
        protected PendingAction pendingAction;
        
        protected struct PendingAction
        {
            public IDestructible[] targets;
            public InventorySlot tool;
            public ToolInteractionType interactionType;
            public Vector3 position;
            
            public bool IsValid => targets != null && (tool == null || !tool.IsEmpty);
            
            public static PendingAction Empty => new PendingAction();
        }
        
        protected enum ToolInteractionType
        {
            Mining,
            Attack,
            Special
        }
        
        protected virtual void Awake()
        {
            characterTransform = transform;
            
            // ツール使用ポイントを自動検索
            var toolPoint = transform.Find("ToolUsagePoint");
            if (toolPoint != null)
            {
                toolUsagePoint = toolPoint;
            }
            else
            {
                // フォールバック：キャラクターの前方
                toolUsagePoint = characterTransform;
            }
        }
        
        /// <summary>
        /// 現在のツールでターゲットを攻撃準備
        /// </summary>
        public virtual bool TryUseToolOnTarget(GameObject target, InventorySlot toolSlot)
        {
            if (target == null)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[{GetType().Name}] Invalid target");
                return false;
            }
            
            // Companionの場合toolSlotはnullの可能性がある
            if (toolSlot != null && toolSlot.IsEmpty)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[{GetType().Name}] Tool slot is empty");
                return false;
            }
            
            var destructibles = FindDestructiblesInTarget(target);
            if (destructibles.Count == 0)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[{GetType().Name}] No destructible components found on {target.name}");
                return false;
            }
            
            // アニメーション用に攻撃を準備
            pendingAction = new PendingAction
            {
                targets = destructibles.ToArray(),
                tool = toolSlot,
                interactionType = ToolInteractionType.Attack,
                position = target.transform.position
            };
            
            if (showDebugInfo)
                Debug.Log($"[{GetType().Name}] Prepared tool usage: {toolSlot?.ItemData?.name ?? "None"} on {target.name}");
            
            return true;
        }
        
        /// <summary>
        /// 範囲内の破壊可能オブジェクトにツール使用を準備
        /// </summary>
        public virtual bool TryUseToolInRange(InventorySlot toolSlot)
        {
            // Companionの場合toolSlotはnullの可能性がある
            if (toolSlot != null && toolSlot.IsEmpty)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[{GetType().Name}] Tool slot is empty");
                return false;
            }
            
            var targets = FindDestructiblesInRange();
            if (targets.Count == 0)
            {
                if (showDebugInfo)
                    Debug.Log($"[{GetType().Name}] No targets found in range");
                return false;
            }
            
            pendingAction = new PendingAction
            {
                targets = targets.ToArray(),
                tool = toolSlot,
                interactionType = ToolInteractionType.Mining,
                position = GetToolUsagePosition()
            };
            
            if (showDebugInfo)
                Debug.Log($"[{GetType().Name}] Prepared range tool usage: {toolSlot?.ItemData?.name ?? "None"}, {targets.Count} targets");
            
            return true;
        }
        
        /// <summary>
        /// アニメーションイベントから呼び出される実際のツール使用実行
        /// </summary>
        public virtual void ExecuteToolUsage()
        {
            if (!pendingAction.IsValid)
            {
                if (showDebugInfo)
                    Debug.LogWarning($"[{GetType().Name}] No valid pending action to execute");
                return;
            }
            
            var tool = pendingAction.tool;
            var targets = pendingAction.targets;
            
            string toolName = tool?.ItemData?.name ?? "DefaultTool";
            if (showDebugInfo)
                Debug.Log($"[{GetType().Name}] Executing tool usage: {toolName} on {targets.Length} targets");
            
            bool anySuccess = false;
            int totalHits = 0;
            
            foreach (var target in targets)
            {
                if (target == null || target.IsDestroyed) continue;
                
                // ツールタイプに応じたダメージを取得
                var toolData = GetToolData(tool);
                if (!toolData.HasValue) continue;
                
                var tool_data = toolData.Value;
                
                // ターゲットがそのツールで破壊可能かチェック
                if (!target.CanBeDestroyedBy(tool_data.ToolType))
                {
                    if (showDebugInfo)
                        Debug.Log($"[{GetType().Name}] {target} cannot be destroyed by {tool_data.ToolType}");
                    continue;
                }
                
                // ダメージを適用
                float damage = GetDamageForTool(tool_data);
                ApplyDamageToTarget(target, damage, tool_data.ToolType);
                
                totalHits++;
                anySuccess = true;
                
                // イベント通知
                OnTargetInteracted?.Invoke(target, tool_data.ToolType);
                
                if (showDebugInfo)
                    Debug.Log($"[{GetType().Name}] Applied {damage} damage to {target} with {tool_data.ToolType}");
            }
            
            // ツール耐久度を減らす
            if (totalHits > 0 && tool != null)
            {
                ConsumeTool(tool, totalHits);
            }
            
            // 結果を通知
            var toolTypeForEvent = GetToolTypeForEvent(tool);
            OnToolUsageResult?.Invoke(toolTypeForEvent, anySuccess);
            
            // 保留アクションをクリア
            pendingAction = PendingAction.Empty;
            
            if (showDebugInfo)
                Debug.Log($"[{GetType().Name}] Tool usage completed. Hits: {totalHits}, Success: {anySuccess}");
        }
        
        /// <summary>
        /// 保留中のアクションをクリア
        /// </summary>
        public virtual void ClearPendingAction()
        {
            pendingAction = PendingAction.Empty;
            if (showDebugInfo)
                Debug.Log($"[{GetType().Name}] Pending action cleared");
        }
        
        /// <summary>
        /// ターゲットから破壊可能コンポーネントを検索
        /// </summary>
        protected virtual List<IDestructible> FindDestructiblesInTarget(GameObject target)
        {
            var destructibles = new List<IDestructible>();
            
            // 自身から検索
            var destructible = target.GetComponent<IDestructible>();
            if (destructible != null)
            {
                destructibles.Add(destructible);
            }
            
            // 親から検索
            destructible = target.GetComponentInParent<IDestructible>();
            if (destructible != null && !destructibles.Contains(destructible))
            {
                destructibles.Add(destructible);
            }
            
            return destructibles;
        }
        
        /// <summary>
        /// 範囲内の破壊可能オブジェクトを検索
        /// </summary>
        protected virtual List<IDestructible> FindDestructiblesInRange()
        {
            var destructibles = new List<IDestructible>();
            Vector3 boxCenter = GetToolUsagePosition();
            
            Collider[] colliders = Physics.OverlapBox(boxCenter, miningBoxSize / 2f, 
                characterTransform.rotation, destructibleLayers);
            
            foreach (var collider in colliders)
            {
                if (collider.transform == characterTransform) continue;
                
                var destructibleList = FindDestructiblesInTarget(collider.gameObject);
                foreach (var destructible in destructibleList)
                {
                    if (!destructibles.Contains(destructible))
                    {
                        destructibles.Add(destructible);
                    }
                }
            }
            
            return destructibles;
        }
        
        /// <summary>
        /// ツール使用位置を取得
        /// </summary>
        protected virtual Vector3 GetToolUsagePosition()
        {
            if (toolUsagePoint != null)
            {
                return toolUsagePoint.position;
            }
            
            return characterTransform.position + characterTransform.forward * (defaultAttackRange / 2f);
        }
        
        /// <summary>
        /// ツールデータを取得（継承クラスで実装）
        /// </summary>
        protected abstract ToolData? GetToolData(InventorySlot toolSlot);
        
        /// <summary>
        /// ツールのダメージを取得（継承クラスで実装）
        /// </summary>
        protected abstract float GetDamageForTool(ToolData toolData);
        
        /// <summary>
        /// ターゲットにダメージを適用（継承クラスで実装）
        /// </summary>
        protected abstract void ApplyDamageToTarget(IDestructible target, float damage, ToolType toolType);
        
        /// <summary>
        /// ツールを消費（継承クラスで実装）
        /// </summary>
        protected abstract void ConsumeTool(InventorySlot toolSlot, int usageCount);
        
        /// <summary>
        /// イベント用のツールタイプを取得
        /// </summary>
        protected virtual ToolType GetToolTypeForEvent(InventorySlot toolSlot)
        {
            if (toolSlot?.ItemData != null)
            {
                return toolSlot.ItemData.toolType;
            }
            return ToolType.IronPipe; // デフォルト
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            Vector3 toolPosition = GetToolUsagePosition();
            
            // 採掘範囲
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(toolPosition, characterTransform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, miningBoxSize);
            
            // 攻撃範囲
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(toolPosition, defaultAttackRange);
        }
    }
    
    /// <summary>
    /// ツールの基本情報を格納する構造体
    /// </summary>
    public struct ToolData
    {
        public ToolType ToolType;
        public float Damage;
    }
}