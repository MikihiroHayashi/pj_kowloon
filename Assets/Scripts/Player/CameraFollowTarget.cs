using UnityEngine;

namespace KowloonBreak.Player
{
    public class CameraFollowTarget : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private bool useFixedUpdate = true;

        [Header("Smoothing")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float positionDamping = 2f;

        [Header("Height Adjustment")]
        [SerializeField] private bool adjustHeightForCrouch = true;
        [SerializeField] private float crouchHeightOffset = -1f;

        private Vector3 targetPosition;
        private EnhancedPlayerController playerController;

        public Transform Target 
        { 
            get => target; 
            set => target = value; 
        }

        public Vector3 Offset 
        { 
            get => offset; 
            set => offset = value; 
        }

        private void Start()
        {
            if (target == null)
            {
                target = transform.parent;
            }

            if (target != null)
            {
                playerController = target.GetComponent<EnhancedPlayerController>();
            }

            InitializePosition();
        }

        private void Update()
        {
            if (!useFixedUpdate)
            {
                UpdatePosition();
            }
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
            {
                UpdatePosition();
            }
        }

        private void InitializePosition()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private void UpdatePosition()
        {
            if (target == null) return;

            CalculateTargetPosition();

            if (enableSmoothing)
            {
                transform.position = Vector3.Lerp(
                    transform.position, 
                    targetPosition, 
                    positionDamping * Time.deltaTime
                );
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        private void CalculateTargetPosition()
        {
            Vector3 currentOffset = offset;

            // しゃがみ時の高さ調整
            if (adjustHeightForCrouch && playerController != null && playerController.IsCrouching)
            {
                currentOffset.y += crouchHeightOffset;
            }

            targetPosition = target.position + currentOffset;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            if (target != null)
            {
                playerController = target.GetComponent<EnhancedPlayerController>();
                InitializePosition();
            }
        }

        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        public void SetFollowSpeed(float speed)
        {
            followSpeed = speed;
            positionDamping = speed;
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position + offset, 0.5f);
            }
        }
    }
}