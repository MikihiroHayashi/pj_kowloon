using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Managers;
using KowloonBreak.UI;

namespace KowloonBreak.Player
{
    public class MiningSystem : MonoBehaviour
    {
        [Header("Mining Settings")]
        [SerializeField] private float miningRange = 3f;
        [SerializeField] private float miningAngle = 45f;
        [SerializeField] private LayerMask mineableLayerMask = -1;
        [SerializeField] private KeyCode miningKey = KeyCode.E;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugRays = true;
        
        private Transform playerTransform;
        private EnhancedResourceManager resourceManager;
        private ToolSelectionHUDController toolHUD;
        
        public System.Action<IDestructible, ToolType> OnMiningAttempt;
        public System.Action<IDestructible, ToolType> OnMiningSuccess;
        
        private void Awake()
        {
            // プレイヤートランスフォームを取得
            playerTransform = transform;
        }
        
        private void Start()
        {
            // 必要なコンポーネントを取得
            resourceManager = EnhancedResourceManager.Instance;
            toolHUD = FindObjectOfType<ToolSelectionHUDController>();
            
            // ツールHUDのイベントに接続
            if (toolHUD != null)
            {
                toolHUD.OnToolUsed += OnToolUsedFromHUD;
            }
        }
        
        private void Update()
        {
            HandleMiningInput();
        }
        
        private void HandleMiningInput()
        {
            // 採掘キーが押された時
            if (Input.GetKeyDown(miningKey))
            {
                PerformMining();
            }
        }
        
        private void OnToolUsedFromHUD(int toolIndex, InventorySlot toolSlot)
        {
            // ツールHUDから使用された場合も採掘を実行
            if (toolSlot != null && !toolSlot.IsEmpty)
            {
                PerformMining();
            }
        }
        
        public void PerformMining()
        {
            // 現在選択されているツールを取得
            var currentTool = GetCurrentTool();
            if (currentTool == null || currentTool.IsEmpty)
            {
                Debug.Log("No tool selected for mining");
                return;
            }
            
            // ツールが採掘に使用できるかチェック
            if (!IsToolSuitableForMining(currentTool))
            {
                Debug.Log("Current tool is not suitable for mining");
                return;
            }
            
            // 前方の採掘可能オブジェクトを検索
            var targets = FindMineableTargets();
            
            if (targets.Length > 0)
            {
                // 最も近いターゲットを採掘
                var closestTarget = FindClosestTarget(targets);
                if (closestTarget != null)
                {
                    AttemptMining(closestTarget, currentTool);
                }
            }
            else
            {
                Debug.Log("No mineable objects in range");
            }
        }
        
        private InventorySlot GetCurrentTool()
        {
            if (toolHUD != null)
            {
                return toolHUD.GetSelectedTool();
            }
            return null;
        }
        
        private bool IsToolSuitableForMining(InventorySlot toolSlot)
        {
            if (toolSlot?.ItemData == null) return false;
            
            // ツールタイプをチェック（つるはしのみ採掘可能）
            return toolSlot.ItemData.toolType == ToolType.Pickaxe;
        }
        
        private IDestructible[] FindMineableTargets()
        {
            if (playerTransform == null) return new IDestructible[0];
            
            // プレイヤーの前方方向にレイキャストを複数発射
            Vector3 forward = playerTransform.forward;
            Vector3 origin = playerTransform.position + Vector3.up * 1.5f; // プレイヤーの胸の高さから
            
            var targets = new System.Collections.Generic.List<IDestructible>();
            
            // 中央のレイ
            if (Physics.Raycast(origin, forward, out RaycastHit centerHit, miningRange, mineableLayerMask))
            {
                var destructible = centerHit.collider.GetComponent<IDestructible>();
                if (destructible != null && !targets.Contains(destructible))
                {
                    targets.Add(destructible);
                }
            }
            
            // 角度範囲内の複数レイ
            int rayCount = 5;
            for (int i = 0; i < rayCount; i++)
            {
                float angle = Mathf.Lerp(-miningAngle, miningAngle, i / (float)(rayCount - 1));
                Vector3 direction = Quaternion.AngleAxis(angle, playerTransform.up) * forward;
                
                if (Physics.Raycast(origin, direction, out RaycastHit hit, miningRange, mineableLayerMask))
                {
                    var destructible = hit.collider.GetComponent<IDestructible>();
                    if (destructible != null && !targets.Contains(destructible))
                    {
                        targets.Add(destructible);
                    }
                }
                
                // 垂直方向の角度も考慮
                direction = Quaternion.AngleAxis(angle, playerTransform.right) * forward;
                if (Physics.Raycast(origin, direction, out RaycastHit verticalHit, miningRange, mineableLayerMask))
                {
                    var destructible = verticalHit.collider.GetComponent<IDestructible>();
                    if (destructible != null && !targets.Contains(destructible))
                    {
                        targets.Add(destructible);
                    }
                }
            }
            
            return targets.ToArray();
        }
        
        private IDestructible FindClosestTarget(IDestructible[] targets)
        {
            if (targets.Length == 0) return null;
            
            IDestructible closest = null;
            float closestDistance = float.MaxValue;
            Vector3 playerPos = playerTransform.position;
            
            foreach (var target in targets)
            {
                if (target is MonoBehaviour mono)
                {
                    float distance = Vector3.Distance(playerPos, mono.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = target;
                    }
                }
            }
            
            return closest;
        }
        
        private void AttemptMining(IDestructible target, InventorySlot toolSlot)
        {
            var toolType = toolSlot.ItemData.toolType;
            
            // 採掘試行イベントを発火
            OnMiningAttempt?.Invoke(target, toolType);
            
            // ターゲットがそのツールで破壊可能かチェック
            if (target.CanBeDestroyedBy(toolType))
            {
                float damage = toolSlot.ItemData.attackDamage;
                
                // ダメージを与える
                target.TakeDamage(damage, toolType);
                
                // ツールの耐久度を減らす
                bool toolStillUsable = toolSlot.UseDurability(1);
                
                // 成功イベントを発火
                OnMiningSuccess?.Invoke(target, toolType);
                
                Debug.Log($"Mining: Dealt {damage} damage to {target} with {toolType}");
                
                // ツールが壊れた場合の処理
                if (!toolStillUsable)
                {
                    Debug.Log($"Tool {toolSlot.ItemData.itemName} broke!");
                }
            }
            else
            {
                Debug.Log($"Cannot mine this object with {toolType}");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebugRays || playerTransform == null) return;
            
            Vector3 origin = playerTransform.position + Vector3.up * 1.5f;
            Vector3 forward = playerTransform.forward;
            
            // 採掘範囲を可視化
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, forward * miningRange);
            
            // 採掘角度を可視化
            Gizmos.color = Color.green;
            Vector3 leftDirection = Quaternion.AngleAxis(-miningAngle, playerTransform.up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(miningAngle, playerTransform.up) * forward;
            
            Gizmos.DrawRay(origin, leftDirection * miningRange);
            Gizmos.DrawRay(origin, rightDirection * miningRange);
            
            // 上下の角度も表示
            Vector3 upDirection = Quaternion.AngleAxis(-miningAngle, playerTransform.right) * forward;
            Vector3 downDirection = Quaternion.AngleAxis(miningAngle, playerTransform.right) * forward;
            
            Gizmos.DrawRay(origin, upDirection * miningRange);
            Gizmos.DrawRay(origin, downDirection * miningRange);
        }
        
        private void OnDestroy()
        {
            if (toolHUD != null)
            {
                toolHUD.OnToolUsed -= OnToolUsedFromHUD;
            }
        }
    }
}