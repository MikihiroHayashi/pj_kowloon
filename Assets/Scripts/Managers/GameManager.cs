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
            // ゲーム初期化処理
        }

        private void StartGame()
        {
            ChangePhase(GamePhase.SURVIVAL);
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
                
                OnPhaseChanged?.Invoke(currentPhase);
                
                HandlePhaseTransition(previousPhase, currentPhase);
            }
        }

        public void AdvanceDay()
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
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
            // サバイバルフェーズ初期化
        }

        private void InitializeDefensePhase()
        {
            // 防衛フェーズ初期化
        }

        private void InitializeEscapePhase()
        {
            // 脱出フェーズ初期化
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }

}