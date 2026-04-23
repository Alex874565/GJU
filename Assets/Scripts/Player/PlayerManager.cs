using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Lantern lantern;
    
    [Header("Settings")]
    [SerializeField] private int anxietyGrowthRate = 1;

    private int currentAnxiety;

    private bool lanternOff;
    private bool lightsOff;

    private void Start()
    {
        ResetAnxiety();
        lantern.OnLanternTurnedOff += HandleLanternTurnedOff;
        lantern.OnLanternTurnedOn += HandleLanternTurnedOn;
    }

    private void Update()
    {
        if(lanternOff && lightsOff)
        {
            if (currentAnxiety < 100)
            {
                currentAnxiety += anxietyGrowthRate;
            }
        }
    }

    private void OnDestroy()
    {
        if(lantern != null)
        {
            lantern.OnLanternTurnedOff -= HandleLanternTurnedOff;
            lantern.OnLanternTurnedOn -= HandleLanternTurnedOn;
        }
    }

    public void ResetAnxiety()
    {
        currentAnxiety = 0;
    }

    private void HandleLanternTurnedOn()
    {
        lanternOff = true;
    }

    private void HandleLanternTurnedOff()
    {
        lanternOff = false;
    }
}