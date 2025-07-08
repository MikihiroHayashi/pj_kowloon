using System;
using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private GamePhase currentPhase = GamePhase.SURVIVAL;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private float gameTime = 0f;

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<int> OnDayChanged;

        public GamePhase CurrentPhase => currentPhase;
        public int CurrentDay => currentDay;
        public float GameTime => gameTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            UpdateGameState();
        }

        private void InitializeGame()
        {
            Debug.Log("Kowloon Break - Game Initialized");
        }

        private void StartGame()
        {
            ChangePhase(GamePhase.SURVIVAL);
            Debug.Log($"Game Started - Phase: {currentPhase}, Day: {currentDay}");
        }

        private void UpdateGameState()
        {
            gameTime += Time.deltaTime;
        }

        public void ChangePhase(GamePhase newPhase)
        {
            if (currentPhase != newPhase)
            {
                GamePhase previousPhase = currentPhase;
                currentPhase = newPhase;
                
                Debug.Log($"Phase changed from {previousPhase} to {currentPhase}");
                OnPhaseChanged?.Invoke(currentPhase);
                
                HandlePhaseTransition(previousPhase, currentPhase);
            }
        }

        public void AdvanceDay()
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
            Debug.Log($"Day advanced to: {currentDay}");
        }

        private void HandlePhaseTransition(GamePhase from, GamePhase to)
        {
            switch (to)
            {
                case GamePhase.SURVIVAL:
                    InitializeSurvivalPhase();
                    break;
                case GamePhase.DEFENSE:
                    InitializeDefensePhase();
                    break;
                case GamePhase.ESCAPE:
                    InitializeEscapePhase();
                    break;
            }
        }

        private void InitializeSurvivalPhase()
        {
            Debug.Log("Initializing Survival Phase");
        }

        private void InitializeDefensePhase()
        {
            Debug.Log("Initializing Defense Phase");
        }

        private void InitializeEscapePhase()
        {
            Debug.Log("Initializing Escape Phase");
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            Debug.Log("Game Paused");
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            Debug.Log("Game Resumed");
        }

        public void QuitGame()
        {
            Debug.Log("Game Quit");
            Application.Quit();
        }
    }

}