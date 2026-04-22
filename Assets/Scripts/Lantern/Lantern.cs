using UnityEngine;

public class Lantern : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light lanternLight;

    [Header("Battery")]
    [Range(0f, 1f)]
    [SerializeField] private float battery01 = 1f;
    [SerializeField] private float batteryDecay = .05f;
    [SerializeField] private float lowBatteryThreshold = 0.35f;
    [SerializeField] private float criticalBatteryThreshold = 0.15f;

    [Header("Light Output")]
    [SerializeField] private float normalIntensity = 400f;
    [SerializeField] private float lowBatteryIntensity = 300f;
    [SerializeField] private float criticalBatteryIntensity = 200f;

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

    private float baseIntensity;
    private float currentMultiplier = 1f;

    private bool inEpisode;
    private float episodeTimer;
    private float dipTimer;
    private float targetMultiplier = 1f;

    private float noiseSeed;

    private void Start()
    {
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        SetBattery01(battery01 - batteryDecay * Time.deltaTime);

        UpdateBaseIntensity();
        UpdateEpisodeState();
        UpdateNoise();
        ApplyLight();
    }

    private void UpdateBaseIntensity()
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
        baseIntensity = Mathf.Lerp(criticalBatteryIntensity, 8f, ct);
    }

    private void UpdateEpisodeState()
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

    private void UpdateNoise()
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

    public void SetBattery01(float value)
    {
        battery01 = Mathf.Clamp01(value);
    }

    public void AddBattery(float value)
    {
        SetBattery01(battery01 + value);
    }
}