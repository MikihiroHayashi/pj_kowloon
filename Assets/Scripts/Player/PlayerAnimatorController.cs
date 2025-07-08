using UnityEngine;

namespace KowloonBreak.Player
{
    public class PlayerAnimatorController : MonoBehaviour
    {
        [Header("Animator Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private bool autoFindAnimator = true;
        [SerializeField] private bool autoFindTargetTransform = true;
        
        [Header("Angle Calculation")]
        [SerializeField] private bool useLocalRotation = true;
        [SerializeField] private bool smoothAngleTransition = true;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool debugAngle = false;
        
        [Header("Parameter Names")]
        [SerializeField] private string angleParameterName = "Angle";
        
        private float currentAngle = 0f;
        private float targetAngle = 0f;
        private int angleParameterHash;
        
        private void Awake()
        {
            // Animatorを自動取得
            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            // ターゲットTransformを自動取得
            if (autoFindTargetTransform && targetTransform == null)
            {
                targetTransform = transform;
            }
            
            // パラメータハッシュを事前計算
            if (animator != null)
            {
                angleParameterHash = Animator.StringToHash(angleParameterName);
            }
        }
        
        private void Start()
        {
            // 初期角度を設定
            if (targetTransform != null)
            {
                currentAngle = CalculateAngle();
                targetAngle = currentAngle;
                UpdateAnimatorAngle(currentAngle);
            }
        }
        
        private void Update()
        {
            if (animator == null || targetTransform == null) return;
            
            // 角度を計算
            targetAngle = CalculateAngle();
            
            // スムーズな角度遷移
            if (smoothAngleTransition)
            {
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            }
            else
            {
                currentAngle = targetAngle;
            }
            
            // Animatorに角度を設定
            UpdateAnimatorAngle(currentAngle);
            
            // デバッグ表示
            if (debugAngle)
            {
                Debug.Log($"Player Angle: {currentAngle:F1}°");
            }
        }
        
        private float CalculateAngle()
        {
            Vector3 forward;
            
            if (useLocalRotation)
            {
                // ローカル回転を使用
                forward = targetTransform.forward;
            }
            else
            {
                // ワールド回転を使用
                forward = targetTransform.rotation * Vector3.forward;
            }
            
            // Y軸周りの角度を計算（0度 = 前方、時計回りに360度）
            float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            
            // 0-360度の範囲に正規化
            if (angle < 0)
            {
                angle += 360f;
            }
            
            return angle;
        }
        
        private void UpdateAnimatorAngle(float angle)
        {
            if (animator == null) return;
            
            // Animatorパラメータを更新
            animator.SetFloat(angleParameterHash, angle);
        }
        
        /// <summary>
        /// 手動で角度を設定
        /// </summary>
        public void SetAngle(float angle)
        {
            // 0-360度の範囲に正規化
            while (angle < 0) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            
            targetAngle = angle;
            
            if (!smoothAngleTransition)
            {
                currentAngle = targetAngle;
                UpdateAnimatorAngle(currentAngle);
            }
        }
        
        /// <summary>
        /// 現在の角度を取得
        /// </summary>
        public float GetCurrentAngle()
        {
            return currentAngle;
        }
        
        /// <summary>
        /// ターゲット角度を取得
        /// </summary>
        public float GetTargetAngle()
        {
            return targetAngle;
        }
        
        /// <summary>
        /// Animatorを設定
        /// </summary>
        public void SetAnimator(Animator newAnimator)
        {
            animator = newAnimator;
            if (animator != null)
            {
                angleParameterHash = Animator.StringToHash(angleParameterName);
            }
        }
        
        /// <summary>
        /// ターゲットTransformを設定
        /// </summary>
        public void SetTargetTransform(Transform newTarget)
        {
            targetTransform = newTarget;
        }
        
        /// <summary>
        /// 角度パラメータ名を設定
        /// </summary>
        public void SetAngleParameterName(string parameterName)
        {
            angleParameterName = parameterName;
            if (animator != null)
            {
                angleParameterHash = Animator.StringToHash(angleParameterName);
            }
        }
        
        /// <summary>
        /// スムーズ遷移の有効/無効を設定
        /// </summary>
        public void SetSmoothTransition(bool enabled, float speed = 5f)
        {
            smoothAngleTransition = enabled;
            smoothSpeed = speed;
        }
        
        private void OnValidate()
        {
            // エディターでパラメータが変更された時の処理
            if (Application.isPlaying && animator != null)
            {
                angleParameterHash = Animator.StringToHash(angleParameterName);
            }
        }
    }
}