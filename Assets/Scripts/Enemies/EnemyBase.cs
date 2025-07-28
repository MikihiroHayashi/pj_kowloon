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
        protected float playerDetectionTime = 0f;
        protected float detectionMemoryDuration = 5f;
        protected float lastDetectionCheck = 0f;
        protected bool lastDetectionResult = false;
        protected float attackDetectionRange = 8f; // 攻撃時の感知範囲

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
            FindPlayer();
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

            // デバッグ情報（必要に応じてコメントアウト）
            if (Time.frameCount % 60 == 0) // 1秒間隔でログ出力
            {
                LogDetectionStatus();
            }

            if (canDetectPlayer)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                // 攻撃デバッグ情報
                if (Time.frameCount % 30 == 0) // 0.5秒間隔
                {
                    LogAttackStatus(distanceToPlayer);
                }

                // Colliderサイズを考慮した攻撃範囲判定
                float effectiveAttackRange = GetEffectiveAttackRange();

                if (distanceToPlayer <= effectiveAttackRange)
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

        /// <summary>
        /// プレイヤー検知システムのメイン更新メソッド（修正版）
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
                    Debug.Log($"[{gameObject.name}] 継続検知中");
                }
                else
                {
                    // 検知できない場合は記憶期間をチェック
                    if (currentTime - playerDetectionTime > detectionMemoryDuration)
                    {
                        playerDetected = false;
                        Debug.Log($"[{gameObject.name}] 記憶期間経過で追跡停止");
                    }
                    else
                    {
                        Debug.Log($"[{gameObject.name}] 見失い中（記憶残り: {detectionMemoryDuration - (currentTime - playerDetectionTime):F1}秒）");
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
                    Debug.Log($"[{gameObject.name}] プレイヤーを発見！");
                }
            }

            return playerDetected;
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

                // デバッグ情報
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[{gameObject.name}] プレイヤーに向かって移動中: " +
                             $"目標={targetPosition}, " +
                             $"NavMesh状態={navAgent.pathStatus}, " +
                             $"速度={navAgent.velocity.magnitude:F1}");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] NavMeshAgentが無効です！");
            }
        }

        protected virtual void StopMoving()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                bool hadPath = navAgent.hasPath;
                navAgent.ResetPath();

                if (hadPath && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[{gameObject.name}] 移動停止");
                }
            }
        }

        protected virtual void TryAttack()
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            bool canAttack = timeSinceLastAttack >= attackCooldown;

            // 攻撃試行のデバッグログ
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[{gameObject.name}] TryAttack: " +
                         $"クールダウン残り={Mathf.Max(0, attackCooldown - timeSinceLastAttack):F1}秒, " +
                         $"攻撃可能={canAttack}");
            }

            if (canAttack)
            {
                PerformAttack();
                lastAttackTime = Time.time;
                Debug.Log($"[{gameObject.name}] 攻撃実行！");
            }
        }

        protected virtual void PerformAttack()
        {
            // 攻撃アニメーション再生（ダメージはアニメーションイベントで実行）
            if (animator != null)
            {
                animator.SetTrigger(ANIM_ATTACK);
                Debug.Log($"[{gameObject.name}] 攻撃アニメーション再生: {ANIM_ATTACK}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Animatorが見つかりません！攻撃アニメーションを再生できません");
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

            // ステルス攻撃の判定
            bool isStealthAttack = IsStealthAttack() && wasUndetected;
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
            navAgent.stoppingDistance = attackRange * 0.5f; // 攻撃範囲の半分で停止（より近くで止まる）
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            navAgent.avoidancePriority = 50; // 中程度の優先度
            navAgent.radius = avoidanceRadius * 0.5f; // エージェントの半径

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
                        Debug.Log($"[{gameObject.name}] 視線が{hit.collider.name}に遮られている");
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

                Debug.Log($"[{gameObject.name}] しゃがみ検知判定: 確率={detectionChance:F2}, 結果={lastDetectionResult}");
            }

            return lastDetectionResult;
        }

        /// <summary>
        /// デバッグ情報の表示
        /// </summary>
        protected virtual void LogDetectionStatus()
        {
            if (player == null) return;

            bool isCrouching = playerController != null && playerController.IsCrouching;
            bool isMoving = playerController != null && playerController.IsMoving;
            bool isRunning = playerController != null && playerController.IsRunning;
            float distance = Vector3.Distance(transform.position, player.position);

            string playerState = isRunning ? "走行" : isMoving ? "歩行" : isCrouching ? "しゃがみ" : "待機";

            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 playerPosition = player.position + Vector3.up * 1f;
            Vector3 directionToPlayer = (playerPosition - eyePosition).normalized;
            bool canSee = CanDetectByVision(eyePosition, playerPosition, directionToPlayer, distance, isCrouching);

            Debug.Log($"[{gameObject.name}] " +
                     $"状態: {(playerDetected ? "追跡中" : "巡回中")}, " +
                     $"プレイヤー: {playerState}, " +
                     $"距離: {distance:F1}m, " +
                     $"視界内: {canSee}");
        }

        /// <summary>
        /// 攻撃状況のデバッグ情報
        /// </summary>
        protected virtual void LogAttackStatus(float distanceToPlayer)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            float effectiveAttackRange = GetEffectiveAttackRange();
            bool inAttackRange = distanceToPlayer <= effectiveAttackRange;
            bool canAttack = timeSinceLastAttack >= attackCooldown;

            Debug.Log($"[{gameObject.name}] 攻撃状況: " +
                     $"距離={distanceToPlayer:F1}m, " +
                     $"基本攻撃範囲={attackRange}m, " +
                     $"有効攻撃範囲={effectiveAttackRange:F1}m, " +
                     $"範囲内={inAttackRange}, " +
                     $"クールダウン残り={Mathf.Max(0, attackCooldown - timeSinceLastAttack):F1}秒, " +
                     $"攻撃可能={canAttack}");
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

            // しゃがみ時の視覚検知コーン（縮小範囲）
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
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