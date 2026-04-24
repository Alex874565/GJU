using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

public class GeneratorInteractable : MonoBehaviour, IInteractable
{
    [Header("Visual")]
    [SerializeField] private Light indicatorLight;
    [SerializeField] private float blinkSpeed = 3f;

    [Header("Lights To Activate")]
    [SerializeField] private Light[] houseLights;
    [SerializeField] private float delayBetweenLights = 0.25f;
    [SerializeField] private float lightFadeDuration = 0.6f;
    [SerializeField] private float targetIntensity = 1.5f;

    [Header("HDRP Post Processing")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float postProcessFadeDuration = 2f;
    [SerializeField] private float targetVignette = 0.15f;
    [SerializeField] private float targetSaturation = 0f;
    [SerializeField] private float targetContrast = 0f;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueToPlay;
    [SerializeField] private GameObject[] dialogueTriggersToEnable;

    [Header("Player")]
    [SerializeField] private PlayerManager playerManager;

    private bool isHighlighted;
    private bool activated;

    private void Start()
    {
        foreach (Light light in houseLights)
        {
            if (light == null) continue;
            light.enabled = false;
            light.intensity = 0f;
        }
    }

    private void Update()
    {
        if (indicatorLight == null) return;

        if (activated)
        {
            indicatorLight.enabled = true;
            return;
        }

        indicatorLight.enabled = isHighlighted || Mathf.Sin(Time.time * blinkSpeed) > 0f;
    }

    public void ChangeHighlight(bool highlighted)
    {
        if (activated) return;
        isHighlighted = highlighted;
    }

    public void Interact(PlayerInteract player)
    {
        if (activated) return;

        activated = true;

        if (indicatorLight != null)
            indicatorLight.enabled = true;

        StartCoroutine(ActivateGeneratorRoutine());
        StartCoroutine(FadePostProcessing());
    }

    private IEnumerator ActivateGeneratorRoutine()
    {
        if (playerManager != null)
            playerManager.SetLightsOff(false);

        foreach (Light light in houseLights)
        {
            if (light == null) continue;

            StartCoroutine(FadeLightOn(light));
            yield return new WaitForSeconds(delayBetweenLights);
        }

        yield return new WaitForSeconds(lightFadeDuration);

        if (dialogueToPlay != null)
            dialogueToPlay.SetActive(true);

        foreach (GameObject trigger in dialogueTriggersToEnable)
        {
            if (trigger != null)
                trigger.SetActive(true);
        }
    }

    private IEnumerator FadeLightOn(Light light)
    {
        light.enabled = true;

        float timer = 0f;
        float startIntensity = 0f;

        light.intensity = startIntensity;

        while (timer < lightFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / lightFadeDuration);

            light.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

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
}