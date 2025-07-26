using UnityEngine;
using UnityEngine.AI;
using KowloonBreak.Core;
using KowloonBreak.Environment;
using KowloonBreak.Player;

namespace KowloonBreak.Enemies
{
    public class EnemyBase : MonoBehaviour, IDestructible
    {
        // 視覚状態の色定数
        private static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f, 1f);
        [Header("Enemy Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float attackCooldown = 2f;
        
        [Header("Vision System")]
        [SerializeField] protected float visionRange = 8f;
        [SerializeField] protected float visionAngle = 60f;
        [SerializeField] protected LayerMask visionBlockingLayers = -1;
        [SerializeField] protected bool enableVisionSystem = true;
        [SerializeField] protected bool showVisionDebug = false;
        [SerializeField] protected Color visionDebugColor = Color.red;
        
        [Header("Navigation & Avoidance")]
        [SerializeField] protected float obstacleDetectionRange = 3f;
        [SerializeField] protected LayerMask obstacleLayerMask = -1;
        [SerializeField] protected float avoidanceRadius = 1.5f;
        [SerializeField] protected float avoidanceStrength = 2f;
        [SerializeField] protected bool enableObstacleAvoidance = true;
        [SerializeField] protected bool avoidPlayer = false;
        
        [Header("Drop Items")]
        [SerializeField] protected ItemData[] dropItems;
        [SerializeField] protected int[] dropQuantities;
        [SerializeField] protected float[] dropChances;
        
        [Header("Components")]
        [SerializeField] protected NavMeshAgent navAgent;
        [SerializeField] protected Animator animator;
        [SerializeField] protected Collider enemyCollider;
        [SerializeField] protected Renderer modelRenderer;
        
        protected Transform player;
        protected float lastAttackTime;
        protected bool isDead = false;
        
        // 視覚システム関連
        protected bool playerInVision = false;
        protected bool playerDetected = false;
        protected float lastVisionCheckTime;
        protected const float VISION_CHECK_INTERVAL = 0.2f;
        
        // 障害物回避関連
        protected Vector3 avoidanceDirection;
        protected float lastAvoidanceUpdateTime;
        protected const float AVOIDANCE_UPDATE_INTERVAL = 0.1f;
        
        // Animation parameter names
        protected const string ANIM_SPEED = "Speed";
        protected const string ANIM_ATTACK = "Attack";
        protected const string ANIM_DEATH = "Death";
        
        // 視覚デバッグ用プロパティ
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public Transform Player => player;
        public bool ShowVisionDebug => showVisionDebug;
        public Color VisionDebugColor => visionDebugColor;
        
        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            
            // NavMeshAgentの設定
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();
            
            if (navAgent != null)
            {
                SetupNavMeshAgent();
            }
            
            // Animatorの取得
            if (animator == null)
                animator = GetComponent<Animator>();
            
            // Colliderの取得
            if (enemyCollider == null)
                enemyCollider = GetComponent<Collider>();
            
            // Model Rendererの取得（子オブジェクトから検索）
            if (modelRenderer == null)
                modelRenderer = GetComponentInChildren<Renderer>();
        }
        
        protected virtual void Start()
        {
            // プレイヤーを検索（まずTagで検索）
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"[EnemyBase] Found player by tag: {playerObj.name}");
            }
            else
            {
                // Tagで見つからない場合は名前で検索
                playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    Debug.Log($"[EnemyBase] Found player by name: {playerObj.name}");
                }
                else
                {
                    // EnhancedPlayerControllerコンポーネントで検索
                    var enhancedController = FindObjectOfType<EnhancedPlayerController>();
                    if (enhancedController != null)
                    {
                        player = enhancedController.transform;
                        Debug.Log($"[EnemyBase] Found player by EnhancedPlayerController: {enhancedController.name}");
                    }
                    else
                    {
                        Debug.LogError("[EnemyBase] Player not found by any method!");
                    }
                }
            }
        }
        
        protected virtual void Update()
        {
            if (isDead || player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // プレイヤーが検知範囲内または視覚で発見済みかチェック
            bool canDetectPlayer = distanceToPlayer <= detectionRange || playerDetected;
            
            if (canDetectPlayer)
            {
                // 攻撃範囲内なら攻撃
                if (distanceToPlayer <= attackRange)
                {
                    // 攻撃範囲内のデバッグ情報（頻度を抑制）
                    if (Time.frameCount % 60 == 0) // 1秒に1回程度
                    {
                        Debug.Log($"[EnemyBase] {gameObject.name} - Player in attack range: {distanceToPlayer:F1} <= {attackRange}");
                    }
                    StopMoving();
                    TryAttack();
                }
                else
                {
                    // 移動中のデバッグ情報（頻度を抑制）
                    if (Time.frameCount % 60 == 0) // 1秒に1回程度
                    {
                        Debug.Log($"[EnemyBase] {gameObject.name} - Moving to player: distance {distanceToPlayer:F1}");
                    }
                    // プレイヤーに向かって移動
                    MoveToPlayer();
                }
            }
            else
            {
                // 範囲外なら停止
                StopMoving();
            }
            
            UpdateAnimations();
            CheckNavMeshAgentStatus();
            UpdateVisionSystem();
        }
        
        protected virtual void MoveToPlayer()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                Vector3 targetPosition = player.position;
                
                // 障害物回避が有効な場合
                if (enableObstacleAvoidance)
                {
                    targetPosition = GetAvoidanceAdjustedPosition(targetPosition);
                }
                
                navAgent.SetDestination(targetPosition);
            }
        }
        
        protected virtual void StopMoving()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.ResetPath();
            }
        }
        
        protected virtual void TryAttack()
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= attackCooldown)
            {
                Debug.Log($"[EnemyBase] TryAttack: Cooldown ready ({timeSinceLastAttack:F1}s >= {attackCooldown}s), performing attack");
                PerformAttack();
                lastAttackTime = Time.time;
            }
            else
            {
                Debug.Log($"[EnemyBase] TryAttack: Still on cooldown ({timeSinceLastAttack:F1}s < {attackCooldown}s)");
            }
        }
        
        protected virtual void PerformAttack()
        {
            Debug.Log($"[EnemyBase] PerformAttack: Enemy {gameObject.name} starting attack animation");
            
            // 攻撃アニメーション再生（ダメージはアニメーションイベントで実行）
            if (animator != null)
                animator.SetTrigger(ANIM_ATTACK);
        }
        
        /// <summary>
        /// アニメーションイベントから呼ばれるダメージ実行
        /// </summary>
        public virtual void ExecuteAttackDamage()
        {
            Debug.Log($"[EnemyBase] ExecuteAttackDamage: Enemy {gameObject.name} dealing damage: {attackDamage}");
            
            // プレイヤーにダメージを与える処理
            if (player != null)
            {
                Debug.Log($"[EnemyBase] ExecuteAttackDamage: Player found: {player.name}");
                
                // まず直接検索
                var enhancedPlayerController = player.GetComponent<EnhancedPlayerController>();
                
                // 見つからない場合は親階層で検索
                if (enhancedPlayerController == null)
                {
                    enhancedPlayerController = player.GetComponentInParent<EnhancedPlayerController>();
                    Debug.Log($"[EnemyBase] ExecuteAttackDamage: Searching in parent, found: {enhancedPlayerController != null}");
                }
                
                // 見つからない場合は子階層で検索
                if (enhancedPlayerController == null)
                {
                    enhancedPlayerController = player.GetComponentInChildren<EnhancedPlayerController>();
                    Debug.Log($"[EnemyBase] ExecuteAttackDamage: Searching in children, found: {enhancedPlayerController != null}");
                }
                
                if (enhancedPlayerController != null)
                {
                    Debug.Log($"[EnemyBase] ExecuteAttackDamage: EnhancedPlayerController found on {enhancedPlayerController.gameObject.name}, calling TakeDamage({attackDamage})");
                    enhancedPlayerController.TakeDamage(attackDamage);
                    Debug.Log($"[EnemyBase] ExecuteAttackDamage: TakeDamage call completed");
                }
                else
                {
                    Debug.LogError($"[EnemyBase] ExecuteAttackDamage: EnhancedPlayerController component not found on player {player.name} or its hierarchy!");
                    
                    // デバッグ情報: プレイヤーオブジェクトのコンポーネント一覧
                    var components = player.GetComponents<Component>();
                    Debug.Log($"[EnemyBase] ExecuteAttackDamage: Components on {player.name}: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
                }
            }
            else
            {
                Debug.LogError("[EnemyBase] ExecuteAttackDamage: Player reference is null!");
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
            
            currentHealth -= damage;
            
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
            TakeDamage(damage, ToolType.IronPipe); // デフォルトツール
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
            Debug.Log($"[EnemyBase] Die called on {gameObject.name}");
            
            // NavMeshAgentを停止
            if (navAgent != null)
            {
                navAgent.enabled = false;
                Debug.Log($"[EnemyBase] NavMeshAgent disabled on {gameObject.name}");
            }
            
            // 死亡アニメーション再生
            if (animator != null)
            {
                Debug.Log($"[EnemyBase] Animator found, triggering Death animation on {gameObject.name}");
                Debug.Log($"[EnemyBase] Death parameter exists: {HasParameter(ANIM_DEATH)}");
                animator.SetTrigger(ANIM_DEATH);
                Debug.Log($"[EnemyBase] Death trigger set, starting WaitForDeathAnimation coroutine");
                // アニメーション完了後に削除するコルーチンを開始
                StartCoroutine(WaitForDeathAnimation());
            }
            else
            {
                Debug.LogWarning($"[EnemyBase] No Animator found on {gameObject.name}, destroying immediately");
                // Animatorがない場合は即座に削除処理
                DestroyEnemy();
            }
            
            // コライダーを無効化
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
                Debug.Log($"[EnemyBase] Collider disabled on {gameObject.name}");
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
            Debug.Log($"[EnemyBase] WaitForDeathAnimation started on {gameObject.name}");
            
            // Deathアニメーションが開始されるまで少し待機
            yield return new WaitForSeconds(0.1f);
            
            int maxWaitFrames = 300; // 5秒間の最大待機
            int waitFrames = 0;
            
            // Deathアニメーションを探す
            while (waitFrames < maxWaitFrames)
            {
                if (animator == null)
                {
                    Debug.LogWarning($"[EnemyBase] Animator became null during death animation wait");
                    break;
                }
                
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[EnemyBase] Current animation state: {stateInfo.shortNameHash} (IsName Death: {stateInfo.IsName("Death")}, IsTag Death: {stateInfo.IsTag("Death")})");
                
                // Deathアニメーションが再生中かチェック
                if (stateInfo.IsName("Death") || stateInfo.IsTag("Death"))
                {
                    Debug.Log($"[EnemyBase] Death animation found! Length: {stateInfo.length}, NormalizedTime: {stateInfo.normalizedTime}");
                    
                    // アニメーションが完了するまで待機
                    while (stateInfo.normalizedTime < 1.0f)
                    {
                        yield return null;
                        if (animator == null) break;
                        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    }
                    
                    Debug.Log($"[EnemyBase] Death animation completed");
                    break;
                }
                
                waitFrames++;
                yield return null;
            }
            
            if (waitFrames >= maxWaitFrames)
            {
                Debug.LogWarning($"[EnemyBase] Death animation not found after 5 seconds, destroying anyway");
            }
            
            // アニメーション完了後にオブジェクトを削除
            Debug.Log($"[EnemyBase] Destroying enemy {gameObject.name}");
            DestroyEnemy();
        }
        
        private void DestroyEnemy()
        {
            Destroy(gameObject);
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
                    Debug.Log($"[EnemyBase] Unhandled animation event: {eventName}");
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
            navAgent.speed = moveSpeed;
            navAgent.acceleration = moveSpeed * 2f; // 素早い方向転換
            navAgent.angularSpeed = 180f; // 回転速度
            navAgent.stoppingDistance = attackRange * 0.8f; // 攻撃範囲の少し手前で停止
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            navAgent.avoidancePriority = 50; // 中程度の優先度
            navAgent.radius = avoidanceRadius * 0.5f; // エージェントの半径
            
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
        
        #region Vision System
        
        /// <summary>
        /// 視覚システムの更新
        /// </summary>
        protected virtual void UpdateVisionSystem()
        {
            if (!enableVisionSystem || player == null || isDead) return;
            
            // 定期的に視覚をチェック
            if (Time.time - lastVisionCheckTime >= VISION_CHECK_INTERVAL)
            {
                CheckPlayerInVision();
                lastVisionCheckTime = Time.time;
            }
        }
        
        /// <summary>
        /// プレイヤーが視野内にいるかチェック
        /// </summary>
        protected virtual void CheckPlayerInVision()
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            playerInVision = false;
            
            // 視野範囲内かチェック
            if (distanceToPlayer <= visionRange)
            {
                // 視野角度内かチェック
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
                
                if (angleToPlayer <= visionAngle * 0.5f)
                {
                    // 視線が遮られていないかチェック
                    if (HasClearLineOfSight(player.position))
                    {
                        playerInVision = true;
                        playerDetected = true; // 一度発見したら記憶
                    }
                }
            }
            
            // 視野外かつ遠距離の場合、検知をリセット
            if (!playerInVision && distanceToPlayer > detectionRange * 1.5f)
            {
                playerDetected = false;
            }
        }
        
        /// <summary>
        /// 指定位置への視線が通っているかチェック
        /// </summary>
        protected virtual bool HasClearLineOfSight(Vector3 targetPosition)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // 目の高さ
            Vector3 rayDirection = (targetPosition - rayOrigin).normalized;
            float rayDistance = Vector3.Distance(rayOrigin, targetPosition);
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, visionBlockingLayers))
            {
                // 何かにぶつかった場合、それがプレイヤーかチェック
                return IsPlayer(hit.collider.gameObject);
            }
            
            // 何にもぶつからなければ視線は通っている
            return true;
        }
        
        /// <summary>
        /// プレイヤーが視野内にいるかの判定結果を取得
        /// </summary>
        public bool IsPlayerInVision()
        {
            return playerInVision;
        }
        
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
            playerInVision = false;
        }
        
        #endregion
        
        // ギズモで範囲を可視化
        protected virtual void OnDrawGizmosSelected()
        {
            // 検知範囲
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 攻撃範囲
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 視覚システム
            if (enableVisionSystem)
            {
                DrawVisionRange();
            }
            
            // 障害物回避範囲
            if (enableObstacleAvoidance)
            {
                Gizmos.color = Color.cyan;
                Vector3 forward = transform.forward;
                Vector3 checkPosition = transform.position + forward * obstacleDetectionRange;
                Gizmos.DrawWireSphere(checkPosition, avoidanceRadius);
                
                // 回避方向を表示
                if (avoidanceDirection != Vector3.zero)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawRay(transform.position, avoidanceDirection * avoidanceStrength);
                }
                
                // 前方検知線
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, forward * obstacleDetectionRange);
            }
        }
        
        /// <summary>
        /// 視野範囲をGizmosで描画
        /// </summary>
        protected virtual void DrawVisionRange()
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 forward = transform.forward;
            
            // 視野の色を状態に応じて変更
            Color visionColor = visionDebugColor;
            if (Application.isPlaying)
            {
                if (playerInVision)
                    visionColor = Color.red;
                else if (playerDetected)
                    visionColor = ORANGE_COLOR;
                else
                    visionColor = Color.white;
            }
            
            Gizmos.color = visionColor;
            
            // 視野円を描画
            Gizmos.DrawWireSphere(transform.position, visionRange);
            
            // 視野角の範囲を描画
            float halfAngle = visionAngle * 0.5f;
            Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * visionRange;
            Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward * visionRange;
            
            Gizmos.DrawRay(eyePosition, leftBoundary);
            Gizmos.DrawRay(eyePosition, rightBoundary);
            
            // 視野扇形の境界線を描画
            Vector3 lastPoint = leftBoundary;
            int segments = 20;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
                Vector3 nextPoint = Quaternion.AngleAxis(angle, Vector3.up) * forward * visionRange;
                Gizmos.DrawLine(eyePosition + lastPoint, eyePosition + nextPoint);
                lastPoint = nextPoint;
            }
            
            // プレイヤーへの視線を描画
            if (Application.isPlaying && player != null)
            {
                Gizmos.color = playerInVision ? Color.red : Color.gray;
                Gizmos.DrawLine(eyePosition, player.position);
            }
        }
    }
}