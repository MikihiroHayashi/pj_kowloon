using UnityEngine;
#if CINEMACHINE_AVAILABLE
using Cinemachine;
#endif

namespace KowloonBreak.Camera
{
    public class CinemachineSetup : MonoBehaviour
    {
        [Header("Cinemachine Settings")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 8f, -10f);
        [SerializeField] private float rotationX = 30f;
        [SerializeField] private float followDamping = 1f;

#if CINEMACHINE_AVAILABLE
        [Header("Virtual Camera Settings")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private bool useTransposer = true;
        [SerializeField] private bool useComposer = true;

        [Header("Follow Settings")]
        [SerializeField] private float lookAheadTime = 0f;
        [SerializeField] private float lookAheadSmoothing = 10f;

        private CinemachineTransposer transposer;
        private CinemachineComposer composer;
#endif

        public Transform FollowTarget 
        { 
            get => followTarget; 
            set => SetFollowTarget(value); 
        }

        private void Start()
        {
            SetupCinemachine();
        }

        private void SetupCinemachine()
        {
#if CINEMACHINE_AVAILABLE
            if (virtualCamera == null)
            {
                CreateVirtualCamera();
            }

            ConfigureVirtualCamera();
#else
            Debug.LogWarning("Cinemachine package is not installed. Using fallback camera system.");
            SetupFallbackCamera();
#endif
        }

#if CINEMACHINE_AVAILABLE
        private void CreateVirtualCamera()
        {
            GameObject vcamGO = new GameObject("Player Virtual Camera");
            vcamGO.transform.SetParent(transform);
            
            virtualCamera = vcamGO.AddComponent<CinemachineVirtualCamera>();
            virtualCamera.Priority = 10;
        }

        private void ConfigureVirtualCamera()
        {
            if (virtualCamera == null || followTarget == null) return;

            // Follow設定
            virtualCamera.Follow = followTarget;
            
            // Body (Transposer) 設定
            if (useTransposer)
            {
                virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = cameraOffset;
                virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_XDamping = followDamping;
                virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_YDamping = followDamping;
                virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_ZDamping = followDamping;
                
                transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            }
            
            // Aim (Composer) 設定 - 固定視点のため無効化
            if (!useComposer)
            {
                // カメラを固定角度に設定
                virtualCamera.transform.rotation = Quaternion.Euler(rotationX, 0f, 0f);
            }
            else
            {
                virtualCamera.LookAt = followTarget;
                composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_LookaheadTime = lookAheadTime;
                    composer.m_LookaheadSmoothing = lookAheadSmoothing;
                }
            }

            Debug.Log("Cinemachine Virtual Camera configured");
        }
#endif

        private void SetupFallbackCamera()
        {
            // Cinemachineが利用できない場合のフォールバック
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null && followTarget != null)
            {
                var followScript = mainCamera.gameObject.AddComponent<SimpleCameraFollow>();
                followScript.target = followTarget;
                followScript.offset = cameraOffset;
                followScript.rotationX = rotationX;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;

#if CINEMACHINE_AVAILABLE
            if (virtualCamera != null)
            {
                virtualCamera.Follow = target;
                if (useComposer)
                {
                    virtualCamera.LookAt = target;
                }
            }
#endif
        }

        public void SetCameraOffset(Vector3 offset)
        {
            cameraOffset = offset;

#if CINEMACHINE_AVAILABLE
            if (transposer != null)
            {
                transposer.m_FollowOffset = offset;
            }
#endif
        }

        public void SetDamping(float damping)
        {
            followDamping = damping;

#if CINEMACHINE_AVAILABLE
            if (transposer != null)
            {
                transposer.m_XDamping = damping;
                transposer.m_YDamping = damping;
                transposer.m_ZDamping = damping;
            }
#endif
        }
    }

    // Cinemachineが利用できない場合のシンプルなカメラ追従スクリプト
    public class SimpleCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 8f, -10f);
        public float rotationX = 30f;
        public float smoothSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            
            transform.position = smoothedPosition;
            transform.rotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }
}