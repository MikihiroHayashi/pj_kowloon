using UnityEngine;

namespace KowloonBreak.Characters
{
    /// <summary>
    /// コンパニオンのアニメーション制御クラス
    /// PlayerAnimatorControllerと同じ機能を持つ
    /// </summary>
    public class CompanionAnimatorController : MonoBehaviour
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
        [SerializeField] private string deathParameterName = "Death";
        [SerializeField] private string attackParameterName = "Attack";
        [SerializeField] private string digParameterName = "Dig";
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string dodgeParameterName = "Dodge";
        [SerializeField] private string crouchParameterName = "Crouch";
        
        [Header("Speed Values")]
        [Tooltip("アニメーター速度値: 停止状態")]
        [SerializeField] private float idleSpeed = 0f;
        [Tooltip("アニメーター速度値: しゃがみ移動")]
        [SerializeField] private float crouchSpeed = 0.5f;
        [Tooltip("アニメーター速度値: 通常歩行")]
        [SerializeField] private float walkSpeed = 1f;
        [Tooltip("アニメーター速度値: 走行")]
        [SerializeField] private float runSpeed = 2f;
        
        private float currentAngle = 0f;
        private float targetAngle = 0f;
        private int angleParameterHash;
        private int deathParameterHash;
        private int attackParameterHash;
        private int digParameterHash;
        private int speedParameterHash;
        private int dodgeParameterHash;
        private int crouchParameterHash;
        
        // パラメータ存在フラグ
        private bool hasAngleParameter;
        private bool hasDeathParameter;
        private bool hasAttackParameter;
        private bool hasDigParameter;
        private bool hasSpeedParameter;
        private bool hasDodgeParameter;
        private bool hasCrouchParameter;
        
        // 現在のアニメーション状態
        private CompanionMovementState currentMovementState = CompanionMovementState.Idle;
        private bool isCrouching = false;
        private bool isDodging = false;
        
        public CompanionMovementState CurrentMovementState => currentMovementState;
        public bool IsCrouching => isCrouching;
        public bool IsDodging => isDodging;
        
        private void Awake()
        {
            InitializeComponents();
            InitializeParameters();
        }
        
        private void InitializeComponents()
        {
            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            if (autoFindTargetTransform && targetTransform == null)
            {
                targetTransform = transform;
            }
        }
        
        private void InitializeParameters()
        {
            if (animator == null) return;
            
            // パラメータハッシュを計算
            angleParameterHash = Animator.StringToHash(angleParameterName);
            deathParameterHash = Animator.StringToHash(deathParameterName);
            attackParameterHash = Animator.StringToHash(attackParameterName);
            digParameterHash = Animator.StringToHash(digParameterName);
            speedParameterHash = Animator.StringToHash(speedParameterName);
            dodgeParameterHash = Animator.StringToHash(dodgeParameterName);
            crouchParameterHash = Animator.StringToHash(crouchParameterName);
            
            // パラメータの存在確認
            hasAngleParameter = HasParameter(angleParameterHash);
            hasDeathParameter = HasParameter(deathParameterHash);
            hasAttackParameter = HasParameter(attackParameterHash);
            hasDigParameter = HasParameter(digParameterHash);
            hasSpeedParameter = HasParameter(speedParameterHash);
            hasDodgeParameter = HasParameter(dodgeParameterHash);
            hasCrouchParameter = HasParameter(crouchParameterHash);
        }
        
        private bool HasParameter(int parameterHash)
        {
            if (animator == null) return false;
            
            var parameters = animator.parameters;
            foreach (var param in parameters)
            {
                if (param.nameHash == parameterHash)
                    return true;
            }
            return false;
        }
        
        private void Update()
        {
            UpdateAngle();
        }
        
        private void UpdateAngle()
        {
            if (!hasAngleParameter || animator == null || targetTransform == null) return;
            
            Vector3 velocity = targetTransform.GetComponent<UnityEngine.AI.NavMeshAgent>()?.velocity ?? Vector3.zero;
            
            if (velocity.magnitude > 0.1f)
            {
                // 移動方向を基に角度を計算
                Vector3 direction = velocity.normalized;
                Vector3 forward = useLocalRotation ? targetTransform.forward : Vector3.forward;
                Vector3 right = useLocalRotation ? targetTransform.right : Vector3.right;
                
                float dotForward = Vector3.Dot(direction, forward);
                float dotRight = Vector3.Dot(direction, right);
                
                targetAngle = Mathf.Atan2(dotRight, dotForward) * Mathf.Rad2Deg;
            }
            
            if (smoothAngleTransition)
            {
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            }
            else
            {
                currentAngle = targetAngle;
            }
            
            animator.SetFloat(angleParameterHash, currentAngle);
            
            if (debugAngle)
            {
                Debug.Log($"[CompanionAnimatorController] Angle: {currentAngle:F1}°");
            }
        }
        
        /// <summary>
        /// 移動速度を設定（MovementStateから自動判定）
        /// </summary>
        public void SetMovementState(CompanionMovementState state, bool isRunning = false, bool isCrouchingState = false)
        {
            currentMovementState = state;
            isCrouching = isCrouchingState;
            
            float speed = GetSpeedForState(state, isRunning, isCrouchingState);
            SetSpeed(speed);
            SetCrouch(isCrouchingState);
        }
        
        private float GetSpeedForState(CompanionMovementState state, bool isRunning, bool isCrouchingState)
        {
            return state switch
            {
                CompanionMovementState.Idle => idleSpeed,
                CompanionMovementState.Moving when isCrouchingState => crouchSpeed,
                CompanionMovementState.Moving when isRunning => runSpeed,
                CompanionMovementState.Moving => walkSpeed,
                CompanionMovementState.Combat => isRunning ? runSpeed : walkSpeed,
                CompanionMovementState.Dodging => walkSpeed, // ダッジ中は歩行速度
                _ => idleSpeed
            };
        }
        
        public void SetSpeed(float speed)
        {
            if (hasSpeedParameter && animator != null)
            {
                animator.SetFloat(speedParameterHash, speed);
            }
        }
        
        public void SetCrouch(bool crouch)
        {
            isCrouching = crouch;
            if (hasCrouchParameter && animator != null)
            {
                animator.SetBool(crouchParameterHash, crouch);
            }
        }
        
        public void TriggerDodge()
        {
            isDodging = true;
            if (hasDodgeParameter && animator != null)
            {
                animator.SetTrigger(dodgeParameterHash);
            }
        }
        
        public void TriggerAttack()
        {
            if (hasAttackParameter && animator != null)
            {
                animator.SetTrigger(attackParameterHash);
            }
        }
        
        public void TriggerDig()
        {
            if (hasDigParameter && animator != null)
            {
                animator.SetTrigger(digParameterHash);
            }
        }
        
        public void TriggerDeath()
        {
            if (hasDeathParameter && animator != null)
            {
                animator.SetBool(deathParameterHash, true);
            }
        }
        
        // アニメーションイベント用メソッド
        public void OnDodgeAnimationEnd()
        {
            isDodging = false;
        }
        
        public void OnAttackAnimationEnd()
        {
            // 攻撃アニメーション終了処理
        }
        
        // デバッグ用
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeParameters();
            }
        }
    }
    
    public enum CompanionMovementState
    {
        Idle,       // 待機
        Moving,     // 移動中
        Combat,     // 戦闘中
        Dodging,    // ダッジ中
        Dead        // 死亡
    }
}