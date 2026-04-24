using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class GeneratorInteractable : MonoBehaviour, IInteractable, IResettable
{
    [Header("Indicator Light")]
    [SerializeField] private Light indicatorLight;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float minIndicatorIntensity = 0.2f;
    [SerializeField] private float maxIndicatorIntensity = 1.5f;
    [SerializeField] private float indicatorSmoothSpeed = 8f;

    [Header("Activation Surge")]
    [SerializeField] private float activationFlickerDuration = 1f;
    [SerializeField] private Vector2 activationFlickerInterval = new Vector2(0.04f, 0.12f);
    [SerializeField] private float activationSurgeIntensity = 3f;

    [Header("House Lights")]
    [SerializeField] private Light[] houseLights;
    [SerializeField] private float delayBetweenLights = 0.25f;
    [SerializeField] private float randomDelayVariation = 0.15f;
    [SerializeField] private float lightFadeDuration = 0.6f;
    [SerializeField] private float targetIntensity = 1.5f;
    [SerializeField] private float lightOvershootMultiplier = 1.25f;

    [Header("HDRP Post Processing")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float postProcessFadeDuration = 2f;
    [SerializeField] private float targetVignette = 0.15f;
    [SerializeField] private float targetSaturation = 0f;
    [SerializeField] private float targetContrast = 0f;

    [Header("Audio / Particles")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activateSound;
    [SerializeField] private AudioClip humLoop;
    [SerializeField] private ParticleSystem sparks;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueToPlay;
    [SerializeField] private GameObject[] dialogueTriggersToEnable;

    [Header("Other")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    
    private bool isHighlighted;
    private bool activated;
    private Coroutine activationRoutine;

    private void Start()
    {
        ResetState();
    }

    private void Update()
    {
        if (indicatorLight == null || activated) return;

        indicatorLight.enabled = true;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

// stay low longer, quick spike
        pulse = Mathf.Pow(pulse, 4f);
        pulse += Random.Range(-0.05f, 0.05f);
        pulse = Mathf.Clamp01(pulse);

        float pulseIntensity = Mathf.Lerp(minIndicatorIntensity, maxIndicatorIntensity, pulse);
        float target = isHighlighted ? maxIndicatorIntensity : pulseIntensity;

        indicatorLight.intensity = Mathf.Lerp(
            indicatorLight.intensity,
            target,
            1f - Mathf.Exp(-indicatorSmoothSpeed * Time.deltaTime)
        );
    }

    public void ChangeHighlight(bool highlighted)
    {
        if (activated) return;
        isHighlighted = highlighted;
        if (highlighted)
        {
            InteractPrompt.Instance?.Show("Activate");
        }
        else
        {
            InteractPrompt.Instance?.Hide();
        }
    }

    public void Interact(PlayerInteract player)
    {
        if (activated) return;

        activated = true;

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        activationRoutine = StartCoroutine(ActivateGeneratorRoutine());
    }

    private IEnumerator ActivateGeneratorRoutine()
    {
        if (sparks != null)
            sparks.Play();
        
        yield return StartCoroutine(ActivationIndicatorFlicker());
        
        if (activateSound != null)
        {
            AudioManager.PlaySFX(activateSound, transform.position);
            yield return new WaitForSeconds(activateSound.length - 2f);
        }

        if (playerManager != null)
            playerManager.SetLightsOff(false);

        if (audioSource != null && humLoop != null)
        {
            audioSource.clip = humLoop;
            audioSource.loop = true;
            audioSource.Play();
        }

        StartCoroutine(FadePostProcessing());

        foreach (Light light in houseLights)
        {
            if (light == null) continue;

            StartCoroutine(FadeLightOn(light));

            float delay = delayBetweenLights + Random.Range(-randomDelayVariation, randomDelayVariation);
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
        }

        yield return new WaitForSeconds(lightFadeDuration);

        if (dialogueToPlay != null)
            dialogueToPlay.SetActive(true);

        foreach (GameObject trigger in dialogueTriggersToEnable)
        {
            if (trigger != null)
                trigger.SetActive(true);
        }
        
        if (gameManager != null)
            gameManager.ActivateDefaultEnvironment();
    }

    private IEnumerator ActivationIndicatorFlicker()
    {
        if (indicatorLight == null) yield break;

        float timer = 0f;

        while (timer < activationFlickerDuration)
        {
            float intensity = Random.value > 0.35f ? activationSurgeIntensity : minIndicatorIntensity;

            indicatorLight.enabled = true;
            indicatorLight.intensity = intensity;

            float wait = Random.Range(activationFlickerInterval.x, activationFlickerInterval.y);
            timer += wait;

            yield return new WaitForSeconds(wait);
        }

        indicatorLight.enabled = true;
        indicatorLight.intensity = maxIndicatorIntensity;
    }

    private IEnumerator FadeLightOn(Light light)
    {
        light.enabled = true;

        float timer = 0f;
        float overshoot = targetIntensity * lightOvershootMultiplier;

        while (timer < lightFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / lightFadeDuration;

            float intensity;

            if (t < 0.75f)
            {
                float riseT = Mathf.SmoothStep(0f, 1f, t / 0.75f);
                intensity = Mathf.Lerp(0f, overshoot, riseT);
            }
            else
            {
                float settleT = Mathf.SmoothStep(0f, 1f, (t - 0.75f) / 0.25f);
                intensity = Mathf.Lerp(overshoot, targetIntensity, settleT);
            }

            light.intensity = intensity;
            yield return null;
        }

        light.intensity = targetIntensity;
    }

    private IEnumerator FadePostProcessing()
    {
        if (globalVolume == null || globalVolume.profile == null)
            yield break;

        globalVolume.profile.TryGet(out Vignette vignette);
        globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments);

        float startVignette = vignette != null ? vignette.intensity.value : 0f;
        float startSaturation = colorAdjustments != null ? colorAdjustments.saturation.value : 0f;
        float startContrast = colorAdjustments != null ? colorAdjustments.contrast.value : 0f;

        float timer = 0f;

        while (timer < postProcessFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / postProcessFadeDuration);

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, t);

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);
                colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetContrast, t);
            }

            yield return null;
        }
    }

    public void ResetState()
    {
        activated = false;
        isHighlighted = false;

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        if (indicatorLight != null)
        {
            indicatorLight.enabled = true;
            indicatorLight.intensity = minIndicatorIntensity;
        }

        foreach (Light light in houseLights)
        {
            if (light == null) continue;

            light.enabled = false;
            light.intensity = 0f;
        }

        if (dialogueToPlay != null)
            dialogueToPlay.SetActive(false);

        foreach (GameObject trigger in dialogueTriggersToEnable)
        {
            if (trigger != null)
                trigger.SetActive(false);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }

        if (sparks != null)
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (playerManager != null)
            playerManager.SetLightsOff(true);
    }
}