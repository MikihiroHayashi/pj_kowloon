using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [Header("Lighting Configuration")]
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Light globalAmbientLight;
        [SerializeField] private Gradient dayNightCycle;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        [SerializeField] private float dayDuration = 1200f;


        [Header("Atmospheric Effects")]
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private AudioSource ambientAudioSource;

        [Header("Weather System")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [SerializeField] private float weatherTransitionDuration = 30f;
        [SerializeField] private WeatherData[] weatherPresets;

        private float currentTimeOfDay = 0.5f;
        private float weatherTransitionTimer;
        private WeatherType targetWeather;
        private bool isTransitioningWeather;

        public float CurrentTimeOfDay => currentTimeOfDay;
        public WeatherType CurrentWeather => currentWeather;
        public bool IsNight => currentTimeOfDay < 0.25f || currentTimeOfDay > 0.75f;

        public event Action<float> OnTimeOfDayChanged;
        public event Action<WeatherType> OnWeatherChanged;

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
                InitializeEnvironment();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateDayNightCycle();
            UpdateWeatherSystem();
            UpdateAtmosphericEffects();
        }

        private void InitializeEnvironment()
        {
            if (weatherPresets == null || weatherPresets.Length == 0)
            {
                CreateDefaultWeatherPresets();
            }

            SetupInitialLighting();
            
            Debug.Log("Environment Manager Initialized");
        }

        private void CreateDefaultWeatherPresets()
        {
            weatherPresets = new WeatherData[]
            {
                new WeatherData
                {
                    type = WeatherType.Clear,
                    name = "晴れ",
                    ambientIntensity = 0.8f,
                    fogDensity = 0.01f,
                    rainIntensity = 0f,
                    windStrength = 0.3f,
                    ambientColor = new Color(0.3f, 0.4f, 0.6f)
                },
                new WeatherData
                {
                    type = WeatherType.Cloudy,
                    name = "曇り",
                    ambientIntensity = 0.5f,
                    fogDensity = 0.03f,
                    rainIntensity = 0f,
                    windStrength = 0.5f,
                    ambientColor = new Color(0.4f, 0.4f, 0.5f)
                },
                new WeatherData
                {
                    type = WeatherType.Rainy,
                    name = "雨",
                    ambientIntensity = 0.3f,
                    fogDensity = 0.05f,
                    rainIntensity = 0.7f,
                    windStrength = 0.8f,
                    ambientColor = new Color(0.2f, 0.3f, 0.4f)
                },
                new WeatherData
                {
                    type = WeatherType.Foggy,
                    name = "霧",
                    ambientIntensity = 0.2f,
                    fogDensity = 0.1f,
                    rainIntensity = 0f,
                    windStrength = 0.2f,
                    ambientColor = new Color(0.4f, 0.4f, 0.4f)
                },
                new WeatherData
                {
                    type = WeatherType.Storm,
                    name = "嵐",
                    ambientIntensity = 0.1f,
                    fogDensity = 0.07f,
                    rainIntensity = 1f,
                    windStrength = 1f,
                    ambientColor = new Color(0.1f, 0.2f, 0.3f)
                }
            };
        }


        private void SetupInitialLighting()
        {
            if (mainDirectionalLight == null)
            {
                GameObject lightGO = new GameObject("Main Directional Light");
                mainDirectionalLight = lightGO.AddComponent<Light>();
                mainDirectionalLight.type = LightType.Directional;
            }

            UpdateLighting();
        }

        private void UpdateDayNightCycle()
        {
            float gameTime = GameManager.Instance?.GameTime ?? 0f;
            currentTimeOfDay = (gameTime / dayDuration) % 1f;
            
            UpdateLighting();
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        private void UpdateLighting()
        {
            if (mainDirectionalLight != null)
            {
                float lightIntensity = lightIntensityCurve.Evaluate(currentTimeOfDay);
                Color lightColor = dayNightCycle.Evaluate(currentTimeOfDay);
                
                mainDirectionalLight.intensity = lightIntensity;
                mainDirectionalLight.color = lightColor;
                
                Vector3 rotation = new Vector3((currentTimeOfDay * 360f) - 90f, 170f, 0f);
                mainDirectionalLight.transform.rotation = Quaternion.Euler(rotation);
            }

            if (globalAmbientLight != null)
            {
                WeatherData currentWeatherData = GetCurrentWeatherData();
                if (currentWeatherData != null)
                {
                    globalAmbientLight.intensity = currentWeatherData.ambientIntensity * lightIntensityCurve.Evaluate(currentTimeOfDay);
                    globalAmbientLight.color = currentWeatherData.ambientColor;
                }
            }

            RenderSettings.fog = true;
            WeatherData weatherData = GetCurrentWeatherData();
            if (weatherData != null)
            {
                RenderSettings.fogDensity = weatherData.fogDensity;
                RenderSettings.fogColor = weatherData.ambientColor;
            }
        }


        private void UpdateWeatherSystem()
        {
            if (isTransitioningWeather)
            {
                weatherTransitionTimer += Time.deltaTime;
                float progress = weatherTransitionTimer / weatherTransitionDuration;
                
                if (progress >= 1f)
                {
                    CompleteWeatherTransition();
                }
                else
                {
                    UpdateWeatherTransition(progress);
                }
            }
        }

        private void UpdateAtmosphericEffects()
        {
            WeatherData weatherData = GetCurrentWeatherData();
            if (weatherData == null) return;

            if (dustParticles != null)
            {
                var emission = dustParticles.emission;
                emission.rateOverTime = weatherData.windStrength * 10f;
            }

            if (smokeParticles != null)
            {
                var emission = smokeParticles.emission;
                emission.rateOverTime = IsNight ? 15f : 5f;
            }

            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = weatherData.windStrength * 0.5f;
            }
        }

        public void ChangeWeather(WeatherType newWeather)
        {
            if (currentWeather == newWeather || isTransitioningWeather) return;

            targetWeather = newWeather;
            isTransitioningWeather = true;
            weatherTransitionTimer = 0f;
            
            Debug.Log($"Weather transition started: {currentWeather} -> {newWeather}");
        }

        private void CompleteWeatherTransition()
        {
            currentWeather = targetWeather;
            isTransitioningWeather = false;
            weatherTransitionTimer = 0f;
            
            OnWeatherChanged?.Invoke(currentWeather);
            Debug.Log($"Weather changed to: {currentWeather}");
        }

        private void UpdateWeatherTransition(float progress)
        {
            WeatherData currentData = GetWeatherData(currentWeather);
            WeatherData targetData = GetWeatherData(targetWeather);
            
            if (currentData != null && targetData != null)
            {
                float lerpedIntensity = Mathf.Lerp(currentData.ambientIntensity, targetData.ambientIntensity, progress);
                float lerpedFog = Mathf.Lerp(currentData.fogDensity, targetData.fogDensity, progress);
                
                if (globalAmbientLight != null)
                {
                    globalAmbientLight.intensity = lerpedIntensity;
                }
                
                RenderSettings.fogDensity = lerpedFog;
            }
        }

        private WeatherData GetCurrentWeatherData()
        {
            return GetWeatherData(currentWeather);
        }

        private WeatherData GetWeatherData(WeatherType weatherType)
        {
            foreach (var weather in weatherPresets)
            {
                if (weather.type == weatherType)
                {
                    return weather;
                }
            }
            return null;
        }

        public void SetTimeOfDay(float time)
        {
            currentTimeOfDay = Mathf.Clamp01(time);
            UpdateLighting();
        }

        public string GetTimeOfDayString()
        {
            float hours = currentTimeOfDay * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:00}:{minute:00}";
        }


        public float GetEnvironmentalHazardLevel()
        {
            float hazardLevel = 0f;
            
            WeatherData weatherData = GetCurrentWeatherData();
            if (weatherData != null)
            {
                hazardLevel += weatherData.rainIntensity * 0.3f;
                hazardLevel += weatherData.fogDensity * 10f;
                hazardLevel += weatherData.windStrength * 0.2f;
            }
            
            if (IsNight)
            {
                hazardLevel += 0.2f;
            }
            
            return Mathf.Clamp01(hazardLevel);
        }
    }

    [Serializable]
    public class WeatherData
    {
        public WeatherType type;
        public string name;
        public float ambientIntensity;
        public float fogDensity;
        public float rainIntensity;
        public float windStrength;
        public Color ambientColor;
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rainy,
        Foggy,
        Storm
    }
}