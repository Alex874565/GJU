using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lantern : MonoBehaviour
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

    private int currentBatteries;
    private float currentBatteryTime;

    private float baseIntensity;
    private float currentMultiplier = 1f;

    private bool inEpisode;
    private float episodeTimer;
    private float dipTimer;
    private float targetMultiplier = 1f;

    private float noiseSeed;
    
    public bool IsOn { get; private set; }

    public event Action OnLanternTurnedOff;
    public event Action OnLanternTurnedOn;

    private void Start()
    {
        noiseSeed = Random.Range(0f, 1000f);

        currentBatteries = maxBatteries;
        currentBatteryTime = batteryDuration;
    }

    private void Update()
    {
        if(!IsOn)
            return;
        
        UpdateBattery();

        float battery01 = GetBattery01();

        UpdateBaseIntensity(battery01);
        UpdateEpisodeState(battery01);
        UpdateNoise(battery01);
        ApplyLight();
    }

    private void UpdateBattery()
    {
        if (currentBatteries <= 0)
            return;

        currentBatteryTime -= Time.deltaTime;

        while (currentBatteryTime <= 0f && currentBatteries > 0)
        {
            currentBatteries--;

            if (currentBatteries > 0)
            {
                currentBatteryTime += batteryDuration;
            }
            else
            {
                OnLanternTurnedOff?.Invoke();
                currentBatteryTime = 0f;
                break;
            }
        }
    }

    private float GetBattery01()
    {
        float totalTime = maxBatteries * batteryDuration;
        float remainingTime = Mathf.Max(0, (currentBatteries - 1) * batteryDuration + currentBatteryTime);

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
            currentMultiplier = Mathf.Lerp(currentMultiplier, 1f, 12f * Time.deltaTime);
            targetMultiplier = 1f;
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

    private void UpdateNoise(float battery01)
    {
        if (battery01 > lowBatteryThreshold)
            return;

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
        lanternLight.intensity = baseIntensity * currentMultiplier;
    }

    public void AddBattery(int amount)
    {
        if (amount <= 0)
            return;

        currentBatteries = Mathf.Clamp(currentBatteries + amount, 0, maxBatteries);

        if (currentBatteries > 0 && currentBatteryTime <= 0f)
            currentBatteryTime = batteryDuration;
    }

    public int GetCurrentBatteries()
    {
        return currentBatteries;
    }

    public float GetCurrentBatteryTime01()
    {
        return batteryDuration > 0f ? currentBatteryTime / batteryDuration : 0f;
    }

    public float GetTotalBattery01()
    {
        return GetBattery01();
    }

    public void ToggleOnOff()
    {
        if(IsOn)
            TurnOff();
        else if (currentBatteries > 0 && currentBatteryTime > 0f)
            TurnOn();
    }

    private void TurnOff()
    {
        IsOn = false;
        OnLanternTurnedOff?.Invoke();
        lanternLight.intensity = 0f;
    }
    
    private void TurnOn()
    {
        IsOn = true;
        OnLanternTurnedOn?.Invoke();
    }
}