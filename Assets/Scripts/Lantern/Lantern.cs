using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lantern : MonoBehaviour, IResettable
{
    [Header("References")]
    [SerializeField] private Light lanternLight;

    [Header("Battery")]
    [SerializeField] private int maxBatteries = 3;
    [SerializeField] private float batteryDuration = 30f;
    [SerializeField] private float lowBatteryThreshold = 0.35f;
    [SerializeField] private float criticalBatteryThreshold = 0.15f;

    [Header("Light Output")]
    [SerializeField] private float normalIntensity = 400f;
    [SerializeField] private float lowBatteryIntensity = 300f;
    [SerializeField] private float criticalBatteryIntensity = 200f;
    [SerializeField] private float emptyIntensity = 8f;

    [Header("Flicker Episodes")]
    [SerializeField] private float lowBatteryEpisodeChancePerSecond = 0.15f;
    [SerializeField] private float criticalEpisodeChancePerSecond = 0.8f;
    [SerializeField] private Vector2 episodeDurationRange = new Vector2(0.15f, 0.6f);

    [Header("Dip Behavior")]
    [SerializeField] private Vector2 dipIntervalRange = new Vector2(0.02f, 0.08f);
    [SerializeField] private Vector2 dipIntensityMultiplierRange = new Vector2(0.05f, 0.6f);

    [Header("Idle Instability")]
    [SerializeField] private float lowBatteryNoiseAmount = 0.03f;
    [SerializeField] private float criticalNoiseAmount = 0.08f;
    [SerializeField] private float noiseSpeed = 14f;

    [Header("Dust")]
    [SerializeField] private ParticleSystem lanternDust;

    [Header("Audio")]
    [SerializeField] private AudioClip[] turnOnSounds;
    [SerializeField] private AudioClip[] turnOffSounds;

    [Header("Flicker Audio")]
    [SerializeField] private AudioClip[] crackleClips;
    [SerializeField] private float flickerSoundChance = 0.5f;
    [SerializeField] private float flickerSoundCooldown = 0.05f;
    [SerializeField] private int maxSimultaneousCrackles = 2;

    public bool InputLocked { get; set; }
    public bool IsOn { get; private set; }

    public event Action OnLanternTurnedOff;
    public event Action OnLanternTurnedOn;

    private int currentBatteries;
    private float currentBatteryTime;

    private float baseIntensity;
    private float currentMultiplier = 1f;
    private float targetMultiplier = 1f;

    private bool inEpisode;
    private float episodeTimer;
    private float dipTimer;

    private float noiseSeed;
    private float nextFlickerSoundTime;
    private int activeCrackles;

    private void Start()
    {
        noiseSeed = Random.Range(0f, 1000f);

        if (InputManager.Instance != null)
            InputManager.Instance.OnClickPressed += OnClick;

        ResetState();
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnClickPressed -= OnClick;
    }

    private void Update()
    {
        if (!IsOn) return;

        UpdateBattery();
        if (!IsOn) return;

        float battery01 = GetBattery01();

        UpdateBaseIntensity(battery01);
        UpdateEpisodeState(battery01);
        UpdateNoise(battery01);
        ApplyLight();
    }

    public void ResetState()
    {
        StopAllCoroutines();

        currentBatteries = maxBatteries;
        currentBatteryTime = batteryDuration;

        activeCrackles = 0;
        nextFlickerSoundTime = 0f;

        baseIntensity = normalIntensity;
        currentMultiplier = 1f;
        targetMultiplier = 1f;

        inEpisode = false;
        episodeTimer = 0f;
        dipTimer = 0f;

        if (IsOn)
        {
            IsOn = false;
            OnLanternTurnedOff?.Invoke();
        }
        else
        {
            IsOn = false;
        }

        if (lanternLight != null)
        {
            lanternLight.intensity = 0f;
            lanternLight.enabled = false;
        }

        if (lanternDust != null)
            lanternDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnClick()
    {
        ToggleOnOff();
    }

    public void ToggleOnOff()
    {
        if (InputLocked) return;

        if (IsOn)
            TurnOff();
        else if (currentBatteries > 0 && currentBatteryTime > 0f)
            TurnOn();
    }

    public void ForceOff(bool silent = true)
    {
        if (IsOn)
            TurnOff(silent);
    }

    private void TurnOff(bool silent = false)
    {
        IsOn = false;
        OnLanternTurnedOff?.Invoke();

        if (!silent && turnOffSounds != null && turnOffSounds.Length > 0)
            AudioManager.PlaySFX(turnOffSounds, transform.position);

        if (lanternLight != null)
        {
            lanternLight.intensity = 0f;
            lanternLight.enabled = false;
        }

        if (lanternDust != null)
            lanternDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void TurnOn()
    {
        IsOn = true;
        OnLanternTurnedOn?.Invoke();

        if (lanternLight != null)
        {
            lanternLight.enabled = true;
            lanternLight.intensity = normalIntensity;
        }

        if (turnOnSounds != null && turnOnSounds.Length > 0)
            AudioManager.PlaySFX(turnOnSounds, transform.position);

        if (lanternDust != null)
            lanternDust.Play();
    }

    private void UpdateBattery()
    {
        if (currentBatteries <= 0) return;

        currentBatteryTime -= Time.deltaTime;

        while (currentBatteryTime <= 0f && currentBatteries > 0)
        {
            currentBatteries--;

            if (currentBatteries > 0)
                currentBatteryTime += batteryDuration;
            else
            {
                currentBatteryTime = 0f;

                if (IsOn)
                    TurnOff(true);

                break;
            }
        }
    }

    private float GetBattery01()
    {
        float totalTime = maxBatteries * batteryDuration;
        float remainingTime = Mathf.Max(
            0f,
            (currentBatteries - 1) * batteryDuration + currentBatteryTime
        );

        if (currentBatteries <= 0)
            remainingTime = 0f;

        return totalTime > 0f ? remainingTime / totalTime : 0f;
    }

    private void UpdateBaseIntensity(float battery01)
    {
        if (battery01 > lowBatteryThreshold)
        {
            baseIntensity = normalIntensity;
            return;
        }

        if (battery01 > criticalBatteryThreshold)
        {
            float t = Mathf.InverseLerp(lowBatteryThreshold, criticalBatteryThreshold, battery01);
            baseIntensity = Mathf.Lerp(lowBatteryIntensity, normalIntensity, t);
            return;
        }

        float ct = Mathf.InverseLerp(criticalBatteryThreshold, 0f, battery01);
        baseIntensity = Mathf.Lerp(criticalBatteryIntensity, emptyIntensity, ct);
    }

    private void UpdateEpisodeState(float battery01)
    {
        if (battery01 > lowBatteryThreshold)
        {
            inEpisode = false;
            targetMultiplier = 1f;
            currentMultiplier = Mathf.Lerp(currentMultiplier, 1f, 12f * Time.deltaTime);
            return;
        }

        float episodeChance = battery01 > criticalBatteryThreshold
            ? lowBatteryEpisodeChancePerSecond
            : criticalEpisodeChancePerSecond;

        if (!inEpisode)
        {
            if (Random.value < episodeChance * Time.deltaTime)
            {
                inEpisode = true;
                episodeTimer = Random.Range(episodeDurationRange.x, episodeDurationRange.y);
                dipTimer = 0f;
            }

            targetMultiplier = 1f;
        }
        else
        {
            episodeTimer -= Time.deltaTime;
            dipTimer -= Time.deltaTime;

            if (dipTimer <= 0f)
            {
                targetMultiplier = Random.Range(
                    dipIntensityMultiplierRange.x,
                    dipIntensityMultiplierRange.y
                );

                dipTimer = Random.Range(dipIntervalRange.x, dipIntervalRange.y);
                TryPlayFlickerSound();
            }

            if (episodeTimer <= 0f)
            {
                inEpisode = false;
                targetMultiplier = 1f;
            }
        }

        float responseSpeed = inEpisode ? 28f : 10f;
        currentMultiplier = Mathf.Lerp(currentMultiplier, targetMultiplier, responseSpeed * Time.deltaTime);
    }

    private void TryPlayFlickerSound()
    {
        if (crackleClips == null || crackleClips.Length == 0) return;
        if (Time.time < nextFlickerSoundTime) return;
        if (Random.value > flickerSoundChance) return;
        if (activeCrackles >= maxSimultaneousCrackles) return;

        AudioClip clip = crackleClips[Random.Range(0, crackleClips.Length)];

        float dipStrength = 1f - targetMultiplier;
        float volume = Mathf.Lerp(0.1f, 0.6f, dipStrength);

        AudioManager.PlaySFX(clip, transform.position, volume);

        activeCrackles++;
        StartCoroutine(CrackleLifetime(clip.length));

        nextFlickerSoundTime = Time.time + flickerSoundCooldown;
    }

    private IEnumerator CrackleLifetime(float duration)
    {
        yield return new WaitForSeconds(duration);
        activeCrackles = Mathf.Max(0, activeCrackles - 1);
    }

    private void UpdateNoise(float battery01)
    {
        if (battery01 > lowBatteryThreshold) return;

        float noiseAmount = battery01 > criticalBatteryThreshold
            ? lowBatteryNoiseAmount
            : criticalNoiseAmount;

        float noise = Mathf.PerlinNoise(noiseSeed, Time.time * noiseSpeed);
        noise = (noise - 0.5f) * 2f;

        currentMultiplier += noise * noiseAmount;
        currentMultiplier = Mathf.Clamp(currentMultiplier, 0f, 1.1f);
    }

    private void ApplyLight()
    {
        if (lanternLight != null)
            lanternLight.intensity = baseIntensity * currentMultiplier;
    }

    public void AddBattery(int amount)
    {
        if (amount <= 0) return;

        float totalBatteryTime =
            Mathf.Max(0f, (currentBatteries - 1) * batteryDuration + currentBatteryTime);

        totalBatteryTime += amount * batteryDuration;

        float maxTotalBatteryTime = maxBatteries * batteryDuration;
        totalBatteryTime = Mathf.Clamp(totalBatteryTime, 0f, maxTotalBatteryTime);

        currentBatteries = Mathf.CeilToInt(totalBatteryTime / batteryDuration);

        if (currentBatteries <= 0)
        {
            currentBatteries = 0;
            currentBatteryTime = 0f;
            return;
        }

        currentBatteryTime = totalBatteryTime - ((currentBatteries - 1) * batteryDuration);

        if (currentBatteryTime <= 0f)
            currentBatteryTime = batteryDuration;
    }

    public int GetCurrentBatteries() => currentBatteries;

    public float GetCurrentBatteryTime01()
        => batteryDuration > 0f ? currentBatteryTime / batteryDuration : 0f;

    public float GetTotalBattery01() => GetBattery01();
}