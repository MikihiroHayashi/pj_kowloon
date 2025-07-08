using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Systems
{
    public class DefenseSystem : PhaseSystem
    {
        [Header("Defense Settings")]
        [SerializeField] private float defenseLevel = 1f;
        [SerializeField] private int maxWaves = 5;
        [SerializeField] private float waveCooldown = 30f;

        private int currentWave = 0;
        private float waveTimer = 0f;

        protected override void OnSystemActivatedInternal()
        {
            StartDefenseMode();
        }

        protected override void OnSystemDeactivatedInternal()
        {
            StopDefenseMode();
        }

        protected override void HandlePhaseChanged(GamePhase newPhase)
        {
            switch (newPhase)
            {
                case GamePhase.DEFENSE:
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
                UpdateDefense();
            }
        }

        private void StartDefenseMode()
        {
            currentWave = 0;
            waveTimer = 0f;
            Debug.Log("Defense System: Started - Prepare for incoming waves!");
        }

        private void StopDefenseMode()
        {
            Debug.Log("Defense System: Stopped");
        }

        private void UpdateDefense()
        {
            waveTimer += Time.deltaTime;
            
            if (waveTimer >= waveCooldown && currentWave < maxWaves)
            {
                TriggerWave();
            }
        }

        private void TriggerWave()
        {
            currentWave++;
            waveTimer = 0f;
            Debug.Log($"Wave {currentWave} incoming!");
            
            if (currentWave >= maxWaves)
            {
                CompleteDefensePhase();
            }
        }

        private void CompleteDefensePhase()
        {
            Debug.Log("All waves defended! Defense phase complete.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangePhase(GamePhase.ESCAPE);
            }
        }

        public void DeployUnit(Vector3 position)
        {
            if (isActive)
            {
                Debug.Log($"Deploying unit at position: {position}");
            }
        }

        public void RepairFortification()
        {
            if (isActive)
            {
                Debug.Log("Repairing fortifications...");
            }
        }

        public void ActivateDefenses()
        {
            if (isActive)
            {
                Debug.Log("Activating defensive measures...");
            }
        }
    }
}