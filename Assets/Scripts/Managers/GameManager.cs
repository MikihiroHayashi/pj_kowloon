using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] private float dayDuration = 1200f; // 20分 = 1日
        
        [Header("Game State")]
        [SerializeField] private bool isGamePaused = false;
        [SerializeField] private bool isGameInitialized = false;
        [SerializeField] private string currentSaveSlot = "default";
        
        [Header("Manager Dependencies")]
        [SerializeField] private PhaseManager phaseManager;
        [SerializeField] private EnhancedResourceManager resourceManager;
        [SerializeField] private InfectionManager infectionManager;

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<int> OnDayChanged;
        public event Action<bool> OnGamePausedChanged;
        public event Action OnGameInitialized;
        public event Action<string> OnGameSaved;
        public event Action<string> OnGameLoaded;

        public GamePhase CurrentPhase => currentPhase;
        public int CurrentDay => currentDay;
        public float GameTime => gameTime;
        public bool IsGamePaused => isGamePaused;
        public bool IsGameInitialized => isGameInitialized;
        public float DayProgress => (gameTime % dayDuration) / dayDuration;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                // Managerオブジェクトをルートに移動してからDontDestroyOnLoadを適用
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
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
            Debug.Log("Initializing Kowloon Break Game...");
            
            // 依存関係のある他のマネージャーを取得・初期化
            if (phaseManager == null)
                phaseManager = FindObjectOfType<PhaseManager>();
            if (resourceManager == null)
                resourceManager = FindObjectOfType<EnhancedResourceManager>();
            if (infectionManager == null)
                infectionManager = FindObjectOfType<InfectionManager>();
                
            // ゲーム状態の初期化
            gameTime = 0f;
            currentDay = 1;
            
            isGameInitialized = true;
            OnGameInitialized?.Invoke();
            
            Debug.Log("Game initialization completed!");
        }

        private void StartGame()
        {
            if (!isGameInitialized)
            {
                Debug.LogWarning("Game not initialized, initializing now...");
                InitializeGame();
            }
            
            ChangePhase(GamePhase.SURVIVAL);
            Debug.Log("Game started - Survival Phase");
        }

        private void UpdateGameState()
        {
            if (isGamePaused) return;
            
            float previousTime = gameTime;
            gameTime += Time.deltaTime;
            
            // 日の変更チェック
            int previousDay = Mathf.FloorToInt(previousTime / dayDuration);
            int currentDayCalculated = Mathf.FloorToInt(gameTime / dayDuration);
            
            if (currentDayCalculated > previousDay)
            {
                AdvanceDay();
            }
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
            Debug.Log($"Day advanced to {currentDay}");
            OnDayChanged?.Invoke(currentDay);
            
            // 日替わり処理
            HandleDayTransition();
        }
        
        private void HandleDayTransition()
        {
            // リソースの劣化・変化
            if (resourceManager != null)
            {
                // ResourceManagerで日替わり処理を実行
                Debug.Log("Processing daily resource changes...");
            }
            
            // 感染状態の進行
            if (infectionManager != null)
            {
                Debug.Log("Processing daily infection progression...");
            }
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
            Debug.Log("Initializing Survival Phase...");
            // サバイバルフェーズ特有の初期化
            // - リソース収集の有効化
            // - 基本的な敵AIの活性化
            // - 探索システムの開始
        }

        private void InitializeDefensePhase()
        {
            Debug.Log("Initializing Defense Phase...");
            // 防衛フェーズ特有の初期化
            // - 強化された敵の出現
            // - 拠点防衛システムの活性化
            // - 感染拡大システムの加速
        }

        private void InitializeEscapePhase()
        {
            Debug.Log("Initializing Escape Phase...");
            // 脱出フェーズ特有の初期化
            // - 脱出ルートの生成
            // - ボス敵の配置
            // - 最終目標の設定
        }

        public void PauseGame()
        {
            if (!isGamePaused)
            {
                isGamePaused = true;
                Time.timeScale = 0f;
                OnGamePausedChanged?.Invoke(true);
                Debug.Log("Game paused");
            }
        }

        public void ResumeGame()
        {
            if (isGamePaused)
            {
                isGamePaused = false;
                Time.timeScale = 1f;
                OnGamePausedChanged?.Invoke(false);
                Debug.Log("Game resumed");
            }
        }

        public void RestartGame()
        {
            Debug.Log("Restarting game...");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        // セーブ・ロード機能の基盤
        public void SaveGame(string saveSlot = null)
        {
            if (string.IsNullOrEmpty(saveSlot))
                saveSlot = currentSaveSlot;
                
            try
            {
                var saveData = CreateGameSaveData();
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"save_{saveSlot}.json");
                System.IO.File.WriteAllText(filePath, json);
                
                currentSaveSlot = saveSlot;
                OnGameSaved?.Invoke(saveSlot);
                Debug.Log($"Game saved to slot: {saveSlot}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save game: {ex.Message}");
            }
        }
        
        public void LoadGame(string saveSlot = null)
        {
            if (string.IsNullOrEmpty(saveSlot))
                saveSlot = currentSaveSlot;
                
            try
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"save_{saveSlot}.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file not found: {saveSlot}");
                    return;
                }
                
                string json = System.IO.File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<GameSaveData>(json);
                LoadGameSaveData(saveData);
                
                currentSaveSlot = saveSlot;
                OnGameLoaded?.Invoke(saveSlot);
                Debug.Log($"Game loaded from slot: {saveSlot}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load game: {ex.Message}");
            }
        }
        
        private GameSaveData CreateGameSaveData()
        {
            return new GameSaveData
            {
                currentPhase = this.currentPhase,
                currentDay = this.currentDay,
                gameTime = this.gameTime,
                saveTimestamp = System.DateTime.Now.ToBinary(),
                version = "1.0"
            };
        }
        
        private void LoadGameSaveData(GameSaveData saveData)
        {
            this.currentPhase = saveData.currentPhase;
            this.currentDay = saveData.currentDay;
            this.gameTime = saveData.gameTime;
            
            // フェーズを適切に設定
            ChangePhase(saveData.currentPhase);
        }
        
        public bool HasSaveFile(string saveSlot = null)
        {
            if (string.IsNullOrEmpty(saveSlot))
                saveSlot = currentSaveSlot;
                
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"save_{saveSlot}.json");
            return System.IO.File.Exists(filePath);
        }
    }
    
    [System.Serializable]
    public class GameSaveData
    {
        public GamePhase currentPhase;
        public int currentDay;
        public float gameTime;
        public long saveTimestamp;
        public string version;
        
        public System.DateTime GetSaveDateTime()
        {
            return System.DateTime.FromBinary(saveTimestamp);
        }
    }
}