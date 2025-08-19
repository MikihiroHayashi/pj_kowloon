using UnityEngine;

namespace KowloonBreak.Environment
{
    /// <summary>
    /// シンプル化されたダンジョンブロック
    /// エディター専用の基本的な情報保持とGizmo表示のみ
    /// </summary>
    [System.Serializable]
    public class DungeonBlock : MonoBehaviour
    {
        [Header("Block Configuration")]
        [SerializeField] private DungeonBlockConfiguration configuration;
        
        private Vector2Int gridPosition;
        private float cellSize = 1f;
        
        // 必須プロパティ（他クラスからアクセス）
        public Vector2Int BlockSize => configuration?.size ?? Vector2Int.one;
        public DungeonBlockType BlockType => configuration?.blockType ?? DungeonBlockType.Room;  
        public float CellSize => cellSize;
        public DungeonBlockConfiguration Configuration => configuration;
        public Vector2Int GridPosition 
        { 
            get => gridPosition; 
            set => gridPosition = value; 
        }
        
        public Vector3 WorldSize => configuration?.GetWorldSize(cellSize) ?? new Vector3(cellSize, 0, cellSize);
        
        /// <summary>
        /// Configuration から初期化
        /// </summary>
        public void InitializeFromConfiguration(DungeonBlockConfiguration config, float cellSize)
        {
            this.configuration = config;
            this.cellSize = cellSize;
            
            if (config != null)
            {
                config.ValidateAndFix();
            }
        }
        
        /// <summary>
        /// グリッド位置を設定し、ワールド座標に変換
        /// </summary>
        public void SetGridPosition(Vector2Int gridPos)
        {
            this.gridPosition = gridPos;
            if (configuration != null)
            {
                transform.position = configuration.GetWorldPosition(gridPos, cellSize);
            }
            else
            {
                transform.position = new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// エディター用Gizmo表示（簡略化版）
        /// </summary>
        private void OnDrawGizmos()
        {
            // 選択時はカスタムエディターに任せる
            if (UnityEditor.Selection.activeGameObject == gameObject)
                return;
                
            // 基本的なワイヤーフレーム表示
            Gizmos.color = GetGizmoColor();
            Vector3 size = WorldSize;
            Vector3 center = transform.position + new Vector3(size.x * 0.5f, 0, size.z * 0.5f);
            
            Gizmos.DrawWireCube(center, size);
            
            // ブロック情報をラベル表示
            UnityEditor.Handles.Label(center + Vector3.up * 2f, 
                $"{BlockType}\n{BlockSize.x}x{BlockSize.y}");
        }
        
        private Color GetGizmoColor()
        {
            if (configuration != null)
            {
                return configuration.debugColor;
            }
            
            return DungeonBlockConfiguration.GetDefaultColor(BlockType);
        }
#endif
    }
    
    /// <summary>
    /// ダンジョンブロックタイプ
    /// </summary>
    [System.Serializable]
    public enum DungeonBlockType
    {
        Room,
        Corridor,
        Junction,
        Special,
        Entrance,
        Exit,
        Road
    }
    
    /// <summary>
    /// 方向enum（将来の拡張用）
    /// </summary>
    [System.Serializable]
    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}