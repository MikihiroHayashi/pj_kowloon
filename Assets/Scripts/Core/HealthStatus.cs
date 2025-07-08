using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class HealthStatus
    {
        [SerializeField] private HealthCondition condition = HealthCondition.Healthy;
        [SerializeField] private float severity = 0f;
        [SerializeField] private float duration = 0f;

        public HealthCondition Condition => condition;
        public float Severity => severity;
        public float Duration => duration;

        public event Action<HealthCondition> OnConditionChanged;
        public event Action<float> OnSeverityChanged;

        public void SetCondition(HealthCondition newCondition, float newSeverity = 0f, float newDuration = 0f)
        {
            HealthCondition previousCondition = condition;
            condition = newCondition;
            severity = Mathf.Clamp01(newSeverity);
            duration = Mathf.Max(0f, newDuration);

            if (previousCondition != newCondition)
            {
                OnConditionChanged?.Invoke(condition);
            }
            
            OnSeverityChanged?.Invoke(severity);
        }

        public void UpdateCondition(float deltaTime)
        {
            if (condition == HealthCondition.Healthy) return;

            if (duration > 0f)
            {
                duration -= deltaTime;
                if (duration <= 0f)
                {
                    SetCondition(HealthCondition.Healthy);
                }
            }
        }

        public void Heal(float healAmount)
        {
            if (healAmount <= 0f) return;

            severity = Mathf.Max(0f, severity - healAmount);
            OnSeverityChanged?.Invoke(severity);

            if (severity <= 0f)
            {
                SetCondition(HealthCondition.Healthy);
            }
        }

        public void Worsen(float damageAmount)
        {
            if (damageAmount <= 0f) return;

            severity = Mathf.Min(1f, severity + damageAmount);
            OnSeverityChanged?.Invoke(severity);

            if (severity >= 1f && condition != HealthCondition.Critical)
            {
                SetCondition(HealthCondition.Critical, severity);
            }
        }

        public int GetHealthPenalty()
        {
            return condition switch
            {
                HealthCondition.Healthy => 0,
                HealthCondition.Injured => Mathf.RoundToInt(severity * 20),
                HealthCondition.Sick => Mathf.RoundToInt(severity * 30),
                HealthCondition.Critical => Mathf.RoundToInt(severity * 50),
                _ => 0
            };
        }

        public float GetMovementPenalty()
        {
            return condition switch
            {
                HealthCondition.Healthy => 0f,
                HealthCondition.Injured => severity * 0.2f,
                HealthCondition.Sick => severity * 0.3f,
                HealthCondition.Critical => severity * 0.5f,
                _ => 0f
            };
        }
    }

    public enum HealthCondition
    {
        Healthy,
        Injured,
        Sick,
        Critical
    }
}