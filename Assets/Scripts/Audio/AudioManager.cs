using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Sources")]
    public List<AudioSource> sfxSources = new List<AudioSource>();

    [Header("Ambiance Sources")]
    public List<AudioSource> ambianceSources = new List<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyAll();
    }

    public void ApplyAll()
    {
        float sfx = SettingsController.GetSFXVolume();
        float ambiance = SettingsController.GetAmbianceVolume();

        foreach (AudioSource src in sfxSources)
            if (src != null) src.volume = sfx;

        foreach (AudioSource src in ambianceSources)
            if (src != null) src.volume = ambiance;
    }

    public static void PlaySFX(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.volume = SettingsController.GetSFXVolume();
        source.PlayOneShot(clip);
    }

    public static void PlayAmbiance(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.volume = SettingsController.GetAmbianceVolume();
        if (!source.isPlaying)
            source.Play();
    }

    public void RefreshVolumes()
    {
        ApplyAll();
    }
}