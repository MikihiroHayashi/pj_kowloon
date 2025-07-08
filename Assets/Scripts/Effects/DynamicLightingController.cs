using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using KowloonBreak.Environment;

namespace KowloonBreak.Effects
{
    public class DynamicLightingController : MonoBehaviour
    {
        public static DynamicLightingController Instance { get; private set; }

        [Header("Dynamic Lighting Settings")]
        [SerializeField] private Light[] dynamicLights;
        [SerializeField] private float lightFlickerSpeed = 2f;
        [SerializeField] private float lightFlickerIntensity = 0.2f;
        [SerializeField] private bool enableLightFlicker = true;

        [Header("Volumetric Lighting")]
        [SerializeField] private Light mainVolumetricLight;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private float volumetricIntensity = 1f;
        [SerializeField] private bool enableVolumetricFog = true;

        [Header("Emergency Lighting")]
        [SerializeField] private Light[] emergencyLights;
        [SerializeField] private Color emergencyColor = Color.red;
        [SerializeField] private float emergencyFlashSpeed = 3f;
        [SerializeField] private bool emergencyMode = false;

        [Header("Neon District Lighting")]
        [SerializeField] private Transform neonLightContainer;
        [SerializeField] private Light neonLightPrefab;
        [SerializeField] private Color[] neonColors;
        [SerializeField] private float neonIntensityMultiplier = 1.5f;

        [Header("Interactive Lighting")]
        [SerializeField] private LayerMask interactiveLayers = -1;
        [SerializeField] private float maxInteractionDistance = 10f;
        [SerializeField] private AnimationCurve lightDistanceCurve;

        private Dictionary<string, LightData> registeredLights;
        private List<Light> neonLights;
        private Transform playerTransform;
        private EnvironmentManager environmentManager;

        public bool EmergencyMode => emergencyMode;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLighting();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            environmentManager = EnvironmentManager.Instance;
            SetupPlayerReference();
            CreateNeonLighting();
        }

        private void Update()
        {
            UpdateDynamicLighting();
            UpdateVolumetricLighting();
            UpdateEmergencyLighting();
            UpdateInteractiveLighting();
        }

        private void InitializeLighting()
        {
            registeredLights = new Dictionary<string, LightData>();
            neonLights = new List<Light>();
            
            RegisterInitialLights();
            
            Debug.Log("Dynamic Lighting Controller Initialized");
        }

        private void RegisterInitialLights()
        {
            if (dynamicLights != null)
            {
                for (int i = 0; i < dynamicLights.Length; i++)
                {
                    var light = dynamicLights[i];
                    if (light != null)
                    {
                        RegisterLight($"Dynamic_{i}", light, LightType.Dynamic);
                    }
                }
            }
        }

        private void SetupPlayerReference()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void CreateNeonLighting()
        {
            if (neonLightContainer == null || neonLightPrefab == null) return;

            for (int i = 0; i < 20; i++)
            {
                Vector3 randomPosition = new Vector3(
                    Random.Range(-50f, 50f),
                    Random.Range(5f, 25f),
                    Random.Range(-30f, 30f)
                );

                var neonLight = Instantiate(neonLightPrefab, neonLightContainer);
                neonLight.transform.localPosition = randomPosition;
                
                Color randomColor = neonColors[Random.Range(0, neonColors.Length)];
                neonLight.color = randomColor;
                neonLight.intensity = Random.Range(0.8f, 2f) * neonIntensityMultiplier;
                
                neonLights.Add(neonLight);
                RegisterLight($"Neon_{i}", neonLight, LightType.Neon);
            }
        }

        private void UpdateDynamicLighting()
        {
            if (!enableLightFlicker) return;

            float flickerValue = Mathf.Sin(Time.time * lightFlickerSpeed) * lightFlickerIntensity;
            
            foreach (var lightData in registeredLights.Values)
            {
                if (lightData.Type == LightType.Dynamic && lightData.Light != null)
                {
                    float baseIntensity = lightData.OriginalIntensity;
                    lightData.Light.intensity = baseIntensity + (flickerValue * baseIntensity);
                }
            }
        }

        private void UpdateVolumetricLighting()
        {
            if (!enableVolumetricFog) return;

            if (environmentManager != null)
            {
                float timeOfDay = environmentManager.CurrentTimeOfDay;
                bool isNight = environmentManager.IsNight;
                
                if (mainVolumetricLight != null)
                {
                    float targetIntensity = isNight ? volumetricIntensity * 0.3f : volumetricIntensity;
                    mainVolumetricLight.intensity = Mathf.Lerp(mainVolumetricLight.intensity, targetIntensity, Time.deltaTime);
                }
                
                if (dustParticles != null)
                {
                    var emission = dustParticles.emission;
                    emission.rateOverTime = isNight ? 30f : 10f;
                }
            }
        }

        private void UpdateEmergencyLighting()
        {
            if (!emergencyMode || emergencyLights == null) return;

            float flashValue = Mathf.Sin(Time.time * emergencyFlashSpeed) > 0 ? 1f : 0.3f;
            
            foreach (var emergencyLight in emergencyLights)
            {
                if (emergencyLight != null)
                {
                    emergencyLight.intensity = flashValue;
                    emergencyLight.color = emergencyColor;
                }
            }
        }

        private void UpdateInteractiveLighting()
        {
            if (playerTransform == null) return;

            foreach (var lightData in registeredLights.Values)
            {
                if (lightData.Light == null) continue;

                float distance = Vector3.Distance(playerTransform.position, lightData.Light.transform.position);
                
                if (distance <= maxInteractionDistance)
                {
                    float distanceRatio = distance / maxInteractionDistance;
                    float intensityMultiplier = lightDistanceCurve.Evaluate(1f - distanceRatio);
                    
                    float targetIntensity = lightData.OriginalIntensity * intensityMultiplier;
                    lightData.Light.intensity = Mathf.Lerp(lightData.Light.intensity, targetIntensity, Time.deltaTime * 2f);
                }
            }
        }

        public void RegisterLight(string id, Light light, LightType type)
        {
            if (light == null) return;

            var lightData = new LightData
            {
                Light = light,
                Type = type,
                OriginalIntensity = light.intensity,
                OriginalColor = light.color
            };

            registeredLights[id] = lightData;
        }

        public void UnregisterLight(string id)
        {
            if (registeredLights.ContainsKey(id))
            {
                registeredLights.Remove(id);
            }
        }

        public void SetEmergencyMode(bool enabled)
        {
            emergencyMode = enabled;
            
            if (enabled)
            {
                foreach (var lightData in registeredLights.Values)
                {
                    if (lightData.Type == LightType.Dynamic && lightData.Light != null)
                    {
                        lightData.Light.color = emergencyColor;
                        lightData.Light.intensity = lightData.OriginalIntensity * 0.5f;
                    }
                }
            }
            else
            {
                foreach (var lightData in registeredLights.Values)
                {
                    if (lightData.Light != null)
                    {
                        lightData.Light.color = lightData.OriginalColor;
                        lightData.Light.intensity = lightData.OriginalIntensity;
                    }
                }
            }
            
            Debug.Log($"Emergency lighting mode: {(enabled ? "ON" : "OFF")}");
        }

        public void SetNeonIntensity(float multiplier)
        {
            neonIntensityMultiplier = multiplier;
            
            foreach (var lightData in registeredLights.Values)
            {
                if (lightData.Type == LightType.Neon && lightData.Light != null)
                {
                    lightData.Light.intensity = lightData.OriginalIntensity * multiplier;
                }
            }
        }

        public void FlickerLight(string lightId, float duration = 1f)
        {
            if (registeredLights.TryGetValue(lightId, out LightData lightData))
            {
                StartCoroutine(FlickerLightCoroutine(lightData, duration));
            }
        }

        private System.Collections.IEnumerator FlickerLightCoroutine(LightData lightData, float duration)
        {
            if (lightData.Light == null) yield break;

            float elapsedTime = 0f;
            float originalIntensity = lightData.Light.intensity;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                
                float flickerValue = Random.Range(0.1f, 1f);
                lightData.Light.intensity = originalIntensity * flickerValue;
                
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
            
            lightData.Light.intensity = originalIntensity;
        }

        public void SetGlobalLightingIntensity(float intensity)
        {
            foreach (var lightData in registeredLights.Values)
            {
                if (lightData.Light != null)
                {
                    lightData.Light.intensity = lightData.OriginalIntensity * intensity;
                }
            }
        }

        public void CreateDynamicLight(Vector3 position, Color color, float intensity, float lifetime = 0f)
        {
            if (neonLightPrefab == null) return;

            var dynamicLight = Instantiate(neonLightPrefab);
            dynamicLight.transform.position = position;
            dynamicLight.color = color;
            dynamicLight.intensity = intensity;
            
            string lightId = $"Dynamic_{System.Guid.NewGuid()}";
            RegisterLight(lightId, dynamicLight, LightType.Dynamic);
            
            if (lifetime > 0f)
            {
                StartCoroutine(DestroyLightAfterTime(lightId, dynamicLight.gameObject, lifetime));
            }
        }

        private System.Collections.IEnumerator DestroyLightAfterTime(string lightId, GameObject lightObject, float time)
        {
            yield return new WaitForSeconds(time);
            
            UnregisterLight(lightId);
            if (lightObject != null)
            {
                Destroy(lightObject);
            }
        }

        public void SetLightFlicker(bool enabled)
        {
            enableLightFlicker = enabled;
        }

        public void SetVolumetricFog(bool enabled)
        {
            enableVolumetricFog = enabled;
            
            if (dustParticles != null)
            {
                var emission = dustParticles.emission;
                emission.enabled = enabled;
            }
        }

        public List<Light> GetLightsInRange(Vector3 position, float range)
        {
            var lightsInRange = new List<Light>();
            
            foreach (var lightData in registeredLights.Values)
            {
                if (lightData.Light != null)
                {
                    float distance = Vector3.Distance(position, lightData.Light.transform.position);
                    if (distance <= range)
                    {
                        lightsInRange.Add(lightData.Light);
                    }
                }
            }
            
            return lightsInRange;
        }
    }

    [System.Serializable]
    public class LightData
    {
        public Light Light;
        public LightType Type;
        public float OriginalIntensity;
        public Color OriginalColor;
    }

    public enum LightType
    {
        Dynamic,
        Neon,
        Emergency,
        Static,
        Interactive
    }
}