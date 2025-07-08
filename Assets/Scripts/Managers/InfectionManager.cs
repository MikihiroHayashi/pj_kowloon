using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Characters;

namespace KowloonBreak.Managers
{
    public class InfectionManager : MonoBehaviour
    {
        public static InfectionManager Instance { get; private set; }

        [Header("Infection Configuration")]
        [SerializeField] private float cityInfectionRate = 0.1f;
        [SerializeField] private float baseInfectionRisk = 0.01f;
        [SerializeField] private float spreadMultiplier = 1.5f;
        [SerializeField] private float treatmentEffectiveness = 0.3f;

        [Header("Infection Events")]
        [SerializeField] private InfectionEventData[] infectionEvents;
        [SerializeField] private float eventCheckInterval = 300f;

        private Dictionary<string, float> characterInfectionRisk;
        private List<InfectionEvent> activeOutbreaks;
        private float eventTimer;
        private BaseManager baseManager;

        public float CityInfectionRate => cityInfectionRate;
        public List<InfectionEvent> ActiveOutbreaks => activeOutbreaks;

        public event Action<float> OnCityInfectionRateChanged;
        public event Action<InfectionEvent> OnOutbreakStarted;
        public event Action<InfectionEvent> OnOutbreakEnded;
        public event Action<CompanionCharacter> OnCharacterInfected;
        public event Action<CompanionCharacter> OnCharacterTurned;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInfectionManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            baseManager = BaseManager.Instance;
        }

        private void Update()
        {
            UpdateInfectionSpread(Time.deltaTime);
            UpdateInfectionEvents();
        }

        private void InitializeInfectionManager()
        {
            characterInfectionRisk = new Dictionary<string, float>();
            activeOutbreaks = new List<InfectionEvent>();
            
            CreateDefaultInfectionEvents();
            
            Debug.Log("Infection Manager Initialized");
        }

        private void CreateDefaultInfectionEvents()
        {
            if (infectionEvents == null || infectionEvents.Length == 0)
            {
                infectionEvents = new InfectionEventData[]
                {
                    new InfectionEventData
                    {
                        eventName = "小規模感染",
                        description = "近隣エリアで小規模な感染が発生",
                        riskIncrease = 0.05f,
                        duration = 600f,
                        probability = 0.3f
                    },
                    new InfectionEventData
                    {
                        eventName = "水源汚染",
                        description = "水源が汚染され、感染リスクが上昇",
                        riskIncrease = 0.15f,
                        duration = 1200f,
                        probability = 0.1f
                    },
                    new InfectionEventData
                    {
                        eventName = "大規模感染",
                        description = "広範囲での感染拡大が発生",
                        riskIncrease = 0.25f,
                        duration = 1800f,
                        probability = 0.05f
                    }
                };
            }
        }

        public void UpdateInfectionSpread(float deltaTime)
        {
            UpdateCityInfectionRate(deltaTime);
            UpdateCharacterInfections(deltaTime);
        }

        private void UpdateCityInfectionRate(float deltaTime)
        {
            float previousRate = cityInfectionRate;
            
            float baseIncrease = baseInfectionRisk * deltaTime;
            float outbreakIncrease = CalculateOutbreakRiskIncrease() * deltaTime;
            float facilityReduction = CalculateFacilityReduction() * deltaTime;
            
            cityInfectionRate += baseIncrease + outbreakIncrease - facilityReduction;
            cityInfectionRate = Mathf.Clamp01(cityInfectionRate);
            
            if (Mathf.Abs(cityInfectionRate - previousRate) > 0.001f)
            {
                OnCityInfectionRateChanged?.Invoke(cityInfectionRate);
            }
        }

        private float CalculateOutbreakRiskIncrease()
        {
            float totalRisk = 0f;
            foreach (var outbreak in activeOutbreaks)
            {
                totalRisk += outbreak.RiskIncrease;
            }
            return totalRisk;
        }

        private float CalculateFacilityReduction()
        {
            float reduction = 0f;
            
            if (baseManager != null)
            {
                if (baseManager.HasFacility(FacilityType.Infirmary))
                {
                    int infirmaryLevel = baseManager.GetFacilityLevel(FacilityType.Infirmary);
                    reduction += infirmaryLevel * 0.01f;
                }
            }
            
            return reduction;
        }

        private void UpdateCharacterInfections(float deltaTime)
        {
            var companions = UnityEngine.Object.FindObjectsOfType<CompanionCharacter>();
            
            foreach (var companion in companions)
            {
                if (companion.Infection.Level == InfectionLevel.Zombie) continue;
                
                float environmentalRisk = CalculateEnvironmentalRisk(companion);
                companion.Infection.UpdateInfection(deltaTime, environmentalRisk);
                
                if (companion.Infection.Level != InfectionLevel.Clean)
                {
                    SpreadInfectionToNearbyCharacters(companion, deltaTime);
                }
            }
        }

        private float CalculateEnvironmentalRisk(CompanionCharacter character)
        {
            float baseRisk = cityInfectionRate * 0.1f;
            
            string characterId = character.CharacterId;
            if (characterInfectionRisk.TryGetValue(characterId, out float additionalRisk))
            {
                baseRisk += additionalRisk;
            }
            
            foreach (var outbreak in activeOutbreaks)
            {
                baseRisk += outbreak.RiskIncrease * 0.2f;
            }
            
            return baseRisk;
        }

        private void SpreadInfectionToNearbyCharacters(CompanionCharacter infectedCharacter, float deltaTime)
        {
            var companions = UnityEngine.Object.FindObjectsOfType<CompanionCharacter>();
            
            foreach (var companion in companions)
            {
                if (companion == infectedCharacter) continue;
                if (companion.Infection.Level == InfectionLevel.Zombie) continue;
                
                float distance = Vector3.Distance(infectedCharacter.transform.position, companion.transform.position);
                if (distance <= 10f)
                {
                    float spreadChance = CalculateSpreadChance(infectedCharacter, companion, distance);
                    if (UnityEngine.Random.Range(0f, 1f) < spreadChance * deltaTime)
                    {
                        IncreaseCharacterInfectionRisk(companion.CharacterId, 0.1f);
                    }
                }
            }
        }

        private float CalculateSpreadChance(CompanionCharacter infected, CompanionCharacter target, float distance)
        {
            float baseChance = 0.01f;
            
            float infectionLevelMultiplier = infected.Infection.Level switch
            {
                InfectionLevel.Exposed => 0.5f,
                InfectionLevel.Infected => 1f,
                InfectionLevel.Turning => 2f,
                _ => 0f
            };
            
            float distanceMultiplier = Mathf.Lerp(1f, 0.1f, distance / 10f);
            float immunityMultiplier = target.Infection.Immunity;
            
            return baseChance * infectionLevelMultiplier * distanceMultiplier * (1f - immunityMultiplier);
        }

        private void UpdateInfectionEvents()
        {
            eventTimer += Time.deltaTime;
            
            if (eventTimer >= eventCheckInterval)
            {
                eventTimer = 0f;
                CheckForNewOutbreaks();
            }
            
            UpdateActiveOutbreaks();
        }

        private void CheckForNewOutbreaks()
        {
            foreach (var eventData in infectionEvents)
            {
                if (UnityEngine.Random.Range(0f, 1f) < eventData.probability)
                {
                    StartOutbreak(eventData);
                }
            }
        }

        private void StartOutbreak(InfectionEventData eventData)
        {
            var outbreak = new InfectionEvent(eventData);
            activeOutbreaks.Add(outbreak);
            
            OnOutbreakStarted?.Invoke(outbreak);
            
            Debug.Log($"Outbreak started: {eventData.eventName}");
        }

        private void UpdateActiveOutbreaks()
        {
            for (int i = activeOutbreaks.Count - 1; i >= 0; i--)
            {
                var outbreak = activeOutbreaks[i];
                outbreak.Update(Time.deltaTime);
                
                if (outbreak.IsEnded)
                {
                    activeOutbreaks.RemoveAt(i);
                    OnOutbreakEnded?.Invoke(outbreak);
                    Debug.Log($"Outbreak ended: {outbreak.EventName}");
                }
            }
        }

        public void IncreaseCharacterInfectionRisk(string characterId, float amount)
        {
            if (!characterInfectionRisk.ContainsKey(characterId))
            {
                characterInfectionRisk[characterId] = 0f;
            }
            
            characterInfectionRisk[characterId] += amount;
        }

        public void DecreaseCharacterInfectionRisk(string characterId, float amount)
        {
            if (characterInfectionRisk.ContainsKey(characterId))
            {
                characterInfectionRisk[characterId] = Mathf.Max(0f, characterInfectionRisk[characterId] - amount);
            }
        }

        public bool TreatCharacter(CompanionCharacter character)
        {
            if (character.Infection.Level == InfectionLevel.Clean || character.Infection.Level == InfectionLevel.Zombie)
            {
                return false;
            }
            
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null && resourceManager.ConsumeResources(ResourceType.Medicine, 1))
            {
                character.Infection.TreatInfection(treatmentEffectiveness);
                DecreaseCharacterInfectionRisk(character.CharacterId, 0.2f);
                
                Debug.Log($"Treated character: {character.Name}");
                return true;
            }
            
            return false;
        }

        public void SetCityInfectionRate(float rate)
        {
            cityInfectionRate = Mathf.Clamp01(rate);
            OnCityInfectionRateChanged?.Invoke(cityInfectionRate);
        }

        public float GetCharacterInfectionRisk(string characterId)
        {
            return characterInfectionRisk.TryGetValue(characterId, out float risk) ? risk : 0f;
        }

        public InfectionLevel GetOverallInfectionThreat()
        {
            if (cityInfectionRate >= 0.8f) return InfectionLevel.Zombie;
            if (cityInfectionRate >= 0.6f) return InfectionLevel.Turning;
            if (cityInfectionRate >= 0.4f) return InfectionLevel.Infected;
            if (cityInfectionRate >= 0.2f) return InfectionLevel.Exposed;
            return InfectionLevel.Clean;
        }
    }

    [Serializable]
    public class InfectionEvent
    {
        public string EventName { get; private set; }
        public string Description { get; private set; }
        public float RiskIncrease { get; private set; }
        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public bool IsEnded => RemainingTime <= 0f;

        public InfectionEvent(InfectionEventData data)
        {
            EventName = data.eventName;
            Description = data.description;
            RiskIncrease = data.riskIncrease;
            Duration = data.duration;
            RemainingTime = data.duration;
        }

        public void Update(float deltaTime)
        {
            RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
        }

        public float GetProgress()
        {
            return 1f - (RemainingTime / Duration);
        }
    }

    [Serializable]
    public class InfectionEventData
    {
        public string eventName;
        public string description;
        public float riskIncrease;
        public float duration;
        public float probability;
    }
}