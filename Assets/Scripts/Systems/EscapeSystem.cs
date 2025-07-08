using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Systems
{
    public class EscapeSystem : PhaseSystem
    {
        [Header("Escape Settings")]
        [SerializeField] private float escapeProgress = 0f;
        [SerializeField] private float maxEscapeProgress = 100f;
        [SerializeField] private float progressRate = 1f;

        private bool escapeInProgress = false;

        protected override void OnSystemActivatedInternal()
        {
            StartEscapeMode();
        }

        protected override void OnSystemDeactivatedInternal()
        {
            StopEscapeMode();
        }

        protected override void HandlePhaseChanged(GamePhase newPhase)
        {
            switch (newPhase)
            {
                case GamePhase.ESCAPE:
                    ActivateSystem();
                    break;
                default:
                    DeactivateSystem();
                    break;
            }
        }

        private void Update()
        {
            if (isActive && escapeInProgress)
            {
                UpdateEscape();
            }
        }

        private void StartEscapeMode()
        {
            escapeProgress = 0f;
            escapeInProgress = true;
            Debug.Log("Escape System: Started - Find a way out of Kowloon!");
        }

        private void StopEscapeMode()
        {
            escapeInProgress = false;
            Debug.Log("Escape System: Stopped");
        }

        private void UpdateEscape()
        {
            escapeProgress += progressRate * Time.deltaTime;
            
            if (escapeProgress >= maxEscapeProgress)
            {
                CompleteEscape();
            }
        }

        private void CompleteEscape()
        {
            escapeInProgress = false;
            escapeProgress = maxEscapeProgress;
            Debug.Log("Escape successful! You've made it out of Kowloon!");
        }

        public void InitiateEscapeRoute()
        {
            if (isActive)
            {
                Debug.Log("Initiating escape route...");
                escapeInProgress = true;
            }
        }

        public void NegotiateWithFaction()
        {
            if (isActive)
            {
                Debug.Log("Negotiating with faction for safe passage...");
                escapeProgress += 10f;
            }
        }

        public void OvercomeObstacle()
        {
            if (isActive)
            {
                Debug.Log("Overcoming escape obstacle...");
                escapeProgress += 15f;
            }
        }

        public float GetEscapeProgress()
        {
            return isActive ? (escapeProgress / maxEscapeProgress) * 100f : 0f;
        }
    }
}