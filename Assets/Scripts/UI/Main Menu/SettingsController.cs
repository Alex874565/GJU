using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Volume")]
    public Slider sfxSlider;
    public Slider ambianceSlider;

    [Header("Refresh Rate")]
    public TMP_Dropdown refreshRateDropdown;

    void Start()
    {
        LoadSFX();
        LoadAmbiance();
        PopulateRefreshRates();
    }
    
    private void ApplyMixerVolume(string parameter, float value)
    {
        if (audioMixer == null) return;

        float db = value <= 0.001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(parameter, db);
    }

    void LoadSFX()
    {
        float saved = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (sfxSlider != null)
        {
            sfxSlider.value = saved;
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
        ApplyMixerVolume("SFXVolume", saved);
    }

    void LoadAmbiance()
    {
        float saved = PlayerPrefs.GetFloat("AmbianceVolume", 1f);
        if (ambianceSlider != null)
        {
            ambianceSlider.value = saved;
            ambianceSlider.onValueChanged.AddListener(OnAmbianceChanged);
        }
        ApplyMixerVolume("AmbienceVolume", saved);
    }

    void PopulateRefreshRates()
    {
        if (refreshRateDropdown == null) return;

        refreshRateDropdown.ClearOptions();
        List<string> options = new List<string> { "30", "60", "120", "144" };
        refreshRateDropdown.AddOptions(options);

        int saved = PlayerPrefs.GetInt("RefreshRate", 1);
        refreshRateDropdown.value = saved;
        refreshRateDropdown.RefreshShownValue();
        refreshRateDropdown.onValueChanged.AddListener(OnRefreshRateChanged);
        ApplyRefreshRate(saved);
    }

    public void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplyMixerVolume("SFXVolume", value);
    }

    public void OnAmbianceChanged(float value)
    {
        PlayerPrefs.SetFloat("AmbianceVolume", value);
        ApplyMixerVolume("AmbienceVolume", value);
    }

    public void OnRefreshRateChanged(int index)
    {
        PlayerPrefs.SetInt("RefreshRate", index);
        ApplyRefreshRate(index);
    }

    void ApplyRefreshRate(int index)
    {
        switch (index)
        {
            case 0: Application.targetFrameRate = 30; break;
            case 1: Application.targetFrameRate = 60; break;
            case 2: Application.targetFrameRate = 120; break;
            case 3: Application.targetFrameRate = 144; break;
        }
    }

    public static float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public static float GetAmbianceVolume()
    {
        return PlayerPrefs.GetFloat("AmbianceVolume", 1f);
    }
}