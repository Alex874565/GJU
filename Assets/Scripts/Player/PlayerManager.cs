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

    private bool lanternOff;
    private bool lightsOff;
    private bool seesMonster;
    private bool inEncounter;

    public float Anxiety => currentAnxiety;
    public float Anxiety01 => currentAnxiety / 100f;
    public float Fear01 => currentFear;
    
    public bool IsHidden { get; private set; }

    private void Start()
    {
        if (lantern != null)
        {
            lantern.OnLanternTurnedOff += HandleLanternTurnedOff;
            lantern.OnLanternTurnedOn += HandleLanternTurnedOn;
        }
    }

    private void Update()
    {
        UpdateAnxiety();
        UpdateFear();
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
        seesMonster = value;
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
        currentAnxiety = 0f;
        currentFear = 0f;

        seesMonster = false;
        inEncounter = false;
        lightsOff = true;
        lanternOff = false;
        darknessTimer = 0f;
    }
}