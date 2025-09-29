using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using KowloonBreak.Core;
using KowloonBreak.Player;
using KowloonBreak.Environment;
using KowloonBreak.UI;

namespace KowloonBreak.Characters
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CompanionCharacter))]
    public class CompanionAI : MonoBehaviour, IDestructible, IKnockbackable
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
        [SerializeField] private ToolType weaponType = ToolType.IronPipe;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private Vector3 attackBoxSize = new Vector3(2f, 2f, 3f);
        
        [Header("Health System")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        
        [Header("HP Bar (UI System)")]
        // 古いSpriteRendererベースのHP表示は削除し、UIシステムを使用
        
        [Header("Damage Display")]
        [SerializeField] protected Transform damageDisplayPoint;
        
        [Header("Visual Effects")]
        [SerializeField] private SpriteRenderer companionRenderer;
        
        [Header("Dialogue System")]
        [SerializeField] private CompanionDialogue dialogueData;
        [SerializeField] private Vector3 dialogueOffset = Vector3.up * 2.5f;
        [SerializeField] private bool enableDialogueSystem = true;
        [SerializeField] private float dialogueCooldown = 3f;
        [SerializeField] private float combatDialogueCooldown = 5f;

        [Header("Knockback System")]
        [SerializeField] private KnockbackSystem knockbackSystem = new KnockbackSystem();
        
        // 現在表示中のDialogueText管理
        private GameObject currentDialogueText;
        private bool hasActiveDialogue => currentDialogueText != null;
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform interactionPromptAnchor;
        [SerializeField] private Transform dialogueAnchor; // DialogueText表示用アンカー
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool debugStateChanges = true;
        [SerializeField] private bool debugCommandExecution = true;
        [SerializeField] private bool debugNavMeshAgent = false;
        [SerializeField] private AIState currentState = AIState.Follow;
        
        // Stay命令の状態を記録
        private bool isStayCommandActive = false;
        
        // セリフ管理
        private Dictionary<CombatDialogueType, float> combatDialogueTimers = new Dictionary<CombatDialogueType, float>();
        private Dictionary<CompanionCommand, float> commandDialogueTimers = new Dictionary<CompanionCommand, float>();
        private Dictionary<AIState, float> stateDialogueTimers = new Dictionary<AIState, float>();
        private float lastGeneralDialogueTime = 0f;

        // 感染ダイアログ管理
        private Dictionary<InfectionDialogueType, float> infectionDialogueTimers = new Dictionary<InfectionDialogueType, float>();
        private float lastInfectionDialogueTime = 0f;
        private float infectionDialogueCooldown = 8f; // 感染中ダイアログのクールダウン（適度な頻度に調整）
        
        private NavMeshAgent navAgent;
        private CompanionCharacter companionCharacter;
        private CompanionAnimatorController animatorController;
        private CompanionToolInteractionSystem toolSystem;
        private GameObject currentTarget;
        private Vector3 lastPlayerPosition;
        private float lastUpdateTime;
        private UnityEngine.Camera playerCamera;
        
        // 統合された攻撃システム
        private IDestructible pendingTarget;
        private ToolType pendingToolType;
        private float pendingDamage;
        private float lastAttackTime;
        private bool isUsingTool = false; // 攻撃モーション中の移動制限用
        
        // Health System
        protected bool isDead = false;
        
        // UI Health Bar System
        private GameObject uiHealthBar;
        
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
        public Transform DialogueAnchor => dialogueAnchor;
        
        public System.Action<AIState> OnStateChanged;
        
        // 意思決定システム
        private Dictionary<string, float> decisionFactors = new Dictionary<string, float>();
        private Queue<CompanionDecision> decisionQueue = new Queue<CompanionDecision>();
        private float nextDecisionTime = 0f;
        private float decisionInterval = 2f;
        
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
            toolSystem = GetComponent<CompanionToolInteractionSystem>();

            // CompanionCharacterがない場合は作成
            if (companionCharacter == null)
            {
                if (debugCommandExecution)
                companionCharacter = gameObject.AddComponent<CompanionCharacter>();
            }

            if (animatorController == null)
            {
                animatorController = GetComponentInChildren<CompanionAnimatorController>();
            }

            // companionRendererを自動で見つける
            if (companionRenderer == null)
            {
                companionRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Health system initialization
            currentHealth = maxHealth;

            InitializeAgent();
            InitializeUIHealthBar();
        }

        private void Start()
        {
            FindPlayer();
            FindPlayerCamera();
            InitializeKnockbackSystem();
            InitializeInfectionBar();
            InitializeInfectionEvents();
            SetState(AIState.Follow);
            StartCoroutine(UpdateAILoop());
        }

        /// <summary>
        /// 感染バーUIを初期化
        /// </summary>
        private void InitializeInfectionBar()
        {
            // UIManagerが利用可能で、感染バーがまだ作成されていない場合に作成
            if (UI.UIManager.Instance != null && !UI.UIManager.Instance.HasInfectionBarForCompanion(this))
            {
                UI.UIManager.Instance.CreateInfectionBarForCompanion(this);
            }
        }

        /// <summary>
        /// 感染イベントを初期化
        /// </summary>
        private void InitializeInfectionEvents()
        {
            if (companionCharacter != null && companionCharacter.Infection != null)
            {
                // 感染ダイアログイベントを購読
                companionCharacter.Infection.OnInfectionDialogueTriggered += HandleInfectionDialogue;
            }
        }

        /// <summary>
        /// 感染ダイアログを処理
        /// </summary>
        private void HandleInfectionDialogue(InfectionDialogueType dialogueType)
        {
            if (enableDialogueSystem)
            {
                ShowInfectionDialogue(dialogueType);
            }
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
                }
            }
        }

        private IEnumerator UpdateAILoop()
        {
            while (true)
            {
                float currentUpdateRate = GetCurrentUpdateRate();
                yield return new WaitForSeconds(currentUpdateRate);

                // 感染ダイアログは感染状態でも実行する必要があるため、先に処理
                if (companionCharacter != null && companionCharacter.Infection.IsInfected)
                {
                    UpdateInfectionDialogue();
                }

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

            // ノックバック状態の更新
            knockbackSystem.UpdateKnockbackState();

            // ノックバック中はAI処理を一時停止
            if (knockbackSystem.IsKnockedBack) return;

            if (player == null)
            {
                FindPlayer();
                return;
            }

            // 意思決定システムの実行
            UpdateDecisionSystem();


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
            // Stay命令中は緊急時以外は状態変更しない
            if (isStayCommandActive) return;

            // Find the most threatening enemy near the player
            GameObject mostThreatening = FindMostThreateningEnemy();

            if (mostThreatening != null)
            {
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
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh && navAgent.hasPath && navAgent.remainingDistance < 0.5f)
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
            // Idle状態（Stay命令中）はテレポートしない
            if (currentState == AIState.Idle) return false;
            
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
                // Stay命令が有効な場合はIdle状態に戻す、そうでなければFollow状態に戻す
                if (isStayCommandActive)
                {
                    SetState(AIState.Idle);
                }
                else
                {
                    SetState(AIState.Follow);
                }
                return;
            }

            // ターゲットが破壊されているかチェック
            if (IsTargetDestroyed(currentTarget))
            {
                currentTarget = null;
                
                // 保留攻撃をクリア
                ClearPendingAttack();
                
                // Stay命令が有効な場合はIdle状態に戻す、そうでなければFollow状態に戻す
                if (isStayCommandActive)
                {
                    SetState(AIState.Idle);
                }
                else
                {
                    SetState(AIState.Follow);
                }
                return;
            }

            // ターゲットまでの距離チェック
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            // 15メートル以上離れたら諦める
            if (distanceToTarget > 15f)
            {
                currentTarget = null;
                
                // 保留攻撃をクリア
                ClearPendingAttack();
                
                // Stay命令が有効な場合はIdle状態に戻す、そうでなければFollow状態に戻す
                if (isStayCommandActive)
                {
                    SetState(AIState.Idle);
                }
                else
                {
                    SetState(AIState.Follow);
                }
                return;
            }
            
            // 3メートル以内なら攻撃
            if (distanceToTarget <= 3f)
            {
                // NavMeshAgentが有効かつアクティブな場合のみResetPathを呼び出す
                if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                // 攻撃範囲内では継続的に敵を向く（滑らかな回転）
                LookAtTarget(currentTarget.transform.position);
                PerformAttack();
            }
            else
            {
                // ターゲットに向かって移動（移動中も徐々に敵を向く）
                LookAtTarget(currentTarget.transform.position);
                MoveToPosition(currentTarget.transform.position);
            }
        }



        private void HandleIdleState()
        {
            // Stay命令中：その場に留まる
            // NavMeshAgentのパスをリセットして移動を停止
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh && navAgent.hasPath)
            {
                navAgent.ResetPath();
            }
            
            // 敵検出と戦闘は UpdateAI() の CheckForEnemies() で自動的に処理される
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
                currentTarget = nearestEnemy;
                
                // 敵発見時に即座に敵を向く
                LookAtTargetInstantly(nearestEnemy.transform.position);
                
                ShowCombatDialogue(CombatDialogueType.EnemySpotted);
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
            // 攻撃モーション中は移動を制限
            if (isUsingTool)
            {
                return;
            }

            if (navAgent == null)
            {
                if (debugNavMeshAgent)
                return;
            }
            
            if (!navAgent.isActiveAndEnabled)
            {
                if (debugNavMeshAgent)
                return;
            }
            
            if (!navAgent.isOnNavMesh)
            {
                if (debugNavMeshAgent)
                return;
            }
            
            bool pathSet = navAgent.SetDestination(targetPosition);
            
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
        
        /// <summary>
        /// 攻撃時に即座にターゲットを向く（補間なし）
        /// </summary>
        private void LookAtTargetInstantly(Vector3 targetPosition)
        {
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            lookDirection.y = 0f; // Y軸の回転のみ
            
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = targetRotation; // 即座に回転
            }
        }

        private void PerformAttack()
        {
            // 攻撃クールダウンをチェック
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack < attackCooldown) return;

            if (currentTarget != null && animatorController != null)
            {
                // 攻撃前に必ず敵を向く（即座に向く）
                LookAtTargetInstantly(currentTarget.transform.position);

                // 攻撃開始のセリフを表示
                ShowCombatDialogue(CombatDialogueType.AttackStart);

                // 統合された攻撃準備
                bool attackPrepared = PrepareAttack(currentTarget);

                // 攻撃開始フラグを設定（移動制限のため）
                isUsingTool = true;

                // 武器タイプに応じたアニメーション開始
                if (weaponType == ToolType.Pickaxe)
                {
                    animatorController.TriggerDig();
                }
                else
                {
                    animatorController.TriggerAttack();
                }

                // 最後の攻撃時間を更新
                lastAttackTime = Time.time;
            }
        }

        /// <summary>
        /// ツール使用効果実行（統合されたツールシステム使用）
        /// </summary>
        public virtual void ExecuteToolUsageEffect()
        {
            
            // 統合ツールシステムを使用
            if (toolSystem != null)
            {
                toolSystem.ExecuteToolUsage();
            }
            else
            {
                // フォールバック：既存の統合システム
                ExecuteAttack();
            }
        }

        /// <summary>
        /// 道具使用アニメーション終了時の処理（プレイヤーと同様）
        /// </summary>
        public void OnToolUsageAnimationEnd()
        {
            isUsingTool = false;


            // 統合ツールシステムの保留状態をクリア
            if (toolSystem != null)
            {
                toolSystem.ClearPendingAction();
            }
        }

        #region 統合された攻撃システム
        
        /// <summary>
        /// 攻撃を準備（統合ツールシステム使用）
        /// </summary>
        public bool PrepareAttack(GameObject target)
        {
            if (target == null) 
            {
                return false;
            }
            
            
            // 統合ツールシステムを使用
            if (toolSystem != null)
            {
                bool success = toolSystem.PrepareAttackOnTarget(target);
                if (success)
                {
                    return true;
                }
            }
            
            // フォールバック：既存システムを使用
            var destructible = target.GetComponent<IDestructible>();
            if (destructible == null)
            {
                destructible = target.GetComponentInParent<IDestructible>();
            }
            
            if (destructible == null) 
            {
                return false;
            }
            
            // 既存の攻撃準備
            pendingTarget = destructible;
            pendingToolType = weaponType;
            pendingDamage = attackDamage;
            
            return true;
        }
        
        /// <summary>
        /// 保留攻撃をクリア
        /// </summary>
        public void ClearPendingAttack()
        {
            if (toolSystem != null)
            {
                toolSystem.ClearPendingAction();
            }
            
            if (pendingTarget != null)
            {
                pendingTarget = null;
            }
        }
        
        /// <summary>
        /// 攻撃を実行（アニメーションイベントから呼ばれる）
        /// </summary>
        public void ExecuteAttack()
        {
            if (pendingTarget == null)
            {
                // フォールバック：既存の範囲攻撃
                ExecuteFallbackAttack();
                return;
            }
            
            // ターゲットが既に破壊されているかチェック
            if (pendingTarget.IsDestroyed)
            {
                pendingTarget = null;
                return;
            }
            
            // MonoBehaviourとして有効性をチェック
            if (pendingTarget is MonoBehaviour targetMono && targetMono == null)
            {
                pendingTarget = null;
                return;
            }
            
            AttemptAttack(pendingTarget, pendingToolType, pendingDamage);
            
            // クリア
            pendingTarget = null;
        }
        
        /// <summary>
        /// 実際の攻撃処理
        /// </summary>
        private void AttemptAttack(IDestructible target, ToolType toolType, float damage)
        {
            // ターゲットがそのツールで破壊可能かチェック
            bool canDestroy = target.CanBeDestroyedBy(toolType);
            
            if (canDestroy)
            {
                // EnemyBaseの場合は攻撃者情報を渡す
                if (target is Enemies.EnemyBase enemyBase)
                {
                    enemyBase.TakeDamage(damage, toolType, transform);
                }
                else
                {
                    // 一般的な破壊可能オブジェクト
                    target.TakeDamage(damage, toolType);
                }
                
                // ダメージテキスト表示
                if (UI.UIManager.Instance != null && target is MonoBehaviour targetMono)
                {
                    UI.UIManager.Instance.ShowDamageText(targetMono.transform.position + Vector3.up * 1.5f, damage, false);
                }
                
                // 信頼度向上
                companionCharacter?.ChangeTrustLevel(1);
                
            }
        }
        
        /// <summary>
        /// フォールバック攻撃（範囲スキャン）
        /// </summary>
        private void ExecuteFallbackAttack()
        {
            Vector3 attackPos = transform.position + transform.forward * 1.5f;
            float damage = attackDamage;
            
            // 範囲内のコライダーを取得
            Collider[] hits = Physics.OverlapSphere(attackPos, attackRange);
            
            bool hitSomething = false;
            
            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == gameObject) continue;
                
                // EnemyBaseを優先チェック
                var enemy = hit.GetComponent<Enemies.EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, transform);
                    hitSomething = true;
                    
                    if (UI.UIManager.Instance != null)
                    {
                        UI.UIManager.Instance.ShowDamageText(hit.transform.position + Vector3.up * 1.5f, damage, false);
                    }
                    continue;
                }
                
                // 破壊可能オブジェクトもチェック
                var destructible = hit.GetComponent<IDestructible>();
                if (destructible != null && destructible.CanBeDestroyedBy(weaponType))
                {
                    destructible.TakeDamage(damage, weaponType);
                    hitSomething = true;
                }
            }
            
            if (hitSomething)
            {
                companionCharacter?.ChangeTrustLevel(1);
            }
        }
        
        #endregion


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


        /// <summary>
        /// CompanionCharacterコンポーネントを安全に取得
        /// </summary>
        private bool EnsureCompanionCharacter()
        {
            if (companionCharacter == null)
            {
                companionCharacter = GetComponent<CompanionCharacter>();
                if (companionCharacter == null)
                {
                    if (debugCommandExecution)
                    return false;
                }
            }
            return true;
        }

        private int GetIntelligenceLevelFromTrust()
        {
            if (!EnsureCompanionCharacter())
            {
                return 1;
            }

            int trust = companionCharacter.TrustLevel;
            int intelligenceLevel = trust switch
            {
                >= 0 and <= 20 => 1,
                >= 21 and <= 40 => 2,
                >= 41 and <= 60 => 3,
                >= 61 and <= 80 => 4,
                >= 81 and <= 100 => 5,
                _ => 1
            };

            return intelligenceLevel;
        }
        
        // 意思決定システム
        private void UpdateDecisionSystem()
        {
            if (Time.time < nextDecisionTime) return;
            
            nextDecisionTime = Time.time + decisionInterval;
            
            // 決定要因の更新
            UpdateDecisionFactors();
            
            // 決定の評価と実行
            EvaluateAndExecuteDecisions();
        }
        
        private void UpdateDecisionFactors()
        {
            if (!EnsureCompanionCharacter() || player == null) return;
            
            float playerDistance = Vector3.Distance(transform.position, player.position);
            float healthPercentage = currentHealth / maxHealth;
            int trustLevel = companionCharacter.TrustLevel;
            
            decisionFactors["playerDistance"] = playerDistance;
            decisionFactors["healthPercentage"] = healthPercentage;
            decisionFactors["trustLevel"] = trustLevel / 100f;
            decisionFactors["intelligenceLevel"] = GetIntelligenceLevelFromTrust() / 5f;
            
            // 周囲の脅威を評価
            var nearbyEnemies = Physics.OverlapSphere(transform.position, detectionRange, enemyLayerMask);
            decisionFactors["threatLevel"] = nearbyEnemies.Length / 5f; // 最大5体の敵で正規化
        }
        
        private void EvaluateAndExecuteDecisions()
        {
            var decisions = new List<CompanionDecision>();
            
            // 各種決定オプションを評価
            EvaluateHealthDecisions(decisions);
            EvaluateCombatDecisions(decisions);
            EvaluateSupportDecisions(decisions);
            
            if (decisions.Count == 0) return;
            
            // 優先度とコンフィデンスに基づいてソート
            decisions.Sort((a, b) => (b.priority * b.confidence).CompareTo(a.priority * a.confidence));
            
            // 最も適切な決定を実行
            var bestDecision = decisions[0];
            if (bestDecision.confidence > 0.5f) // 50%以上の信頼度
            {
                bestDecision.action?.Invoke();
            }
        }
        
        private void EvaluateHealthDecisions(List<CompanionDecision> decisions)
        {
            float healthPercentage = decisionFactors["healthPercentage"];
            
            // 緊急回復の決定
            if (healthPercentage < 0.3f && companionCharacter.HasSkill(SkillType.Medical))
            {
                decisions.Add(new CompanionDecision(
                    "EmergencyHealing",
                    1.0f,
                    0.9f,
                    $"Health critically low ({healthPercentage:P0})",
                    () => {
                        // 回復行動の実装
                        // 実際の回復ロジックをここに実装
                    }
                ));
            }
        }
        
        private void EvaluateCombatDecisions(List<CompanionDecision> decisions)
        {
            float threatLevel = decisionFactors.GetValueOrDefault("threatLevel", 0f);
            float trustLevel = decisionFactors["trustLevel"];
            
            // 積極的戦闘の決定（Stay命令中は実行しない）
            if (threatLevel > 0.3f && trustLevel > 0.6f && currentState != AIState.Combat && !isStayCommandActive)
            {
                decisions.Add(new CompanionDecision(
                    "EngageCombat",
                    0.8f,
                    trustLevel,
                    $"Threat detected ({threatLevel:P0}) and trust is high ({trustLevel:P0})",
                    () => {
                        if (currentTarget != null)
                            SetState(AIState.Combat);
                    }
                ));
            }
            
            // 退避の決定（Stay命令中は実行しない）
            if (threatLevel > 0.7f && decisionFactors["healthPercentage"] < 0.5f && !isStayCommandActive)
            {
                decisions.Add(new CompanionDecision(
                    "TacticalRetreat",
                    0.9f,
                    0.8f,
                    $"High threat ({threatLevel:P0}) with low health ({decisionFactors["healthPercentage"]:P0})",
                    () => {
                        SetState(AIState.Follow);
                        // プレイヤーの後ろに移動
                        Vector3 retreatPosition = player.position - player.forward * followDistance;
                        navAgent.SetDestination(retreatPosition);
                    }
                ));
            }
        }
        
        private void EvaluateSupportDecisions(List<CompanionDecision> decisions)
        {
            float intelligenceLevel = decisionFactors["intelligenceLevel"];
            float trustLevel = decisionFactors["trustLevel"];
            
            // 積極的サポートの決定（Stay命令中は実行しない）
            if (intelligenceLevel >= 0.8f && trustLevel >= 0.7f && !isStayCommandActive) // レベル4以上、信頼度70%以上
            {
                decisions.Add(new CompanionDecision(
                    "ProactiveSupport",
                    0.6f,
                    intelligenceLevel * trustLevel,
                    $"High intelligence ({intelligenceLevel:P0}) and trust ({trustLevel:P0})",
                    () => {
                        // 積極的なサポート行動
                        // プレイヤーの近くで警戒態勢
                        SetState(AIState.Support);
                    }
                ));
            }
        }

        public void SetState(AIState newState)
        {
            if (currentState != newState)
            {
                AIState previousState = currentState;
                currentState = newState;
                
                
                OnStateChanged?.Invoke(newState);
                
                // セリフを表示
                ShowStateDialogue(newState);
                
                switch (newState)
                {
                    case AIState.Idle:
                        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                        {
                            navAgent.ResetPath();
                        }
                        break;
                    case AIState.Combat:
                        if (navAgent != null && navAgent.isActiveAndEnabled)
                        {
                            navAgent.speed = 4.5f;
                        }
                        break;
                    default:
                        if (navAgent != null && navAgent.isActiveAndEnabled)
                        {
                            navAgent.speed = 3.5f;
                        }
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
                CompanionCommand.Support => intelligenceLevel >= 4, // Level 4+
                CompanionCommand.Retreat => intelligenceLevel >= 5, // Level 5+
                CompanionCommand.Advanced => intelligenceLevel >= 5, // Level 5+
                _ => false
            };
        }

        public bool ExecuteCommand(CompanionCommand command, Vector3 position = default, GameObject target = null)
        {
            
            if (!CanExecuteCommand(command))
            {
                return false;
            }
            
            // コマンド受領時のセリフを表示
            ShowCommandDialogue(command);
            
            switch (command)
            {
                case CompanionCommand.Follow:
                    isStayCommandActive = false; // Stay命令を解除
                    SetState(AIState.Follow);
                    return true;
                    
                case CompanionCommand.Stay:
                    isStayCommandActive = true; // Stay命令を記録
                    SetState(AIState.Idle);
                    return true;
                    
                case CompanionCommand.Attack:
                    if (target != null)
                    {
                        currentTarget = target;
                        
                        // 攻撃コマンド受領時に即座に敵を向く
                        LookAtTargetInstantly(target.transform.position);
                        
                        SetState(AIState.Combat);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    
                case CompanionCommand.Defend:
                    // 防衛モード：プレイヤーの周囲に留まり、敵を積極的に迎撃
                    SetState(AIState.Support);
                    return true;
                    
                case CompanionCommand.Support:
                    SetState(AIState.Support);
                    return true;
                    
                case CompanionCommand.Retreat:
                    // 撤退：プレイヤーの後方に移動し、追従距離を拡大
                    Vector3 retreatPosition = player != null ? 
                        player.position - player.forward * 8f : transform.position - transform.forward * 8f;
                    SetState(AIState.Follow);
                    MoveToPosition(retreatPosition);
                    return true;
                    
                case CompanionCommand.Advanced:
                    // 高度な戦術行動：状況に応じて最適な行動を選択
                    return ExecuteAdvancedTactics();
                    
                default:
                    return false;
            }
        }

        
        /// <summary>
        /// 高度な戦術行動を実行
        /// </summary>
        private bool ExecuteAdvancedTactics()
        {
            
            // 状況を分析
            bool playerInDanger = false;
            bool enemiesNearby = false;
            
            if (player != null)
            {
                var playerController = player.GetComponent<EnhancedPlayerController>();
                if (playerController != null)
                {
                    playerInDanger = playerController.HealthPercentage < 0.5f;
                }
                
                // 周囲の敵をチェック
                Collider[] nearbyEnemies = Physics.OverlapSphere(player.position, 12f, enemyLayerMask);
                enemiesNearby = nearbyEnemies.Length > 0;
            }
            
            // 状況に応じて最適な行動を選択
            if (playerInDanger && enemiesNearby)
            {
                // プレイヤーが危険で敵が近くにいる：積極的な支援
                GameObject threatTarget = FindMostThreateningEnemy();
                if (threatTarget != null)
                {
                    currentTarget = threatTarget;
                    SetState(AIState.Combat);
                    return true;
                }
            }
            else if (enemiesNearby && !playerInDanger)
            {
                // 敵が近くにいるがプレイヤーは安全：偵察行動
                Vector3 scoutPosition = player.position + player.forward * 10f;
                SetState(AIState.Explore);
                MoveToPosition(scoutPosition);
                return true;
            }
            else
            {
                // 平常時：支援位置で待機
                SetState(AIState.Support);
                return true;
            }
            
            return false;
        }

        // Legacy methods for backward compatibility

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

        /// <summary>
        /// プレイヤーの近くにワープする（リトライ時のコンパニオン復活用）
        /// </summary>
        public void WarpToPlayerNearby()
        {
            if (player == null)
            {
                return;
            }

            // プレイヤーの周囲にワープポイントを探す
            Vector3 warpPosition = FindSafeWarpPosition(player.position, 3f, 8f);

            // NavMeshAgentを一時停止してワープ
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.enabled = false;
            }

            // 位置をワープ
            transform.position = warpPosition;

            // NavMeshAgentを再有効化
            if (navAgent != null)
            {
                navAgent.enabled = true;
                navAgent.Warp(warpPosition);
            }

        }

        /// <summary>
        /// 安全なワープ位置を探す
        /// </summary>
        private Vector3 FindSafeWarpPosition(Vector3 centerPosition, float minDistance, float maxDistance)
        {
            // 複数の候補位置を試す
            for (int attempts = 0; attempts < 8; attempts++)
            {
                // ランダムな角度でプレイヤーの周囲に配置
                float angle = attempts * 45f; // 45度ずつ回転
                float distance = UnityEngine.Random.Range(minDistance, maxDistance);

                Vector3 direction = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Vector3 candidatePosition = centerPosition + direction * distance;

                // NavMeshの有効な位置かチェック
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(candidatePosition, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            // 安全な位置が見つからない場合はプレイヤー位置をベースに調整
            Vector3 fallbackPosition = centerPosition + Vector3.forward * minDistance;
            UnityEngine.AI.NavMeshHit fallbackHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(fallbackPosition, out fallbackHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }

            // 最終的にはプレイヤー位置を返す
            return centerPosition;
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
            }
            
            if (destructibleLayers == 0)
            {
            }
            
            if (attackRange <= 0)
            {
            }
        }
        
        #region Health System Implementation
        
        /// <summary>
        /// UI HPバーの初期化
        /// </summary>
        protected virtual void InitializeUIHealthBar()
        {
            // UIManagerを介してヘルスバーを作成
            if (UI.UIManager.Instance != null)
            {
                uiHealthBar = UI.UIManager.Instance.CreateHealthBarForCompanion(this);
            }
            else
            {
                // UIManagerが見つからない場合は少し待ってから再試行
                StartCoroutine(DelayedHealthBarInitialization());
            }
        }
        
        /// <summary>
        /// 遅延ヘルスバー初期化（UIManagerが後から初期化される場合）
        /// </summary>
        private System.Collections.IEnumerator DelayedHealthBarInitialization()
        {
            float timeout = 5f; // 5秒でタイムアウト
            float elapsed = 0f;
            
            while (elapsed < timeout && UI.UIManager.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (UI.UIManager.Instance != null)
            {
                uiHealthBar = UI.UIManager.Instance.CreateHealthBarForCompanion(this);
            }
            else
            {
            }
        }

        /// <summary>
        /// ヘルスバーを強制的に削除する（死亡時など）
        /// </summary>
        protected virtual void DestroyHealthBar()
        {
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.OnCompanionDestroyed(this);
            }
            uiHealthBar = null;
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
        
        /// <summary>
        /// ダメージエフェクト（マテリアルの_Damage_Amountを一瞬1にする）
        /// </summary>
        private System.Collections.IEnumerator DamageEffect()
        {
            if (companionRenderer != null && companionRenderer.material != null)
            {
                // _Damage_Amountを1に設定
                companionRenderer.material.SetFloat("_Damage_Amount", 1f);

                // 0.1秒待機
                yield return new WaitForSeconds(0.1f);

                // _Damage_Amountを0に戻す
                if (companionRenderer != null && companionRenderer.material != null)
                {
                    companionRenderer.material.SetFloat("_Damage_Amount", 0f);
                }
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


            currentHealth -= damage;
            currentHealth = Mathf.Max(0f, currentHealth);

            // ダメージ受領時のセリフを表示
            ShowCombatDialogue(CombatDialogueType.TakingDamage);
            
            // 体力が低い場合は別のセリフを表示
            if (HealthPercentage < 0.3f)
            {
                ShowCombatDialogue(CombatDialogueType.LowHealth);
            }

            // UIベースのHPバーは自動更新されます

            // ダメージテキストを表示
            ShowDamageText(damage);

            // ダメージエフェクトを開始
            StartCoroutine(DamageEffect());

            // 敵のターゲットを自分に変更させる（プレイヤーから攻撃された場合）
            if (player != null)
            {
                NotifyEnemiesOfAttack(transform.position, transform);
            }

            // 信頼度を少し下げる（味方に攻撃されたため）
            if (EnsureCompanionCharacter())
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
            TakeDamage(damage, ToolType.IronPipe); // デフォルトツール
        }

        /// <summary>
        /// 敵からの攻撃でダメージを受ける（ノックバック付き）
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="attackerPosition">攻撃者の位置</param>
        /// <param name="attackType">敵の攻撃タイプ</param>
        /// <param name="knockbackMultiplier">ノックバック乗算値</param>
        public virtual void TakeDamage(float damage, Vector3 attackerPosition, EnemyAttackType attackType, float knockbackMultiplier = 1.0f)
        {
            if (isDead) return;

            // 通常のダメージ処理
            TakeDamage(damage);

            // 生存している場合のみノックバック適用
            if (!isDead && knockbackSystem != null)
            {
                // ノックバック力を敵の攻撃乗算値で調整
                var originalSettings = (knockbackSystem.KnockbackForce, knockbackSystem.KnockbackDuration);
                knockbackSystem.SetKnockbackSettings(
                    knockbackSystem.KnockbackForce * knockbackMultiplier,
                    knockbackSystem.KnockbackDuration
                );

                // ノックバックを実行（isEnemyAttack=trueで武器乗算値を1.0固定）
                knockbackSystem.StartKnockback(attackerPosition, ToolType.IronPipe,
                    onKnockbackStart: OnKnockbackStart,
                    onKnockbackEnd: () => {
                        OnKnockbackEnd();
                        // 設定を元に戻す
                        knockbackSystem.SetKnockbackSettings(originalSettings.KnockbackForce, originalSettings.KnockbackDuration);
                    },
                    isEnemyAttack: true);
            }
        }

        /// <summary>
        /// 複合ダメージを受ける（物理ダメージ + 感染ダメージ）
        /// </summary>
        /// <param name="physicalDamage">物理ダメージ量</param>
        /// <param name="infectionDamage">感染ダメージ量</param>
        /// <param name="attackerPosition">攻撃者の位置</param>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="knockbackMultiplier">ノックバック乗算値</param>
        public virtual void TakeCombinedDamage(float physicalDamage, float infectionDamage, Vector3 attackerPosition, EnemyAttackType attackType, float knockbackMultiplier = 1.0f)
        {
            if (isDead) return;

            // 物理ダメージ処理
            if (physicalDamage > 0)
            {
                TakeDamage(physicalDamage, attackerPosition, attackType, knockbackMultiplier);
            }

            // 感染ダメージ処理
            if (infectionDamage > 0 && EnsureCompanionCharacter())
            {
                companionCharacter.TakeInfectionDamage(infectionDamage, attackType);
            }
        }

        /// <summary>
        /// 敵の攻撃タイプを疑似的にToolTypeに変換
        /// </summary>
        /// <param name="attackType">敵の攻撃タイプ</param>
        /// <returns>対応するToolType</returns>
        private ToolType ConvertEnemyAttackToToolType(EnemyAttackType attackType)
        {
            switch (attackType)
            {
                case EnemyAttackType.Slam:
                case EnemyAttackType.Tackle:
                case EnemyAttackType.Charge:
                    return ToolType.Pickaxe;  // 重い攻撃はつるはし扱い
                default:
                    return ToolType.IronPipe; // その他は鉄パイプ扱い
            }
        }
        
        /// <summary>
        /// ヒーリング処理
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }

        /// <summary>
        /// HPを全快にする（リトライ時のコンパニオン復活用）
        /// </summary>
        public virtual void RestoreFullHealth()
        {
            currentHealth = maxHealth;
            isDead = false;

            // NavMeshAgentを再有効化
            if (navAgent != null && !navAgent.enabled)
            {
                navAgent.enabled = true;
            }

            // オブジェクトを再アクティブ化
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            // ヘルスバーを再初期化
            if (uiHealthBar == null)
            {
                InitializeUIHealthBar();
            }

        }
        
        /// <summary>
        /// 死亡処理
        /// </summary>
        protected virtual void Die()
        {
            if (isDead) return;
            
            isDead = true;

            // 死亡時のセリフを表示
            ShowDeathDialogue();

            // NavMeshAgentを停止
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }

            // ヘルスバーを削除する
            DestroyHealthBar();

            // 死亡アニメーション再生
            if (animatorController != null)
            {
                animatorController.TriggerDeath();
            }
            else
            {
            }

            // コンパニオンキャラクターに死亡を通知
            if (EnsureCompanionCharacter())
            {
                // CompanionCharacterクラスの死亡処理を呼び出す（最大ダメージを与えて死亡状態にする）
                companionCharacter.Stats?.TakeDamage(companionCharacter.Stats.MaxHealth);
            }

            // 死亡セリフが表示される時間を考慮して少し遅らせて死亡シーケンス開始
            StartCoroutine(HandleDeathSequence());
        }


        /// <summary>
        /// 死亡シーケンス処理
        /// </summary>
        private System.Collections.IEnumerator HandleDeathSequence()
        {
            // 死亡セリフが表示される時間を確保（4秒間死亡状態を維持）
            yield return new WaitForSeconds(4f);
            
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
                }
            }
        }
        
        #endregion
        
        #region Dialogue System
        
        /// <summary>
        /// ステート変更時にセリフを表示
        /// </summary>
        /// <param name="newState">新しいステート</param>
        private void ShowStateDialogue(AIState newState)
        {
            if (!enableDialogueSystem || dialogueData == null)
                return;

            // 既に表示中の場合はスキップ
            if (hasActiveDialogue)
                return;

            // クールダウンチェック
            if (stateDialogueTimers.ContainsKey(newState))
            {
                if (Time.time - stateDialogueTimers[newState] < dialogueCooldown)
                    return;
            }

            string dialogue = dialogueData.GetStateDialogue(newState);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
                
                // タイマーを更新
                stateDialogueTimers[newState] = Time.time;
            }
        }

        /// <summary>
        /// コマンド受領時にセリフを表示
        /// </summary>
        /// <param name="command">受領したコマンド</param>
        private void ShowCommandDialogue(CompanionCommand command)
        {
            if (!enableDialogueSystem || dialogueData == null)
                return;

            // 既に表示中の場合はスキップ
            if (hasActiveDialogue)
                return;

            // クールダウンチェック
            if (commandDialogueTimers.ContainsKey(command))
            {
                if (Time.time - commandDialogueTimers[command] < dialogueCooldown)
                    return;
            }

            string dialogue = dialogueData.GetCommandDialogue(command);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
                
                // タイマーを更新
                commandDialogueTimers[command] = Time.time;
            }
        }

        /// <summary>
        /// 戦闘関連のセリフを表示
        /// </summary>
        /// <param name="type">戦闘状況タイプ</param>
        private void ShowCombatDialogue(CombatDialogueType type)
        {
            if (!enableDialogueSystem || dialogueData == null)
                return;

            // 既に表示中の場合はスキップ
            if (hasActiveDialogue)
                return;

            // クールダウンチェック（戦闘セリフは長めのクールダウン）
            if (combatDialogueTimers.ContainsKey(type))
            {
                if (Time.time - combatDialogueTimers[type] < combatDialogueCooldown)
                    return;
            }

            string dialogue = dialogueData.GetCombatDialogue(type);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
                
                // タイマーを更新
                combatDialogueTimers[type] = Time.time;
            }
        }

        /// <summary>
        /// 一般的なセリフを表示
        /// </summary>
        /// <param name="type">一般状況タイプ</param>
        public void ShowGeneralDialogue(GeneralDialogueType type)
        {
            if (!enableDialogueSystem || dialogueData == null)
                return;

            // 既に表示中の場合はスキップ
            if (hasActiveDialogue)
                return;

            // 一般セリフのクールダウンチェック
            if (Time.time - lastGeneralDialogueTime < dialogueCooldown)
                return;

            string dialogue = dialogueData.GetGeneralDialogue(type);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
                
                // タイマーを更新
                lastGeneralDialogueTime = Time.time;
            }
        }
        
        /// <summary>
        /// 頭上にDialogueTextを表示（一つのみ）
        /// </summary>
        /// <param name="dialogue">表示するセリフ</param>
        private void ShowDialogueTextAboveHead(string dialogue)
        {
            // 既存のDialogueTextがある場合は削除
            if (currentDialogueText != null)
            {
                var existingDialogue = currentDialogueText.GetComponent<DialogueText>();
                if (existingDialogue != null)
                {
                    existingDialogue.ForceDestroy();
                }
                currentDialogueText = null;
            }

            // UIManagerが利用可能かチェック
            if (UI.UIManager.Instance?.DialogueTextPrefab == null) return;

            // 新しいDialogueTextを作成（UIManagerが自動的にコールバックを管理）
            currentDialogueText = UI.UIManager.Instance.CreateDialogueTextForCompanion(this, dialogue);
        }
        
        /// <summary>
        /// DialogueTextが削除された際のコールバック
        /// </summary>
        public void OnDialogueTextDestroyed()
        {
            currentDialogueText = null;
        }
        
        /// <summary>
        /// 現在のDialogueTextの位置を取得（アンカーベース）
        /// </summary>
        public Vector3 GetDialoguePosition()
        {
            // dialogueAnchorが設定されている場合はそれを使用
            if (dialogueAnchor != null)
            {
                return dialogueAnchor.position;
            }
            
            // アンカーが設定されていない場合は従来の方式（コンパニオン位置 + オフセット）
            return transform.position + dialogueOffset;
        }
        
        /// <summary>
        /// 感染中のセリフを表示
        /// </summary>
        public void ShowInfectionDialogue(InfectionDialogueType dialogueType)
        {
            if (!enableDialogueSystem || dialogueData == null) return;

            string dialogue = dialogueData.GetInfectionDialogue(dialogueType);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
            }
        }

        /// <summary>
        /// テスト用：感染ダイアログを強制表示
        /// </summary>
        [ContextMenu("Test Infection Dialogue")]
        public void TestInfectionDialogue()
        {
            // テスト用のダイアログを表示
            ShowInfectionDialogue(InfectionDialogueType.InfectionPain);
        }

        /// <summary>
        /// テスト用：通常のダイアログを表示
        /// </summary>
        [ContextMenu("Test Normal Dialogue")]
        public void TestNormalDialogue()
        {
            // テスト用の通常ダイアログを表示
            if (dialogueData != null)
            {
                string dialogue = dialogueData.GetGeneralDialogue(GeneralDialogueType.Greeting);
                if (!string.IsNullOrEmpty(dialogue))
                {
                    ShowDialogueTextAboveHead(dialogue);
                }
            }
        }

        /// <summary>
        /// テスト用：感染状態を設定してダイアログをテスト
        /// </summary>
        [ContextMenu("Test Force Infection and Dialogue")]
        public void TestForceInfectionAndDialogue()
        {
            if (companionCharacter != null)
            {
                // 強制的に感染状態にする
                companionCharacter.Infection.SetInfectionForTesting(100f);

                // 感染ダイアログを表示
                ShowInfectionDialogue(InfectionDialogueType.JustInfected);
            }
        }

        /// <summary>
        /// テスト用：ダイアログデータの状態をチェック
        /// </summary>
        [ContextMenu("Debug Dialogue Data")]
        public void DebugDialogueData()
        {

            if (dialogueData != null)
            {
                // 各タイプのダイアログをテスト
                string testDialogue = dialogueData.GetInfectionDialogue(InfectionDialogueType.InfectionPain);

                testDialogue = dialogueData.GetGeneralDialogue(GeneralDialogueType.Greeting);
            }

            if (companionCharacter != null)
            {
            }
        }

        /// <summary>
        /// 感染中のダイアログ更新処理
        /// </summary>
        private void UpdateInfectionDialogue()
        {
            if (!enableDialogueSystem || dialogueData == null || companionCharacter == null) return;

            // 感染状態でない場合は処理しない
            if (!companionCharacter.Infection.IsInfected) return;

            float currentTime = Time.time;

            // クールダウン中は処理しない
            if (currentTime - lastInfectionDialogueTime < infectionDialogueCooldown)
            {
                return;
            }

            // ランダムで感染中のセリフを選択
            InfectionDialogueType[] randomDialogueTypes = {
                InfectionDialogueType.InfectionPain,
                InfectionDialogueType.FeelingSick,
                InfectionDialogueType.FearOfInfection,
                InfectionDialogueType.PleaForHelp,
                InfectionDialogueType.InfectionSpread,
                InfectionDialogueType.RecoveryHope
            };

            // ランダムでダイアログタイプを選択
            InfectionDialogueType selectedType = randomDialogueTypes[UnityEngine.Random.Range(0, randomDialogueTypes.Length)];

            // 同じダイアログタイプのクールダウンをチェック
            if (infectionDialogueTimers.TryGetValue(selectedType, out float lastTime))
            {
                if (currentTime - lastTime < infectionDialogueCooldown * 1.5f) // 個別ダイアログタイプは1.5倍のクールダウン
                    return;
            }

            // 確率でダイアログを表示（感染中は定期的だが頻繁すぎないように）
            if (UnityEngine.Random.Range(0f, 1f) < 0.4f) // 40%の確率に調整
            {
                ShowInfectionDialogue(selectedType);
                lastInfectionDialogueTime = currentTime;
                infectionDialogueTimers[selectedType] = currentTime;
            }
        }

        /// <summary>
        /// 死亡時のセリフを表示（クールダウン無視）
        /// </summary>
        private void ShowDeathDialogue()
        {
            if (!enableDialogueSystem || dialogueData == null)
                return;

            string dialogue = dialogueData.GetCombatDialogue(CombatDialogueType.Death);
            if (!string.IsNullOrEmpty(dialogue))
            {
                ShowDialogueTextAboveHead(dialogue);
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        /// <summary>
        /// オブジェクト破棄時のクリーンアップ
        /// </summary>
        private void OnDestroy()
        {
            // 感染イベントの購読解除
            if (companionCharacter != null && companionCharacter.Infection != null)
            {
                companionCharacter.Infection.OnInfectionDialogueTriggered -= HandleInfectionDialogue;
            }

            // ヘルスバーのクリーンアップ
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.OnCompanionDestroyed(this);
            }
        }
        
        #endregion
        
        #region Debug and Diagnostics


        #endregion

        #region Knockback System

        /// <summary>
        /// ノックバックシステムの初期化
        /// </summary>
        private void InitializeKnockbackSystem()
        {
            // Rigidbodyコンポーネントを取得または追加
            Rigidbody companionRigidbody = GetComponent<Rigidbody>();
            if (companionRigidbody == null)
            {
                companionRigidbody = gameObject.AddComponent<Rigidbody>();
                companionRigidbody.isKinematic = true;
                companionRigidbody.useGravity = false;
            }

            // Animatorを取得
            Animator companionAnimator = GetComponent<Animator>();

            // ノックバックシステムを初期化
            knockbackSystem.Initialize(this, companionRigidbody, companionAnimator);
        }

        /// <summary>
        /// ノックバックを開始（IKnockbackableインターフェース実装）
        /// </summary>
        /// <param name="attackerPosition">攻撃者の位置</param>
        /// <param name="toolType">使用された武器のタイプ</param>
        public void StartKnockback(Vector3 attackerPosition, ToolType toolType = ToolType.IronPipe)
        {
            knockbackSystem.StartKnockback(attackerPosition, toolType,
                onKnockbackStart: OnKnockbackStart,
                onKnockbackEnd: OnKnockbackEnd);
        }

        /// <summary>
        /// ノックバック中かどうか（IKnockbackableインターフェース実装）
        /// </summary>
        public bool IsKnockedBack => knockbackSystem.IsKnockedBack;

        /// <summary>
        /// ノックバック設定を変更（IKnockbackableインターフェース実装）
        /// </summary>
        /// <param name="force">ノックバック力</param>
        /// <param name="duration">ノックバック持続時間</param>
        public void SetKnockbackSettings(float force, float duration)
        {
            knockbackSystem.SetKnockbackSettings(force, duration);
        }

        /// <summary>
        /// ノックバック機能の有効/無効を切り替え（IKnockbackableインターフェース実装）
        /// </summary>
        /// <param name="enabled">有効かどうか</param>
        public void SetKnockbackEnabled(bool enabled)
        {
            knockbackSystem.SetKnockbackEnabled(enabled);
        }

        /// <summary>
        /// ノックバック開始時のコールバック
        /// </summary>
        private void OnKnockbackStart()
        {
            // NavMeshAgentを一時的に停止
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.enabled = false;
            }
        }

        /// <summary>
        /// ノックバック終了時のコールバック
        /// </summary>
        private void OnKnockbackEnd()
        {
            // Rigidbodyがkinematicに戻ってから少し遅れてNavMeshAgentを再有効化
            StartCoroutine(ReenableNavMeshAgentAfterKnockback());
        }

        private System.Collections.IEnumerator ReenableNavMeshAgentAfterKnockback()
        {
            // 物理演算が完全に終了するまで少し待機
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.1f);

            // NavMeshAgentを再有効化
            if (navAgent != null)
            {
                navAgent.enabled = true;

                // NavMesh上にワープして位置を修正（より安全に）
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                }
            }

            // 戦闘状態に復帰（元の状態に応じて）
            if (!isDead && currentState != AIState.Combat)
            {
                // 敵が近くにいる場合は戦闘状態に
                CheckForEnemies();
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
        Support,    // Level 4+
        Retreat,    // Level 5+
        Advanced    // Level 5+
    }

    // 意思決定システム関連の構造体
    [System.Serializable]
    public struct CompanionDecision
    {
        public string decisionType;
        public float priority;
        public float confidence;
        public string reasoning;
        public System.Action action;
        
        public CompanionDecision(string type, float priority, float confidence, string reasoning, System.Action action)
        {
            this.decisionType = type;
            this.priority = priority;
            this.confidence = confidence;
            this.reasoning = reasoning;
            this.action = action;
        }
    }
}