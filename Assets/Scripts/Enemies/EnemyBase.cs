using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Player;

namespace KowloonBreak.Enemies
{
    public enum EnemyState
    {
        Patrol,    // パトロール（待機含む）
        Chase,     // 追跡
        Return     // 復帰
    }
    
    public class EnemyBase : MonoBehaviour, IDestructible
    {
        // 視覚状態の色定数
        private static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f, 1f);
        [Header("Enemy Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float attackCooldown = 2f;

        [Header("Vision System")]
        [SerializeField] protected float visionAngle = 120f; // 視野角度
        [SerializeField] protected LayerMask visionBlockingLayers = -1; // 視線を遮るレイヤー
        [SerializeField] protected bool useLineOfSight = true; // 視線チェックを使用するか


        [Header("Base Movement")]
        [SerializeField] protected float moveSpeed = 3f;  // ベース移動速度
        
        [Header("Stealth Detection")]
        [SerializeField] protected float crouchDetectionMultiplier = 0.3f; // しゃがみ時の視覚検知範囲倍率
        [SerializeField] protected float minDetectionChance = 0.1f;
        [SerializeField] protected float maxDetectionChance = 0.9f;

        [Header("Navigation & Avoidance")]
        [SerializeField] protected float obstacleDetectionRange = 3f;
        [SerializeField] protected LayerMask obstacleLayerMask = -1;
        [SerializeField] protected float avoidanceRadius = 1.5f;
        [SerializeField] protected float avoidanceStrength = 2f;
        [SerializeField] protected bool enableObstacleAvoidance = true;
        [SerializeField] protected bool avoidPlayer = false;
        
        [Header("Patrol System")]
        [SerializeField] protected PatrolRoute patrolRoute;
        [SerializeField] protected float lostPlayerReturnDelay = 5f;
        [SerializeField] protected bool enablePatrol = true;
        
        [Header("Speed Multipliers (Base: Move Speed)")]
        [SerializeField] protected float chaseSpeedMultiplier = 1.2f;    // 追跡時の速度倍率 (Move Speed × この値)
        [SerializeField] protected float returnSpeedMultiplier = 1.0f;   // 復帰時の速度倍率 (Move Speed × この値)

        [Header("Drop Items")]
        [SerializeField] protected ItemData[] dropItems;
        [SerializeField] protected int[] dropQuantities;
        [SerializeField] protected float[] dropChances;

        [Header("Components")]
        [SerializeField] protected NavMeshAgent navAgent;
        [SerializeField] protected Animator animator;
        [SerializeField] protected Collider enemyCollider;
        [SerializeField] protected Renderer modelRenderer;

        [Header("HP Bar")]
        [SerializeField] protected Slider healthBar;

        [Header("Damage Display")]
        [SerializeField] protected Transform damageDisplayPoint;

        protected Transform player;
        protected EnhancedPlayerController playerController;
        protected float lastAttackTime;
        protected bool isDead = false;

        // 検知システム関連
        protected bool playerDetected = false;
        protected float playerDetectionTime = 0f;
        protected float detectionMemoryDuration = 5f;
        protected float lastDetectionCheck = 0f;
        protected bool lastDetectionResult = false;
        protected float attackDetectionRange = 8f; // 攻撃時の感知範囲

        // 障害物回避関連
        protected Vector3 avoidanceDirection;
        protected float lastAvoidanceUpdateTime;
        protected const float AVOIDANCE_UPDATE_INTERVAL = 0.1f;
        
        // パトロールシステム関連
        protected EnemyState currentState = EnemyState.Patrol;
        protected int currentPatrolIndex = 0;
        protected float lastPlayerSeenTime = 0f;
        protected float waitStartTime = 0f;
        protected bool isWaitingAtPatrol = false;
        protected bool isMovingForward = true;
        protected Vector3 originalPosition;

        // Animation parameter names
        protected const string ANIM_SPEED = "Speed";
        protected const string ANIM_ATTACK = "Attack";
        protected const string ANIM_DEATH = "Death";

        // 公開プロパティ
        public Transform Player => player;
        public float VisionAngle => visionAngle;
        public float DetectionRange => detectionRange;
        public EnemyState CurrentState => currentState;
        public PatrolRoute PatrolRoute => patrolRoute;
        public int CurrentPatrolIndex => currentPatrolIndex;
        
        // 速度関連プロパティ
        public float BaseMoveSpeed => moveSpeed;
        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public float ReturnSpeedMultiplier => returnSpeedMultiplier;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;

            // NavMeshAgentの取得（設定はStartで行う）
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();

            // Animatorの取得
            if (animator == null)
                animator = GetComponent<Animator>();

            // Colliderの取得
            if (enemyCollider == null)
                enemyCollider = GetComponent<Collider>();

            // Model Rendererの取得（子オブジェクトから検索）
            if (modelRenderer == null)
                modelRenderer = GetComponentInChildren<Renderer>();

            // HPバーの初期化
            InitializeHealthBar();
        }

        protected virtual void Start()
        {
            FindPlayer();
            
            // NavMeshAgentの設定（継承先でのパラメータ設定後に実行）
            if (navAgent != null)
            {
                SetupNavMeshAgent();
            }
            
            InitializePatrol();
        }
        
        protected virtual void InitializePatrol()
        {
            originalPosition = transform.position;
            
            if (patrolRoute != null && patrolRoute.IsValidRoute())
            {
                // 最も近いパトロールポイントから開始
                currentPatrolIndex = patrolRoute.GetNearestPointIndex(transform.position);
            }
        }

        protected virtual void FindPlayer()
        {
            // タグ検索を優先
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                // コンポーネント検索をフォールバック
                playerController = FindObjectOfType<EnhancedPlayerController>();
                if (playerController != null)
                {
                    player = playerController.transform;
                    return;
                }
            }
            else
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<EnhancedPlayerController>();
                if (playerController == null)
                {
                    playerController = playerObj.GetComponentInChildren<EnhancedPlayerController>();
                }
            }
        }

        protected virtual void Update()
        {
            if (isDead || player == null) return;

            bool canDetectPlayer = UpdatePlayerDetection();
            
            // 統合された移動システム
            UpdateMovement(canDetectPlayer);
            
            UpdateAnimations();
            CheckNavMeshAgentStatus();
            UpdateHealthBar();
        }
        
        /// <summary>
        /// 統合された移動制御システム
        /// </summary>
        protected virtual void UpdateMovement(bool canSeePlayer)
        {
            switch (currentState)
            {
                case EnemyState.Patrol:
                    if (canSeePlayer)
                    {
                        ChangeState(EnemyState.Chase);
                    }
                    else
                    {
                        UpdatePatrol();
                    }
                    break;
                    
                case EnemyState.Chase:
                    if (canSeePlayer)
                    {
                        lastPlayerSeenTime = Time.time;
                        UpdateChase();
                    }
                    else if (Time.time - lastPlayerSeenTime > lostPlayerReturnDelay)
                    {
                        ChangeState(EnemyState.Return);
                    }
                    else
                    {
                        // プレイヤーを見失ったが、まだ記憶している間は最後の位置を探索
                        UpdateChase();
                    }
                    break;
                    
                case EnemyState.Return:
                    UpdateReturn();
                    break;
            }
        }
        
        /// <summary>
        /// 状態変更処理
        /// </summary>
        protected virtual void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            
            EnemyState previousState = currentState;
            currentState = newState;
            
            OnStateChanged(previousState, newState);
        }
        
        /// <summary>
        /// 状態変更時の処理
        /// </summary>
        protected virtual void OnStateChanged(EnemyState from, EnemyState to)
        {
            switch (to)
            {
                case EnemyState.Patrol:
                    // パトロール復帰時の処理
                    float patrolSpeed = GetPatrolSpeed();
                    SetMovementSpeed(patrolSpeed);
                    Debug.Log($"[{gameObject.name}] State changed to Patrol. Speed: {patrolSpeed}");
                    break;
                    
                case EnemyState.Chase:
                    // 追跡開始時の処理
                    lastPlayerSeenTime = Time.time;
                    float chaseSpeed = GetChaseSpeed();
                    SetMovementSpeed(chaseSpeed);
                    Debug.Log($"[{gameObject.name}] State changed to Chase. Speed: {chaseSpeed} (Move Speed: {moveSpeed} × {chaseSpeedMultiplier})");
                    break;
                    
                case EnemyState.Return:
                    // 復帰開始時の処理
                    float returnSpeed = GetReturnSpeed();
                    SetMovementSpeed(returnSpeed);
                    Debug.Log($"[{gameObject.name}] State changed to Return. Speed: {returnSpeed} (Move Speed: {moveSpeed} × {returnSpeedMultiplier})");
                    break;
            }
        }
        
        /// <summary>
        /// パトロール時の移動速度を取得
        /// </summary>
        protected virtual float GetPatrolSpeed()
        {
            // PatrolRouteに設定された速度を優先し、なければMove Speed
            return patrolRoute != null && patrolRoute.patrolSpeed > 0 
                ? patrolRoute.patrolSpeed 
                : moveSpeed;
        }
        
        /// <summary>
        /// 追跡時の移動速度を取得 (Move Speed × Chase Multiplier)
        /// </summary>
        protected virtual float GetChaseSpeed()
        {
            return moveSpeed * chaseSpeedMultiplier;
        }
        
        /// <summary>
        /// 復帰時の移動速度を取得 (Move Speed × Return Multiplier)
        /// </summary>
        protected virtual float GetReturnSpeed()
        {
            return moveSpeed * returnSpeedMultiplier;
        }
        
        /// <summary>
        /// NavMeshAgentの移動速度を設定
        /// </summary>
        protected virtual void SetMovementSpeed(float speed)
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.speed = speed;
                // 加速度も速度に比例して調整
                navAgent.acceleration = speed * 2f;
            }
        }

        /// <summary>
        /// プレイヤー検知システムのメイン更新メソッド（統合版）
        /// </summary>
        protected virtual bool UpdatePlayerDetection()
        {
            float currentTime = Time.time;

            // 現在の視界・検知状況をチェック
            bool currentlyCanDetect = CanSeePlayer();

            if (playerDetected)
            {
                // 既に検知済みの場合
                if (currentlyCanDetect)
                {
                    // まだ検知できる場合は記憶時間を更新
                    playerDetectionTime = currentTime;
                }
                else
                {
                    // 検知できない場合は記憶期間をチェック
                    if (currentTime - playerDetectionTime > detectionMemoryDuration)
                    {
                        playerDetected = false;
                    }
                }
            }
            else
            {
                // 未検知の場合
                if (currentlyCanDetect)
                {
                    playerDetected = true;
                    playerDetectionTime = currentTime;
                }
            }

            return playerDetected;
        }

        /// <summary>
        /// パトロール更新処理
        /// </summary>
        protected virtual void UpdatePatrol()
        {
            if (!enablePatrol || patrolRoute == null || !patrolRoute.IsValidRoute())
            {
                StopMoving();
                return;
            }
            
            var currentPoint = patrolRoute.GetPoint(currentPatrolIndex);
            if (currentPoint.transform == null) return;
            
            float distance = Vector3.Distance(transform.position, currentPoint.transform.position);
            
            if (distance < 1f && !isWaitingAtPatrol)
            {
                // パトロールポイントに到着
                isWaitingAtPatrol = true;
                waitStartTime = Time.time;
                StopMoving();
                
                // 到着時の処理（向きを変える等）
                OnPatrolPointReached(currentPatrolIndex);
            }
            else if (isWaitingAtPatrol)
            {
                // 待機中
                if (Time.time - waitStartTime >= currentPoint.waitTime)
                {
                    MoveToNextPatrolPoint();
                }
            }
            else
            {
                // パトロールポイントに向かって移動
                MoveToTarget(currentPoint.transform.position);
            }
        }
        
        /// <summary>
        /// 追跡更新処理
        /// </summary>
        protected virtual void UpdateChase()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float effectiveAttackRange = GetEffectiveAttackRange();
            
            if (distanceToPlayer <= effectiveAttackRange)
            {
                StopMoving();
                TryAttack();
            }
            else
            {
                MoveToTarget(player.position);
            }
        }
        
        /// <summary>
        /// 復帰更新処理
        /// </summary>
        protected virtual void UpdateReturn()
        {
            Vector3 targetPosition;
            
            if (patrolRoute != null && patrolRoute.IsValidRoute())
            {
                // パトロールルートがある場合は最も近いポイントに復帰
                int nearestIndex = patrolRoute.GetNearestPointIndex(transform.position);
                var nearestPoint = patrolRoute.GetPoint(nearestIndex);
                
                if (nearestPoint.transform != null)
                {
                    targetPosition = nearestPoint.transform.position;
                    
                    // 復帰完了判定
                    if (Vector3.Distance(transform.position, targetPosition) < 1.5f)
                    {
                        currentPatrolIndex = nearestIndex;
                        isWaitingAtPatrol = false;
                        ChangeState(EnemyState.Patrol);
                        return;
                    }
                }
                else
                {
                    targetPosition = originalPosition;
                }
            }
            else
            {
                // パトロールルートがない場合は元の位置に復帰
                targetPosition = originalPosition;
                
                // 復帰完了判定
                if (Vector3.Distance(transform.position, targetPosition) < 1.5f)
                {
                    ChangeState(EnemyState.Patrol);
                    return;
                }
            }
            
            MoveToTarget(targetPosition);
        }
        
        /// <summary>
        /// 統一された移動メソッド（旧MoveToPlayerを拡張）
        /// </summary>
        protected virtual void MoveToTarget(Vector3 targetPosition)
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                // 障害物回避が有効な場合
                if (enableObstacleAvoidance)
                {
                    targetPosition = GetAvoidanceAdjustedPosition(targetPosition);
                }

                navAgent.SetDestination(targetPosition);
            }
        }
        
        /// <summary>
        /// 次のパトロールポイントに移動
        /// </summary>
        protected virtual void MoveToNextPatrolPoint()
        {
            if (patrolRoute == null || !patrolRoute.IsValidRoute()) return;
            
            // 方向転換が必要かチェック
            if (patrolRoute.ShouldChangeDirection(currentPatrolIndex, isMovingForward))
            {
                isMovingForward = !isMovingForward;
            }
            
            // 次のインデックスを取得
            currentPatrolIndex = patrolRoute.GetNextIndex(currentPatrolIndex, isMovingForward);
            isWaitingAtPatrol = false;
        }
        
        /// <summary>
        /// パトロールポイント到着時の処理
        /// </summary>
        protected virtual void OnPatrolPointReached(int pointIndex)
        {
            // 継承先でカスタマイズ可能
        }

        protected virtual void StopMoving()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                bool hadPath = navAgent.hasPath;
                navAgent.ResetPath();

            }
        }

        protected virtual void TryAttack()
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            bool canAttack = timeSinceLastAttack >= attackCooldown;


            if (canAttack)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        protected virtual void PerformAttack()
        {
            // 攻撃アニメーション再生（ダメージはアニメーションイベントで実行）
            if (animator != null)
            {
                animator.SetTrigger(ANIM_ATTACK);
            }
        }

        /// <summary>
        /// アニメーションイベントから呼ばれるダメージ実行
        /// </summary>
        public virtual void ExecuteAttackDamage()
        {
            // プレイヤーにダメージを与える処理
            if (player != null)
            {
                var enhancedPlayerController = player.GetComponent<EnhancedPlayerController>();

                // 見つからない場合は親階層で検索
                if (enhancedPlayerController == null)
                {
                    enhancedPlayerController = player.GetComponentInParent<EnhancedPlayerController>();
                }

                // 見つからない場合は子階層で検索
                if (enhancedPlayerController == null)
                {
                    enhancedPlayerController = player.GetComponentInChildren<EnhancedPlayerController>();
                }

                if (enhancedPlayerController != null)
                {
                    enhancedPlayerController.TakeDamage(attackDamage);
                }
            }
        }

        protected virtual void UpdateAnimations()
        {
            if (animator == null) return;

            // 移動速度をAnimatorに設定
            float speed = navAgent != null ? navAgent.velocity.magnitude : 0f;
            animator.SetFloat(ANIM_SPEED, speed);
        }

        // IDestructible interface implementation
        public virtual bool CanBeDestroyedBy(ToolType toolType)
        {
            // 全ての武器で攻撃可能（必要に応じて個別に制限可能）
            return toolType == ToolType.Pickaxe || toolType == ToolType.IronPipe;
        }

        public virtual void TakeDamage(float damage, ToolType toolType)
        {
            if (isDead) return;

            // 攻撃を受けたら必ず発見状態になる（しゃがみ状態でも）
            bool wasUndetected = !playerDetected;
            playerDetected = true;
            playerDetectionTime = Time.time;

            // 確実に追跡状態を維持するため、記憶期間を延長
            detectionMemoryDuration = 10f; // 5秒→10秒に延長

            // 攻撃を周囲の敵に通知
            if (player != null)
            {
                NotifyPlayerAttack(player.position);
            }

            // ステルス攻撃の判定（プレイヤーが発見されていない場合）
            bool isStealthAttack = wasUndetected;
            float finalDamage = damage;

            if (isStealthAttack)
            {
                finalDamage *= 3f;

                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification("ステルス攻撃成功！", UI.NotificationType.Success);
                }
            }

            currentHealth -= finalDamage;

            // HPバーを更新
            UpdateHealthBar();

            // ダメージテキストを表示
            ShowDamageText(finalDamage, isStealthAttack);

            // ダメージエフェクトを開始
            if (modelRenderer != null && modelRenderer.material != null)
            {
                StartCoroutine(DamageEffect());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // IDestructibleプロパティ
        public bool IsDestroyed => isDead;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        // 既存のTakeDamageメソッドをオーバーロードとして残す
        public virtual void TakeDamage(float damage)
        {
            Debug.Log($"[EnemyBase] {gameObject.name} - TakeDamage called with damage: {damage}");
            TakeDamage(damage, ToolType.IronPipe); // デフォルトツール
        }

        /// <summary>
        /// 強制的にプレイヤーを発見状態にする（攻撃を受けた時など）
        /// </summary>
        public virtual void ForceDetectPlayer()
        {
            if (player != null)
            {
                playerDetected = true;
                playerDetectionTime = Time.time;
            }
        }

        private System.Collections.IEnumerator DamageEffect()
        {
            // _Damage_Amountを1に設定
            modelRenderer.material.SetFloat("_Damage_Amount", 1f);

            // 0.1秒待機
            yield return new WaitForSeconds(0.1f);

            // _Damage_Amountを0に戻す
            if (modelRenderer != null && modelRenderer.material != null)
            {
                modelRenderer.material.SetFloat("_Damage_Amount", 0f);
            }
        }

        protected virtual void Die()
        {
            isDead = true;

            // NavMeshAgentを停止
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }

            // 死亡アニメーション再生
            if (animator != null)
            {
                animator.SetTrigger(ANIM_DEATH);
                // アニメーション完了後に削除するコルーチンを開始
                StartCoroutine(WaitForDeathAnimation());
            }
            else
            {
                // Animatorがない場合は即座に削除処理
                DestroyEnemy();
            }

            // コライダーを無効化
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            // アイテムドロップ
            DropItems();
        }

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

        private System.Collections.IEnumerator WaitForDeathAnimation()
        {

            // Deathアニメーションが開始されるまで少し待機
            yield return new WaitForSeconds(0.1f);

            int maxWaitFrames = 300; // 5秒間の最大待機
            int waitFrames = 0;

            // Deathアニメーションを探す
            while (waitFrames < maxWaitFrames)
            {
                if (animator == null)
                {
                    break;
                }

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                // Deathアニメーションが再生中かチェック
                if (stateInfo.IsName("Death") || stateInfo.IsTag("Death"))
                {

                    // アニメーションが完了するまで待機
                    while (stateInfo.normalizedTime < 1.0f)
                    {
                        yield return null;
                        if (animator == null) break;
                        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    }

                    break;
                }

                waitFrames++;
                yield return null;
            }

            if (waitFrames >= maxWaitFrames)
            {
            }

            // アニメーション完了後にオブジェクトを削除
            DestroyEnemy();
        }

        private void DestroyEnemy()
        {
            Destroy(gameObject);
        }
        
        /// <summary>
        /// パトロールルートを設定
        /// </summary>
        public virtual void SetPatrolRoute(PatrolRoute route)
        {
            patrolRoute = route;
            InitializePatrol();
        }
        
        /// <summary>
        /// パトロールを有効/無効にする
        /// </summary>
        public virtual void SetPatrolEnabled(bool enabled)
        {
            enablePatrol = enabled;
            
            if (!enabled && currentState == EnemyState.Patrol)
            {
                StopMoving();
            }
        }
        
        /// <summary>
        /// 強制的にパトロール状態に戻す
        /// </summary>
        public virtual void ForceReturnToPatrol()
        {
            ChangeState(EnemyState.Return);
        }
        
        /// <summary>
        /// 追跡時の速度倍率を設定
        /// </summary>
        public virtual void SetChaseSpeedMultiplier(float multiplier)
        {
            chaseSpeedMultiplier = multiplier;
            
            // 現在追跡中の場合は即座に速度を更新
            if (currentState == EnemyState.Chase)
            {
                SetMovementSpeed(GetChaseSpeed());
            }
        }
        
        
        /// <summary>
        /// 復帰時の速度倍率を設定
        /// </summary>
        public virtual void SetReturnSpeedMultiplier(float multiplier)
        {
            returnSpeedMultiplier = multiplier;
            
            // 現在復帰中の場合は即座に速度を更新
            if (currentState == EnemyState.Return)
            {
                SetMovementSpeed(GetReturnSpeed());
            }
        }
        
        /// <summary>
        /// 現在の移動速度を取得
        /// </summary>
        public virtual float GetCurrentMovementSpeed()
        {
            return navAgent != null ? navAgent.speed : 0f;
        }
        
        /// <summary>
        /// 各状態の速度設定値を取得
        /// </summary>
        public virtual (float patrol, float chase, float returnSpeed) GetSpeedSettings()
        {
            return (GetPatrolSpeed(), GetChaseSpeed(), GetReturnSpeed());
        }

        protected virtual void DropItems()
        {
            if (dropItems == null || dropItems.Length == 0) return;

            for (int i = 0; i < dropItems.Length; i++)
            {
                if (dropItems[i] == null) continue;

                // ドロップ確率をチェック
                float chance = i < dropChances.Length ? dropChances[i] : 1f;
                if (Random.Range(0f, 1f) > chance) continue;

                // ドロップ数を決定
                int quantity = i < dropQuantities.Length ? dropQuantities[i] : 1;

                // アイテムをドロップ
                for (int j = 0; j < quantity; j++)
                {
                    Vector3 dropPosition = transform.position + Random.insideUnitSphere * 1f;
                    dropPosition.y = transform.position.y;

                    if (dropItems[i].droppedItemPrefab != null)
                    {
                        GameObject droppedItem = Instantiate(dropItems[i].droppedItemPrefab, dropPosition, Quaternion.identity);

                        // ドロップアイテムにランダムな力を加える
                        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 randomForce = new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), Random.Range(-2f, 2f));
                            rb.AddForce(randomForce, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        #region Animation Event Methods

        /// <summary>
        /// 攻撃アニメーション終了時の処理
        /// </summary>
        public virtual void OnAttackAnimationEnd()
        {
            // 攻撃終了処理
        }

        /// <summary>
        /// アニメーションから足音を再生
        /// </summary>
        public virtual void PlayFootstepFromAnimation()
        {
            // 足音再生処理
        }

        /// <summary>
        /// 死亡アニメーション完了時の処理
        /// </summary>
        public virtual void OnDeathAnimationComplete()
        {
            DestroyEnemy();
        }

        /// <summary>
        /// カスタムアニメーションイベントの処理
        /// </summary>
        public virtual void HandleCustomAnimationEvent(string eventName)
        {
            switch (eventName)
            {
                case "spawn_effect":
                    // スポーンエフェクト
                    break;
                case "roar":
                    // 咆哮エフェクト
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Navigation and Obstacle Avoidance

        /// <summary>
        /// NavMeshAgentの設定を最適化
        /// </summary>
        protected virtual void SetupNavMeshAgent()
        {
            float initialSpeed = GetPatrolSpeed();
            SetMovementSpeed(initialSpeed); // 初期状態はパトロール速度
            
            // プレイヤーとの体感速度を合わせるための調整
            navAgent.acceleration = 50f;      // 加速度を上げる（デフォルト：8）
            navAgent.angularSpeed = 360f;     // 回転速度を上げる（デフォルト：120）
            navAgent.stoppingDistance = 0.1f; // 停止距離を短く（デフォルト：0.5）
            
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            navAgent.avoidancePriority = 50; // 中程度の優先度
            navAgent.radius = avoidanceRadius * 0.5f; // エージェントの半径
            
            Debug.Log($"[{gameObject.name}] NavMeshAgent setup completed. Initial patrol speed: {initialSpeed} (Move Speed: {moveSpeed}, chase: ×{chaseSpeedMultiplier})");
        }

        /// <summary>
        /// Colliderサイズを考慮した有効攻撃範囲を取得
        /// </summary>
        protected virtual float GetEffectiveAttackRange()
        {
            float baseRange = attackRange;
            float colliderBonus = 0f;

            // 自分のColliderサイズを考慮
            if (enemyCollider != null)
            {
                if (enemyCollider is CapsuleCollider capsule)
                {
                    colliderBonus += capsule.radius;
                }
                else if (enemyCollider is SphereCollider sphere)
                {
                    colliderBonus += sphere.radius;
                }
                else if (enemyCollider is BoxCollider box)
                {
                    colliderBonus += Mathf.Min(box.size.x, box.size.z) * 0.5f;
                }
            }

            // プレイヤーのColliderサイズを考慮
            if (player != null)
            {
                Collider playerCollider = player.GetComponent<Collider>();
                if (playerCollider == null)
                    playerCollider = player.GetComponentInChildren<Collider>();

                if (playerCollider != null)
                {
                    if (playerCollider is CapsuleCollider playerCapsule)
                    {
                        colliderBonus += playerCapsule.radius;
                    }
                    else if (playerCollider is SphereCollider playerSphere)
                    {
                        colliderBonus += playerSphere.radius;
                    }
                    else if (playerCollider is BoxCollider playerBox)
                    {
                        colliderBonus += Mathf.Min(playerBox.size.x, playerBox.size.z) * 0.5f;
                    }
                }
            }

            // 少し余裕を持たせる
            return baseRange + colliderBonus + 0.2f;
        }

        /// <summary>
        /// 障害物回避を考慮した目標位置を取得
        /// </summary>
        protected virtual Vector3 GetAvoidanceAdjustedPosition(Vector3 originalTarget)
        {
            // 定期的に障害物をチェック
            if (Time.time - lastAvoidanceUpdateTime >= AVOIDANCE_UPDATE_INTERVAL)
            {
                UpdateAvoidanceDirection();
                lastAvoidanceUpdateTime = Time.time;
            }

            // 回避方向がある場合は目標位置を調整
            if (avoidanceDirection != Vector3.zero)
            {
                Vector3 adjustedTarget = originalTarget + avoidanceDirection * avoidanceStrength;
                return adjustedTarget;
            }

            return originalTarget;
        }

        /// <summary>
        /// 障害物検知と回避方向の更新
        /// </summary>
        protected virtual void UpdateAvoidanceDirection()
        {
            avoidanceDirection = Vector3.zero;

            // 前方の障害物をチェック
            Vector3 forward = transform.forward;
            Vector3 checkPosition = transform.position + forward * obstacleDetectionRange;

            // 球体で障害物を検知
            Collider[] obstacles = Physics.OverlapSphere(checkPosition, avoidanceRadius, obstacleLayerMask);

            Vector3 totalAvoidance = Vector3.zero;
            int obstacleCount = 0;

            foreach (var obstacle in obstacles)
            {
                // 自分自身と同じエネミーは常に除外
                if (obstacle.gameObject == gameObject || obstacle.GetComponent<EnemyBase>() != null)
                    continue;

                // プレイヤーの回避判定
                bool isPlayerObject = IsPlayer(obstacle.gameObject);
                if (isPlayerObject && !avoidPlayer)
                {
                    continue;
                }

                Vector3 obstacleDirection = obstacle.transform.position - transform.position;
                float distance = obstacleDirection.magnitude;

                if (distance > 0.1f && distance <= obstacleDetectionRange)
                {
                    // 障害物から離れる方向を計算
                    Vector3 avoidDirection = -obstacleDirection.normalized;

                    // 距離に基づいて強度を調整（近いほど強く回避）
                    float avoidanceIntensity = (obstacleDetectionRange - distance) / obstacleDetectionRange;

                    totalAvoidance += avoidDirection * avoidanceIntensity;
                    obstacleCount++;
                }
            }

            if (obstacleCount > 0)
            {
                avoidanceDirection = (totalAvoidance / obstacleCount).normalized;
            }
        }

        /// <summary>
        /// 経路が見つからない場合の代替移動
        /// </summary>
        protected virtual void HandlePathfindingFailure()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                // プレイヤー方向に直接移動を試行
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                Vector3 alternativeTarget = transform.position + directionToPlayer * 2f;

                navAgent.SetDestination(alternativeTarget);
            }
        }

        /// <summary>
        /// 指定されたゲームオブジェクトがプレイヤーかどうかを判定
        /// </summary>
        protected virtual bool IsPlayer(GameObject obj)
        {
            // プレイヤーの参照と直接比較
            if (player != null && obj == player.gameObject)
                return true;

            // プレイヤータグで判定
            if (obj.CompareTag("Player"))
                return true;

            // EnhancedPlayerControllerコンポーネントで判定
            if (obj.GetComponent<EnhancedPlayerController>() != null)
                return true;

            // 親オブジェクトにEnhancedPlayerControllerがある場合
            if (obj.GetComponentInParent<EnhancedPlayerController>() != null)
                return true;

            return false;
        }

        /// <summary>
        /// NavMeshAgentの状態をチェックして問題があれば対処
        /// </summary>
        protected virtual void CheckNavMeshAgentStatus()
        {
            if (navAgent == null || !navAgent.isActiveAndEnabled || isDead) return;

            // 経路が見つからない場合
            if (navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
            {
                HandlePathfindingFailure();
            }
            else if (navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                HandlePathfindingFailure();
            }

            // NavMeshから外れた場合
            if (!navAgent.isOnNavMesh)
            {
                // 最も近いNavMesh上の点を見つけて移動
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                }
            }
        }

        #endregion

        #region Detection System (修正版)

        /// <summary>
        /// 統合検知システム：プレイヤーの移動状態に応じた感知（修正版）
        /// </summary>
        protected virtual bool CanSeePlayer()
        {
            if (player == null) return false;

            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 playerPosition = player.position + Vector3.up * 1f;
            Vector3 directionToPlayer = (playerPosition - eyePosition).normalized;
            float distanceToPlayer = Vector3.Distance(eyePosition, playerPosition);

            // プレイヤーの状態をチェック
            bool isPlayerCrouching = playerController != null && playerController.IsCrouching;
            bool isPlayerMoving = playerController != null && playerController.IsMoving;
            bool isPlayerRunning = playerController != null && playerController.IsRunning;

            // 基本的な距離チェック
            float effectiveDetectionRange = GetEffectiveDetectionRange(isPlayerCrouching, isPlayerMoving, isPlayerRunning);
            if (distanceToPlayer > effectiveDetectionRange)
            {
                return false;
            }

            // 移動状態による即座の検知判定
            if (CanDetectByMovement(isPlayerCrouching, isPlayerMoving, isPlayerRunning, distanceToPlayer))
            {
                return true;
            }

            // 視界による検知判定
            return CanDetectByVision(eyePosition, playerPosition, directionToPlayer, distanceToPlayer, isPlayerCrouching);
        }

        /// <summary>
        /// プレイヤーの状態に応じた有効検知範囲を取得
        /// </summary>
        protected virtual float GetEffectiveDetectionRange(bool isCrouching, bool isMoving, bool isRunning)
        {
            if (isRunning)
            {
                // 走っている場合は検知範囲が拡大
                return detectionRange * 1.3f;
            }
            else if (isMoving && !isCrouching)
            {
                // 歩いている場合は通常範囲
                return detectionRange;
            }
            else if (isCrouching)
            {
                // しゃがんでいる場合は縮小（視界のみ）
                return detectionRange * crouchDetectionMultiplier;
            }
            else
            {
                // 待機状態は通常範囲
                return detectionRange;
            }
        }

        /// <summary>
        /// プレイヤーの移動による即座の検知判定
        /// </summary>
        protected virtual bool CanDetectByMovement(bool isCrouching, bool isMoving, bool isRunning, float distance)
        {
            // しゃがみ状態では移動による検知は発生しない
            if (isCrouching)
                return false;

            // 走っている場合は遠くからでも音で検知
            if (isRunning && distance <= detectionRange * 1.3f)
                return true;

            // 歩いている場合は通常の検知範囲内で検知
            if (isMoving && distance <= detectionRange)
                return true;

            // 待機状態でも一定範囲内では検知
            if (!isMoving && distance <= detectionRange * 0.8f)
                return true;

            return false;
        }

        /// <summary>
        /// 視界による検知判定（改善版）
        /// </summary>
        protected virtual bool CanDetectByVision(Vector3 eyePosition, Vector3 playerPosition, Vector3 directionToPlayer, float distance, bool isCrouching)
        {
            // 視野角チェック
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer > visionAngle / 2f)
                return false;

            // 視線チェック（常に実行）
            if (useLineOfSight)
            {
                RaycastHit hit;
                if (Physics.Raycast(eyePosition, directionToPlayer, out hit, distance, visionBlockingLayers))
                {
                    if (!IsPlayer(hit.collider.gameObject))
                    {
                        return false;
                    }
                }
            }

            // しゃがみ時は確率的検知
            if (isCrouching)
            {
                return CheckCrouchDetection(distance);
            }

            return true;
        }

        /// <summary>
        /// しゃがみ時の確率的検知（改善版）
        /// </summary>
        protected virtual bool CheckCrouchDetection(float distance)
        {
            // 更新間隔をチェック
            if (Time.time - lastDetectionCheck >= 0.2f) // 0.2秒間隔に変更
            {
                float effectiveRange = detectionRange * crouchDetectionMultiplier;
                float detectionChance = Mathf.Lerp(maxDetectionChance, minDetectionChance, distance / effectiveRange);

                // 距離による補正
                if (distance < effectiveRange * 0.3f)
                {
                    detectionChance = Mathf.Max(detectionChance, 0.7f); // 近い場合は最低70%
                }

                lastDetectionResult = Random.Range(0f, 1f) < detectionChance;
                lastDetectionCheck = Time.time;
            }

            return lastDetectionResult;
        }



        #endregion

        #region Health Bar Management

        /// <summary>
        /// HPバーの初期化
        /// </summary>
        protected virtual void InitializeHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.maxValue = 1f;
                healthBar.value = 1f;
                // 初期状態ではMAXなので非表示
                healthBar.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// HPバーの更新
        /// </summary>
        protected virtual void UpdateHealthBar()
        {
            if (healthBar == null) return;

            float healthPercentage = currentHealth / maxHealth;

            // HPがMAXの場合は非表示、それ以外は表示
            if (healthPercentage >= 1f)
            {
                healthBar.gameObject.SetActive(false);
            }
            else
            {
                healthBar.gameObject.SetActive(true);
                healthBar.value = healthPercentage;

                // HPに応じてバーの色を変更
                UpdateHealthBarColor(healthPercentage);
            }
        }

        /// <summary>
        /// HPバーの色を更新
        /// </summary>
        protected virtual void UpdateHealthBarColor(float healthPercentage)
        {
            if (healthBar == null) return;

            var fillArea = healthBar.fillRect?.GetComponent<Image>();
            if (fillArea == null) return;

            Color barColor = healthPercentage switch
            {
                <= 0.2f => Color.red,        // 20%以下：赤
                <= 0.5f => new Color(1f, 0.5f, 0f), // 50%以下：オレンジ
                <= 0.8f => Color.yellow,     // 80%以下：黄色
                _ => Color.green             // 80%以上：緑
            };

            fillArea.color = barColor;
        }

        /// <summary>
        /// ダメージテキストを表示
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isCritical">クリティカルダメージかどうか</param>
        protected virtual void ShowDamageText(float damage, bool isCritical = false)
        {
            if (UI.UIManager.Instance != null)
            {
                // ダメージ表示位置を決定（専用オブジェクトがあればそれを使用、なければデフォルト位置）
                Vector3 damagePosition = damageDisplayPoint != null 
                    ? damageDisplayPoint.position 
                    : transform.position + Vector3.up * 1.5f;
                    
                UI.UIManager.Instance.ShowDamageText(damagePosition, damage, isCritical);
            }
        }

        #endregion

        /// <summary>
        /// プレイヤーが一度でも発見されたかの判定結果を取得
        /// </summary>
        public bool IsPlayerDetected()
        {
            return playerDetected;
        }

        /// <summary>
        /// プレイヤー検知状態をリセット
        /// </summary>
        public void ResetPlayerDetection()
        {
            playerDetected = false;
            playerDetectionTime = 0f;
            lastDetectionCheck = 0f;
            lastDetectionResult = false;
        }

        /// <summary>
        /// 静的メソッド：プレイヤーの攻撃を全敵に通知
        /// </summary>
        public static void NotifyPlayerAttack(Vector3 attackPosition)
        {
            EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>();

            foreach (var enemy in allEnemies)
            {
                if (enemy.isDead) continue;

                float distance = Vector3.Distance(enemy.transform.position, attackPosition);

                // 攻撃時の感知範囲内にいる場合は感知
                if (distance <= enemy.attackDetectionRange)
                {
                    enemy.playerDetected = true;
                    enemy.playerDetectionTime = Time.time;
                }
            }
        }

        /// <summary>
        /// ステルス攻撃かどうかを判定
        /// </summary>
        protected virtual bool IsStealthAttack()
        {
            if (playerController == null) return false;

            // プレイヤーが発見されていない場合はステルス攻撃
            bool isUndetected = !playerDetected;

            // 敵の背後からの攻撃かチェック
            bool isFromBehind = IsAttackFromBehind();

            return isUndetected && isFromBehind;
        }

        /// <summary>
        /// 攻撃が敵の背後からかどうかを判定
        /// </summary>
        protected virtual bool IsAttackFromBehind()
        {
            if (player == null) return false;

            Vector3 enemyForward = transform.forward;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;

            // 敵の後方120度以内からの攻撃を背後攻撃とする
            float dot = Vector3.Dot(enemyForward, directionToPlayer);
            return dot < -0.5f; // cos(120°) = -0.5
        }

        // ギズモで範囲を可視化
        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 forward = transform.forward;

            // 現在の状態を色で表示
            Gizmos.color = currentState switch
            {
                EnemyState.Patrol => Color.green,
                EnemyState.Chase => Color.red,
                EnemyState.Return => Color.yellow,
                _ => Color.white
            };
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);

            // 移動感知範囲円（緑色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // しゃがみ時の視覚検知範囲円（黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange * crouchDetectionMultiplier);

            // 基本攻撃範囲（赤色）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // 有効攻撃範囲（ピンク色）
            Gizmos.color = Color.magenta;
            float effectiveRange = GetEffectiveAttackRange();
            Gizmos.DrawWireSphere(transform.position, effectiveRange);

            // 視覚検知コーン（通常範囲）
            Gizmos.color = playerDetected ? ORANGE_COLOR : Color.blue;
            float halfAngle = visionAngle / 2f;
            Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
            Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;

            Gizmos.DrawRay(eyePosition, leftBoundary * detectionRange);
            Gizmos.DrawRay(eyePosition, rightBoundary * detectionRange);

            // 視覚検知コーンの円弧を描画
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / 9f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Gizmos.DrawRay(eyePosition, direction * detectionRange);
            }

            // プレイヤーへの視線
            if (player != null)
            {
                Vector3 playerPos = player.position + Vector3.up * 1f;
                bool canSee = CanSeePlayer();
                Gizmos.color = canSee ? Color.red : Color.gray;
                Gizmos.DrawLine(eyePosition, playerPos);

                // プレイヤーの状態表示
                if (playerController != null)
                {
                    if (playerController.IsCrouching)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(playerPos, 0.3f); // しゃがみ状態
                    }
                    else if (playerController.IsRunning)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(playerPos, 0.7f); // 走り状態
                    }
                    else if (playerController.IsMoving)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(playerPos, 0.5f); // 歩き状態
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(playerPos, 0.4f); // 待機状態
                    }
                }
            }

            // 現在のパトロールポイントを強調表示
            if (patrolRoute != null && patrolRoute.IsValidRoute())
            {
                var currentPoint = patrolRoute.GetPoint(currentPatrolIndex);
                if (currentPoint != null && currentPoint.transform != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(currentPoint.transform.position, 1f);
                    Gizmos.DrawLine(transform.position, currentPoint.transform.position);
                }
            }
        }

    }
}