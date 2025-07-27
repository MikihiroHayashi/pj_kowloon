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
        [SerializeField] protected float visionAngle = 120f; // 視野角度
        [SerializeField] protected LayerMask visionBlockingLayers = -1; // 視線を遮るレイヤー
        [SerializeField] protected bool useLineOfSight = true; // 視線チェックを使用するか
        
        
        [Header("Stealth Detection")]
        [SerializeField] protected float crouchDetectionMultiplier = 0.3f; // しゃがみ時の検知範囲倍率
        [SerializeField] protected float minDetectionChance = 0.1f;
        [SerializeField] protected float maxDetectionChance = 0.9f;
        
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
        protected EnhancedPlayerController playerController;
        protected float lastAttackTime;
        protected bool isDead = false;
        
        // 検知システム関連
        protected bool playerDetected = false;
        protected bool hasLoggedDetection = false; // デバッグログ用フラグ
        
        // 障害物回避関連
        protected Vector3 avoidanceDirection;
        protected float lastAvoidanceUpdateTime;
        protected const float AVOIDANCE_UPDATE_INTERVAL = 0.1f;
        
        // Animation parameter names
        protected const string ANIM_SPEED = "Speed";
        protected const string ANIM_ATTACK = "Attack";
        protected const string ANIM_DEATH = "Death";
        
        // 公開プロパティ
        public Transform Player => player;
        public float VisionAngle => visionAngle;
        public float DetectionRange => detectionRange;
        
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
            // プレイヤーを検索
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<EnhancedPlayerController>();
            }
            else
            {
                playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    playerController = playerObj.GetComponent<EnhancedPlayerController>();
                }
                else
                {
                    playerController = FindObjectOfType<EnhancedPlayerController>();
                    if (playerController != null)
                    {
                        player = playerController.transform;
                    }
                }
            }
            
            // プレイヤーコントローラーが見つからない場合の追加検索
            if (player != null && playerController == null)
            {
                playerController = player.GetComponent<EnhancedPlayerController>();
                if (playerController == null)
                {
                    playerController = player.GetComponentInParent<EnhancedPlayerController>();
                }
                if (playerController == null)
                {
                    playerController = player.GetComponentInChildren<EnhancedPlayerController>();
                }
            }
        }
        
        protected virtual void Update()
        {
            if (isDead || player == null) return;
            
            bool canDetectPlayer = false;
            
            // 一度発見されたら永続的に追跡
            if (playerDetected)
            {
                canDetectPlayer = true;
            }
            // 視界システムによるプレイヤー検知（しゃがんでいても発見される）
            else if (CanSeePlayer())
            {
                canDetectPlayer = true;
                playerDetected = true; // 発見したら永続的に記憶
            }
            
            // プレイヤーが検出されている場合の行動
            if (canDetectPlayer)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                if (distanceToPlayer <= attackRange)
                {
                    StopMoving();
                    TryAttack();
                }
                else
                {
                    MoveToPlayer();
                }
            }
            else
            {
                StopMoving();
            }
            
            UpdateAnimations();
            CheckNavMeshAgentStatus();
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
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        
        protected virtual void PerformAttack()
        {
            // 攻撃アニメーション再生（ダメージはアニメーションイベントで実行）
            if (animator != null)
                animator.SetTrigger(ANIM_ATTACK);
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
            
            // ステルス攻撃の判定
            bool isSteathAttack = IsStealhAttack() && wasUndetected;
            float finalDamage = damage;
            
            if (isSteathAttack)
            {
                // ステルス攻撃は3倍ダメージ
                finalDamage *= 3f;
                
                // UIに通知
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.ShowNotification("ステルス攻撃成功！", UI.NotificationType.Success);
                }
            }
            
            currentHealth -= finalDamage;
            
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

        /// <summary>
        /// 強制的にプレイヤーを発見状態にする（攻撃を受けた時など）
        /// </summary>
        public virtual void ForceDetectPlayer()
        {
            if (player != null)
            {
                playerDetected = true;
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
        
        /// <summary>
        /// 統合検知システム：視界とステルスを考慮してプレイヤーを検知
        /// </summary>
        protected virtual bool CanSeePlayer()
        {
            if (player == null) return false;
            
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f; // 目の高さ
            Vector3 playerPosition = player.position + Vector3.up * 1f; // プレイヤーの中心
            Vector3 directionToPlayer = (playerPosition - eyePosition).normalized;
            float distanceToPlayer = Vector3.Distance(eyePosition, playerPosition);
            
            // プレイヤーのしゃがみ状態をチェック
            bool isPlayerCrouching = playerController != null && playerController.IsCrouching;
            
            // しゃがみ時は検知範囲が縮小される
            float effectiveDetectionRange = isPlayerCrouching ? 
                detectionRange * crouchDetectionMultiplier : detectionRange;
            
            // 距離チェック（ステルス考慮）
            if (distanceToPlayer > effectiveDetectionRange)
                return false;
            
            // 視野角チェック
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer > visionAngle / 2f)
                return false;
            
            // 視線チェック（障害物に遮られていないか）
            if (useLineOfSight)
            {
                RaycastHit hit;
                if (Physics.Raycast(eyePosition, directionToPlayer, out hit, distanceToPlayer, visionBlockingLayers))
                {
                    // プレイヤー自身にヒットした場合はOK
                    if (!IsPlayer(hit.collider.gameObject))
                    {
                        return false; // 障害物に遮られている
                    }
                }
            }
            
            // しゃがみ時は確率的検知
            if (isPlayerCrouching)
            {
                float detectionChance = Mathf.Lerp(maxDetectionChance, minDetectionChance, 
                    distanceToPlayer / effectiveDetectionRange);
                return Random.Range(0f, 1f) < detectionChance;
            }
            
            return true;
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
        }

        /// <summary>
        /// ステルス攻撃かどうかを判定
        /// </summary>
        protected virtual bool IsStealhAttack()
        {
            if (playerController == null) return false;
            
            // プレイヤーが発見されていない場合はステルス攻撃
            bool isUndetected = !playerDetected;
            
            // 敵の背後からの攻撃かチェック
            bool isFromBehind = IsAttackFromBehind();
            
            // ステルス攻撃の条件：未発見 AND 背後から
            // しゃがみ状態は検知システムで考慮済みなのでここでは除外
            bool isStealth = isUndetected && isFromBehind;
            
            return isStealth;
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
            
            // 通常の検知範囲円（薄い黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // しゃがみ時の検知範囲円（濃い黄色）
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange * crouchDetectionMultiplier);
            
            // 攻撃範囲
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // 視野コーン（通常範囲）
            Gizmos.color = playerDetected ? ORANGE_COLOR : Color.green;
            float halfAngle = visionAngle / 2f;
            Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
            Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;
            
            Gizmos.DrawRay(eyePosition, leftBoundary * detectionRange);
            Gizmos.DrawRay(eyePosition, rightBoundary * detectionRange);
            
            // 視野コーンの円弧を描画
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / 9f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Gizmos.DrawRay(eyePosition, direction * detectionRange);
            }
            
            // しゃがみ時の視野コーン（内側、薄い色）
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
            float crouchRange = detectionRange * crouchDetectionMultiplier;
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / 7f);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Gizmos.DrawRay(eyePosition, direction * crouchRange);
            }
            
            // プレイヤーへの視線
            if (player != null)
            {
                Vector3 playerPos = player.position + Vector3.up * 1f;
                bool canSee = CanSeePlayer();
                Gizmos.color = canSee ? Color.red : Color.gray;
                Gizmos.DrawLine(eyePosition, playerPos);
                
                // プレイヤーがしゃがんでいる場合の表示
                if (playerController != null && playerController.IsCrouching)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(playerPos, 0.5f);
                    
                    // しゃがみ時の検知確率を表示
                    float distanceToPlayer = Vector3.Distance(eyePosition, playerPos);
                    float effectiveRange = detectionRange * crouchDetectionMultiplier;
                    
                    if (distanceToPlayer <= effectiveRange)
                    {
                        float detectionChance = Mathf.Lerp(maxDetectionChance, minDetectionChance, 
                            distanceToPlayer / effectiveRange);
                        
                        // 確率に応じて色を変更（赤=高確率、黄=中確率、緑=低確率）
                        if (detectionChance > 0.7f)
                            Gizmos.color = Color.red;
                        else if (detectionChance > 0.4f)
                            Gizmos.color = Color.yellow;
                        else
                            Gizmos.color = Color.green;
                        
                        Gizmos.DrawWireCube(playerPos, Vector3.one * 0.3f);
                    }
                }
            }
            
            // 障害物回避範囲
            if (enableObstacleAvoidance)
            {
                Gizmos.color = Color.cyan;
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
        
    }
}