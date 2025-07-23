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
        [SerializeField] private Vector3 miningBoxSize = new Vector3(2f, 2f, 3f);
        [SerializeField] private LayerMask mineableLayerMask = -1;
        [SerializeField] private KeyCode miningKey = KeyCode.E;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugBox = true;
        
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
            Debug.Log("[MiningSystem] PerformMining called");
            
            // 現在選択されているツールを取得
            var currentTool = GetCurrentTool();
            if (currentTool == null || currentTool.IsEmpty)
            {
                Debug.Log("[MiningSystem] No tool selected for mining");
                return;
            }
            
            Debug.Log($"[MiningSystem] Current tool: {currentTool.ItemData.itemName} (Type: {currentTool.ItemData.toolType})");
            
            // ツールが採掘に使用できるかチェック
            if (!IsToolSuitableForMining(currentTool))
            {
                Debug.Log($"[MiningSystem] Current tool {currentTool.ItemData.toolType} is not suitable for mining");
                return;
            }
            
            // 前方の採掘可能オブジェクトを検索
            var targets = FindMineableTargets();
            
            Debug.Log($"[MiningSystem] Found {targets.Length} mineable targets");
            
            if (targets.Length > 0)
            {
                // 最も近いターゲットを採掘
                var closestTarget = FindClosestTarget(targets);
                if (closestTarget != null)
                {
                    Debug.Log($"[MiningSystem] Attempting to mine: {closestTarget}");
                    AttemptMining(closestTarget, currentTool);
                }
            }
            else
            {
                Debug.Log("[MiningSystem] No mineable objects in range");
                DebugMiningArea();
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
            
            // ツールタイプをチェック（つるはしと鉄パイプを採掘可能に）
            bool isSuitable = toolSlot.ItemData.toolType == ToolType.Pickaxe || toolSlot.ItemData.toolType == ToolType.IronPipe;
            Debug.Log($"[MiningSystem] Tool {toolSlot.ItemData.toolType} suitable for mining: {isSuitable}");
            return isSuitable;
        }
        
        private IDestructible[] FindMineableTargets()
        {
            if (playerTransform == null) return new IDestructible[0];
            
            // プレイヤーの前方にボックス判定
            Vector3 boxCenter = playerTransform.position + Vector3.up * 1.5f + playerTransform.forward * (miningBoxSize.z / 2f);
            
            var targets = new System.Collections.Generic.List<IDestructible>();
            
            // ボックス範囲内のコライダーを取得
            Collider[] colliders = Physics.OverlapBox(boxCenter, miningBoxSize / 2f, playerTransform.rotation, mineableLayerMask);
            
            Debug.Log($"[MiningSystem] Search Box Center: {boxCenter}, Size: {miningBoxSize}, LayerMask: {mineableLayerMask.value}");
            Debug.Log($"[MiningSystem] Found {colliders.Length} colliders in range");
            
            foreach (var collider in colliders)
            {
                // プレイヤー自身のコライダーをスキップ
                if (collider.transform.root == playerTransform.root)
                {
                    Debug.Log($"[MiningSystem] Skipping player collider: {collider.name}");
                    continue;
                }
                
                Debug.Log($"[MiningSystem] Checking collider: {collider.name} on layer {collider.gameObject.layer}");
                
                // 自身と親オブジェクトからIDestructibleを検索
                var destructible = collider.GetComponent<IDestructible>();
                if (destructible == null)
                {
                    destructible = collider.GetComponentInParent<IDestructible>();
                }
                
                if (destructible != null && !targets.Contains(destructible))
                {
                    targets.Add(destructible);
                    Debug.Log($"[MiningSystem] Added destructible target: {collider.name} (IDestructible found on {((MonoBehaviour)destructible).name})");
                }
                else if (destructible == null)
                {
                    Debug.Log($"[MiningSystem] Collider {collider.name} has no IDestructible component (checked parent too)");
                }
            }
            
            Debug.Log($"[MiningSystem] Total targets found: {targets.Count}");
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
            
            Debug.Log($"[MiningSystem] Attempting to mine with tool: {toolType}");
            Debug.Log($"[MiningSystem] Tool damage: {toolSlot.ItemData.attackDamage}");
            
            // 採掘試行イベントを発火
            OnMiningAttempt?.Invoke(target, toolType);
            
            // ターゲットがそのツールで破壊可能かチェック
            bool canDestroy = target.CanBeDestroyedBy(toolType);
            Debug.Log($"[MiningSystem] Can destroy target with {toolType}: {canDestroy}");
            
            if (canDestroy)
            {
                float damage = toolSlot.ItemData.attackDamage;
                
                Debug.Log($"[MiningSystem] Dealing {damage} damage to target");
                
                // ダメージを与える
                target.TakeDamage(damage, toolType);
                
                // ツールの耐久度を減らす
                bool toolStillUsable = toolSlot.UseDurability(1);
                
                // 成功イベントを発火
                OnMiningSuccess?.Invoke(target, toolType);
                
                Debug.Log($"[MiningSystem] Mining successful: Dealt {damage} damage to {target} with {toolType}");
                
                // ツールが壊れた場合の処理
                if (!toolStillUsable)
                {
                    Debug.Log($"[MiningSystem] Tool {toolSlot.ItemData.itemName} broke!");
                }
            }
            else
            {
                Debug.Log($"[MiningSystem] Cannot mine this object with {toolType}");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebugBox || playerTransform == null) return;
            
            // ボックス判定範囲を可視化
            Vector3 boxCenter = playerTransform.position + Vector3.up * 1.5f + playerTransform.forward * (miningBoxSize.z / 2f);
            
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, playerTransform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, miningBoxSize);
            
            // リセット
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        private void DebugMiningArea()
        {
            if (playerTransform == null) return;
            
            Vector3 boxCenter = playerTransform.position + Vector3.up * 1.5f + playerTransform.forward * (miningBoxSize.z / 2f);
            
            Debug.Log($"[MiningSystem DEBUG] Player position: {playerTransform.position}");
            Debug.Log($"[MiningSystem DEBUG] Box center: {boxCenter}");
            Debug.Log($"[MiningSystem DEBUG] Box size: {miningBoxSize}");
            Debug.Log($"[MiningSystem DEBUG] Layer mask value: {mineableLayerMask.value}");
            
            // 全てのコライダーを取得（レイヤーは無視）
            Collider[] allColliders = Physics.OverlapBox(boxCenter, miningBoxSize / 2f, playerTransform.rotation);
            Debug.Log($"[MiningSystem DEBUG] All colliders in box (no layer filter): {allColliders.Length}");
            
            foreach (var collider in allColliders)
            {
                bool isPlayer = collider.transform.root == playerTransform.root;
                var destructible = collider.GetComponent<IDestructible>() ?? collider.GetComponentInParent<IDestructible>();
                Debug.Log($"[MiningSystem DEBUG] Found: {collider.name}, Layer: {collider.gameObject.layer}, HasIDestructible: {destructible != null}, IsPlayer: {isPlayer}");
            }
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