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
        [SerializeField] private string deathParameterName = "Death";
        [SerializeField] private string attackParameterName = "Attack";
        [SerializeField] private string digParameterName = "Dig";
        [SerializeField] private string speedParameterName = "Speed";
        
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
        
        // パラメータ存在フラグ
        private bool hasAngleParameter;
        private bool hasDeathParameter;
        private bool hasAttackParameter;
        private bool hasDigParameter;
        private bool hasSpeedParameter;
        
        // 速度値のプロパティ
        public float IdleSpeed => idleSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        
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
                Debug.Log($"[PlayerAnimatorController] Using Inspector-assigned Animator: {animator.name}");
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
            
            // パラメータの存在チェック
            hasAngleParameter = HasParameter(angleParameterName);
            hasDeathParameter = HasParameter(deathParameterName);
            hasAttackParameter = HasParameter(attackParameterName);
            hasDigParameter = HasParameter(digParameterName);
            hasSpeedParameter = HasParameter(speedParameterName);
            
            // 存在しないパラメータをログ出力
            if (!hasAngleParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{angleParameterName}' not found in Animator");
            if (!hasDeathParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{deathParameterName}' not found in Animator");
            if (!hasAttackParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{attackParameterName}' not found in Animator");
            if (!hasDigParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{digParameterName}' not found in Animator");
            if (!hasSpeedParameter) Debug.LogWarning($"[PlayerAnimatorController] Parameter '{speedParameterName}' not found in Animator");
            
            // 利用可能なパラメータを一覧表示
            LogAvailableParameters();
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
        
        /// <summary>
        /// 利用可能なパラメータ一覧をログ出力
        /// </summary>
        private void LogAvailableParameters()
        {
            if (animator == null) return;
            
            Debug.Log($"[PlayerAnimatorController] Available Animator Parameters ({animator.parameters.Length} total):");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"  - {param.name} ({param.type})");
            }
        }
        
        private void UpdateAnimatorAngle(float angle)
        {
            if (animator == null || !hasAngleParameter) return;
            
            // Animatorパラメータを更新
            animator.SetFloat(angleParameterHash, angle);
        }
        
        /// <summary>
        /// 手動で角度を設定
        /// </summary>
        public void SetAngle(float angle)
        {
            if (!hasAngleParameter) return;
            
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
                Debug.Log($"[PlayerAnimatorController] Triggering Attack animation for weapon");
                animator.SetTrigger(attackParameterHash);
            }
            else if (animator == null)
            {
                Debug.LogWarning("[PlayerAnimatorController] Cannot trigger Attack - Animator is null");
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimatorController] Cannot trigger Attack - Parameter '{attackParameterName}' not found in Animator");
            }
        }
        
        /// <summary>
        /// 掘削アニメーションを再生（つるはし用）
        /// </summary>
        public void TriggerDig()
        {
            if (animator != null && hasDigParameter)
            {
                Debug.Log($"[PlayerAnimatorController] Triggering Dig animation for pickaxe");
                animator.SetTrigger(digParameterHash);
            }
            else if (animator == null)
            {
                Debug.LogWarning("[PlayerAnimatorController] Cannot trigger Dig - Animator is null");
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimatorController] Cannot trigger Dig - Parameter '{digParameterName}' not found in Animator");
                // Digパラメーターがない場合はAttackで代用
                if (hasAttackParameter)
                {
                    Debug.Log("[PlayerAnimatorController] Fallback: Using Attack animation for dig");
                    animator.SetTrigger(attackParameterHash);
                }
            }
        }
        
        /// <summary>
        /// Deathアニメーションを再生
        /// </summary>
        public void TriggerDeath()
        {
            if (animator != null && hasDeathParameter)
            {
                Debug.Log($"[PlayerAnimatorController] Triggering Death animation");
                animator.SetTrigger(deathParameterHash);
            }
            else if (animator == null)
            {
                Debug.LogWarning("[PlayerAnimatorController] Cannot trigger Death - Animator is null");
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimatorController] Cannot trigger Death - Parameter '{deathParameterName}' not found in Animator");
            }
        }
        
        /// <summary>
        /// 移動速度を設定（歩行・走行・しゃがみをすべて速度値で管理）
        /// </summary>
        /// <param name="speed">速度値（Inspectorで設定された値に対応）</param>
        public void SetSpeed(float speed)
        {
            if (animator != null && hasSpeedParameter)
            {
                animator.SetFloat(speedParameterHash, speed);
            }
        }
        
        /// <summary>
        /// 速度値をランタイムで変更
        /// </summary>
        public void SetSpeedValues(float idle, float crouch, float walk, float run)
        {
            idleSpeed = idle;
            crouchSpeed = crouch;
            walkSpeed = walk;
            runSpeed = run;
        }
        
        /// <summary>
        /// 現在の速度設定を取得
        /// </summary>
        public (float idle, float crouch, float walk, float run) GetSpeedValues()
        {
            return (idleSpeed, crouchSpeed, walkSpeed, runSpeed);
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
        /// 速度値の妥当性を検証
        /// </summary>
        private void ValidateSpeedValues()
        {
            // 負の値を防ぐ
            if (idleSpeed < 0f) idleSpeed = 0f;
            if (crouchSpeed < 0f) crouchSpeed = 0f;
            if (walkSpeed < 0f) walkSpeed = 0f;
            if (runSpeed < 0f) runSpeed = 0f;
            
            // 論理的な順序を確認（警告のみ）
            if (crouchSpeed > walkSpeed)
                Debug.LogWarning("[PlayerAnimatorController] Crouch speed is higher than walk speed. This might cause unexpected animation behavior.");
            
            if (walkSpeed > runSpeed)
                Debug.LogWarning("[PlayerAnimatorController] Walk speed is higher than run speed. This might cause unexpected animation behavior.");
        }
    }
}