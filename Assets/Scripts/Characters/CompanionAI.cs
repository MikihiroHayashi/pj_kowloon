using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using KowloonBreak.Core;
using KowloonBreak.Player;

namespace KowloonBreak.Characters
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CompanionCharacter))]
    public class CompanionAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float maxFollowDistance = 10f;
        [SerializeField] private float baseUpdateRate = 0.2f;
        [SerializeField] private float stoppingDistance = 1.5f;
        
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
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform interactionPromptAnchor;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private AIState currentState = AIState.Follow;
        
        private NavMeshAgent navAgent;
        private CompanionCharacter companionCharacter;
        private CompanionAnimatorController animatorController;
        private GameObject currentTarget;
        private Vector3 lastPlayerPosition;
        private float lastUpdateTime;
        
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

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            companionCharacter = GetComponent<CompanionCharacter>();
            animatorController = GetComponent<CompanionAnimatorController>();
            
            if (animatorController == null)
            {
                animatorController = GetComponentInChildren<CompanionAnimatorController>();
            }
            
            InitializeAgent();
        }

        private void Start()
        {
            FindPlayer();
            SetState(AIState.Follow);
            StartCoroutine(UpdateAILoop());
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
            
            if (distanceToPlayer > maxFollowDistance)
            {
                Vector3 teleportPosition = player.position - player.forward * followDistance;
                if (NavMesh.SamplePosition(teleportPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    navAgent.Warp(hit.position);
                }
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

        private void HandleCombatState()
        {
            if (currentTarget == null)
            {
                SetState(AIState.Follow);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // Intelligence-based combat range adjustment
            float combatRange = GetCombatRange(intelligenceLevel);
            float retreatRange = GetRetreatRange(intelligenceLevel);
            
            if (distanceToTarget > retreatRange)
            {
                currentTarget = null;
                SetState(AIState.Follow);
                return;
            }
            
            // Advanced tactical positioning for higher intelligence levels
            if (intelligenceLevel >= 3)
            {
                HandleTacticalCombat(distanceToTarget, combatRange);
            }
            else
            {
                HandleBasicCombat(distanceToTarget, combatRange);
            }
        }

        private void HandleBasicCombat(float distanceToTarget, float combatRange)
        {
            if (distanceToTarget > combatRange)
            {
                MoveToPosition(currentTarget.transform.position);
            }
            else
            {
                navAgent.ResetPath();
                LookAtTarget(currentTarget.transform.position);
                PerformAttack();
            }
        }

        private void HandleTacticalCombat(float distanceToTarget, float combatRange)
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // ダッジロールの使用判定（知能レベル4以上でランダムに使用）
            if (intelligenceLevel >= 4 && 
                distanceToTarget < combatRange * 0.7f && 
                UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10%の確率
            {
                TryPerformDodgeRoll();
                return;
            }
            
            // Try to position tactically (flanking, maintaining distance)
            Vector3 tacticalPosition = CalculateTacticalPosition();
            
            if (distanceToTarget > combatRange)
            {
                MoveToPosition(tacticalPosition);
            }
            else if (distanceToTarget < combatRange * 0.5f && intelligenceLevel >= 4)
            {
                // Smart retreat when too close
                Vector3 retreatPosition = CalculateRetreatPosition();
                MoveToPosition(retreatPosition);
            }
            else
            {
                LookAtTarget(currentTarget.transform.position);
                PerformAttack();
            }
        }

        private Vector3 CalculateTacticalPosition()
        {
            if (currentTarget == null || player == null) return transform.position;
            
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // Level 5 AI: Coordinated flanking with other companions
            if (intelligenceLevel >= 5)
            {
                return CalculateCoordinatedPosition();
            }
            // Level 4 AI: Smart flanking
            else if (intelligenceLevel >= 4)
            {
                return CalculateFlankingPosition();
            }
            // Level 3 AI: Basic positioning
            else
            {
                return CalculateBasicTacticalPosition();
            }
        }

        private Vector3 CalculateCoordinatedPosition()
        {
            // Find other companions and coordinate positioning
            var otherCompanions = FindNearbyCompanions();
            Vector3 playerToEnemy = (currentTarget.transform.position - player.position).normalized;
            Vector3 flankDirection = Vector3.Cross(playerToEnemy, Vector3.up);
            
            // Coordinate with other companions to avoid clustering
            int companionCount = otherCompanions.Count + 1; // Include self
            int myIndex = GetCompanionIndex(otherCompanions);
            
            float angleStep = 360f / (companionCount + 1); // +1 for player space
            float myAngle = angleStep * (myIndex + 1);
            
            Vector3 coordinatedDirection = Quaternion.AngleAxis(myAngle, Vector3.up) * playerToEnemy;
            Vector3 tacticalPos = currentTarget.transform.position + coordinatedDirection * 4f;
            
            return tacticalPos;
        }

        private Vector3 CalculateFlankingPosition()
        {
            Vector3 playerToEnemy = (currentTarget.transform.position - player.position).normalized;
            Vector3 enemyToPlayer = -playerToEnemy;
            
            // Calculate optimal flanking angle (90 degrees from player-enemy line)
            Vector3 flankDirection = Vector3.Cross(playerToEnemy, Vector3.up);
            
            // Choose the side that provides better cover or tactical advantage
            Vector3 leftFlank = currentTarget.transform.position + flankDirection * 3.5f;
            Vector3 rightFlank = currentTarget.transform.position - flankDirection * 3.5f;
            
            // Choose the flank position that has better navigation path
            if (NavMesh.SamplePosition(leftFlank, out NavMeshHit leftHit, 2f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(rightFlank, out NavMeshHit rightHit, 2f, NavMesh.AllAreas))
            {
                // Choose based on current position to avoid unnecessary movement
                float leftDistance = Vector3.Distance(transform.position, leftHit.position);
                float rightDistance = Vector3.Distance(transform.position, rightHit.position);
                
                return leftDistance < rightDistance ? leftHit.position : rightHit.position;
            }
            else if (NavMesh.SamplePosition(leftFlank, out leftHit, 2f, NavMesh.AllAreas))
            {
                return leftHit.position;
            }
            else if (NavMesh.SamplePosition(rightFlank, out rightHit, 2f, NavMesh.AllAreas))
            {
                return rightHit.position;
            }
            
            return CalculateBasicTacticalPosition();
        }

        private Vector3 CalculateBasicTacticalPosition()
        {
            Vector3 playerToEnemy = (currentTarget.transform.position - player.position).normalized;
            Vector3 flankDirection = Vector3.Cross(playerToEnemy, Vector3.up);
            
            // Simple left or right flank
            if (Random.value > 0.5f) flankDirection = -flankDirection;
            
            Vector3 tacticalPos = currentTarget.transform.position + flankDirection * 3f;
            
            if (NavMesh.SamplePosition(tacticalPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return transform.position;
        }

        private List<CompanionAI> FindNearbyCompanions()
        {
            var companions = new List<CompanionAI>();
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 15f);
            
            foreach (var collider in nearbyColliders)
            {
                var companion = collider.GetComponent<CompanionAI>();
                if (companion != null && companion != this && companion.CurrentState == AIState.Combat)
                {
                    companions.Add(companion);
                }
            }
            
            return companions;
        }

        private int GetCompanionIndex(List<CompanionAI> companions)
        {
            for (int i = 0; i < companions.Count; i++)
            {
                if (Vector3.Distance(transform.position, companions[i].transform.position) > 
                    Vector3.Distance(transform.position, transform.position))
                {
                    return i;
                }
            }
            return companions.Count;
        }

        private Vector3 CalculateRetreatPosition()
        {
            if (currentTarget == null) return transform.position;
            
            Vector3 retreatDirection = (transform.position - currentTarget.transform.position).normalized;
            return transform.position + retreatDirection * 2f;
        }

        private float GetCombatRange(int intelligenceLevel)
        {
            return intelligenceLevel switch
            {
                1 => 1.5f,  // Very close combat only
                2 => 2f,    // Basic combat range
                3 => 2.5f,  // Improved combat range
                4 => 3f,    // Advanced combat range
                5 => 3.5f,  // Optimal combat range
                _ => 2f
            };
        }

        private float GetRetreatRange(int intelligenceLevel)
        {
            return intelligenceLevel switch
            {
                1 => detectionRange,         // Give up easily
                2 => detectionRange * 1.2f,  // Basic persistence
                3 => detectionRange * 1.4f,  // Good persistence
                4 => detectionRange * 1.6f,  // High persistence
                5 => detectionRange * 1.8f,  // Maximum persistence
                _ => detectionRange * 1.5f
            };
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
            if (intelligenceLevel < 2) return;

            float effectiveDetectionRange = GetEffectiveDetectionRange();
            float effectiveAngle = GetEffectiveDetectionAngle();
            
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, effectiveDetectionRange, enemyLayerMask);
            
            foreach (Collider enemy in enemiesInRange)
            {
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angle < effectiveAngle * 0.5f)
                {
                    if (HasLineOfSight(enemy.transform.position))
                    {
                        currentTarget = enemy.gameObject;
                        SetState(AIState.Combat);
                        break;
                    }
                }
            }
        }

        private float GetEffectiveDetectionRange()
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            return intelligenceLevel switch
            {
                1 => detectionRange * 0.5f,  // Very limited detection
                2 => detectionRange * 0.7f,  // Basic detection
                3 => detectionRange * 0.9f,  // Good detection
                4 => detectionRange * 1.1f,  // Enhanced detection
                5 => detectionRange * 1.3f,  // Superior detection
                _ => detectionRange
            };
        }

        private float GetEffectiveDetectionAngle()
        {
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            return intelligenceLevel switch
            {
                1 => detectionAngle * 0.6f,  // Narrow vision
                2 => detectionAngle * 0.8f,  // Limited vision
                3 => detectionAngle,         // Normal vision
                4 => detectionAngle * 1.2f,  // Wide vision
                5 => detectionAngle * 1.4f,  // Very wide vision
                _ => detectionAngle
            };
        }

        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 rayStart = transform.position + Vector3.up * 1.6f;
            Vector3 directionToTarget = (targetPosition - rayStart).normalized;
            float distanceToTarget = Vector3.Distance(rayStart, targetPosition);
            
            return !Physics.Raycast(rayStart, directionToTarget, distanceToTarget, obstacleLayerMask);
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
            
            if (currentTarget != null)
            {
                ExecuteAttackDamage();
                Debug.Log($"{gameObject.name} attacking {currentTarget.name}");
            }
        }

        public virtual void ExecuteAttackDamage()
        {
            if (currentTarget == null) return;

            // 攻撃アニメーションをトリガー
            if (animatorController != null)
            {
                animatorController.TriggerAttack();
            }

            var destructible = currentTarget.GetComponent<Environment.IDestructible>();
            if (destructible != null && destructible.CanBeDestroyedBy(Core.ToolType.IronPipe))
            {
                float damage = CalculateAttackDamage();
                destructible.TakeDamage(damage, Core.ToolType.IronPipe);
                
                // Trust level gain on successful attack
                companionCharacter.ChangeTrustLevel(1);
                
                Debug.Log($"{gameObject.name} dealt {damage} damage to {currentTarget.name}");
            }
        }

        private float CalculateAttackDamage()
        {
            float baseDamage = 15f; // Base companion attack damage
            float intelligenceMultiplier = 1f + (GetIntelligenceLevelFromTrust() - 1) * 0.2f;
            
            // Role-based damage modifiers
            float roleMultiplier = companionCharacter.Role switch
            {
                CharacterRole.Fighter => 1.3f,
                CharacterRole.Scout => 0.9f,
                CharacterRole.Medic => 0.7f,
                CharacterRole.Engineer => 1.0f,
                CharacterRole.Negotiator => 0.8f,
                _ => 1.0f
            };

            return baseDamage * intelligenceMultiplier * roleMultiplier;
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

            // NavMeshAgentの速度から移動状態を判定
            float velocity = navAgent.velocity.magnitude;
            CompanionMovementState targetMovementState;
            
            if (velocity < 0.1f)
            {
                targetMovementState = CompanionMovementState.Idle;
                isRunning = false;
            }
            else
            {
                targetMovementState = currentState == AIState.Combat ? CompanionMovementState.Combat : CompanionMovementState.Moving;
                
                // 知能レベルに応じて移動速度を調整
                DetermineMovementBehavior();
                
                // NavMeshAgentの速度を設定
                ApplyMovementSpeed();
            }

            // アニメーションステートが変化した場合のみ更新
            if (currentMovementState != targetMovementState || 
                animatorController.IsCrouching != isCrouching)
            {
                currentMovementState = targetMovementState;
                animatorController.SetMovementState(currentMovementState, isRunning, isCrouching);
            }
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
        /// ダッジロールを実行（知能レベルに応じて）
        /// </summary>
        public void TryPerformDodgeRoll()
        {
            if (!canDodge || animatorController == null) return;
            
            int intelligenceLevel = GetIntelligenceLevelFromTrust();
            
            // 知能レベル3以上でダッジロール可能
            if (intelligenceLevel < 3) return;
            
            // ダッジロール中でない場合のみ実行
            if (!animatorController.IsDodging)
            {
                animatorController.TriggerDodge();
                
                // ダッジロール方向を計算（敵から離れる方向）
                Vector3 dodgeDirection = Vector3.zero;
                
                if (currentTarget != null)
                {
                    dodgeDirection = (transform.position - currentTarget.transform.position).normalized;
                }
                else if (player != null)
                {
                    // ターゲットがない場合はプレイヤーの側方にダッジ
                    dodgeDirection = Vector3.Cross(player.forward, Vector3.up).normalized;
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                        dodgeDirection = -dodgeDirection;
                }
                
                // ダッジロール先の位置を計算
                Vector3 dodgeTarget = transform.position + dodgeDirection * 3f;
                
                if (UnityEngine.AI.NavMesh.SamplePosition(dodgeTarget, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    navAgent.SetDestination(hit.position);
                }
            }
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
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
            
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, player.position);
            }
            
            Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftBoundary);
            Gizmos.DrawRay(transform.position, rightBoundary);
        }

        private void OnValidate()
        {
            if (navAgent != null)
            {
                navAgent.stoppingDistance = stoppingDistance;
            }
        }
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