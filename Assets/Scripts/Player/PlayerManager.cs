using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Lantern lantern;

    [Header("Anxiety")]
    [SerializeField] private float anxietyGainDarkness = 8f;
    [SerializeField] private float anxietyGainEncounter = 6f;
    [SerializeField] private float anxietyGainSeeingMonster = 15f;
    [SerializeField] private float anxietyDecay = 3f;

    [Header("Darkness Delay")]
    [SerializeField] private float darknessGraceTime = 3f;
    private float darknessTimer = 0f;

    [Range(0f, 100f)]
    [SerializeField] private float currentAnxiety = 0f;

    [Header("Fear")]
    [SerializeField] private float fearRiseSeeingMonster = 3.5f;
    [SerializeField] private float fearRiseEncounter = 2f;
    [SerializeField] private float fearDecay = 1.5f;

    [Range(0f, 1f)]
    [SerializeField] private float currentFear = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip seeMonsterSfx;
    [SerializeField] private float seeMonsterSfxCooldown = 3f;
    
    [Header("Heartbeat Audio")]
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private float heartbeatAnxietyThreshold = 35f;
    [SerializeField] private float highFearThreshold = 0.75f;

    private float nextSeeMonsterSfxTime;// one-shot
    [SerializeField] private AudioSource fearLoopSource;     // looping source
    [SerializeField] private AudioClip fearLoopClip;
    [SerializeField] private float fearLoopThreshold = 0.4f;
    
    [Header("Heartbeat Instability")]
    [SerializeField] private float instabilityThreshold = 0.65f;
    [SerializeField] private float maxPitchWobble = 0.08f;
    [SerializeField] private float maxVolumeWobble = 0.12f;
    [SerializeField] private float wobbleSpeed = 10f;
    
    private float fearLoopVolumeBeforePause;
    
    public bool IsLanternOff => lanternOff;
    public bool AreLightsOff => lightsOff;

    private bool lanternOff = true;
    private bool lightsOff = true;
    private bool seesMonster;
    private bool inEncounter;

    public float Anxiety => currentAnxiety;
    public float Anxiety01 => currentAnxiety / 100f;
    public float Fear01 => currentFear;
    
    public bool IsHidden { get; private set; }
    
    private float lastTime;
    private float lastFearLoopTime;

    private void Start()
    {
        if (lantern != null)
        {
            lantern.OnLanternTurnedOff += HandleLanternTurnedOff;
            lantern.OnLanternTurnedOn += HandleLanternTurnedOn;
        }
        
        if (heartbeatSource != null && AudioManager.Instance != null)
            AudioManager.Instance.RegisterManagedLoop(heartbeatSource);
        
        if (fearLoopSource != null && AudioManager.Instance != null)
            AudioManager.Instance.RegisterManagedLoop(fearLoopSource);
    }

    private void Update()
    {
        UpdateAnxiety();
        UpdateFear();
        UpdateFearAudio();
        UpdateHeartbeatAudio();
    }

    private void OnDestroy()
    {
        if (lantern != null)
        {
            lantern.OnLanternTurnedOff -= HandleLanternTurnedOff;
            lantern.OnLanternTurnedOn -= HandleLanternTurnedOn;
        }
    }

    // ------------------------
    // ANXIETY (WITH DECAY)
    // ------------------------
    private void UpdateAnxiety()
    {
        float gain = 0f;

        if (lanternOff && lightsOff)
        {
            darknessTimer += Time.deltaTime;
            if (darknessTimer >= darknessGraceTime)
                gain += anxietyGainDarkness;
        }
        else
        {
            darknessTimer = 0f;
        }

        if (inEncounter)
            gain += anxietyGainEncounter;

        if (seesMonster)
            gain += anxietyGainSeeingMonster;

        if (gain > 0f)
        {
            currentAnxiety += gain * Time.deltaTime;
            currentAnxiety = Mathf.Clamp(currentAnxiety, 0f, 100f);
        }
        else
        {
            currentAnxiety -= anxietyDecay * Time.deltaTime;
            currentAnxiety = Mathf.Clamp(currentAnxiety, 0f, 100f);
        }
    }

    public void AddAnxiety(float amount)
    {
        currentAnxiety += amount;
        currentAnxiety = Mathf.Clamp(currentAnxiety, 0f, 100f);
    }
    
    private void UpdateHeartbeatAudio()
    {
        if (heartbeatSource == null || heartbeatClip == null)
            return;

        bool fearMaxed = currentFear >= highFearThreshold;
        bool shouldPlay = currentAnxiety >= heartbeatAnxietyThreshold || fearMaxed;

        if (shouldPlay)
        {
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.clip = heartbeatClip;
                heartbeatSource.loop = true;
                heartbeatSource.Play();
            }

            float anxietyT = Mathf.InverseLerp(heartbeatAnxietyThreshold, 100f, currentAnxiety);
            float fearT = Mathf.InverseLerp(highFearThreshold, 1f, currentFear);

// blend instead of override
            float combinedT = Mathf.Lerp(anxietyT, 1f, fearT * 0.6f);

            combinedT = Mathf.SmoothStep(0f, 1f, combinedT);

            float instability = Mathf.InverseLerp(instabilityThreshold, 1f, combinedT);

            float pitchWobble = 0f;
            float volumeWobble = 0f;

            if (instability > 0f)
            {
                float noise = Mathf.PerlinNoise(Time.time * wobbleSpeed, 0.37f);
                noise = (noise - 0.5f) * 2f;

                pitchWobble = noise * maxPitchWobble * instability;
                volumeWobble = noise * maxVolumeWobble * instability;
            }
            
            float targetVolume = Mathf.Lerp(0.15f, 1f, combinedT) + volumeWobble;
            float targetPitch = Mathf.Lerp(0.85f, 1.25f, combinedT) + pitchWobble;

            targetVolume = Mathf.Clamp01(targetVolume);
            targetPitch = Mathf.Clamp(targetPitch, 0.75f, 1.4f);

            heartbeatSource.volume = Mathf.Lerp(
                heartbeatSource.volume,
                targetVolume,
                Time.deltaTime * 5f
            );

            heartbeatSource.pitch = Mathf.Lerp(
                heartbeatSource.pitch,
                targetPitch,
                Time.deltaTime * 3f
            );
        }
        else
        {
            heartbeatSource.volume = Mathf.Lerp(
                heartbeatSource.volume,
                0f,
                Time.deltaTime * 5f
            );

            if (heartbeatSource.volume < 0.01f && heartbeatSource.isPlaying)
                heartbeatSource.Stop();
        }
    }
    
    // ------------------------
    // FEAR (TEMPORARY)
    // ------------------------
    private void UpdateFear()
    {
        float targetFear = 0f;

        if (inEncounter)
            targetFear = Mathf.Max(targetFear, 0.6f);

        if (seesMonster)
            targetFear = 1f;

        float riseSpeed = seesMonster ? fearRiseSeeingMonster : fearRiseEncounter;

        if (currentFear < targetFear)
        {
            currentFear = Mathf.MoveTowards(currentFear, targetFear, riseSpeed * Time.deltaTime);
        }
        else
        {
            currentFear = Mathf.MoveTowards(currentFear, targetFear, fearDecay * Time.deltaTime);
        }
    }

    // ------------------------
    // STATE SETTERS
    // ------------------------
    public void SetLightsOff(bool value)
    {
        lightsOff = value;
    }

    public void SetEncounter(bool value)
    {
        inEncounter = value;
    }

    public void SetSeeingMonster(bool value)
    {
        if (!seesMonster && value && Time.time >= nextSeeMonsterSfxTime)
        {
            if (seeMonsterSfx != null)
                AudioManager.PlaySFX(seeMonsterSfx, transform.position);

            nextSeeMonsterSfxTime = Time.time + seeMonsterSfxCooldown;
        }

        seesMonster = value;
    }
    
    private void UpdateFearAudio()
    {
        if (fearLoopSource == null || fearLoopClip == null)
            return;

        float fear = currentFear;
        bool shouldPlay = fear >= fearLoopThreshold;

        if (shouldPlay)
        {
            if (!fearLoopSource.isPlaying)
            {
                fearLoopSource.clip = fearLoopClip;
                fearLoopSource.loop = true;
                fearLoopSource.pitch = Random.Range(0.95f, 1.05f);
                lastFearLoopTime = 0f;
                fearLoopSource.Play();
            }

            float t = Mathf.InverseLerp(fearLoopThreshold, 1f, fear);
            t = Mathf.SmoothStep(0f, 1f, t);

            float targetVolume = Mathf.Lerp(0.2f, 1f, t);

            fearLoopSource.volume = Mathf.Lerp(
                fearLoopSource.volume,
                targetVolume,
                Time.deltaTime * 6f
            );
        }
        else
        {
            fearLoopSource.volume = Mathf.Lerp(
                fearLoopSource.volume,
                0f,
                Time.deltaTime * 6f
            );

            if (fearLoopSource.volume < 0.01f && fearLoopSource.isPlaying)
                fearLoopSource.Stop();
        }
        
        if (fearLoopSource.isPlaying)
        {
            // loop restarted
            if (fearLoopSource.time < lastFearLoopTime)
            {
                float fearT = Mathf.InverseLerp(fearLoopThreshold, 1f, fear);

                float minPitch = Mathf.Lerp(.9f, 1f, fearT);
                float maxPitch = Mathf.Lerp(1f, 1.1f, fearT);

                fearLoopSource.pitch = Random.Range(minPitch, maxPitch);
            }

            lastFearLoopTime = fearLoopSource.time;
        }
    }

    public void PauseFearAudio()
    {
        if (fearLoopSource == null) return;

        fearLoopVolumeBeforePause = fearLoopSource.volume;
        StartCoroutine(FadeFearAudio(0f, true));
    }

    public void ResumeFearAudio()
    {
        if (fearLoopSource == null) return;

        if (fearLoopSource.clip != null && !fearLoopSource.isPlaying)
            fearLoopSource.UnPause();

        fearLoopSource.volume = 0f;
        StartCoroutine(FadeFearAudio(fearLoopVolumeBeforePause, false));
    }

    private IEnumerator FadeFearAudio(float targetVolume, bool pauseAfter)
    {
        float startVolume = fearLoopSource.volume;
        float timer = 0f;
        float duration = 0.35f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);

            fearLoopSource.volume = Mathf.Lerp(startVolume, targetVolume, t);

            yield return null;
        }

        fearLoopSource.volume = targetVolume;

        if (pauseAfter && fearLoopSource.isPlaying)
            fearLoopSource.Pause();
    }
    
    // ------------------------
    // LANTERN EVENTS (FIXED)
    // ------------------------
    private void HandleLanternTurnedOn()
    {
        lanternOff = false;
    }

    private void HandleLanternTurnedOff()
    {
        lanternOff = true;
    }
    
    public void ToggleLantern(bool value)
    {
        lanternOff = value;
    }

    public void SetHidden(bool value)
    {
        IsHidden = value;
    }

    // ------------------------
    // GETTERS
    // ------------------------
    public float GetAnxiety01() => Anxiety01;
    public float GetFear01() => Fear01;
    
    // ------------------------
// RESETS (for respawn)
// ------------------------
    public void ResetAnxiety()
    {
        currentAnxiety = 0f;
    }

    public void ResetFear()
    {
        currentFear = 0f;
    }

    public void ResetAllStates()
    {
        nextSeeMonsterSfxTime = 0f;
        currentAnxiety = 0f;
        currentFear = 0f;

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
            heartbeatSource.volume = 0f;
            heartbeatSource.pitch = 1f;
        }
        
        seesMonster = false;
        inEncounter = false;
        lightsOff = true;
        lanternOff = false;
        darknessTimer = 0f;
    }
}