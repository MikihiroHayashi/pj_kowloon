using System;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Systems
{
    public abstract class PhaseSystem : MonoBehaviour
    {
        [Header("Phase System")]
        [SerializeField] protected bool isActive = false;

        public bool IsActive => isActive;

        public event Action OnSystemActivated;
        public event Action OnSystemDeactivated;

        protected virtual void Awake()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        protected virtual void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        public virtual void ActivateSystem()
        {
            if (!isActive)
            {
                isActive = true;
                OnSystemActivated?.Invoke();
                OnSystemActivatedInternal();
                Debug.Log($"{GetType().Name} activated");
            }
        }

        public virtual void DeactivateSystem()
        {
            if (isActive)
            {
                isActive = false;
                OnSystemDeactivated?.Invoke();
                OnSystemDeactivatedInternal();
                Debug.Log($"{GetType().Name} deactivated");
            }
        }

        protected abstract void OnSystemActivatedInternal();
        protected abstract void OnSystemDeactivatedInternal();
        protected abstract void HandlePhaseChanged(GamePhase newPhase);
    }
}