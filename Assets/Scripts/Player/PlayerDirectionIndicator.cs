using UnityEngine;

namespace KowloonBreak.Player
{
    public class PlayerDirectionIndicator : MonoBehaviour
    {
        [Header("Direction Indicator Settings")]
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private Color arrowColor = Color.green;
        [SerializeField] private float arrowLength = 2f;
        [SerializeField] private float arrowWidth = 0.3f;
        [SerializeField] private float arrowHeadSize = 0.5f;
        [SerializeField] private Vector3 arrowOffset = new Vector3(0, 0.1f, 0);
        
        private void OnDrawGizmos()
        {
            if (!showInEditor) return;
            
            DrawDirectionArrow();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showInEditor) return;
            
            // 選択時は少し太い矢印を描画
            Color originalColor = arrowColor;
            arrowColor = Color.yellow;
            float originalWidth = arrowWidth;
            arrowWidth = arrowWidth * 1.5f;
            
            DrawDirectionArrow();
            
            arrowColor = originalColor;
            arrowWidth = originalWidth;
        }
        
        private void DrawDirectionArrow()
        {
            Gizmos.color = arrowColor;
            
            Vector3 startPos = transform.position + arrowOffset;
            Vector3 endPos = startPos + transform.forward * arrowLength;
            
            // 矢印の本体（線）
            Gizmos.DrawLine(startPos, endPos);
            
            // 矢印の頭部
            Vector3 arrowHead1 = endPos - transform.forward * arrowHeadSize + transform.right * arrowHeadSize * 0.5f;
            Vector3 arrowHead2 = endPos - transform.forward * arrowHeadSize - transform.right * arrowHeadSize * 0.5f;
            
            Gizmos.DrawLine(endPos, arrowHead1);
            Gizmos.DrawLine(endPos, arrowHead2);
            
            // 矢印の幅を示すライン
            if (arrowWidth > 0)
            {
                Vector3 rightOffset = transform.right * arrowWidth * 0.5f;
                Vector3 leftOffset = -rightOffset;
                
                // 矢印の両側にラインを描画
                Gizmos.DrawLine(startPos + rightOffset, endPos + rightOffset);
                Gizmos.DrawLine(startPos + leftOffset, endPos + leftOffset);
                
                // 矢印の根元を繋ぐ
                Gizmos.DrawLine(startPos + rightOffset, startPos + leftOffset);
            }
            
            // 追加の視覚的要素：正面方向のラベル
            DrawDirectionLabel(endPos);
        }
        
        private void DrawDirectionLabel(Vector3 position)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = arrowColor;
            UnityEditor.Handles.Label(position + Vector3.up * 0.2f, "FRONT", 
                new GUIStyle()
                {
                    normal = { textColor = arrowColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });
#endif
        }
        
        /// <summary>
        /// エディターでの表示を切り替え
        /// </summary>
        public void SetShowInEditor(bool show)
        {
            showInEditor = show;
        }
        
        /// <summary>
        /// 矢印の色を設定
        /// </summary>
        public void SetArrowColor(Color color)
        {
            arrowColor = color;
        }
        
        /// <summary>
        /// 矢印の長さを設定
        /// </summary>
        public void SetArrowLength(float length)
        {
            arrowLength = Mathf.Max(0.1f, length);
        }
        
        /// <summary>
        /// 矢印のオフセットを設定
        /// </summary>
        public void SetArrowOffset(Vector3 offset)
        {
            arrowOffset = offset;
        }
    }
}