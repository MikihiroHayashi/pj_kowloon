using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Managers
{
    public class PhaseManager : MonoBehaviour
    {
        public static PhaseManager Instance { get; private set; }

        [Header("Phase Configuration")]
        [SerializeField] private PhaseData[] phaseConfigurations;
        [SerializeField] private float phaseTransitionDelay = 2f;

        [Header("Current Phase Info")]
        [SerializeField] private GamePhase currentPhase;
        [SerializeField] private float currentPhaseTime;
        [SerializeField] private bool isTransitioning;

        private Dictionary<GamePhase, PhaseData> phaseDictionary;
        private GameManager gameManager;

        public GamePhase CurrentPhase => currentPhase;
        public float CurrentPhaseTime => currentPhaseTime;
        public bool IsTransitioning => isTransitioning;

        public event Action<GamePhase, GamePhase> OnPhaseTransition;
        public event Action<GamePhase> OnPhaseStarted;
        public event Action<GamePhase> OnPhaseEnded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePhaseManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void Update()
        {
            if (!isTransitioning)
            {
                UpdateCurrentPhase();
            }
        }

        private void InitializePhaseManager()
        {
            phaseDictionary = new Dictionary<GamePhase, PhaseData>();
            
            if (phaseConfigurations == null || phaseConfigurations.Length == 0)
            {
                CreateDefaultPhaseConfigurations();
            }

            foreach (var phaseData in phaseConfigurations)
            {
                phaseDictionary[phaseData.phase] = phaseData;
            }

            Debug.Log("Phase Manager Initialized");
        }

        private void CreateDefaultPhaseConfigurations()
        {
            phaseConfigurations = new PhaseData[]
            {
                new PhaseData
                {
                    phase = GamePhase.SURVIVAL,
                    phaseName = "生存フェーズ",
                    description = "資源を集め、仲間を探し、拠点を構築する",
                    objectives = new string[] { "食料と水を確保する", "仲間を見つける", "拠点を建設する" },
                    maxDuration = 1800f,
                    canSkip = false
                },
                new PhaseData
                {
                    phase = GamePhase.DEFENSE,
                    phaseName = "防衛フェーズ",
                    description = "襲撃から拠点を守り、感染拡大を防ぐ",
                    objectives = new string[] { "拠点を強化する", "襲撃を撃退する", "感染者を治療する" },
                    maxDuration = 1200f,
                    canSkip = false
                },
                new PhaseData
                {
                    phase = GamePhase.ESCAPE,
                    phaseName = "脱出フェーズ",
                    description = "クーロン城から脱出する",
                    objectives = new string[] { "脱出ルートを確保する", "最終ボスを倒す", "仲間と共に脱出する" },
                    maxDuration = 900f,
                    canSkip = false
                }
            };
        }

        private void UpdateCurrentPhase()
        {
            currentPhaseTime += Time.deltaTime;

            if (phaseDictionary.TryGetValue(currentPhase, out PhaseData phaseData))
            {
                if (phaseData.maxDuration > 0 && currentPhaseTime >= phaseData.maxDuration)
                {
                    TriggerPhaseTransition();
                }
            }
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == currentPhase) return;

            StartPhaseTransition(currentPhase, newPhase);
        }

        private void StartPhaseTransition(GamePhase fromPhase, GamePhase toPhase)
        {
            if (isTransitioning) return;

            isTransitioning = true;
            OnPhaseTransition?.Invoke(fromPhase, toPhase);
            
            Debug.Log($"Phase transition started: {fromPhase} -> {toPhase}");
            
            Invoke(nameof(CompletePhaseTransition), phaseTransitionDelay);
        }

        private void CompletePhaseTransition()
        {
            GamePhase previousPhase = currentPhase;
            currentPhase = gameManager.CurrentPhase;
            currentPhaseTime = 0f;
            isTransitioning = false;

            OnPhaseEnded?.Invoke(previousPhase);
            OnPhaseStarted?.Invoke(currentPhase);

            Debug.Log($"Phase transition completed: {currentPhase}");
        }

        private void TriggerPhaseTransition()
        {
            GamePhase nextPhase = GetNextPhase(currentPhase);
            if (nextPhase != currentPhase)
            {
                gameManager.ChangePhase(nextPhase);
            }
        }

        private GamePhase GetNextPhase(GamePhase current)
        {
            return current switch
            {
                GamePhase.SURVIVAL => GamePhase.DEFENSE,
                GamePhase.DEFENSE => GamePhase.ESCAPE,
                GamePhase.ESCAPE => GamePhase.ESCAPE,
                _ => GamePhase.SURVIVAL
            };
        }

        public PhaseData GetPhaseData(GamePhase phase)
        {
            return phaseDictionary.TryGetValue(phase, out PhaseData data) ? data : null;
        }

        public PhaseData GetCurrentPhaseData()
        {
            return GetPhaseData(currentPhase);
        }

        public string[] GetCurrentObjectives()
        {
            PhaseData data = GetCurrentPhaseData();
            return data?.objectives ?? new string[0];
        }

        public float GetPhaseProgress()
        {
            PhaseData data = GetCurrentPhaseData();
            if (data == null || data.maxDuration <= 0) return 0f;

            return Mathf.Clamp01(currentPhaseTime / data.maxDuration);
        }

        public bool CanSkipPhase()
        {
            PhaseData data = GetCurrentPhaseData();
            return data?.canSkip ?? false;
        }

        public void ForcePhaseTransition(GamePhase targetPhase)
        {
            if (isTransitioning) return;

            gameManager.ChangePhase(targetPhase);
        }

        public void ResetPhaseTime()
        {
            currentPhaseTime = 0f;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
    }

    [Serializable]
    public class PhaseData
    {
        public GamePhase phase;
        public string phaseName;
        public string description;
        public string[] objectives;
        public float maxDuration;
        public bool canSkip;
    }
}