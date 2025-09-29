using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Characters
{
    [Serializable]
    public class CompanionCharacter : MonoBehaviour
    {
        [Header("Character Info")]
        [SerializeField] private string characterName;
        [SerializeField] private CharacterRole role;
        [SerializeField] private int trustLevel = 50;
        [SerializeField] private string characterId;

        [Header("Stats")]
        [SerializeField] private CharacterStats stats;
        [SerializeField] private HealthStatus health;
        [SerializeField] private SimpleInfectionStatus infection;

        [Header("Skills")]
        [SerializeField] private List<Skill> skills = new List<Skill>();

        [Header("Behavior")]
        [SerializeField] private bool isAvailable = true;
        [SerializeField] private CompanionActivity currentActivity = CompanionActivity.Idle;
        [SerializeField] private float activityTimer = 0f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Resource Effects")]
        [SerializeField] private float resourceEfficiencyBonus = 1.0f;
        [SerializeField] private float dailyResourceConsumption = 1.0f;
        [SerializeField] private float resourceProductionRate = 0.0f;

        public string Name => characterName;
        public CharacterRole Role => role;
        public int TrustLevel => trustLevel;
        public string CharacterId => characterId;
        public CharacterStats Stats => stats;
        public HealthStatus Health => health;
        public SimpleInfectionStatus Infection => infection;
        public List<Skill> Skills => skills;
        public bool IsAvailable => isAvailable && infection.CanMove;
        public CompanionActivity CurrentActivity => currentActivity;

        public event Action<int> OnTrustLevelChanged;
        public event Action<CompanionActivity> OnActivityChanged;
        public event Action<CompanionCharacter> OnCharacterDied;
        public event Action<CompanionCharacter> OnCharacterTurned;
        public event Action<ResourceType, int> OnResourceProduced;
        public event Action<ResourceType, int> OnResourceConsumed;

        private void Awake()
        {
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = Guid.NewGuid().ToString();
            }

            InitializeCharacter();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdateCharacter();
        }

        private void InitializeCharacter()
        {
            if (stats == null)
                stats = new CharacterStats();

            if (health == null)
                health = new HealthStatus();

            if (infection == null)
                infection = new SimpleInfectionStatus();

            if (animator == null)
                animator = GetComponent<Animator>();

            InitializeSkills();
        }

        private void InitializeSkills()
        {
            if (skills.Count == 0)
            {
                switch (role)
                {
                    case CharacterRole.Fighter:
                        skills.Add(new Skill("Combat", SkillType.Combat, "戦闘能力"));
                        skills.Add(new Skill("Survival", SkillType.Survival, "生存能力"));
                        break;
                    case CharacterRole.Scout:
                        skills.Add(new Skill("Stealth", SkillType.Stealth, "隠密行動"));
                        skills.Add(new Skill("Survival", SkillType.Survival, "生存能力"));
                        break;
                    case CharacterRole.Medic:
                        skills.Add(new Skill("Medical", SkillType.Medical, "医療技術"));
                        skills.Add(new Skill("Crafting", SkillType.Crafting, "薬品製造"));
                        break;
                    case CharacterRole.Engineer:
                        skills.Add(new Skill("Engineering", SkillType.Engineering, "建設・修理"));
                        skills.Add(new Skill("Crafting", SkillType.Crafting, "道具製造"));
                        break;
                    case CharacterRole.Negotiator:
                        skills.Add(new Skill("Negotiation", SkillType.Negotiation, "交渉術"));
                        skills.Add(new Skill("Leadership", SkillType.Leadership, "指導力"));
                        break;
                }
            }
        }

        private void SubscribeToEvents()
        {
            stats.OnDeath += HandleCharacterDeath;
            infection.OnBecameInfected += HandleCharacterTurned;
            infection.OnInfectionDialogueTriggered += HandleInfectionDialogue;
        }

        private void UpdateCharacter()
        {
            float deltaTime = Time.deltaTime;
            
            health.UpdateCondition(deltaTime);

            if (activityTimer > 0f)
            {
                activityTimer -= deltaTime;
                if (activityTimer <= 0f)
                {
                    CompleteActivity();
                }
            }

            ApplyHealthPenalties();
        }

        private void ApplyHealthPenalties()
        {
            int healthPenalty = health.GetHealthPenalty();

            if (healthPenalty > 0)
            {
                stats.Health = stats.MaxHealth - healthPenalty;
            }
        }

        public void ChangeTrustLevel(int amount)
        {
            int previousTrust = trustLevel;
            trustLevel = Mathf.Clamp(trustLevel + amount, 0, 100);
            
            if (previousTrust != trustLevel)
            {
                OnTrustLevelChanged?.Invoke(trustLevel);
                HandleTrustLevelChange(previousTrust, trustLevel);
            }
        }
        
        private void HandleTrustLevelChange(int previous, int current)
        {
            // 信頼度レベルに基づく行動変化
            if (current >= 80 && previous < 80)
            {
                // 高信頼度: より積極的な行動
                Debug.Log($"{characterName} now has high trust - becoming more proactive");
            }
            else if (current <= 20 && previous > 20)
            {
                // 低信頼度: 消極的な行動
                Debug.Log($"{characterName} trust is low - becoming more cautious");
            }
        }
        
        public bool AttemptNegotiation(string requestType, int difficulty)
        {
            var negotiationSkill = GetSkill(SkillType.Negotiation);
            if (negotiationSkill == null)
            {
                Debug.LogWarning($"{characterName} has no negotiation skill");
                return false;
            }
            
            // 交渉成功率の計算
            float baseSuccessRate = negotiationSkill.Level * 0.1f; // スキルレベル * 10%
            float trustModifier = (trustLevel - 50) * 0.01f; // 信頼度による修正
            float difficultyModifier = -difficulty * 0.1f; // 難易度による修正
            
            float successRate = Mathf.Clamp01(baseSuccessRate + trustModifier + difficultyModifier);
            
            bool success = UnityEngine.Random.value < successRate;
            
            if (success)
            {
                Debug.Log($"{characterName} negotiation successful! ({successRate:P0} chance)");
                // 成功時の信頼度上昇
                ChangeTrustLevel(UnityEngine.Random.Range(1, 4));
                
                // スキル経験値獲得
                negotiationSkill.GainExperience(difficulty * 10);
            }
            else
            {
                Debug.Log($"{characterName} negotiation failed. ({successRate:P0} chance)");
                // 失敗時の軽微な信頼度低下
                ChangeTrustLevel(-1);
            }
            
            return success;
        }
        
        public bool CanPerformAction(string actionType)
        {
            // 基本的な行動可能性チェック
            if (!IsAvailable || health.IsCritical || infection.IsInfected)
                return false;
            
            // 信頼度による行動制限
            switch (actionType.ToLower())
            {
                case "combat":
                    return trustLevel >= 30;
                case "explore":
                    return trustLevel >= 40;
                case "negotiate":
                    return trustLevel >= 50;
                case "leadership":
                    return trustLevel >= 70;
                default:
                    return trustLevel >= 20;
            }
        }

        public void AssignActivity(CompanionActivity activity, float duration = 0f)
        {
            if (!IsAvailable) return;

            currentActivity = activity;
            activityTimer = duration;
            isAvailable = false;
            
            OnActivityChanged?.Invoke(currentActivity);
        }

        private void CompleteActivity()
        {
            switch (currentActivity)
            {
                case CompanionActivity.Resting:
                    stats.RestoreStamina(20);
                    health.Heal(0.1f);

                    // デバッグログ（感染状態でもスタミナ回復することを確認）
                    if (infection.IsInfected)
                    {
                        Debug.Log($"[CompanionCharacter] {characterName} resting while infected - Stamina restored (Infection: {infection.InfectionPercentage:P0})");
                    }
                    break;
                case CompanionActivity.Training:
                    GainSkillExperience(GetPrimarySkill(), 10f);
                    break;
                case CompanionActivity.Socializing:
                    ChangeTrustLevel(1);
                    break;
                case CompanionActivity.Working:
                    ApplyRoleBasedResourceEffects();
                    break;
                case CompanionActivity.Crafting:
                    ApplyCraftingEffects();
                    break;
                case CompanionActivity.Healing:
                    ApplyHealingEffects();
                    break;
            }

            // 日次リソース消費
            ConsumeResourcesForMaintenance();

            currentActivity = CompanionActivity.Idle;
            isAvailable = true;
            OnActivityChanged?.Invoke(currentActivity);
        }

        private Skill GetPrimarySkill()
        {
            if (skills.Count == 0) return null;
            
            return role switch
            {
                CharacterRole.Fighter => skills.Find(s => s.SkillType == SkillType.Combat),
                CharacterRole.Scout => skills.Find(s => s.SkillType == SkillType.Stealth),
                CharacterRole.Medic => skills.Find(s => s.SkillType == SkillType.Medical),
                CharacterRole.Engineer => skills.Find(s => s.SkillType == SkillType.Engineering),
                CharacterRole.Negotiator => skills.Find(s => s.SkillType == SkillType.Negotiation),
                _ => skills[0]
            };
        }

        public void GainSkillExperience(Skill skill, float amount)
        {
            if (skill == null) return;
            skill.GainExperience(amount);
        }

        public Skill GetSkill(SkillType skillType)
        {
            return skills.Find(s => s.SkillType == skillType);
        }

        public bool HasSkill(SkillType skillType)
        {
            return GetSkill(skillType) != null;
        }

        public float GetSkillEffectiveness(SkillType skillType)
        {
            Skill skill = GetSkill(skillType);
            if (skill == null) return 0f;

            float baseEffectiveness = skill.GetEffectiveness();
            float healthPenalty = health.GetMovementPenalty();

            // 感染状態や腕切断状態による能力低下
            float infectionPenalty = infection.IsInfected ? 1f : 0f; // 感染状態なら完全に行動不能
            float amputationPenalty = infection.HasArmAmputated ? 0.3f : 0f; // 腕切断で30%低下

            return baseEffectiveness * (1f - healthPenalty - infectionPenalty - amputationPenalty);
        }

        private void HandleCharacterDeath()
        {
            isAvailable = false;
            OnCharacterDied?.Invoke(this);
        }

        private void HandleCharacterTurned()
        {
            isAvailable = false;

            // 感染状態のAnimationトリガーを発動
            if (animator != null)
            {
                animator.SetTrigger("Infection");
                Debug.Log($"{characterName} became infected - playing infection animation");
            }

            OnCharacterTurned?.Invoke(this);
        }

        private void HandleInfectionDialogue(InfectionDialogueType dialogueType)
        {
            // CompanionAIからダイアログ表示メソッドを呼び出す
            var companionAI = GetComponent<CompanionAI>();
            if (companionAI != null)
            {
                companionAI.ShowInfectionDialogue(dialogueType);
            }
        }

        /// <summary>
        /// 感染ダメージを受ける（敵の攻撃による感染）
        /// </summary>
        /// <param name="infectionDamage">感染ダメージ量</param>
        /// <param name="attackType">攻撃タイプ（感染ダメージ計算用）</param>
        public void TakeInfectionDamage(float infectionDamage, Core.EnemyAttackType attackType = Core.EnemyAttackType.Punch)
        {
            if (infectionDamage <= 0 || infection.IsInfected) return;

            infection.IncreaseInfection(infectionDamage);
            Debug.Log($"{characterName} took {infectionDamage} infection damage from {attackType} attack. Current infection: {infection.InfectionPercentage:P0}");

            // 感染バーが存在しない場合は作成
            if (UI.UIManager.Instance != null && !UI.UIManager.Instance.HasInfectionBarForCompanion(GetComponent<CompanionAI>()))
            {
                UI.UIManager.Instance.CreateInfectionBarForCompanion(GetComponent<CompanionAI>());
            }

            // UIに表示
            if (UI.UIManager.Instance != null)
            {
                UI.UIManager.Instance.ShowDamageText(transform.position, infectionDamage, UI.DamageType.Infection);
            }
        }

        public void SetCharacterData(string name, CharacterRole characterRole)
        {
            characterName = name;
            role = characterRole;
            InitializeSkills();
        }

        #region Resource Effects

        /// <summary>
        /// 役割に応じたリソース効果を適用
        /// </summary>
        private void ApplyRoleBasedResourceEffects()
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) return;

            float efficiencyMultiplier = GetRoleEfficiencyMultiplier();

            switch (role)
            {
                case CharacterRole.Fighter:
                    // 防衛力向上・材料消費で武器メンテナンス
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 2))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 2);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 2);
                        Debug.Log($"{characterName} (Fighter) used materials for weapon maintenance");
                    }
                    break;

                case CharacterRole.Scout:
                    // 探索により情報・素材発見
                    int materialsFound = Mathf.RoundToInt(2 * efficiencyMultiplier);
                    resourceManager.AddResources(ResourceType.Materials, materialsFound);
                    resourceManager.AddResources(ResourceType.Information, 1);
                    OnResourceProduced?.Invoke(ResourceType.Materials, materialsFound);
                    OnResourceProduced?.Invoke(ResourceType.Information, 1);
                    Debug.Log($"{characterName} (Scout) found {materialsFound} materials and 1 information");
                    break;

                case CharacterRole.Medic:
                    // 薬品製造・チーム回復
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 3))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 3);
                        int medicineProduced = Mathf.RoundToInt(2 * efficiencyMultiplier);
                        resourceManager.AddResources(ResourceType.Medicine, medicineProduced);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 3);
                        OnResourceProduced?.Invoke(ResourceType.Medicine, medicineProduced);
                        Debug.Log($"{characterName} (Medic) produced {medicineProduced} medicine");
                    }
                    break;

                case CharacterRole.Engineer:
                    // 設備修理・改良
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 4))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 4);
                        int toolsProduced = Mathf.RoundToInt(1 * efficiencyMultiplier);
                        resourceManager.AddResources(ResourceType.Tools, toolsProduced);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 4);
                        OnResourceProduced?.Invoke(ResourceType.Tools, toolsProduced);
                        Debug.Log($"{characterName} (Engineer) produced {toolsProduced} tools");
                    }
                    break;

                case CharacterRole.Negotiator:
                    // 交渉による外部リソース確保
                    if (AttemptNegotiation("resource_trade", 3))
                    {
                        int foodGained = Mathf.RoundToInt(3 * efficiencyMultiplier);
                        int waterGained = Mathf.RoundToInt(2 * efficiencyMultiplier);
                        resourceManager.AddResources(ResourceType.Food, foodGained);
                        resourceManager.AddResources(ResourceType.Water, waterGained);
                        OnResourceProduced?.Invoke(ResourceType.Food, foodGained);
                        OnResourceProduced?.Invoke(ResourceType.Water, waterGained);
                        Debug.Log($"{characterName} (Negotiator) negotiated for {foodGained} food and {waterGained} water");
                    }
                    break;
            }
        }

        /// <summary>
        /// クラフト効果を適用
        /// </summary>
        private void ApplyCraftingEffects()
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) return;

            // 役割に応じたクラフト効率
            switch (role)
            {
                case CharacterRole.Medic:
                    // 薬品クラフト
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 2))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 2);
                        resourceManager.AddResources(ResourceType.Medicine, 3);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 2);
                        OnResourceProduced?.Invoke(ResourceType.Medicine, 3);
                    }
                    break;

                case CharacterRole.Engineer:
                    // ツールクラフト
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 3))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 3);
                        resourceManager.AddResources(ResourceType.Tools, 2);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 3);
                        OnResourceProduced?.Invoke(ResourceType.Tools, 2);
                    }
                    break;

                default:
                    // 基本クラフト
                    if (resourceManager.HasEnoughResources(ResourceType.Materials, 4))
                    {
                        resourceManager.ConsumeResources(ResourceType.Materials, 4);
                        resourceManager.AddResources(ResourceType.Tools, 1);
                        OnResourceConsumed?.Invoke(ResourceType.Materials, 4);
                        OnResourceProduced?.Invoke(ResourceType.Tools, 1);
                    }
                    break;
            }
        }

        /// <summary>
        /// 治療効果を適用
        /// </summary>
        private void ApplyHealingEffects()
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) return;

            if (role == CharacterRole.Medic)
            {
                // メディック: 効率的な治療
                if (resourceManager.HasEnoughResources(ResourceType.Medicine, 1))
                {
                    resourceManager.ConsumeResources(ResourceType.Medicine, 1);
                    OnResourceConsumed?.Invoke(ResourceType.Medicine, 1);
                    // チーム全体の健康回復効果
                    Debug.Log($"{characterName} (Medic) provided efficient healing to the team");
                }
            }
            else
            {
                // 他の役割: 基本的な治療
                if (resourceManager.HasEnoughResources(ResourceType.Medicine, 2))
                {
                    resourceManager.ConsumeResources(ResourceType.Medicine, 2);
                    OnResourceConsumed?.Invoke(ResourceType.Medicine, 2);
                    Debug.Log($"{characterName} provided basic healing");
                }
            }
        }

        /// <summary>
        /// 日次メンテナンスリソース消費
        /// </summary>
        private void ConsumeResourcesForMaintenance()
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) return;

            // 基本生存需要
            int foodConsumption = Mathf.RoundToInt(dailyResourceConsumption);
            int waterConsumption = Mathf.RoundToInt(dailyResourceConsumption);

            if (resourceManager.HasEnoughResources(ResourceType.Food, foodConsumption))
            {
                resourceManager.ConsumeResources(ResourceType.Food, foodConsumption);
                OnResourceConsumed?.Invoke(ResourceType.Food, foodConsumption);
            }
            else
            {
                // 食料不足時の健康ペナルティ
                health.Worsen(0.1f);
                if (health.Condition == Core.HealthCondition.Healthy)
                {
                    health.SetCondition(Core.HealthCondition.Sick, 0.1f);
                }
                Debug.LogWarning($"{characterName} is suffering from lack of food");
            }

            if (resourceManager.HasEnoughResources(ResourceType.Water, waterConsumption))
            {
                resourceManager.ConsumeResources(ResourceType.Water, waterConsumption);
                OnResourceConsumed?.Invoke(ResourceType.Water, waterConsumption);
            }
            else
            {
                // 水不足時の健康ペナルティ
                health.Worsen(0.15f);
                if (health.Condition == Core.HealthCondition.Healthy)
                {
                    health.SetCondition(Core.HealthCondition.Sick, 0.15f);
                }
                Debug.LogWarning($"{characterName} is suffering from dehydration");
            }
        }

        /// <summary>
        /// 役割に基づく効率倍率を取得
        /// </summary>
        private float GetRoleEfficiencyMultiplier()
        {
            float baseEfficiency = resourceEfficiencyBonus;
            float trustBonus = (trustLevel - 50) * 0.01f; // 信頼度50を基準とした効率ボーナス
            float healthPenalty = health.GetMovementPenalty(); // 健康状態による効率ペナルティ

            return Mathf.Max(0.1f, baseEfficiency + trustBonus - healthPenalty);
        }

        /// <summary>
        /// 特定リソースの生産能力を取得
        /// </summary>
        public float GetResourceProductionRate(ResourceType resourceType)
        {
            switch (role)
            {
                case CharacterRole.Scout:
                    return resourceType == ResourceType.Materials || resourceType == ResourceType.Information
                        ? resourceProductionRate * GetRoleEfficiencyMultiplier() : 0f;
                case CharacterRole.Medic:
                    return resourceType == ResourceType.Medicine
                        ? resourceProductionRate * GetRoleEfficiencyMultiplier() : 0f;
                case CharacterRole.Engineer:
                    return resourceType == ResourceType.Tools
                        ? resourceProductionRate * GetRoleEfficiencyMultiplier() : 0f;
                case CharacterRole.Negotiator:
                    return resourceType == ResourceType.Food || resourceType == ResourceType.Water
                        ? resourceProductionRate * GetRoleEfficiencyMultiplier() : 0f;
                default:
                    return 0f;
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (stats != null)
                stats.OnDeath -= HandleCharacterDeath;

            if (infection != null)
            {
                infection.OnBecameInfected -= HandleCharacterTurned;
                infection.OnInfectionDialogueTriggered -= HandleInfectionDialogue;
            }
        }
    }

    public enum CharacterRole
    {
        Fighter,
        Scout,
        Medic,
        Engineer,
        Negotiator
    }

    public enum CompanionActivity
    {
        Idle,
        Resting,
        Training,
        Working,
        Socializing,
        Patrol,
        Crafting,
        Healing
    }
}