using System.Collections;
using UnityEngine;

namespace KowloonBreak.Environment
{
    public class NeonSign : MonoBehaviour
    {
        [Header("Neon Configuration")]
        [SerializeField] private Light[] neonLights;
        [SerializeField] private Renderer[] neonRenderers;
        [SerializeField] private Material[] neonMaterials;
        [SerializeField] private Color baseColor = Color.magenta;
        [SerializeField] private float baseIntensity = 2f;
        [SerializeField] private bool isActive = true;

        [Header("Flicker Settings")]
        [SerializeField] private bool enableFlicker = true;
        [SerializeField] private float flickerChance = 0.05f;
        [SerializeField] private float flickerDuration = 0.1f;
        [SerializeField] private float powerOutageChance = 0.001f;
        [SerializeField] private float powerOutageDuration = 5f;

        [Header("Animation")]
        [SerializeField] private bool enableColorCycle = false;
        [SerializeField] private Color[] colorCycle;
        [SerializeField] private float colorCycleSpeed = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource buzzAudioSource;
        [SerializeField] private AudioClip buzzSound;
        [SerializeField] private AudioClip flickerSound;

        private bool isFlickering;
        private bool isPoweredOff;
        private float originalIntensity;
        private Color originalColor;
        private Coroutine flickerCoroutine;
        private Coroutine powerOutageCoroutine;
        private int currentColorIndex;
        private float colorTimer;

        public bool IsActive => isActive && !isPoweredOff;
        public Color CurrentColor => baseColor;

        public void Initialize()
        {
            originalIntensity = baseIntensity;
            originalColor = baseColor;
            
            SetupAudio();
            ValidateComponents();
            SetNeonState(isActive);
            
            if (enableColorCycle && colorCycle != null && colorCycle.Length > 0)
            {
                currentColorIndex = 0;
                colorTimer = 0f;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            HandleColorCycle();
            HandleRandomFlicker();
            HandleRandomPowerOutage();
        }

        private void SetupAudio()
        {
            if (buzzAudioSource == null)
            {
                buzzAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            buzzAudioSource.clip = buzzSound;
            buzzAudioSource.loop = true;
            buzzAudioSource.volume = 0.3f;
            buzzAudioSource.spatialBlend = 1f;
            buzzAudioSource.maxDistance = 20f;
        }

        private void ValidateComponents()
        {
            if (neonLights == null || neonLights.Length == 0)
            {
                neonLights = GetComponentsInChildren<Light>();
            }
            
            if (neonRenderers == null || neonRenderers.Length == 0)
            {
                neonRenderers = GetComponentsInChildren<Renderer>();
            }
            
            if (neonMaterials == null || neonMaterials.Length == 0)
            {
                var materials = new System.Collections.Generic.List<Material>();
                foreach (var renderer in neonRenderers)
                {
                    materials.AddRange(renderer.materials);
                }
                neonMaterials = materials.ToArray();
            }
        }

        private void HandleColorCycle()
        {
            if (!enableColorCycle || colorCycle == null || colorCycle.Length <= 1) return;

            colorTimer += Time.deltaTime * colorCycleSpeed;
            
            if (colorTimer >= 1f)
            {
                colorTimer = 0f;
                currentColorIndex = (currentColorIndex + 1) % colorCycle.Length;
                baseColor = colorCycle[currentColorIndex];
                UpdateNeonColor(baseColor);
            }
        }

        private void HandleRandomFlicker()
        {
            if (!enableFlicker || isFlickering || isPoweredOff) return;

            if (Random.Range(0f, 1f) < flickerChance * Time.deltaTime)
            {
                StartFlicker();
            }
        }

        private void HandleRandomPowerOutage()
        {
            if (isPoweredOff || isFlickering) return;

            if (Random.Range(0f, 1f) < powerOutageChance * Time.deltaTime)
            {
                StartPowerOutage();
            }
        }

        public void UpdateFlicker(float flickerValue, float nightMultiplier)
        {
            if (!IsActive || isPoweredOff) return;

            float currentIntensity = baseIntensity * nightMultiplier;
            currentIntensity += flickerValue * baseIntensity * 0.1f;
            
            UpdateNeonIntensity(currentIntensity);
        }

        private void StartFlicker()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
            }
            
            flickerCoroutine = StartCoroutine(FlickerSequence());
        }

        private void StartPowerOutage()
        {
            if (powerOutageCoroutine != null)
            {
                StopCoroutine(powerOutageCoroutine);
            }
            
            powerOutageCoroutine = StartCoroutine(PowerOutageSequence());
        }

        private IEnumerator FlickerSequence()
        {
            isFlickering = true;
            
            PlayFlickerSound();
            
            int flickerCount = Random.Range(2, 6);
            for (int i = 0; i < flickerCount; i++)
            {
                SetNeonState(false);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                
                SetNeonState(true);
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            }
            
            isFlickering = false;
        }

        private IEnumerator PowerOutageSequence()
        {
            isPoweredOff = true;
            SetNeonState(false);
            
            Debug.Log($"Neon sign power outage: {gameObject.name}");
            
            yield return new WaitForSeconds(powerOutageDuration);
            
            isPoweredOff = false;
            SetNeonState(true);
            
            Debug.Log($"Neon sign power restored: {gameObject.name}");
        }

        private void SetNeonState(bool state)
        {
            bool actualState = state && isActive && !isPoweredOff;
            
            UpdateNeonIntensity(actualState ? baseIntensity : 0f);
            UpdateNeonEmission(actualState);
            UpdateAudio(actualState);
        }

        private void UpdateNeonIntensity(float intensity)
        {
            if (neonLights != null)
            {
                foreach (var light in neonLights)
                {
                    if (light != null)
                    {
                        light.intensity = intensity;
                    }
                }
            }
        }

        private void UpdateNeonColor(Color color)
        {
            if (neonLights != null)
            {
                foreach (var light in neonLights)
                {
                    if (light != null)
                    {
                        light.color = color;
                    }
                }
            }
            
            UpdateNeonEmission(IsActive, color);
        }

        private void UpdateNeonEmission(bool enabled, Color? customColor = null)
        {
            if (neonMaterials == null) return;

            Color emissionColor = customColor ?? baseColor;
            
            foreach (var material in neonMaterials)
            {
                if (material != null && material.HasProperty("_EmissionColor"))
                {
                    if (enabled)
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", emissionColor * baseIntensity);
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        private void UpdateAudio(bool enabled)
        {
            if (buzzAudioSource == null) return;

            if (enabled && !isPoweredOff)
            {
                if (!buzzAudioSource.isPlaying)
                {
                    buzzAudioSource.Play();
                }
            }
            else
            {
                if (buzzAudioSource.isPlaying)
                {
                    buzzAudioSource.Stop();
                }
            }
        }

        private void PlayFlickerSound()
        {
            if (buzzAudioSource != null && flickerSound != null)
            {
                buzzAudioSource.PlayOneShot(flickerSound, 0.5f);
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
            SetNeonState(active);
        }

        public void SetColor(Color color)
        {
            baseColor = color;
            originalColor = color;
            UpdateNeonColor(color);
        }

        public void SetIntensity(float intensity)
        {
            baseIntensity = intensity;
            originalIntensity = intensity;
            if (IsActive)
            {
                UpdateNeonIntensity(intensity);
            }
        }

        public void ForceFlicker()
        {
            if (!isPoweredOff)
            {
                StartFlicker();
            }
        }

        public void ForcePowerOutage(float duration = 0f)
        {
            if (duration > 0f)
            {
                powerOutageDuration = duration;
            }
            StartPowerOutage();
        }

        private void OnDisable()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
            }
            
            if (powerOutageCoroutine != null)
            {
                StopCoroutine(powerOutageCoroutine);
            }
        }
    }
}