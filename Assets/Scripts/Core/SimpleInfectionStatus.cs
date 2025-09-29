using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class SimpleInfectionStatus
    {
        [Header("Infection Settings")]
        [SerializeField] private float maxInfectionValue = 100f;
        [SerializeField] private float currentInfection = 0f;
        [SerializeField] private bool isInfected = false;

        [Header("Treatment Effects")]
        [SerializeField] private bool hasArmAmputated = false;
        [SerializeField] private bool canRun = true;
        [SerializeField] private bool canAttack = true;

        public float MaxInfectionValue => maxInfectionValue;
        public float CurrentInfection => currentInfection;
        public float InfectionPercentage => currentInfection / maxInfectionValue;
        public bool IsInfected => isInfected;
        public bool HasArmAmputated => hasArmAmputated;
        public bool CanRun => canRun;
        public bool CanAttack => canAttack;
        public bool CanMove => !isInfected;

        public event Action<float> OnInfectionChanged;
        public event Action OnBecameInfected;
        public event Action OnCured;
        public event Action OnArmAmputated;
        public event Action<InfectionDialogueType> OnInfectionDialogueTriggered;

        public void IncreaseInfection(float amount)
        {
            if (isInfected) return; // 既に感染済みの場合は増加しない

            float previousInfection = currentInfection;
            currentInfection = Mathf.Clamp(currentInfection + amount, 0f, maxInfectionValue);

            OnInfectionChanged?.Invoke(currentInfection);

            // 感染メーターが満タンになったら感染状態に
            if (!isInfected && currentInfection >= maxInfectionValue)
            {
                BecomeInfected();
            }
        }

        private void BecomeInfected()
        {
            isInfected = true;
            OnBecameInfected?.Invoke();
            OnInfectionDialogueTriggered?.Invoke(InfectionDialogueType.JustInfected);
            Debug.Log("Character became infected!");
        }

        public void CureWithVaccine()
        {
            if (!isInfected) return;

            OnInfectionDialogueTriggered?.Invoke(InfectionDialogueType.AfterVaccine);

            currentInfection = 0f;
            isInfected = false;
            OnInfectionChanged?.Invoke(currentInfection);
            OnCured?.Invoke();
            Debug.Log("Character cured with vaccine!");
        }

        public void CureWithAmputation()
        {
            if (!isInfected) return;

            OnInfectionDialogueTriggered?.Invoke(InfectionDialogueType.BeforeAmputation);

            currentInfection = 0f;
            isInfected = false;
            hasArmAmputated = true;
            canRun = false;
            canAttack = false;

            OnInfectionChanged?.Invoke(currentInfection);
            OnCured?.Invoke();
            OnArmAmputated?.Invoke();
            OnInfectionDialogueTriggered?.Invoke(InfectionDialogueType.AfterAmputation);
            Debug.Log("Character cured with arm amputation - can no longer run or attack!");
        }

        public void ResetInfection()
        {
            currentInfection = 0f;
            isInfected = false;
            hasArmAmputated = false;
            canRun = true;
            canAttack = true;
            OnInfectionChanged?.Invoke(currentInfection);
        }

        public bool CanPerformAction(string actionType)
        {
            switch (actionType.ToLower())
            {
                case "move":
                    return CanMove;
                case "run":
                    return canRun && CanMove;
                case "attack":
                    return canAttack && CanMove;
                case "crouch":
                case "dodge":
                    return CanMove;
                default:
                    return CanMove;
            }
        }

        // デバッグ用メソッド
        public void SetInfectionForTesting(float value)
        {
            currentInfection = Mathf.Clamp(value, 0f, maxInfectionValue);
            OnInfectionChanged?.Invoke(currentInfection);

            if (value >= maxInfectionValue && !isInfected)
            {
                BecomeInfected();
            }
            else if (value < maxInfectionValue && isInfected)
            {
                isInfected = false;
            }
        }
    }
}