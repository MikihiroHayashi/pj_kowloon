using UnityEngine;

namespace KowloonBreak.Enemies
{
    [System.Serializable]
    public class PatrolPoint
    {
        public Transform transform;
        public float waitTime = 2f;
        
        [Header("Optional Settings")]
        public bool lookAround = true;     // 到着後周囲を見回すか
        public string arrivalSound = "";   // 到着時の音
    }

    public class PatrolRoute : MonoBehaviour
    {
        [Header("Patrol Settings")]
        public PatrolPoint[] points;
        public bool loop = true;           // ループするか
        public float patrolSpeed = 2f;     // パトロール時の移動速度
        
        [Header("Visualization")]
        public Color routeColor = Color.green;
        public Color pointColor = Color.yellow;
        public float pointSize = 0.5f;
        
        /// <summary>
        /// 指定されたインデックスのパトロールポイントを取得
        /// </summary>
        public PatrolPoint GetPoint(int index)
        {
            if (points == null || points.Length == 0) return null;
            return points[Mathf.Clamp(index, 0, points.Length - 1)];
        }
        
        /// <summary>
        /// 次のパトロールポイントのインデックスを取得
        /// </summary>
        public int GetNextIndex(int currentIndex, bool isMovingForward)
        {
            if (points == null || points.Length == 0) return 0;
            
            if (loop)
            {
                // ループモード：常に前進
                return (currentIndex + 1) % points.Length;
            }
            else
            {
                // 往復モード：端で方向転換
                if (isMovingForward)
                {
                    if (currentIndex >= points.Length - 1)
                    {
                        // 最後に到達したら逆方向に
                        return currentIndex - 1;
                    }
                    return currentIndex + 1;
                }
                else
                {
                    if (currentIndex <= 0)
                    {
                        // 最初に到達したら順方向に
                        return currentIndex + 1;
                    }
                    return currentIndex - 1;
                }
            }
        }
        
        /// <summary>
        /// 移動方向が変わったかどうかを判定
        /// </summary>
        public bool ShouldChangeDirection(int currentIndex, bool isMovingForward)
        {
            if (loop) return false; // ループモードでは方向転換しない
            
            return (isMovingForward && currentIndex >= points.Length - 1) || 
                   (!isMovingForward && currentIndex <= 0);
        }
        
        /// <summary>
        /// 最も近いパトロールポイントのインデックスを取得
        /// </summary>
        public int GetNearestPointIndex(Vector3 position)
        {
            if (points == null || points.Length == 0) return 0;
            
            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;
            
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].transform == null) continue;
                
                float distance = Vector3.Distance(position, points[i].transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }
        
        /// <summary>
        /// パトロールルートが有効かどうかを判定
        /// </summary>
        public bool IsValidRoute()
        {
            return points != null && points.Length > 0 && points[0].transform != null;
        }
        
        void OnDrawGizmos()
        {
            if (points == null || points.Length == 0) return;
            
            // パトロールポイントを描画
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].transform == null) continue;
                
                // ポイントを描画
                Gizmos.color = pointColor;
                Gizmos.DrawWireSphere(points[i].transform.position, pointSize);
                
                // 番号を表示（Editor限定）
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    points[i].transform.position + Vector3.up * (pointSize + 0.5f),
                    i.ToString()
                );
                #endif
                
                // 経路を描画
                int nextIndex = GetNextIndex(i, true);
                if (nextIndex != i && nextIndex < points.Length && points[nextIndex].transform != null)
                {
                    Gizmos.color = routeColor;
                    Gizmos.DrawLine(points[i].transform.position, points[nextIndex].transform.position);
                    
                    // 方向矢印を描画
                    Vector3 direction = (points[nextIndex].transform.position - points[i].transform.position).normalized;
                    Vector3 arrowPos = Vector3.Lerp(points[i].transform.position, points[nextIndex].transform.position, 0.7f);
                    
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(arrowPos, direction * 0.5f);
                    Gizmos.DrawRay(arrowPos, Quaternion.Euler(0, 45, 0) * direction * 0.3f);
                    Gizmos.DrawRay(arrowPos, Quaternion.Euler(0, -45, 0) * direction * 0.3f);
                }
            }
        }
        
        void OnDrawGizmosSelected()
        {
            OnDrawGizmos();
            
            // 選択時はより詳細な情報を表示
            if (points == null) return;
            
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].transform == null) continue;
                
                // 待機時間の可視化
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.DrawSphere(points[i].transform.position, pointSize * 1.5f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    points[i].transform.position + Vector3.up * (pointSize + 1f),
                    $"Wait: {points[i].waitTime}s"
                );
                #endif
            }
        }
    }
}