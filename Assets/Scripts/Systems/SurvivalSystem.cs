using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Systems
{
    public class SurvivalSystem : PhaseSystem
    {
        [Header("Survival Settings")]
        [SerializeField] private float resourceDecayRate = 1f;
        [SerializeField] private float explorationSpeed = 1f;

        protected override void OnSystemActivatedInternal()
        {
            StartSurvivalMode();
        }

        protected override void OnSystemDeactivatedInternal()
        {
            StopSurvivalMode();
        }

        protected override void HandlePhaseChanged(GamePhase newPhase)
        {
            switch (newPhase)
            {
                case GamePhase.SURVIVAL:
                    ActivateSystem();
                    break;
                default:
                    DeactivateSystem();
                    break;
            }
        }

        private void Update()
        {
            if (isActive)
            {
                UpdateSurvival();
            }
        }

        private void StartSurvivalMode()
        {
            Debug.Log("Survival System: Started");
        }

        private void StopSurvivalMode()
        {
            Debug.Log("Survival System: Stopped");
        }

        private void UpdateSurvival()
        {
        }

        public void StartExploration()
        {
            if (isActive)
            {
                Debug.Log("Starting exploration...");
            }
        }

        public void ManageResources()
        {
            if (isActive)
            {
                Debug.Log("Managing resources...");
            }
        }

        public void BuildFacility()
        {
            if (isActive)
            {
                Debug.Log("Building facility...");
            }
        }
    }
}