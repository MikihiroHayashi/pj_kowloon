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
        
        // 速度しきい値のプロパティ
        public float IdleSpeedThreshold => idleSpeedThreshold;
        public float CrouchSpeedThreshold => crouchSpeedThreshold;
        public float WalkSpeedThreshold => walkSpeedThreshold;
        public float RunSpeedThreshold => runSpeedThreshold;
        
        // 現在の実際の速度値
        private float currentRealSpeed = 0f;
        public float CurrentRealSpeed => currentRealSpeed;
        
        private void Awake()
        {
            // Animatorを自動取得（Inspector設定が優先）
            if (animator == null && autoFindAnimator)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            // ターゲットTransformを自動取得（Inspector設定が優先）
            if (targetTransform == null && autoFindTargetTransform)
            {
                targetTransform = transform;
            }
            
            // パラメータハッシュを事前計算
            if (animator != null)
            {
                CacheParameterHashes();
            }
            else
            {
                Debug.LogWarning("[PlayerAnimatorController] No Animator assigned in Inspector and auto-find failed");
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
                // LerpAngle結果も正規化して累積を防ぐ
                currentAngle = NormalizeAngle360(currentAngle);
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
                Debug.Log($"Player Angle: {currentAngle:F1}° (Target: {targetAngle:F1}°)");
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
        /// アニメーターパラメータハッシュをキャッシュ
        /// </summary>
        private void CacheParameterHashes()
        {
            if (animator == null) return;
            
            angleParameterHash = Animator.StringToHash(angleParameterName);
            deathParameterHash = Animator.StringToHash(deathParameterName);
            attackParameterHash = Animator.StringToHash(attackParameterName);
            digParameterHash = Animator.StringToHash(digParameterName);
            speedParameterHash = Animator.StringToHash(speedParameterName);
            dodgeParameterHash = Animator.StringToHash(dodgeParameterName);
            crouchParameterHash = Animator.StringToHash(crouchParameterName);
            
            // パラメータの存在チェック
            hasAngleParameter = HasParameter(angleParameterName);
            hasDeathParameter = HasParameter(deathParameterName);
            hasAttackParameter = HasParameter(attackParameterName);
            hasDigParameter = HasParameter(digParameterName);
            hasSpeedParameter = HasParameter(speedParameterName);
            hasDodgeParameter = HasParameter(dodgeParameterName);
            hasCrouchParameter = HasParameter(crouchParameterName);
            
            // 存在しないパラメータをログ出力（重要な警告のみ）
            if (!hasAngleParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{angleParameterName}' not found in Animator");
            if (!hasSpeedParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{speedParameterName}' not found in Animator");
        }
        
        /// <summary>
        /// パラメータが存在するかチェック
        /// </summary>
        private bool HasParameter(string parameterName)
        {
            if (animator == null) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == parameterName)
                {
                    return true;
                }
            }
            return false;
        }
        
        
        private void UpdateAnimatorAngle(float angle)
        {
            if (animator == null || !hasAngleParameter) return;
            
            // 角度を正規化してからAnimatorパラメータを更新
            float normalizedAngle = NormalizeAngle360(angle);
            animator.SetFloat(angleParameterHash, normalizedAngle);
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
                CacheParameterHashes();
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
                CacheParameterHashes();
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
        
        /// <summary>
        /// 攻撃アニメーションを再生（鉄パイプなど武器用）
        /// </summary>
        public void TriggerAttack()
        {
            if (animator != null && hasAttackParameter)
            {
                animator.SetTrigger(attackParameterHash);
            }
        }
        
        /// <summary>
        /// 掘削アニメーションを再生（つるはし用）
        /// </summary>
        public void TriggerDig()
        {
            if (animator != null && hasDigParameter)
            {
                animator.SetTrigger(digParameterHash);
            }
            else if (animator != null && hasAttackParameter)
            {
                // Digパラメーターがない場合はAttackで代用
                animator.SetTrigger(attackParameterHash);
            }
        }
        
        /// <summary>
        /// Deathアニメーションを再生
        /// </summary>
        public void TriggerDeath()
        {
            if (animator != null && hasDeathParameter)
            {
                animator.SetTrigger(deathParameterHash);
            }
        }

        /// <summary>
        /// Dodgeアニメーションを再生
        /// </summary>
        public void TriggerDodge()
        {
            if (animator != null && hasDodgeParameter)
            {
                animator.SetTrigger(dodgeParameterHash);
            }
        }
        
        /// <summary>
        /// 実際の移動速度を設定（単位/秒）
        /// </summary>
        /// <param name="actualSpeed">実際の移動速度 (単位/秒)</param>
        public void SetRealSpeed(float actualSpeed)
        {
            currentRealSpeed = actualSpeed;
            
            if (animator != null && hasSpeedParameter)
            {
                animator.SetFloat(speedParameterHash, actualSpeed);
                
                if (debugSpeed)
                {
                    Debug.Log($"[PlayerAnimatorController] Set Real Speed: {actualSpeed:F2} units/sec");
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
            if (animator != null && hasSpeedParameter)
            {
                animator.SetFloat(speedParameterHash, speed);
                
            }
        }
        
        /// <summary>
        /// しゃがみ状態を設定
        /// </summary>
        /// <param name="isCrouching">しゃがみ中かどうか</param>
        public void SetCrouch(bool isCrouching)
        {
            if (animator != null && hasCrouchParameter)
            {
                animator.SetBool(crouchParameterHash, isCrouching);
            }
        }
        
        /// <summary>
        /// 速度しきい値をランタイムで変更
        /// </summary>
        public void SetSpeedThresholds(float idle, float crouch, float walk, float run)
        {
            idleSpeedThreshold = idle;
            crouchSpeedThreshold = crouch;
            walkSpeedThreshold = walk;
            runSpeedThreshold = run;
        }
        
        /// <summary>
        /// 現在の速度しきい値を取得
        /// </summary>
        public (float idle, float crouch, float walk, float run) GetSpeedThresholds()
        {
            return (idleSpeedThreshold, crouchSpeedThreshold, walkSpeedThreshold, runSpeedThreshold);
        }
        
        /// <summary>
        /// 実際の速度から移動状態を判定
        /// </summary>
        public string GetMovementStateFromSpeed()
        {
            if (currentRealSpeed <= idleSpeedThreshold)
                return "Idle";
            else if (currentRealSpeed <= crouchSpeedThreshold)
                return "Crouch";
            else if (currentRealSpeed <= walkSpeedThreshold)
                return "Walk";
            else
                return "Run";
        }
        
        private void OnValidate()
        {
            // エディターでパラメータが変更された時の処理
            if (Application.isPlaying && animator != null)
            {
                CacheParameterHashes();
            }
            
            // 速度値の検証
            ValidateSpeedValues();
        }
        
        /// <summary>
        /// 速度しきい値の妥当性を検証
        /// </summary>
        private void ValidateSpeedValues()
        {
            // 負の値を防ぐ
            if (idleSpeedThreshold < 0f) idleSpeedThreshold = 0f;
            if (crouchSpeedThreshold < 0f) crouchSpeedThreshold = 0f;
            if (walkSpeedThreshold < 0f) walkSpeedThreshold = 0f;
            if (runSpeedThreshold < 0f) runSpeedThreshold = 0f;
            
            // 論理的な順序を確認（警告は削除）
        }
    }
}