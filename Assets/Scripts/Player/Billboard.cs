using UnityEngine;

namespace KowloonBreak.Player
{
    public class Billboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool lockY = true;
        [SerializeField] private bool lockX = false;
        [SerializeField] private bool lockZ = false;
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private bool reverseDirection = false;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        
        [Header("Performance")]
        [SerializeField] private bool updateInFixedUpdate = false;
        [SerializeField] private float updateRate = 0.1f;
        
        private UnityEngine.Camera cameraToLookAt;
        private Transform cameraTransform;
        private float nextUpdateTime = 0f;
        
        private void Start()
        {
            // カメラを取得
            if (useMainCamera)
            {
                cameraToLookAt = UnityEngine.Camera.main;
            }
            else if (targetCamera != null)
            {
                cameraToLookAt = targetCamera;
            }
            
            if (cameraToLookAt == null)
            {
                cameraToLookAt = FindObjectOfType<UnityEngine.Camera>();
            }
            
            if (cameraToLookAt != null)
            {
                cameraTransform = cameraToLookAt.transform;
            }
            else
            {
                Debug.LogWarning("Billboard: No camera found for billboard effect", this);
            }
        }
        
        private void Update()
        {
            if (!updateInFixedUpdate)
            {
                UpdateBillboard();
            }
        }
        
        private void FixedUpdate()
        {
            if (updateInFixedUpdate)
            {
                UpdateBillboard();
            }
        }
        
        private void UpdateBillboard()
        {
            if (cameraTransform == null) return;
            
            // 更新レート制限
            if (updateRate > 0f && Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateRate;
            
            Vector3 targetPosition = cameraTransform.position;
            Vector3 lookDirection;
            
            if (reverseDirection)
            {
                lookDirection = transform.position - targetPosition;
            }
            else
            {
                lookDirection = targetPosition - transform.position;
            }
            
            // 軸のロックを適用
            if (lockY) lookDirection.y = 0;
            if (lockX) lookDirection.x = 0;
            if (lockZ) lookDirection.z = 0;
            
            // 方向がゼロの場合は回転しない
            if (lookDirection.magnitude < 0.001f) return;
            
            // 回転を計算
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            // オフセットを適用
            if (rotationOffset != Vector3.zero)
            {
                targetRotation *= Quaternion.Euler(rotationOffset);
            }
            
            transform.rotation = targetRotation;
        }
        
        /// <summary>
        /// ターゲットカメラを設定
        /// </summary>
        public void SetTargetCamera(UnityEngine.Camera camera)
        {
            targetCamera = camera;
            cameraToLookAt = camera;
            cameraTransform = camera != null ? camera.transform : null;
            useMainCamera = false;
        }
        
        /// <summary>
        /// メインカメラを使用するかどうかを設定
        /// </summary>
        public void SetUseMainCamera(bool useMain)
        {
            useMainCamera = useMain;
            if (useMain)
            {
                cameraToLookAt = UnityEngine.Camera.main;
                cameraTransform = cameraToLookAt != null ? cameraToLookAt.transform : null;
            }
        }
        
        /// <summary>
        /// Y軸のロックを設定
        /// </summary>
        public void SetLockY(bool lockAxis)
        {
            lockY = lockAxis;
        }
        
        /// <summary>
        /// 回転オフセットを設定
        /// </summary>
        public void SetRotationOffset(Vector3 offset)
        {
            rotationOffset = offset;
        }
        
        /// <summary>
        /// 手動で強制更新
        /// </summary>
        public void ForceUpdate()
        {
            nextUpdateTime = 0f;
            UpdateBillboard();
        }
    }
}