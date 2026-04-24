using UnityEngine;
using System.Collections;

public class LightningManager : MonoBehaviour
{
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
    [SerializeField] private AudioSource thunderAudio;
    [SerializeField] private AudioClip[] thunderClips;
    [SerializeField] private float thunderDelay = 1.2f;
        
    [Header("Extra Realism")]
    [SerializeField] private float minFlashDuration = 0.03f;
    [SerializeField] private float maxFlashDuration = 0.12f;
    [SerializeField] private float minGapBetweenFlashes = 0.04f;
    [SerializeField] private float maxGapBetweenFlashes = 0.18f;
    [SerializeField] private float mainFlashChance = 0.35f;
    [SerializeField] private float mainFlashMultiplier = 1.8f;
    [SerializeField] private float singleBigFlashChance = 0.25f;

    private void Start()
    {
        foreach (Light light in lightningLights)
        {
            if (light == null) continue;
            light.enabled = false;
        }

        StartCoroutine(LightningLoop());
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

            yield return new WaitForSeconds(duration);

            light.enabled = false;

            yield return new WaitForSeconds(gap);
        }

        if (thunderAudio != null && thunderClips.Length > 0)
            StartCoroutine(PlayThunderDelayed());
    }

    private IEnumerator PlayThunderDelayed()
    {
        yield return new WaitForSeconds(thunderDelay);

        thunderAudio.clip = thunderClips[Random.Range(0, thunderClips.Length)];
        thunderAudio.Play();
    }
}