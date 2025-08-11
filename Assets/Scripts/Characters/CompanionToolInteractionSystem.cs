using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using static KowloonBreak.Core.BaseToolInteractionSystem;

namespace KowloonBreak.Characters
{
    /// <summary>
    /// Companion用の簡易ツール使用システム
    /// BaseToolInteractionSystemを継承してCompanion固有の機能を提供
    /// </summary>
    public class CompanionToolInteractionSystem : BaseToolInteractionSystem
    {
        [Header("Companion Tool Settings")]
        [SerializeField] private ToolType defaultTool = ToolType.IronPipe;
        [SerializeField] private float defaultDamage = 15f;
        
        private CompanionAI companionAI;
        
        protected override void Awake()
        {
            base.Awake();
            companionAI = GetComponent<CompanionAI>();
        }
        
        /// <summary>
        /// Companion用のツールデータ取得（固定値）
        /// </summary>
        protected override ToolData? GetToolData(InventorySlot toolSlot)
        {
            // CompanionはInventorySlotを使わないので、固定のツールデータを返す
            return new ToolData
            {
                ToolType = defaultTool,
                Damage = defaultDamage
            };
        }
        
        /// <summary>
        /// Companion用のダメージ取得
        /// </summary>
        protected override float GetDamageForTool(ToolData toolData)
        {
            return toolData.Damage;
        }
        
        /// <summary>
        /// Companion用のダメージ適用
        /// </summary>
        protected override void ApplyDamageToTarget(IDestructible target, float damage, ToolType toolType)
        {
            if (target is Enemies.EnemyBase enemyBase)
            {
                // 敵にダメージを与える場合は攻撃者情報を渡す
                enemyBase.TakeDamage(damage, toolType, characterTransform);
            }
            else
            {
                // 一般的な破壊可能オブジェクト
                target.TakeDamage(damage, toolType);
            }
            
            // ダメージテキスト表示
            if (UI.UIManager.Instance != null && target is MonoBehaviour targetMono)
            {
                UI.UIManager.Instance.ShowDamageText(
                    targetMono.transform.position + Vector3.up * 1.5f, 
                    damage, 
                    false
                );
            }
        }
        
        /// <summary>
        /// Companion用のツール消費（実際には消費しない）
        /// </summary>
        protected override void ConsumeTool(InventorySlot toolSlot, int usageCount)
        {
            // Companionはツールを消費しない
            if (showDebugInfo)
                Debug.Log($"[CompanionToolInteractionSystem] Tool usage completed (no consumption)");
        }
        
        /// <summary>
        /// 指定されたターゲットに攻撃準備（Companion専用）
        /// </summary>
        public bool PrepareAttackOnTarget(GameObject target)
        {
            // 仮想のツールスロットを作成
            var virtualToolSlot = CreateVirtualToolSlot();
            return TryUseToolOnTarget(target, virtualToolSlot);
        }
        
        /// <summary>
        /// 範囲攻撃準備（Companion専用）
        /// </summary>
        public bool PrepareRangeAttack()
        {
            var virtualToolSlot = CreateVirtualToolSlot();
            return TryUseToolInRange(virtualToolSlot);
        }
        
        /// <summary>
        /// 仮想のツールスロットを作成
        /// </summary>
        private InventorySlot CreateVirtualToolSlot()
        {
            // Companionは実際のインベントリを持たないので、nullを返して基底クラスでハンドリング
            return null;
        }
        
        /// <summary>
        /// Companion用のツールタイプ取得
        /// </summary>
        protected override ToolType GetToolTypeForEvent(InventorySlot toolSlot)
        {
            return defaultTool;
        }
    }
    
}