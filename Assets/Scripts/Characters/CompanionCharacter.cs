using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;

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
        [SerializeField] private InfectionStatus infection;

        [Header("Skills")]
        [SerializeField] private List<Skill> skills = new List<Skill>();

        [Header("Behavior")]
        [SerializeField] private bool isAvailable = true;
        [SerializeField] private CompanionActivity currentActivity = CompanionActivity.Idle;
        [SerializeField] private float activityTimer = 0f;

        public string Name => characterName;
        public CharacterRole Role => role;
        public int TrustLevel => trustLevel;
        public string CharacterId => characterId;
        public CharacterStats Stats => stats;
        public HealthStatus Health => health;
        public InfectionStatus Infection => infection;
        public List<Skill> Skills => skills;
        public bool IsAvailable => isAvailable && infection.CanPerformAction();
        public CompanionActivity CurrentActivity => currentActivity;

        public event Action<int> OnTrustLevelChanged;
        public event Action<CompanionActivity> OnActivityChanged;
        public event Action<CompanionCharacter> OnCharacterDied;
        public event Action<CompanionCharacter> OnCharacterTurned;

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
                infection = new InfectionStatus();

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
            infection.OnTurnedZombie += HandleCharacterTurned;
        }

        private void UpdateCharacter()
        {
            float deltaTime = Time.deltaTime;
            
            health.UpdateCondition(deltaTime);
            infection.UpdateInfection(deltaTime);

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
            float infectionPenalty = infection.GetPerformancePenalty();
            
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
            if (!IsAvailable || health.IsCritical || infection.IsTurned)
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
                    break;
                case CompanionActivity.Training:
                    GainSkillExperience(GetPrimarySkill(), 10f);
                    break;
                case CompanionActivity.Socializing:
                    ChangeTrustLevel(1);
                    break;
            }

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
            float infectionPenalty = infection.GetPerformancePenalty();

            return baseEffectiveness * (1f - healthPenalty - infectionPenalty);
        }

        private void HandleCharacterDeath()
        {
            isAvailable = false;
            OnCharacterDied?.Invoke(this);
        }

        private void HandleCharacterTurned()
        {
            isAvailable = false;
            OnCharacterTurned?.Invoke(this);
        }

        public void SetCharacterData(string name, CharacterRole characterRole)
        {
            characterName = name;
            role = characterRole;
            InitializeSkills();
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.OnDeath -= HandleCharacterDeath;
            
            if (infection != null)
                infection.OnTurnedZombie -= HandleCharacterTurned;
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