using UnityEngine;
using System.Collections;

public class LightningManager : MonoBehaviour
{
    [SerializeField] private bool startLoopOnStart = false;
    
    [Header("Lightning Lights")]
    [SerializeField] private Light[] lightningLights;

    [Header("Random Timing")]
    [SerializeField] private float minDelay = 8f;
    [SerializeField] private float maxDelay = 25f;

    [Header("Flash Settings")]
    [SerializeField] private float flashIntensity = 8f;
    [SerializeField] private int minFlashes = 1;
    [SerializeField] private int maxFlashes = 3;

    [Header("Audio")]
    [SerializeField] private AudioClip[] thunderClips;
    [SerializeField] private float minThunderDelay = 0.6f;
    [SerializeField] private float maxThunderDelay = 2.5f;
        
    [Header("Extra Realism")]
    [SerializeField] private float minFlashDuration = 0.03f;
    [SerializeField] private float maxFlashDuration = 0.12f;
    [SerializeField] private float minGapBetweenFlashes = 0.04f;
    [SerializeField] private float maxGapBetweenFlashes = 0.18f;
    [SerializeField] private float mainFlashChance = 0.35f;
    [SerializeField] private float mainFlashMultiplier = 1.8f;
    [SerializeField] private float singleBigFlashChance = 0.25f;

    private Coroutine lightningLoopCoroutine;

    private void Start()
    {
        foreach (Light light in lightningLights)
        {
            if (light == null) continue;
            light.enabled = false;
        }

        if (startLoopOnStart)
            StartLoop();
    }

    public void StartLoop()
    {
        StopLoop();
        lightningLoopCoroutine = StartCoroutine(LightningLoop());
    }

    public void StopLoop()
    {
        if (lightningLoopCoroutine != null)
        {
            StopCoroutine(lightningLoopCoroutine);
            lightningLoopCoroutine = null;
        }
    }

    private IEnumerator LightningLoop()
    {
        while (true)
        {
            float wait = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(wait);

            yield return StartCoroutine(Strike());
        }
    }

    private IEnumerator Strike()
    {
        
        float strongestFlash = 0f;
        
        if (lightningLights.Length == 0) yield break;

        Light light = lightningLights[Random.Range(0, lightningLights.Length)];
        if (light == null) yield break;

        int flashCount = Random.Range(minFlashes, maxFlashes + 1);

        if (Random.value < singleBigFlashChance)
            flashCount = 1;

        for (int i = 0; i < flashCount; i++)
        {
            float duration = Random.Range(minFlashDuration, maxFlashDuration);
            float intensity = Random.Range(flashIntensity * 0.5f, flashIntensity);
            float gap = Random.Range(minGapBetweenFlashes, maxGapBetweenFlashes);

            bool mainFlash = i == 0 && Random.value < mainFlashChance;

            if (mainFlash)
            {
                intensity *= mainFlashMultiplier;
                duration *= 1.3f;
            }

            light.enabled = true;
            light.intensity = intensity;
            strongestFlash = Mathf.Max(strongestFlash, intensity);

            yield return new WaitForSeconds(duration);

            light.enabled = false;

            yield return new WaitForSeconds(gap);
        }

        StartCoroutine(PlayThunderDelayed(strongestFlash, light.transform.position));
    }

    private IEnumerator PlayThunderDelayed(float flashIntensity, Vector3 thunderPosition)
    {
        float normalized = Mathf.InverseLerp(
            flashIntensity * 0.5f,
            flashIntensity * mainFlashMultiplier,
            flashIntensity
        );

        float delay = Mathf.Lerp(maxThunderDelay, minThunderDelay, normalized);

        yield return new WaitForSeconds(delay);

        AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
        if (clip == null) yield break;

        float volume = Mathf.Lerp(0.5f, 1f, normalized);

        AudioManager.PlaySFX(clip, thunderPosition, volume);
    }

    public IEnumerator StrikeOnce()
    {
        yield return StartCoroutine(Strike());
    }
}