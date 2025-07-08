using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KowloonBreak.Effects
{
    public class CyberpunkEffectsManager : MonoBehaviour
    {
        public static CyberpunkEffectsManager Instance { get; private set; }

        [Header("Post Processing")]
        [SerializeField] private Volume globalVolume;
        [SerializeField] private VolumeProfile cyberpunkProfile;
        [SerializeField] private VolumeProfile normalProfile;
        [SerializeField] private float profileTransitionSpeed = 1f;

        [Header("Glitch Effects")]
        [SerializeField] private Material glitchMaterial;
        [SerializeField] private float glitchIntensity = 0.1f;
        [SerializeField] private float glitchFrequency = 0.05f;
        [SerializeField] private bool enableRandomGlitch = true;

        [Header("Scan Lines")]
        [SerializeField] private Material scanLineMaterial;
        [SerializeField] private float scanLineSpeed = 2f;
        [SerializeField] private float scanLineIntensity = 0.3f;
        [SerializeField] private bool enableScanLines = true;

        [Header("Chromatic Aberration")]
        [SerializeField] private float chromaticAberrationIntensity = 0.1f;
        [SerializeField] private bool enableChromaticAberration = true;

        [Header("Bloom & Glow")]
        [SerializeField] private float bloomIntensity = 1.5f;
        [SerializeField] private float bloomThreshold = 1.1f;
        [SerializeField] private bool enableBloom = true;

        [Header("Film Grain")]
        [SerializeField] private float filmGrainIntensity = 0.2f;
        [SerializeField] private bool enableFilmGrain = true;

        [Header("Color Grading")]
        [SerializeField] private Color tintColor = new Color(0.8f, 0.9f, 1.2f, 1f);
        [SerializeField] private float saturation = -20f;
        [SerializeField] private float contrast = 10f;

        [Header("Screen Effects")]
        [SerializeField] private UnityEngine.Camera effectCamera;
        [SerializeField] private RenderTexture screenTexture;

        // Post-processing components (requires URP package)
        // private Bloom bloomComponent;
        // private ChromaticAberration chromaticAberrationComponent;
        // private FilmGrain filmGrainComponent;
        // private ColorAdjustments colorAdjustmentsComponent;
        private Coroutine glitchCoroutine;
        private bool isGlitching;

        public bool IsEffectsEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEffects();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupPostProcessing();
            StartGlitchSystem();
        }

        private void Update()
        {
            UpdateScanLines();
            UpdateRandomGlitch();
        }

        private void InitializeEffects()
        {
            if (effectCamera == null)
            {
                effectCamera = UnityEngine.Camera.main;
            }

            if (globalVolume == null)
            {
                globalVolume = FindObjectOfType<Volume>();
            }

            CreateScreenTexture();
            
            Debug.Log("Cyberpunk Effects Manager Initialized");
        }

        private void CreateScreenTexture()
        {
            if (screenTexture == null)
            {
                screenTexture = new RenderTexture(Screen.width, Screen.height, 24);
                screenTexture.name = "CyberpunkScreenTexture";
            }
        }

        private void SetupPostProcessing()
        {
            if (globalVolume == null || globalVolume.profile == null) return;

            // TODO: Setup post-processing components when URP is available
            // globalVolume.profile.TryGet(out bloomComponent);
            // globalVolume.profile.TryGet(out chromaticAberrationComponent);
            // globalVolume.profile.TryGet(out filmGrainComponent);
            // globalVolume.profile.TryGet(out colorAdjustmentsComponent);

            ApplyInitialSettings();
        }

        private void ApplyInitialSettings()
        {
            SetBloomSettings();
            SetChromaticAberrationSettings();
            SetFilmGrainSettings();
            SetColorGradingSettings();
        }

        private void SetBloomSettings()
        {
            // TODO: Implement when URP is available
            // if (bloomComponent == null) return;
            // bloomComponent.active = enableBloom;
            // if (enableBloom)
            // {
            //     bloomComponent.intensity.value = bloomIntensity;
            //     bloomComponent.threshold.value = bloomThreshold;
            // }
        }

        private void SetChromaticAberrationSettings()
        {
            // TODO: Implement when URP is available
            // if (chromaticAberrationComponent == null) return;
            // chromaticAberrationComponent.active = enableChromaticAberration;
            // if (enableChromaticAberration)
            // {
            //     chromaticAberrationComponent.intensity.value = chromaticAberrationIntensity;
            // }
        }

        private void SetFilmGrainSettings()
        {
            // TODO: Implement when URP is available
            // if (filmGrainComponent == null) return;
            // filmGrainComponent.active = enableFilmGrain;
            // if (enableFilmGrain)
            // {
            //     filmGrainComponent.intensity.value = filmGrainIntensity;
            //     filmGrainComponent.type.value = FilmGrainLookup.Medium1;
            // }
        }

        private void SetColorGradingSettings()
        {
            // TODO: Implement when URP is available
            // if (colorAdjustmentsComponent == null) return;
            // colorAdjustmentsComponent.colorFilter.value = tintColor;
            // colorAdjustmentsComponent.saturation.value = saturation;
            // colorAdjustmentsComponent.contrast.value = contrast;
        }

        private void StartGlitchSystem()
        {
            if (enableRandomGlitch && glitchCoroutine == null)
            {
                glitchCoroutine = StartCoroutine(RandomGlitchRoutine());
            }
        }

        private void UpdateScanLines()
        {
            if (!enableScanLines || scanLineMaterial == null) return;

            float scanLineOffset = Time.time * scanLineSpeed;
            scanLineMaterial.SetFloat("_ScanLineOffset", scanLineOffset % 1f);
            scanLineMaterial.SetFloat("_ScanLineIntensity", scanLineIntensity);
        }

        private void UpdateRandomGlitch()
        {
            if (!enableRandomGlitch || isGlitching) return;

            if (Random.Range(0f, 1f) < glitchFrequency * Time.deltaTime)
            {
                TriggerGlitchEffect(Random.Range(0.1f, 0.5f));
            }
        }

        private IEnumerator RandomGlitchRoutine()
        {
            while (enableRandomGlitch)
            {
                yield return new WaitForSeconds(Random.Range(5f, 15f));
                
                if (!isGlitching)
                {
                    TriggerGlitchEffect(Random.Range(0.2f, 0.8f));
                }
            }
        }

        public void TriggerGlitchEffect(float duration = 0.3f)
        {
            if (isGlitching) return;

            StartCoroutine(GlitchEffectCoroutine(duration));
        }

        private IEnumerator GlitchEffectCoroutine(float duration)
        {
            isGlitching = true;
            
            // TODO: Implement when URP is available
            // float originalChromaticIntensity = chromaticAberrationComponent?.intensity.value ?? 0f;
            // float glitchChromaticIntensity = originalChromaticIntensity + 0.5f;
            
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_GlitchIntensity", glitchIntensity);
            }
            
            // if (chromaticAberrationComponent != null)
            // {
            //     chromaticAberrationComponent.intensity.value = glitchChromaticIntensity;
            // }
            
            yield return new WaitForSeconds(duration);
            
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_GlitchIntensity", 0f);
            }
            
            // if (chromaticAberrationComponent != null)
            // {
            //     chromaticAberrationComponent.intensity.value = originalChromaticIntensity;
            // }
            
            isGlitching = false;
        }

        public void SetNeonBoost(bool enabled)
        {
            // TODO: Implement when URP is available
            // if (bloomComponent != null)
            // {
            //     float targetIntensity = enabled ? bloomIntensity * 1.5f : bloomIntensity;
            //     StartCoroutine(LerpBloomIntensity(targetIntensity, 1f));
            // }
        }

        private IEnumerator LerpBloomIntensity(float targetIntensity, float duration)
        {
            // TODO: Implement when URP is available
            // if (bloomComponent == null) yield break;
            // 
            // float startIntensity = bloomComponent.intensity.value;
            // float elapsedTime = 0f;
            // 
            // while (elapsedTime < duration)
            // {
            //     elapsedTime += Time.deltaTime;
            //     float t = elapsedTime / duration;
            //     bloomComponent.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            //     yield return null;
            // }
            // 
            // bloomComponent.intensity.value = targetIntensity;
            yield break;
        }

        public void SetCyberpunkMode(bool enabled)
        {
            if (globalVolume == null) return;

            VolumeProfile targetProfile = enabled ? cyberpunkProfile : normalProfile;
            if (targetProfile != null)
            {
                StartCoroutine(TransitionToProfile(targetProfile));
            }
        }

        private IEnumerator TransitionToProfile(VolumeProfile targetProfile)
        {
            float transitionTime = 0f;
            VolumeProfile startProfile = globalVolume.profile;
            
            while (transitionTime < 1f)
            {
                transitionTime += Time.deltaTime * profileTransitionSpeed;
                globalVolume.weight = Mathf.Lerp(1f, 0f, transitionTime * 0.5f);
                yield return null;
            }
            
            globalVolume.profile = targetProfile;
            
            transitionTime = 0f;
            while (transitionTime < 1f)
            {
                transitionTime += Time.deltaTime * profileTransitionSpeed;
                globalVolume.weight = Mathf.Lerp(0f, 1f, transitionTime);
                yield return null;
            }
        }

        public void SetScreenDistortion(float intensity)
        {
            if (glitchMaterial != null)
            {
                glitchMaterial.SetFloat("_DistortionIntensity", intensity);
            }
        }

        public void EnableEffects(bool enable)
        {
            IsEffectsEnabled = enable;
            
            if (globalVolume != null)
            {
                globalVolume.enabled = enable;
            }
            
            enableScanLines = enable;
            enableRandomGlitch = enable;
            
            if (!enable && glitchCoroutine != null)
            {
                StopCoroutine(glitchCoroutine);
                glitchCoroutine = null;
            }
            else if (enable && glitchCoroutine == null)
            {
                StartGlitchSystem();
            }
        }

        public void SetEmergencyMode(bool enabled)
        {
            if (enabled)
            {
                TriggerGlitchEffect(2f);
                SetNeonBoost(true);
                
                // TODO: Implement when URP is available
                // if (colorAdjustmentsComponent != null)
                // {
                //     colorAdjustmentsComponent.colorFilter.value = Color.red;
                // }
            }
            else
            {
                SetNeonBoost(false);
                
                // TODO: Implement when URP is available
                // if (colorAdjustmentsComponent != null)
                // {
                //     colorAdjustmentsComponent.colorFilter.value = tintColor;
                // }
            }
        }

        public void SetInfectionVisuals(float infectionLevel)
        {
            // TODO: Implement when URP is available
            // if (chromaticAberrationComponent != null)
            // {
            //     chromaticAberrationComponent.intensity.value = chromaticAberrationIntensity + (infectionLevel * 0.3f);
            // }
            // 
            // if (colorAdjustmentsComponent != null)
            // {
            //     Color infectionTint = Color.Lerp(tintColor, Color.green, infectionLevel * 0.5f);
            //     colorAdjustmentsComponent.colorFilter.value = infectionTint;
            // }
        }

        private void OnDestroy()
        {
            if (screenTexture != null)
            {
                screenTexture.Release();
            }
            
            if (glitchCoroutine != null)
            {
                StopCoroutine(glitchCoroutine);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!IsEffectsEnabled)
            {
                Graphics.Blit(source, destination);
                return;
            }
            
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height);
            
            if (enableScanLines && scanLineMaterial != null)
            {
                Graphics.Blit(source, temp, scanLineMaterial);
            }
            else
            {
                Graphics.Blit(source, temp);
            }
            
            if (isGlitching && glitchMaterial != null)
            {
                Graphics.Blit(temp, destination, glitchMaterial);
            }
            else
            {
                Graphics.Blit(temp, destination);
            }
            
            RenderTexture.ReleaseTemporary(temp);
        }
    }
}