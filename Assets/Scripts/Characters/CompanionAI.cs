using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using KowloonBreak.Core;
using KowloonBreak.Player;
using KowloonBreak.Environment;

namespace KowloonBreak.Characters
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CompanionCharacter))]
    public class CompanionAI : MonoBehaviour, IDestructible
    {
        [Header("AI Settings")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float maxFollowDistance = 10f;
        [SerializeField] private float baseUpdateRate = 0.2f;
        [SerializeField] private float stoppingDistance = 1.5f;
        
        [Header("Smart Teleport System")]
        [SerializeField] private float teleportDistance = 10f;
        [SerializeField] private float teleportOffscreenMargin = 1f;
        [SerializeField] private bool enableSmartTeleport = true;
        
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private bool canRun = true;
        [SerializeField] private bool canCrouch = true;
        [SerializeField] private bool canDodge = true;
        
        [Header("Detection")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float detectionAngle = 90f;
        [SerializeField] private LayerMask enemyLayerMask = -1;
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        
        [Header("Attack System")]
        [SerializeField] private LayerMask destructibleLayers = -1;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private Transform attackPoint;
        
        [Header("Health System")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        
        [Header("HP Bar")]
        [SerializeField] protected SpriteRenderer healthBarBackground;
        [SerializeField] protected SpriteRenderer healthBarFill;
        
        [Header("Damage Display")]
        [SerializeField] protected Transform damageDisplayPoint;
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform interactionPromptAnchor;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private AIState currentState = AIState.Follow;
        
        private NavMeshAgent navAgent;
        private CompanionCharacter companionCharacter;
        private CompanionAnimatorController animatorController;
        private CompanionMiningSystem miningSystem;
        private GameObject currentTarget;
        private Vector3 lastPlayerPosition;
        private float lastUpdateTime;
        private UnityEngine.Camera playerCamera;
        
        // Health System
        protected bool isDead = false;
        
        // Movement state tracking
        private CompanionMovementState currentMovementState = CompanionMovementState.Idle;
        private bool isRunning = false;
        private bool isCrouching = false;
        private bool shouldRun = false;
        private bool shouldCrouch = false;
        
        public AIState CurrentState => currentState;
        public bool HasTarget => currentTarget != null;
        public Transform Player => player;
        public int IntelligenceLevel => GetIntelligenceLevelFromTrust();
        public Transform InteractionPromptAnchor => interactionPromptAnchor;
        
        public System.Action<AIState> OnStateChanged;
        
        // Health Properties
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => !isDead && currentHealth > 0f;
        
        // IDestructible Properties
        public bool IsDestroyed => isDead;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            companionCharacter = GetComponent<CompanionCharacter>();
            animatorController = GetComponent<CompanionAnimatorController>();
            miningSystem = GetComponent<CompanionMiningSystem>();
            
            if (animatorController == null)
            {
                animatorController = GetComponentInChildren<CompanionAnimatorController>();
            }
            
            // Health system initialization
            currentHealth = maxHealth;
            
            InitializeAgent();
            InitializeHealthBar();
        }

        private void Start()
        {
            FindPlayer();
            FindPlayerCamera();
            SetState(AIState.Follow);
            StartCoroutine(UpdateAILoop());
        }
        
        private void FindPlayerCamera()
        {
            if (player != null)
            {
                // プレイヤーの子オブジェクトからカメラを探す
                playerCamera = player.GetComponentInChildren<UnityEngine.Camera>();
                
                // 見つからない場合はメインカメラを使用
                if (playerCamera == null)
                {
                    playerCamera = UnityEngine.Camera.main;
                }
                
                if (playerCamera == null)
                {
                    Debug.LogWarning("[CompanionAI] Player camera not found! Smart teleport may not work properly.");
                }
            }
        }

        private void InitializeAgent()
        {
            if (navAgent != null)
            {
                navAgent.stoppingDistance = stoppingDistance;
                navAgent.speed = 3.5f;
                navAgent.angularSpeed = 120f;
                navAgent.acceleration = 8f;
                navAgent.autoBraking = true;
                navAgent.autoRepath = true;
            }
        }

        private void FindPlayer()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                    lastPlayerPosition = player.position;
                }
                else
                {
                    Debug.LogWarning($"Player not found for companion {gameObject.name}");
                }
            }
        }

        private IEnumerator UpdateAILoop()
        {
            while (true)
            {
                float currentUpdateRate = GetCurrentUpdateRate();
                yield return new WaitForSeconds(currentUpdateRate);
                
                if (!companionCharacter.IsAvailable)
                    continue;
                
                UpdateAI();
            }
        }

        private float GetCurrentUpdateRate()
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            return intelligenceLevel switch
            {
                1 => baseUpdateRate * 2f,    // Slow reactions
                2 => baseUpdateRate * 1.5f,  // Delayed reactions
                3 => baseUpdateRate,         // Normal reactions
                4 => baseUpdateRate * 0.8f,  // Quick reactions
                5 => baseUpdateRate * 0.6f,  // Very quick reactions
                _ => baseUpdateRate
            };
        }

        private void UpdateAI()
        {
            if (isDead) return;
            
            if (player == null)
            {
                FindPlayer();
                return;
            }

            // High-level AI: Monitor player danger and provide proactive support
            if (GetIntelligenceLevelFromTrust() >= 4)
            {
                CheckPlayerDangerStatus();
            }

            switch (currentState)
            {
                case AIState.Follow:
                    HandleFollowState();
                    break;
                case AIState.Combat:
                    HandleCombatState();
                    break;
                case AIState.Idle:
                    HandleIdleState();
                    break;
                case AIState.Explore:
                    HandleExploreState();
                    break;
                case AIState.Support:
                    HandleSupportState();
                    break;
            }
            
            CheckForEnemies();
            UpdateLastPlayerPosition();
            UpdateMovementAnimation();
            UpdateHealthBar();
        }

        private void CheckPlayerDangerStatus()
        {
            if (player == null || currentState == AIState.Combat) return;

            var playerController = player.GetComponent<EnhancedPlayerController>();
            if (playerController == null) return;

            // Check if player is in danger
            bool playerInDanger = IsPlayerInDanger(playerController);
            
            if (playerInDanger && GetIntelligenceLevelFromTrust() >= 4)
            {
                RespondToPlayerDanger();
            }
        }

        private bool IsPlayerInDanger(EnhancedPlayerController playerController)
        {
            // Check player health
            float healthPercentage = playerController.HealthPercentage;
            if (healthPercentage < 0.3f) return true;

            // Check for nearby enemies
            Collider[] nearbyEnemies = Physics.OverlapSphere(player.position, 8f, enemyLayerMask);
            if (nearbyEnemies.Length >= 2) return true; // Multiple enemies near player

            // Check player movement state (if being attacked)
            if (playerController.CurrentMovementState == MovementState.Stunned) return true;

            return false;
        }

        private void RespondToPlayerDanger()
        {
            // Find the most threatening enemy near the player
            GameObject mostThreatening = FindMostThreateningEnemy();
            
            if (mostThreatening != null)
            {
                Debug.Log($"{gameObject.name} responding to player danger - targeting {mostThreatening.name}");
                currentTarget = mostThreatening;
                SetState(AIState.Combat);
                
                // Gain trust for protective behavior
                companionCharacter.ChangeTrustLevel(2);
            }
            else
            {
                // No specific threat found, move to support position
                SetState(AIState.Support);
            }
        }

        private GameObject FindMostThreateningEnemy()
        {
            if (player == null) return null;

            Collider[] nearbyEnemies = Physics.OverlapSphere(player.position, 10f, enemyLayerMask);
            GameObject mostThreatening = null;
            float highestThreatLevel = 0f;

            foreach (var enemy in nearbyEnemies)
            {
                // シンプルなエネミーかチェック
                if (!IsSimpleEnemy(enemy.gameObject))
                {
                    continue;
                }
                
                float threatLevel = CalculateThreatLevel(enemy.gameObject);
                if (threatLevel > highestThreatLevel)
                {
                    highestThreatLevel = threatLevel;
                    mostThreatening = enemy.gameObject;
                }
            }

            if (mostThreatening != null)
            {
                Debug.Log($"[CompanionAI] {gameObject.name} - Most threatening enemy: {mostThreatening.name} (threat level: {highestThreatLevel:F2})");
            }

            return mostThreatening;
        }

        private float CalculateThreatLevel(GameObject enemy)
        {
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, player.position);
            float proximityThreat = Mathf.Max(0f, 10f - distanceToPlayer) / 10f; // Closer = more threatening

            // Check if enemy is currently targeting/attacking player
            var enemyBase = enemy.GetComponent<Enemies.EnemyBase>();
            float aggressionThreat = 0f;
            
            if (enemyBase != null)
            {
                // If enemy is in chase or combat state, higher threat
                if (enemyBase.CurrentState == Enemies.EnemyState.Chase)
                {
                    aggressionThreat = 0.8f;
                }
            }

            return proximityThreat + aggressionThreat;
        }

        private void HandleFollowState()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // スマートワープシステムの適用
            if (enableSmartTeleport && ShouldSmartTeleport(distanceToPlayer))
            {
                PerformSmartTeleport();
                return;
            }
            
            if (distanceToPlayer > followDistance)
            {
                Vector3 targetPosition = CalculateFollowPosition();
                MoveToPosition(targetPosition);
            }
            else
            {
                if (navAgent.hasPath && navAgent.remainingDistance < 0.5f)
                {
                    navAgent.ResetPath();
                }
            }
        }

        private Vector3 CalculateFollowPosition()
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 targetPosition = player.position - directionToPlayer * followDistance;
            
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return player.position;
        }
        
        #region Smart Teleport System
        
        /// <summary>
        /// スマートワープの条件をチェック
        /// </summary>
        private bool ShouldSmartTeleport(float distanceToPlayer)
        {
            // 距離条件：設定した距離以上離れている
            if (distanceToPlayer < teleportDistance) return false;
            
            // カメラ視野条件：画面外にいる
            if (IsVisibleInCamera()) return false;
            
            return true;
        }
        
        /// <summary>
        /// カメラの視野内にいるかどうかを判定
        /// </summary>
        private bool IsVisibleInCamera()
        {
            if (playerCamera == null) return false;
            
            // コンパニオンの位置をスクリーン座標に変換
            Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);
            
            // 画面内判定（0-1の範囲内で、かつカメラの前方にある）
            bool isInView = screenPoint.x >= 0 && screenPoint.x <= 1 &&
                           screenPoint.y >= 0 && screenPoint.y <= 1 &&
                           screenPoint.z > 0;
                           
            // 障害物による遮蔽判定（オプション）
            if (isInView)
            {
                Vector3 directionToCompanion = (transform.position - playerCamera.transform.position).normalized;
                float distanceToCompanion = Vector3.Distance(playerCamera.transform.position, transform.position);
                
                // カメラからコンパニオンへのレイキャストで障害物チェック
                if (Physics.Raycast(playerCamera.transform.position, directionToCompanion, out RaycastHit hit, distanceToCompanion, obstacleLayerMask))
                {
                    // 障害物に遮られている場合は見えていないと判定
                    return false;
                }
            }
            
            return isInView;
        }
        
        /// <summary>
        /// スマートワープを実行
        /// </summary>
        private void PerformSmartTeleport()
        {
            Vector3 teleportPosition = CalculateSmartTeleportPosition();
            
            if (NavMesh.SamplePosition(teleportPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                navAgent.Warp(hit.position);
                
                // デバッグ用ログ
                if (showDebugGizmos)
                {
                    Debug.Log($"[CompanionAI] Smart teleport executed to position: {hit.position}");
                }
            }
            else
            {
                // フォールバック：従来の方式でワープ
                Vector3 fallbackPosition = player.position - player.forward * followDistance;
                if (NavMesh.SamplePosition(fallbackPosition, out NavMeshHit fallbackHit, 2f, NavMesh.AllAreas))
                {
                    transform.position = fallbackHit.position;
                    navAgent.Warp(fallbackHit.position);
                }
            }
        }
        
        /// <summary>
        /// 画面外1メートルの位置を計算
        /// </summary>
        private Vector3 CalculateSmartTeleportPosition()
        {
            if (playerCamera == null) return player.position - player.forward * followDistance;
            
            // カメラの視野から画面外1メートルの位置を計算
            UnityEngine.Camera cam = playerCamera;
            
            // プレイヤーからの方向ベクトルを複数候補で試す
            Vector3[] candidateDirections = {
                -cam.transform.right,      // 左側
                cam.transform.right,       // 右側
                -cam.transform.forward,    // 後ろ側
                (-cam.transform.right - cam.transform.forward).normalized,  // 左後ろ
                (cam.transform.right - cam.transform.forward).normalized    // 右後ろ
            };
            
            foreach (Vector3 direction in candidateDirections)
            {
                // 画面外1メートルの位置を計算
                Vector3 candidatePosition = CalculateOffscreenPosition(direction);
                
                // NavMesh上にある有効な位置かチェック
                if (NavMesh.SamplePosition(candidatePosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    // この位置が本当に画面外かチェック
                    Vector3 testScreenPoint = cam.WorldToViewportPoint(hit.position);
                    bool isOffscreen = testScreenPoint.x < -0.1f || testScreenPoint.x > 1.1f ||
                                      testScreenPoint.y < -0.1f || testScreenPoint.y > 1.1f ||
                                      testScreenPoint.z <= 0;
                    
                    if (isOffscreen)
                    {
                        return hit.position;
                    }
                }
            }
            
            // 適切な位置が見つからない場合は、プレイヤーの後方に配置
            return player.position - cam.transform.forward * (followDistance + teleportOffscreenMargin);
        }
        
        /// <summary>
        /// 指定方向の画面外位置を計算
        /// </summary>
        private Vector3 CalculateOffscreenPosition(Vector3 direction)
        {
            // カメラから見て画面外1メートルの距離を計算
            float distanceFromCamera = Vector3.Distance(playerCamera.transform.position, player.position);
            
            // 画面端を超える距離を計算（視野角を考慮）
            float halfFOV = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float offscreenDistance = distanceFromCamera * Mathf.Tan(halfFOV) + teleportOffscreenMargin;
            
            // プレイヤー位置から指定方向にオフセット
            Vector3 basePosition = player.position + direction * offscreenDistance;
            
            // 地面の高さに調整
            if (Physics.Raycast(basePosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                basePosition.y = hit.point.y;
            }
            else
            {
                basePosition.y = player.position.y;
            }
            
            return basePosition;
        }
        
        #endregion

        private void HandleCombatState()
        {
            if (currentTarget == null)
            {
                SetState(AIState.Follow);
                return;
            }

            // ターゲットが破壊されているかチェック
            if (IsTargetDestroyed(currentTarget))
            {
                Debug.Log($"{gameObject.name} - Current target {currentTarget.name} has been destroyed, returning to follow state");
                currentTarget = null;
                
                // MiningSystemの保留状態もクリア
                if (miningSystem != null)
                {
                    miningSystem.ClearPendingAttack();
                }
                
                SetState(AIState.Follow);
                return;
            }

            // ターゲットまでの距離チェック
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // 15メートル以上離れたら諦める
            if (distanceToTarget > 15f)
            {
                currentTarget = null;
                
                // MiningSystemの保留状態もクリア
                if (miningSystem != null)
                {
                    miningSystem.ClearPendingAttack();
                }
                
                SetState(AIState.Follow);
                return;
            }
            
            // 3メートル以内なら攻撃
            if (distanceToTarget <= 3f)
            {
                navAgent.ResetPath();
                LookAtTarget(currentTarget.transform.position);
                PerformAttack();
            }
            else
            {
                // ターゲットに向かって移動
                MoveToPosition(currentTarget.transform.position);
            }
        }



        private void HandleIdleState()
        {
            if (Vector3.Distance(transform.position, player.position) > followDistance * 2f)
            {
                SetState(AIState.Follow);
            }
        }

        private void HandleExploreState()
        {
            if (!navAgent.hasPath || navAgent.remainingDistance < 1f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 10f;
                randomDirection += transform.position;
                
                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    MoveToPosition(hit.position);
                }
                else
                {
                    SetState(AIState.Follow);
                }
            }
        }

        private void HandleSupportState()
        {
            Vector3 supportPosition = player.position + player.right * (followDistance * 0.7f);
            MoveToPosition(supportPosition);
            
            if (Vector3.Distance(transform.position, supportPosition) < 2f)
            {
                SetState(AIState.Follow);
            }
        }

        private void CheckForEnemies()
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // Level 1: No combat participation
            if (intelligenceLevel < 2) 
            {
                return;
            }

            // シンプルな10メートル範囲検索
            float searchRange = 10f;
            Collider[] allColliders = Physics.OverlapSphere(transform.position, searchRange);
            
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in allColliders)
            {
                if (collider == null || collider.gameObject == gameObject) continue;
                
                // シンプルな判定：Enemyタグ または enemyLayerMaskに含まれる
                bool isEnemy = collider.CompareTag("Enemy") || 
                              ((1 << collider.gameObject.layer) & enemyLayerMask) != 0;
                
                if (isEnemy)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.gameObject;
                    }
                }
            }
            
            if (nearestEnemy != null)
            {
                Debug.Log($"[CompanionAI] {gameObject.name} - Found enemy target: {nearestEnemy.name} at distance {nearestDistance:F1}");
                currentTarget = nearestEnemy;
                SetState(AIState.Combat);
            }
        }


        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 rayStart = transform.position + Vector3.up * 1.6f;
            Vector3 directionToTarget = (targetPosition - rayStart).normalized;
            float distanceToTarget = Vector3.Distance(rayStart, targetPosition);
            
            return !Physics.Raycast(rayStart, directionToTarget, distanceToTarget, obstacleLayerMask);
        }

        /// <summary>
        /// シンプルなエネミー判定：Enemyタグまたはレイヤーがあれば攻撃対象
        /// </summary>
        private bool IsSimpleEnemy(GameObject target)
        {
            if (target == null || target == gameObject) return false;
            
            // Enemyタグ または enemyLayerMaskに含まれる
            return target.CompareTag("Enemy") || 
                   ((1 << target.layer) & enemyLayerMask) != 0;
        }

        /// <summary>
        /// ターゲットが破壊されているかをチェック
        /// </summary>
        private bool IsTargetDestroyed(GameObject target)
        {
            if (target == null) return true;
            
            // IDestructibleインターフェースで破壊状態をチェック
            var destructible = target.GetComponent<IDestructible>();
            if (destructible == null)
            {
                destructible = target.GetComponentInParent<IDestructible>();
            }
            
            if (destructible != null && destructible.IsDestroyed)
            {
                return true;
            }
            
            // EnemyBaseで死亡状態をチェック
            var enemyBase = target.GetComponent<Enemies.EnemyBase>();
            if (enemyBase == null)
            {
                enemyBase = target.GetComponentInParent<Enemies.EnemyBase>();
            }
            
            if (enemyBase != null)
            {
                // EnemyBaseはIDestructibleを実装しているので、IsDestroyedプロパティを使用
                return enemyBase.IsDestroyed;
            }
            
            return false;
        }

        private void MoveToPosition(Vector3 targetPosition)
        {
            if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(targetPosition);
            }
        }

        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            lookDirection.y = 0f;
            
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        private void PerformAttack()
        {
            if (Time.time - lastUpdateTime < 1f) return;
            
            lastUpdateTime = Time.time;
            
            if (currentTarget != null && animatorController != null)
            {
                Debug.Log($"{gameObject.name} attacking {currentTarget.name}");
                
                // MiningSystemで攻撃準備
                bool attackPrepared = false;
                if (miningSystem != null)
                {
                    attackPrepared = miningSystem.TryAttackWithTool(currentTarget);
                    Debug.Log($"{gameObject.name} - Mining system attack prepared: {attackPrepared}");
                }
                
                // MiningSystemで準備できなかった場合の対策
                if (!attackPrepared)
                {
                    Debug.Log($"{gameObject.name} - Mining system failed, will use fallback attack system");
                }
                
                // アニメーション開始
                animatorController.TriggerAttack();
            }
        }

        /// <summary>
        /// ツール使用効果実行（プレイヤーシステムと同等）
        /// </summary>
        public virtual void ExecuteToolUsageEffect()
        {
            Debug.Log($"{gameObject.name} - ExecuteToolUsageEffect called");
            
            bool miningSystemExecuted = false;
            
            // MiningSystemがある場合はそれを使用
            if (miningSystem != null)
            {
                Debug.Log($"{gameObject.name} - Using CompanionMiningSystem for tool usage");
                try
                {
                    miningSystem.ExecuteMiningDamage();
                    miningSystemExecuted = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"{gameObject.name} - CompanionMiningSystem failed: {e.Message}");
                    miningSystemExecuted = false;
                }
            }
            
            // MiningSystemが失敗した場合、または存在しない場合のフォールバック
            if (!miningSystemExecuted || miningSystem == null)
            {
                Debug.Log($"{gameObject.name} - Using fallback attack system");
                if (currentTarget != null)
                {
                    ExecuteAttackDamage();
                }
            }
        }

        /// <summary>
        /// シンプルな攻撃ダメージ処理（アニメーションイベントから呼ばれる）
        /// </summary>
        public virtual void ExecuteAttackDamage()
        {
            float damage = 15f; // 固定ダメージ
            Vector3 attackPos = transform.position + transform.forward * 1.5f;
            
            Debug.Log($"{gameObject.name} - ExecuteAttackDamage called, damage: {damage}");
            Debug.Log($"{gameObject.name} - Attack position: {attackPos}, My position: {transform.position}");
            
            // シンプルに2m範囲ですべてのコライダーを取得
            Collider[] hits = Physics.OverlapSphere(attackPos, 2f);
            Debug.Log($"{gameObject.name} - OverlapSphere found {hits.Length} colliders");
            
            bool hitSomething = false;
            
            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == gameObject) continue;
                
                Debug.Log($"{gameObject.name} - Checking collider: {hit.name}, tag: {hit.tag}, layer: {hit.gameObject.layer}");
                
                // EnemyBaseコンポーネントを直接探す
                var enemy = hit.GetComponent<Enemies.EnemyBase>();
                if (enemy != null)
                {
                    Debug.Log($"[COMPANION ATTACK] {gameObject.name} - Found EnemyBase component on {hit.name}");
                    Debug.Log($"[COMPANION ATTACK] Enemy current target before: {enemy.CurrentTarget?.name ?? "None"}");
                    Debug.Log($"[COMPANION ATTACK] Enemy is locked: {enemy.IsTargetLocked}");
                    Debug.Log($"[COMPANION ATTACK] Calling TakeDamage({damage}) with attacker: {transform.name}");
                    
                    enemy.TakeDamage(damage, transform); // 攻撃者として自分を渡す
                    
                    Debug.Log($"[COMPANION ATTACK] Enemy current target after: {enemy.CurrentTarget?.name ?? "None"}");
                    Debug.Log($"[COMPANION ATTACK] Enemy is locked after: {enemy.IsTargetLocked}");
                    Debug.Log($"[COMPANION ATTACK] Enemy state: {enemy.CurrentState}");
                    
                    hitSomething = true;
                    
                    // ダメージテキスト表示
                    if (UI.UIManager.Instance != null)
                    {
                        UI.UIManager.Instance.ShowDamageText(hit.transform.position + Vector3.up * 1.5f, damage, false);
                    }
                    continue;
                }
                
                // 破壊可能オブジェクトもチェック
                var destructible = hit.GetComponent<Environment.IDestructible>();
                if (destructible != null && destructible.CanBeDestroyedBy(Core.ToolType.IronPipe))
                {
                    Debug.Log($"{gameObject.name} - Hitting destructible: {hit.name}");
                    destructible.TakeDamage(damage, Core.ToolType.IronPipe);
                    hitSomething = true;
                }
            }
            
            if (hitSomething)
            {
                companionCharacter.ChangeTrustLevel(1);
                Debug.Log($"{gameObject.name} - Attack hit!");
            }
            else
            {
                Debug.Log($"{gameObject.name} - Attack missed");
            }
        }


        private void UpdateLastPlayerPosition()
        {
            if (player != null)
            {
                lastPlayerPosition = player.position;
            }
        }

        /// <summary>
        /// 移動アニメーションを更新
        /// </summary>
        private void UpdateMovementAnimation()
        {
            if (animatorController == null || navAgent == null) return;

            // NavMeshAgentの実際の速度を取得
            float actualVelocity = navAgent.velocity.magnitude;
            CompanionMovementState targetMovementState;
            
            if (actualVelocity < 0.1f)
            {
                targetMovementState = CompanionMovementState.Idle;
                isRunning = false;
            }
            else
            {
                targetMovementState = currentState == AIState.Combat ? CompanionMovementState.Combat : CompanionMovementState.Moving;
                
                // 知能レベルに応じて移動速度を調整
                DetermineMovementBehavior();
                
                // NavMeshAgentの速度を設定（実際の移動速度）
                ApplyMovementSpeed();
                
                // 実際の移動速度から走行状態を判定
                isRunning = actualVelocity > walkSpeed + 0.5f;
            }

            // 実際の速度をアニメーターに送信（新しいシステム）
            animatorController.SetRealSpeed(actualVelocity);
            
            // アニメーションステートが変化した場合のみ更新（後方互換性のため）
            if (currentMovementState != targetMovementState || 
                animatorController.IsCrouching != isCrouching ||
                HasMovementStateChanged())
            {
                currentMovementState = targetMovementState;
                // 新しいメソッドを使用して実際の速度でステートを設定
                animatorController.SetMovementStateWithRealSpeed(currentMovementState, actualVelocity, isCrouching);
            }
        }
        
        /// <summary>
        /// 移動状態の変化をチェック
        /// </summary>
        private bool HasMovementStateChanged()
        {
            bool previousIsRunning = animatorController.CurrentMovementState == CompanionMovementState.Moving && 
                                   navAgent.speed > walkSpeed + 0.1f;
            return isRunning != previousIsRunning;
        }

        /// <summary>
        /// 知能レベルと状況に応じて移動行動を決定
        /// </summary>
        private void DetermineMovementBehavior()
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // デフォルト値
            shouldRun = false;
            shouldCrouch = false;
            
            switch (currentState)
            {
                case AIState.Follow:
                    DetermineFollowMovement(intelligenceLevel);
                    break;
                case AIState.Combat:
                    DetermineCombatMovement(intelligenceLevel);
                    break;
                case AIState.Explore:
                    DetermineExploreMovement(intelligenceLevel);
                    break;
                case AIState.Support:
                    DetermineSupportMovement(intelligenceLevel);
                    break;
            }
            
            // 能力チェック
            isRunning = shouldRun && canRun;
            isCrouching = shouldCrouch && canCrouch;
        }

        private void DetermineFollowMovement(int intelligenceLevel)
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // 距離に応じて移動速度を調整
            if (distanceToPlayer > followDistance * 2f)
            {
                shouldRun = true; // 遠い場合は走る
            }
            else if (distanceToPlayer < followDistance * 0.5f)
            {
                // 近すぎる場合は歩行またはしゃがみ（知能レベルに応じて）
                if (intelligenceLevel >= 3)
                {
                    shouldCrouch = true; // 賢いコンパニオンはしゃがみで接近
                }
            }
            
            // プレイヤーの状態を模倣（知能レベル4以上）
            if (intelligenceLevel >= 4 && player != null)
            {
                var playerController = player.GetComponent<EnhancedPlayerController>();
                if (playerController != null)
                {
                    // プレイヤーがしゃがんでいる場合は同じようにしゃがむ
                    if (playerController.IsCrouching)
                    {
                        shouldCrouch = true;
                        shouldRun = false;
                    }
                }
            }
        }

        private void DetermineCombatMovement(int intelligenceLevel)
        {
            // 戦闘中は積極的に移動
            shouldRun = intelligenceLevel >= 2;
            
            // 高い知能レベルでは戦術的にしゃがみを使用
            if (intelligenceLevel >= 4 && UnityEngine.Random.Range(0f, 1f) < 0.3f)
            {
                shouldCrouch = true;
                shouldRun = false;
            }
        }

        private void DetermineExploreMovement(int intelligenceLevel)
        {
            // 探索中は通常歩行
            if (intelligenceLevel >= 3 && UnityEngine.Random.Range(0f, 1f) < 0.2f)
            {
                shouldCrouch = true; // 偶にしゃがみで慎重に行動
            }
        }

        private void DetermineSupportMovement(int intelligenceLevel)
        {
            // 支援行動中は迅速に移動
            shouldRun = intelligenceLevel >= 2;
        }

        /// <summary>
        /// NavMeshAgentに移動速度を適用
        /// </summary>
        private void ApplyMovementSpeed()
        {
            if (navAgent == null) return;
            
            float targetSpeed;
            
            if (isCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else if (isRunning)
            {
                targetSpeed = runSpeed;
            }
            else
            {
                targetSpeed = walkSpeed;
            }
            
            navAgent.speed = targetSpeed;
        }


        private int GetIntelligenceLevelFromTrust()
        {
            if (companionCharacter == null) return 1;
            
            int trust = companionCharacter.TrustLevel;
            
            return trust switch
            {
                >= 0 and <= 20 => 1,
                >= 21 and <= 40 => 2,
                >= 41 and <= 60 => 3,
                >= 61 and <= 80 => 4,
                >= 81 and <= 100 => 5,
                _ => 1
            };
        }

        public void SetState(AIState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(newState);
                
                switch (newState)
                {
                    case AIState.Idle:
                        navAgent.ResetPath();
                        break;
                    case AIState.Combat:
                        navAgent.speed = 4.5f;
                        break;
                    default:
                        navAgent.speed = 3.5f;
                        break;
                }
            }
        }

        public bool CanExecuteCommand(CompanionCommand command)
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            return command switch
            {
                CompanionCommand.Follow => true,                    // Always available
                CompanionCommand.Stay => true,                     // Always available
                CompanionCommand.Attack => intelligenceLevel >= 2,  // Level 2+
                CompanionCommand.Defend => intelligenceLevel >= 2,  // Level 2+
                CompanionCommand.MoveTo => intelligenceLevel >= 3,  // Level 3+
                CompanionCommand.Scout => intelligenceLevel >= 3,   // Level 3+
                CompanionCommand.Flank => intelligenceLevel >= 4,   // Level 4+
                CompanionCommand.Support => intelligenceLevel >= 4, // Level 4+
                CompanionCommand.Retreat => intelligenceLevel >= 5, // Level 5+
                CompanionCommand.Advanced => intelligenceLevel >= 5, // Level 5+
                _ => false
            };
        }

        public bool ExecuteCommand(CompanionCommand command, Vector3 position = default, GameObject target = null)
        {
            if (!CanExecuteCommand(command)) return false;
            
            switch (command)
            {
                case CompanionCommand.Follow:
                    SetState(AIState.Follow);
                    return true;
                    
                case CompanionCommand.Stay:
                    SetState(AIState.Idle);
                    return true;
                    
                case CompanionCommand.Attack:
                    if (target != null)
                    {
                        currentTarget = target;
                        SetState(AIState.Combat);
                        return true;
                    }
                    return false;
                    
                case CompanionCommand.MoveTo:
                    if (position != default)
                    {
                        SetState(AIState.Explore);
                        MoveToPosition(position);
                        return true;
                    }
                    return false;
                    
                case CompanionCommand.Support:
                    SetState(AIState.Support);
                    return true;
                    
                default:
                    return false;
            }
        }

        // Legacy methods for backward compatibility
        public void CommandMoveTo(Vector3 position)
        {
            ExecuteCommand(CompanionCommand.MoveTo, position);
        }

        public void CommandAttack(GameObject target)
        {
            ExecuteCommand(CompanionCommand.Attack, target: target);
        }

        public void CommandFollow()
        {
            ExecuteCommand(CompanionCommand.Follow);
        }

        public void CommandSupport()
        {
            ExecuteCommand(CompanionCommand.Support);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            
            // Smart teleport range (replaces old maxFollowDistance)
            if (enableSmartTeleport)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); // オレンジ色
                Gizmos.DrawWireSphere(transform.position, teleportDistance);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
            }
            
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, player.position);
                
                // Camera visibility indicator
                if (enableSmartTeleport && playerCamera != null)
                {
                    bool isVisible = IsVisibleInCamera();
                    Gizmos.color = isVisible ? Color.green : Color.red;
                    Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
                }
            }
            
            Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftBoundary);
            Gizmos.DrawRay(transform.position, rightBoundary);
            
            // シンプルな視覚化
            
            // 10メートル検出範囲
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 10f);
            
            // 3メートル接近範囲
            if (currentState == AIState.Combat)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 3f);
                
                // プレイヤーと同じ攻撃範囲表示
                Vector3 attackPosition = attackPoint != null ? attackPoint.position : transform.position + transform.forward * 1f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(attackPosition, attackRange);
            }
            
            // Current target visualization
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
                Gizmos.DrawWireSphere(currentTarget.transform.position, 0.5f);
            }
            
            // State visualization
            string stateText = $"{gameObject.name}\nState: {currentState}\nIntel: {GetIntelligenceLevelFromTrust()}";
            if (currentTarget != null)
            {
                stateText += $"\nTarget: {currentTarget.name}";
            }
        }

        private void OnValidate()
        {
            if (navAgent != null)
            {
                navAgent.stoppingDistance = stoppingDistance;
            }
            
            // 設定チェック
            if (enemyLayerMask == 0)
            {
                Debug.LogWarning($"[CompanionAI] {gameObject.name} - EnemyLayerMask is not set! Enemy detection may not work properly.");
            }
            
            if (destructibleLayers == 0)
            {
                Debug.LogWarning($"[CompanionAI] {gameObject.name} - DestructibleLayers is not set! Attacks will not work properly.");
            }
            
            if (attackRange <= 0)
            {
                Debug.LogWarning($"[CompanionAI] {gameObject.name} - AttackRange must be greater than 0!");
            }
        }
        
        #region Health System Implementation
        
        /// <summary>
        /// HPバーの初期化
        /// </summary>
        protected virtual void InitializeHealthBar()
        {
            if (healthBarFill != null && healthBarFill.material != null)
            {
                // 初期状態では満タンに設定
                healthBarFill.material.SetFloat("_Fill_1", 1f);
                
                // 初期状態ではMAXなので非表示
                if (healthBarBackground != null)
                {
                    healthBarBackground.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// HPバーの更新
        /// </summary>
        protected virtual void UpdateHealthBar()
        {
            if (healthBarFill == null || healthBarFill.material == null) return;

            float healthPercentage = HealthPercentage;

            // HPがMAXの場合は非表示、それ以外は表示
            if (healthPercentage >= 1f)
            {
                if (healthBarBackground != null)
                {
                    healthBarBackground.gameObject.SetActive(false);
                }
            }
            else
            {
                if (healthBarBackground != null)
                {
                    healthBarBackground.gameObject.SetActive(true);
                }
                
                // _Fill_1パラメータでHP量を制御
                healthBarFill.material.SetFloat("_Fill_1", healthPercentage);
            }
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
        
        
        // IDestructible interface implementation
        public virtual bool CanBeDestroyedBy(ToolType toolType)
        {
            // コンパニオンは攻撃可能（必要に応じて制限可能）
            return toolType == ToolType.Pickaxe || toolType == ToolType.IronPipe;
        }

        public virtual void TakeDamage(float damage, ToolType toolType)
        {
            if (isDead) return;

            Debug.Log($"[CompanionAI] {gameObject.name} - Taking damage: {damage} from {toolType}");

            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            // HPバーを更新
            UpdateHealthBar();

            // ダメージテキストを表示
            ShowDamageText(damage);

            // 敵のターゲットを自分に変更させる（プレイヤーから攻撃された場合）
            if (player != null)
            {
                NotifyEnemiesOfAttack(transform.position, transform);
            }

            // 信頼度を少し下げる（味方に攻撃されたため）
            if (companionCharacter != null)
            {
                companionCharacter.ChangeTrustLevel(-5);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        // 既存のTakeDamageメソッドをオーバーロードとして残す
        public virtual void TakeDamage(float damage)
        {
            Debug.Log($"[CompanionAI] {gameObject.name} - TakeDamage called with damage: {damage}");
            TakeDamage(damage, ToolType.IronPipe); // デフォルトツール
        }
        
        /// <summary>
        /// ヒーリング処理
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[CompanionAI] {gameObject.name} - Healed for {amount}. Current health: {currentHealth}/{maxHealth}");
        }
        
        /// <summary>
        /// 死亡処理
        /// </summary>
        protected virtual void Die()
        {
            if (isDead) return;
            
            isDead = true;
            Debug.Log($"[CompanionAI] {gameObject.name} - Companion has died");

            // NavMeshAgentを停止
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }

            // 死亡アニメーション再生
            if (animatorController != null)
            {
                // CompanionAnimatorControllerに死亡アニメーションがあれば再生
                // TODO: 実装に応じて適切なメソッドを呼び出す
            }

            // コンパニオンキャラクターに死亡を通知
            if (companionCharacter != null)
            {
                // CompanionCharacterクラスの死亡処理を呼び出す（最大ダメージを与えて死亡状態にする）
                companionCharacter.Stats?.TakeDamage(companionCharacter.Stats.MaxHealth);
            }

            // 数秒後にオブジェクトを削除または無効化
            StartCoroutine(HandleDeathSequence());
        }
        
        /// <summary>
        /// 死亡シーケンス処理
        /// </summary>
        private System.Collections.IEnumerator HandleDeathSequence()
        {
            // 3秒間死亡状態を維持
            yield return new WaitForSeconds(3f);
            
            // オブジェクトを非アクティブ化（完全に削除せず、復活可能性を残す）
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 敵に攻撃を通知してターゲットを切り替えさせる
        /// </summary>
        private void NotifyEnemiesOfAttack(Vector3 attackPosition, Transform newTarget)
        {
            Enemies.EnemyBase[] allEnemies = FindObjectsOfType<Enemies.EnemyBase>();

            foreach (var enemy in allEnemies)
            {
                if (enemy.IsDestroyed) continue;

                float distance = Vector3.Distance(enemy.transform.position, attackPosition);

                // 攻撃時の感知範囲内にいる場合はターゲットを切り替える
                if (distance <= 15f) // 15メートル以内の敵が反応
                {
                    enemy.ForceSetTarget(newTarget);
                    Debug.Log($"[CompanionAI] {gameObject.name} - Enemy {enemy.name} target switched to {newTarget.name}");
                }
            }
        }
        
        #endregion
    }

    public enum AIState
    {
        Idle,
        Follow,
        Combat,
        Explore,
        Support
    }

    public enum CompanionCommand
    {
        Follow,     // Always available
        Stay,       // Always available
        Attack,     // Level 2+
        Defend,     // Level 2+
        MoveTo,     // Level 3+
        Scout,      // Level 3+
        Flank,      // Level 4+
        Support,    // Level 4+
        Retreat,    // Level 5+
        Advanced    // Level 5+
    }
}