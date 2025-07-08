using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class Skill
    {
        [SerializeField] private string skillName;
        [SerializeField] private SkillType skillType;
        [SerializeField] private int level = 1;
        [SerializeField] private int maxLevel = 10;
        [SerializeField] private float experience = 0f;
        [SerializeField] private float experienceToNextLevel = 100f;
        [SerializeField] private string description;

        public string SkillName => skillName;
        public SkillType SkillType => skillType;
        public int Level => level;
        public int MaxLevel => maxLevel;
        public float Experience => experience;
        public float ExperienceToNextLevel => experienceToNextLevel;
        public string Description => description;

        public event Action<int> OnLevelUp;
        public event Action<float> OnExperienceGained;

        public Skill(string name, SkillType type, string desc = "")
        {
            skillName = name;
            skillType = type;
            description = desc;
        }

        public void GainExperience(float amount)
        {
            if (level >= maxLevel) return;

            experience += amount;
            OnExperienceGained?.Invoke(amount);

            while (experience >= experienceToNextLevel && level < maxLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            experience -= experienceToNextLevel;
            level++;
            experienceToNextLevel = CalculateExperienceToNextLevel();
            
            OnLevelUp?.Invoke(level);
        }

        private float CalculateExperienceToNextLevel()
        {
            return 100f + (level * 50f);
        }

        public float GetEffectiveness()
        {
            return 1f + (level - 1) * 0.1f;
        }

        public bool CanUse()
        {
            return level > 0;
        }
    }

    public enum SkillType
    {
        Combat,
        Medical,
        Engineering,
        Negotiation,
        Stealth,
        Crafting,
        Leadership,
        Survival
    }
}