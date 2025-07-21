using System.Collections.Generic;
using UnityEngine;

namespace KowloonBreak.Environment
{
    public class SpawnArea : MonoBehaviour
    {
        [Header("Spawn Area Settings")]
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 10f, 50f);
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private LayerMask obstacleLayer = 0;
        [SerializeField] private float minSpawnDistance = 3f;
        [SerializeField] private int maxSpawnAttempts = 50;
        [SerializeField] private float groundCheckDistance = 10f;
        [SerializeField] private float obstacleCheckRadius = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.green;
        
        private BoxCollider areaCollider;
        private List<Vector3> spawnedPositions = new List<Vector3>();
        
        public Vector3 SpawnAreaSize => spawnAreaSize;
        public float MinSpawnDistance => minSpawnDistance;
        public int MaxSpawnAttempts => maxSpawnAttempts;
        public LayerMask GroundLayer => groundLayer;
        public LayerMask ObstacleLayer => obstacleLayer;
        
        private void Awake()
        {
            // BoxColliderを取得または作成
            areaCollider = GetComponent<BoxCollider>();
            if (areaCollider == null)
            {
                areaCollider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Triggerに設定
            areaCollider.isTrigger = true;
            areaCollider.size = spawnAreaSize;
        }
        
        private void Start()
        {
            // エリアサイズをBoxColliderに反映
            UpdateColliderSize();
        }
        
        private void UpdateColliderSize()
        {
            if (areaCollider != null)
            {
                areaCollider.size = spawnAreaSize;
            }
        }
        
        public bool TryGetSpawnPosition(out Vector3 spawnPosition, float objectRadius = 0.5f)
        {
            spawnPosition = Vector3.zero;
            
            for (int attempts = 0; attempts < maxSpawnAttempts; attempts++)
            {
                // ランダムな位置を生成
                Vector3 randomPosition = GetRandomPositionInArea();
                
                // 地面検出
                if (TryFindGroundPosition(randomPosition, out Vector3 groundPosition))
                {
                    // 障害物チェック
                    if (!IsPositionObstructed(groundPosition, objectRadius))
                    {
                        // 他のオブジェクトとの距離チェック
                        if (IsValidSpawnDistance(groundPosition))
                        {
                            spawnPosition = groundPosition;
                            spawnedPositions.Add(spawnPosition);
                            return true;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Failed to find spawn position after {maxSpawnAttempts} attempts");
            return false;
        }
        
        private Vector3 GetRandomPositionInArea()
        {
            Vector3 center = transform.position;
            Vector3 halfSize = spawnAreaSize * 0.5f;
            
            float x = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
            float z = Random.Range(center.z - halfSize.z, center.z + halfSize.z);
            float y = center.y + halfSize.y; // 上部から開始
            
            return new Vector3(x, y, z);
        }
        
        private bool TryFindGroundPosition(Vector3 startPosition, out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            
            // 上から下へRaycast
            Ray ray = new Ray(startPosition, Vector3.down);
            
            if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundLayer))
            {
                groundPosition = hit.point;
                return true;
            }
            
            return false;
        }
        
        private bool IsPositionObstructed(Vector3 position, float radius)
        {
            // 指定された半径内に障害物があるかチェック
            Collider[] overlapping = Physics.OverlapSphere(position, radius, obstacleLayer);
            return overlapping.Length > 0;
        }
        
        private bool IsValidSpawnDistance(Vector3 position)
        {
            // 既にスポーンされたオブジェクトとの距離をチェック
            foreach (Vector3 existingPosition in spawnedPositions)
            {
                if (Vector3.Distance(position, existingPosition) < minSpawnDistance)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public void RegisterSpawnedPosition(Vector3 position)
        {
            spawnedPositions.Add(position);
        }
        
        public void UnregisterSpawnedPosition(Vector3 position)
        {
            spawnedPositions.RemoveAll(pos => Vector3.Distance(pos, position) < 0.1f);
        }
        
        public void ClearSpawnedPositions()
        {
            spawnedPositions.Clear();
        }
        
        public int GetSpawnedObjectCount()
        {
            return spawnedPositions.Count;
        }
        
        public bool IsPositionInArea(Vector3 position)
        {
            Vector3 localPosition = transform.InverseTransformPoint(position);
            Vector3 halfSize = spawnAreaSize * 0.5f;
            
            return Mathf.Abs(localPosition.x) <= halfSize.x &&
                   Mathf.Abs(localPosition.y) <= halfSize.y &&
                   Mathf.Abs(localPosition.z) <= halfSize.z;
        }
        
        public void SetSpawnAreaSize(Vector3 size)
        {
            spawnAreaSize = size;
            UpdateColliderSize();
        }
        
        public void SetMinSpawnDistance(float distance)
        {
            minSpawnDistance = distance;
        }
        
        public void SetMaxSpawnAttempts(int attempts)
        {
            maxSpawnAttempts = attempts;
        }
        
        public void SetGroundLayer(LayerMask layer)
        {
            groundLayer = layer;
        }
        
        public void SetObstacleLayer(LayerMask layer)
        {
            obstacleLayer = layer;
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, spawnAreaSize);
            
            // スポーンされた位置を表示
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            
            foreach (Vector3 position in spawnedPositions)
            {
                Gizmos.DrawWireSphere(position, minSpawnDistance * 0.5f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, spawnAreaSize);
            
            // 地面検出範囲を表示
            Gizmos.color = Color.blue;
            Gizmos.matrix = Matrix4x4.identity;
            
            Vector3 center = transform.position;
            Vector3 topCenter = center + Vector3.up * (spawnAreaSize.y * 0.5f);
            Vector3 bottomCenter = center - Vector3.up * (spawnAreaSize.y * 0.5f + groundCheckDistance);
            
            Gizmos.DrawLine(topCenter, bottomCenter);
        }
        
        private void OnValidate()
        {
            // インスペクターで値が変更された時に更新
            if (areaCollider != null)
            {
                UpdateColliderSize();
            }
        }
    }
}