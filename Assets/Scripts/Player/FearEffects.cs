using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.Cinemachine;

public class FearEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private Volume volume;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Fear Blend")]
    [SerializeField] private float fearRiseSpeed = 2.5f;
    [SerializeField] private float fearFallSpeed = 1.2f;
    [SerializeField] private float maxFear = 1f;

    [Header("First Look Impact")]
    [SerializeField] private float impactZoomDuration = 0.18f;
    [SerializeField] private float impactFovOffset = -6f;
    [SerializeField] private AnimationCurve impactCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float impactCooldown = 0.75f;

    [Header("Fear Offsets")]
    [SerializeField] private float vignetteExtra = 0.18f;
    [SerializeField] private float chromaExtra = 0.25f;
    [SerializeField] private float distortionExtra = -0.15f;
    [SerializeField] private float grainExtra = 0.25f;
    [SerializeField] private float saturationExtra = -20f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.03f;

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private FilmGrain filmGrain;
    private ColorAdjustments colorAdjustments;

    private float baseVignette;
    private float baseChroma;
    private float baseDistortion;
    private float baseGrain;
    private float baseSaturation;
    private float baseFov;

    private float fear;
    private bool wasLookingAtMonster;
    private float lastImpactTime = -999f;

    private Coroutine zoomRoutine;

    private void Awake()
    {
        CacheBaseValues();
    }

    private void Update()
    {
        bool lookingAtMonster = playerInteract != null && playerInteract.IsLookingAtMonster();

        if (lookingAtMonster && !wasLookingAtMonster && Time.time >= lastImpactTime + impactCooldown)
        {
            lastImpactTime = Time.time;
            TriggerImpact();
        }

        float targetFear = lookingAtMonster ? maxFear : 0f;
        float speed = lookingAtMonster ? fearRiseSpeed : fearFallSpeed;
        fear = Mathf.MoveTowards(fear, targetFear, speed * Time.deltaTime);

        ApplyFearToVolume();

        wasLookingAtMonster = lookingAtMonster;
    }

    private void CacheBaseValues()
    {
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
            volume.profile.TryGet(out chromaticAberration);
            volume.profile.TryGet(out lensDistortion);
            volume.profile.TryGet(out filmGrain);
            volume.profile.TryGet(out colorAdjustments);

            if (vignette != null)
            {
                vignette.active = true;
                baseVignette = vignette.intensity.value;
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.active = true;
                baseChroma = chromaticAberration.intensity.value;
            }

            if (lensDistortion != null)
            {
                lensDistortion.active = true;
                baseDistortion = lensDistortion.intensity.value;
            }

            if (filmGrain != null)
            {
                filmGrain.active = true;
                baseGrain = filmGrain.intensity.value;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                baseSaturation = colorAdjustments.saturation.value;
            }
        }

        if (cinemachineCamera != null)
            baseFov = cinemachineCamera.Lens.FieldOfView;
    }

    private void ApplyFearToVolume()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
        float fearPulse = Mathf.Clamp01(fear + pulse * fear);

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = baseVignette + vignetteExtra * fearPulse;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = baseChroma + chromaExtra * fearPulse;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = baseDistortion + distortionExtra * fearPulse;
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = baseGrain + grainExtra * fearPulse;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = baseSaturation + saturationExtra * fearPulse;
        }
    }

    private void TriggerImpact()
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulse();

        if (cinemachineCamera != null)
        {
            if (zoomRoutine != null)
                StopCoroutine(zoomRoutine);

            zoomRoutine = StartCoroutine(DoImpactZoom());
        }
    }

    private IEnumerator DoImpactZoom()
    {
        float startFov = cinemachineCamera.Lens.FieldOfView;
        float zoomedFov = baseFov + impactFovOffset;
        float half = impactZoomDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = impactCurve.Evaluate(Mathf.Clamp01(t / half));

            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(startFov, zoomedFov, k);
            cinemachineCamera.Lens = lens;

            yield return null;
        }

        t = 0f;
        float returnStart = cinemachineCamera.Lens.FieldOfView;

        while (t < half)
        {
            t += Time.deltaTime;
            float k = impactCurve.Evaluate(Mathf.Clamp01(t / half));

            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(returnStart, baseFov, k);
            cinemachineCamera.Lens = lens;

            yield return null;
        }

        var finalLens = cinemachineCamera.Lens;
        finalLens.FieldOfView = baseFov;
        cinemachineCamera.Lens = finalLens;

        zoomRoutine = null;
    }

    public void RefreshBaseFromCurrentProfile()
    {
        CacheBaseValues();
    }
}