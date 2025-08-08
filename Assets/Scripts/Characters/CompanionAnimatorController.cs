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
        [SerializeField] private bool debugSpeed = false;
        
        [Header("Parameter Names")]
        [SerializeField] private string angleParameterName = "Angle";
        [SerializeField] private string deathParameterName = "Death";
        [SerializeField] private string attackParameterName = "Attack";
        [SerializeField] private string digParameterName = "Dig";
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string dodgeParameterName = "Dodge";
        [SerializeField] private string crouchParameterName = "Crouch";
        
        [Header("Speed Values - Actual Velocities")]
        [Tooltip("しきい値: 停止状態の最大速度 (単位/秒)")]
        [SerializeField] private float idleSpeedThreshold = 0.1f;
        [Tooltip("しきい値: しゃがみ移動の最大速度 (単位/秒)")]
        [SerializeField] private float crouchSpeedThreshold = 2f;
        [Tooltip("しきい値: 通常歩行の最大速度 (単位/秒)")]
        [SerializeField] private float walkSpeedThreshold = 4f;
        [Tooltip("しきい値: 走行の最大速度 (単位/秒)")]
        [SerializeField] private float runSpeedThreshold = 8f;
        
        // 現在の実際の速度値
        private float currentRealSpeed = 0f;
        public float CurrentRealSpeed => currentRealSpeed;
        
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
            
            // PlayerAnimatorControllerと同じ計算方法を使用
            targetAngle = CalculateAngle();
            
            if (smoothAngleTransition)
            {
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
                // LerpAngle結果も正規化して累積を防ぐ
                currentAngle = NormalizeAngle360(currentAngle);
            }
            else
            {
                currentAngle = targetAngle;
            }
            
            // 角度を正規化してからAnimatorパラメータを更新
            float normalizedAngle = NormalizeAngle360(currentAngle);
            animator.SetFloat(angleParameterHash, normalizedAngle);
            
            if (debugAngle)
            {
                Debug.Log($"[CompanionAnimatorController] Angle: {normalizedAngle:F1}° (current: {currentAngle:F1}°)");
            }
        }
        
        /// <summary>
        /// PlayerAnimatorControllerと同じ角度計算方法
        /// </summary>
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
            
            // 確実な0-360度正規化
            return NormalizeAngle360(angle);
        }
        
        /// <summary>
        /// 角度を0-360度の範囲に正規化
        /// </summary>
        private static float NormalizeAngle360(float angle)
        {
            // Mathf.Repeat使用でより安全な正規化
            return Mathf.Repeat(angle, 360f);
        }
        
        /// <summary>
        /// 移動速度を設定（MovementStateから自動判定）
        /// </summary>
        public void SetMovementState(CompanionMovementState state, bool isRunning = false, bool isCrouchingState = false)
        {
            currentMovementState = state;
            isCrouching = isCrouchingState;
            
            // 旧システムとの互換性のため、正規化された値を使用
            float normalizedSpeed = GetNormalizedSpeedForState(state, isRunning, isCrouchingState);
            SetSpeed(normalizedSpeed);
            SetCrouch(isCrouchingState);
            
            if (debugSpeed)
            {
                Debug.Log($"[CompanionAnimatorController] SetMovementState: {state}, Running: {isRunning}, Crouching: {isCrouchingState}, Normalized Speed: {normalizedSpeed:F2}");
            }
        }
        
        /// <summary>
        /// 実際の速度で移動状態を設定
        /// </summary>
        public void SetMovementStateWithRealSpeed(CompanionMovementState state, float realSpeed, bool isCrouchingState = false)
        {
            currentMovementState = state;
            isCrouching = isCrouchingState;
            
            SetRealSpeed(realSpeed);
            SetCrouch(isCrouchingState);
            
            if (debugSpeed)
            {
                Debug.Log($"[CompanionAnimatorController] SetMovementState: {state}, Real Speed: {realSpeed:F2} units/sec, Crouching: {isCrouchingState}");
            }
        }
        
        private float GetNormalizedSpeedForState(CompanionMovementState state, bool isRunning, bool isCrouchingState)
        {
            // 後方互換性のための正規化された値（非推奨）
            return state switch
            {
                CompanionMovementState.Idle => 0f,
                CompanionMovementState.Moving when isCrouchingState => 0.5f,
                CompanionMovementState.Moving when isRunning => 2f,
                CompanionMovementState.Moving => 1f,
                CompanionMovementState.Combat => isRunning ? 2f : 1f,
                CompanionMovementState.Dodging => 0f,
                _ => 0f
            };
        }
        
        /// <summary>
        /// 実際の移動速度を設定（単位/秒）
        /// </summary>
        /// <param name="actualSpeed">実際の移動速度 (単位/秒)</param>
        public void SetRealSpeed(float actualSpeed)
        {
            currentRealSpeed = actualSpeed;
            
            if (hasSpeedParameter && animator != null)
            {
                animator.SetFloat(speedParameterHash, actualSpeed);
                
                if (debugSpeed)
                {
                    Debug.Log($"[CompanionAnimatorController] Set Real Speed: {actualSpeed:F2} units/sec");
                }
            }
        }
        
        /// <summary>
        /// 後方互換性のため維持（旧SetSpeedメソッド）
        /// </summary>
        /// <param name="speed">正規化された速度値（非推奨）</param>
        [System.Obsolete("Use SetRealSpeed(float actualSpeed) instead for better animation control")]
        public void SetSpeed(float speed)
        {
            if (hasSpeedParameter && animator != null)
            {
                animator.SetFloat(speedParameterHash, speed);
                
                if (debugSpeed)
                {
                    Debug.Log($"[CompanionAnimatorController] Set Normalized Speed: {speed:F2} (deprecated)");
                }
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
        
        /// <summary>
        /// 手動で角度を設定
        /// </summary>
        public void SetAngle(float angle)
        {
            if (!hasAngleParameter) return;
            
            // 確実な0-360度正規化
            targetAngle = NormalizeAngle360(angle);
            
            if (!smoothAngleTransition)
            {
                currentAngle = targetAngle;
                if (animator != null)
                {
                    float normalizedAngle = NormalizeAngle360(currentAngle);
                    animator.SetFloat(angleParameterHash, normalizedAngle);
                }
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
        /// スムーズ遷移の有効/無効を設定
        /// </summary>
        public void SetSmoothTransition(bool enabled, float speed = 5f)
        {
            smoothAngleTransition = enabled;
            smoothSpeed = speed;
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