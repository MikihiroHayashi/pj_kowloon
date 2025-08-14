using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class InfectionStatus
    {
        [SerializeField] private InfectionLevel level = InfectionLevel.Clean;
        [SerializeField] private float infectionRate = 0f;
        [SerializeField] private float immunity = 1f;
        [SerializeField] private float timeToProgress = 0f;

        public InfectionLevel Level => level;
        public float InfectionRate => infectionRate;
        public float Immunity => immunity;
        public float TimeToProgress => timeToProgress;
        public bool IsTurned => level == InfectionLevel.Zombie;

        public event Action<InfectionLevel> OnInfectionLevelChanged;
        public event Action<float> OnInfectionRateChanged;
        public event Action OnTurnedZombie;

        public void UpdateInfection(float deltaTime, float environmentalRisk = 0f)
        {
            if (level == InfectionLevel.Zombie) return;

            float riskFactor = environmentalRisk * (1f - immunity);
            infectionRate += riskFactor * deltaTime;
            infectionRate = Mathf.Clamp01(infectionRate);

            OnInfectionRateChanged?.Invoke(infectionRate);

            if (timeToProgress > 0f)
            {
                timeToProgress -= deltaTime;
                if (timeToProgress <= 0f)
                {
                    ProgressInfection();
                }
            }
            else
            {
                CheckInfectionProgress();
            }
        }

        private void CheckInfectionProgress()
        {
            InfectionLevel newLevel = level;

            switch (level)
            {
                case InfectionLevel.Clean:
                    if (infectionRate >= 0.25f)
                    {
                        newLevel = InfectionLevel.Exposed;
                        timeToProgress = UnityEngine.Random.Range(300f, 600f);
                    }
                    break;
                case InfectionLevel.Exposed:
                    if (infectionRate >= 0.5f)
                    {
                        newLevel = InfectionLevel.Infected;
                        timeToProgress = UnityEngine.Random.Range(600f, 1200f);
                    }
                    break;
                case InfectionLevel.Infected:
                    if (infectionRate >= 0.75f)
                    {
                        newLevel = InfectionLevel.Turning;
                        timeToProgress = UnityEngine.Random.Range(120f, 300f);
                    }
                    break;
                case InfectionLevel.Turning:
                    if (infectionRate >= 1f)
                    {
                        newLevel = InfectionLevel.Zombie;
                        OnTurnedZombie?.Invoke();
                    }
                    break;
            }

            if (newLevel != level)
            {
                SetInfectionLevel(newLevel);
            }
        }

        private void ProgressInfection()
        {
            switch (level)
            {
                case InfectionLevel.Exposed:
                    SetInfectionLevel(InfectionLevel.Infected);
                    timeToProgress = UnityEngine.Random.Range(600f, 1200f);
                    break;
                case InfectionLevel.Infected:
                    SetInfectionLevel(InfectionLevel.Turning);
                    timeToProgress = UnityEngine.Random.Range(120f, 300f);
                    break;
                case InfectionLevel.Turning:
                    SetInfectionLevel(InfectionLevel.Zombie);
                    OnTurnedZombie?.Invoke();
                    break;
            }
        }

        public void SetInfectionLevel(InfectionLevel newLevel)
        {
            InfectionLevel previousLevel = level;
            level = newLevel;

            if (previousLevel != newLevel)
            {
                OnInfectionLevelChanged?.Invoke(level);
            }
        }

        public void TreatInfection(float treatmentEffectiveness)
        {
            if (level == InfectionLevel.Zombie) return;

            float reduction = treatmentEffectiveness * 0.1f;
            infectionRate = Mathf.Max(0f, infectionRate - reduction);
            
            if (infectionRate <= 0f)
            {
                SetInfectionLevel(InfectionLevel.Clean);
                timeToProgress = 0f;
            }
            else
            {
                timeToProgress += treatmentEffectiveness * 100f;
            }

            OnInfectionRateChanged?.Invoke(infectionRate);
        }

        public void IncreaseImmunity(float amount)
        {
            immunity = Mathf.Min(1f, immunity + amount);
        }

        public void DecreaseImmunity(float amount)
        {
            immunity = Mathf.Max(0f, immunity - amount);
        }

        public bool CanPerformAction()
        {
            return level != InfectionLevel.Zombie && level != InfectionLevel.Turning;
        }

        public float GetPerformancePenalty()
        {
            return level switch
            {
                InfectionLevel.Clean => 0f,
                InfectionLevel.Exposed => 0.05f,
                InfectionLevel.Infected => 0.15f,
                InfectionLevel.Turning => 0.4f,
                InfectionLevel.Zombie => 1f,
                _ => 0f
            };
        }
    }

}