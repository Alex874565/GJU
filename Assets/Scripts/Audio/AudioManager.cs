using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pause Music")]
    [SerializeField] private AudioSource pauseMusicSource;
    [SerializeField] private float audioFadeDuration = 0.5f;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 12;
    [SerializeField] private Vector2 randomPitchRange = new Vector2(0.92f, 1.08f);
    
    [Header("Menu Music")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private float sceneMusicFadeDuration = 0.6f;
    
    [Header("UI Audio")]
    [SerializeField] private AudioSource uiSource;

    private readonly List<AudioSource> sfxPool = new();
    private readonly List<AudioSource> pausedSfxSources = new();
    private readonly List<AudioSource> managedLoopSources = new();
    private readonly Dictionary<AudioSource, float> loopSourceVolumes = new();

    public void RegisterManagedLoop(AudioSource source)
    {
        if (source == null) return;
        if (!managedLoopSources.Contains(source))
            managedLoopSources.Add(source);
    }

    private Coroutine audioFadeRoutine;
    private bool isPausedAudio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateSFXPool();
    }

    private void Start()
    {
        ApplyAll();

        if (pauseMusicSource != null)
        {
            pauseMusicSource.loop = true;

            if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            {
                pauseMusicSource.volume = SettingsController.GetAmbianceVolume();
                pauseMusicSource.Play();
            }
            else
            {
                pauseMusicSource.volume = 0f;
                pauseMusicSource.Pause();
            }
        }
    }

    private void CreateSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject obj = new GameObject($"SFX_Source_{i}");
            obj.transform.SetParent(transform);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            sfxPool.Add(source);
        }
    }

    private AudioSource GetFreeSFXSource()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (!source.isPlaying)
                return source;
        }

        return sfxPool[0];
    }

    public void ApplyAll()
    {
        float sfxVolume = isPausedAudio ? 0f : SettingsController.GetSFXVolume();
        float musicVolume = isPausedAudio ? SettingsController.GetAmbianceVolume() : 0f;

        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = sfxVolume;
        }

        if (pauseMusicSource != null)
            pauseMusicSource.volume = musicVolume;
    }

    public IEnumerator FadeMenuMusic(bool fadeIn)
    {
        float target = fadeIn ? SettingsController.GetAmbianceVolume() : 0f;

        if (fadeIn && !pauseMusicSource.isPlaying)
            pauseMusicSource.UnPause();

        float start = pauseMusicSource.volume;
        float timer = 0f;

        while (timer < sceneMusicFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / sceneMusicFadeDuration);

            pauseMusicSource.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }

        pauseMusicSource.volume = target;

        if (!fadeIn)
            pauseMusicSource.Pause();
    }
    
    public void SetPausedAudio(bool paused)
    {
        isPausedAudio = paused;

        if (audioFadeRoutine != null)
            StopCoroutine(audioFadeRoutine);

        if (paused)
        {
            PauseAllSFX();
            audioFadeRoutine = StartCoroutine(FadePauseAudio(true));
        }
        else
        {
            SetSfxPoolVolume(0f);

            foreach (var src in managedLoopSources)
            {
                if (src != null)
                    src.UnPause();
            }

            SetLoopVolumes(0f);
            ResumeAllSFX();

            audioFadeRoutine = StartCoroutine(FadePauseAudio(false));
        }
    }

    private IEnumerator FadePauseAudio(bool paused)
    {
        float sfxStart = GetCurrentSfxVolume();
        float sfxTarget = paused ? 0f : SettingsController.GetSFXVolume();

        float musicStart = pauseMusicSource != null ? pauseMusicSource.volume : 0f;
        float musicTarget = paused ? SettingsController.GetAmbianceVolume() : 0f;

        if (paused)
            CacheLoopVolumes();

        if (pauseMusicSource != null && paused)
        {
            if (!pauseMusicSource.isPlaying)
                pauseMusicSource.Play();
            else
                pauseMusicSource.UnPause();
        }

        float timer = 0f;

        while (timer < audioFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / audioFadeDuration);

            SetSfxPoolVolume(Mathf.Lerp(sfxStart, sfxTarget, t));

            if (pauseMusicSource != null)
                pauseMusicSource.volume = Mathf.Lerp(musicStart, musicTarget, t);

            SetLoopVolumes(paused ? 1f - t : t);

            yield return null;
        }

        SetSfxPoolVolume(sfxTarget);

        if (pauseMusicSource != null)
        {
            pauseMusicSource.volume = musicTarget;
            if (!paused)
                pauseMusicSource.Pause();
        }

        SetLoopVolumes(paused ? 0f : 1f);

        if (paused)
        {
            foreach (var src in managedLoopSources)
            {
                if (src != null && src.isPlaying)
                    src.Pause();
            }
        }
    }
    
    public static void PlaySFX(AudioClip clip, Vector3 position, float volumeMultiplier)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 1f;

        source.pitch = Random.Range(
            Instance.randomPitchRange.x,
            Instance.randomPitchRange.y
        ) * Mathf.Lerp(1.1f, 0.85f, volumeMultiplier);

        float baseVolume = SettingsController.GetSFXVolume();
        source.volume = baseVolume * volumeMultiplier;

        source.PlayOneShot(clip);
    }

    private float GetCurrentSfxVolume()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                return source.volume;
        }

        return SettingsController.GetSFXVolume();
    }

    private void SetSfxPoolVolume(float volume)
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = volume;
        }
    }

    private void PauseAllSFX()
    {
        pausedSfxSources.Clear();

        foreach (AudioSource source in sfxPool)
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
                pausedSfxSources.Add(source);
            }
        }

        CacheLoopVolumes();
        SetLoopVolumes(1f);
    }

    private void ResumeAllSFX()
    {
        foreach (AudioSource source in pausedSfxSources)
        {
            if (source != null)
                source.UnPause();
        }

        pausedSfxSources.Clear();
    }

    private void CacheLoopVolumes()
    {
        loopSourceVolumes.Clear();

        foreach (var src in managedLoopSources)
        {
            if (src != null)
                loopSourceVolumes[src] = src.volume;
        }
    }

    private void SetLoopVolumes(float multiplier)
    {
        foreach (var src in managedLoopSources)
        {
            if (src == null) continue;

            float baseVolume = loopSourceVolumes.TryGetValue(src, out float v) ? v : src.volume;
            src.volume = baseVolume * multiplier;
        }
    }
    
    public static void PlaySFX(AudioClip[] clips, Vector3 position, float volumeMultiplier = 1f)
    {
        if (Instance == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlaySFX(clip, position, volumeMultiplier);
    }
    
    public static void PlaySFXWithPitch(AudioClip clip, Vector3 position, float volumeMultiplier, float pitch)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 0f;

        source.pitch = pitch;

        source.volume = SettingsController.GetSFXVolume() * volumeMultiplier;
        source.PlayOneShot(clip);
    }
    
    public static void PlaySFX(AudioClip clip, Vector3 position, float volumeMultiplier, float stereoPan)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 0f;
        source.panStereo = stereoPan;

        source.pitch = Random.Range(
            Instance.randomPitchRange.x,
            Instance.randomPitchRange.y
        );

        source.volume = SettingsController.GetSFXVolume() * volumeMultiplier;
        source.PlayOneShot(clip);
    }
    
    public static void PlaySFX(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.localPosition = Vector3.zero;
        source.spatialBlend = 0f;
        source.pitch = Random.Range(
            Instance.randomPitchRange.x,
            Instance.randomPitchRange.y
        );

        source.volume = SettingsController.GetSFXVolume();
        source.PlayOneShot(clip);
    }
    
    public void ResumeAudioForMainMenu()
    {
        isPausedAudio = false;

        if (audioFadeRoutine != null)
            StopCoroutine(audioFadeRoutine);

        foreach (AudioSource source in sfxPool)
        {
            if (source == null) continue;

            source.UnPause();
            source.Stop();
            source.volume = SettingsController.GetSFXVolume();
        }

        pausedSfxSources.Clear();

        if (pauseMusicSource != null)
        {
            pauseMusicSource.loop = true;

            if (!pauseMusicSource.isPlaying)
                pauseMusicSource.Play();

            pauseMusicSource.volume = SettingsController.GetAmbianceVolume();
        }
    }
    
    public static void PlayUISFX(AudioClip clip, float volumeMultiplier = 1f, float pitch = 1f)
    {
        if (Instance == null || clip == null || Instance.uiSource == null) return;

        Instance.uiSource.ignoreListenerPause = true;
        Instance.uiSource.spatialBlend = 0f;
        Instance.uiSource.mute = false;
        Instance.uiSource.pitch = pitch;
        Instance.uiSource.volume = SettingsController.GetSFXVolume() * volumeMultiplier;

        // ❗ NO Stop() here → allows overlapping crackles
        Instance.uiSource.PlayOneShot(clip);
    }

    public static void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 1f;
        source.pitch = Random.Range(
            Instance.randomPitchRange.x,
            Instance.randomPitchRange.y
        );

        source.volume = SettingsController.GetSFXVolume();
        source.PlayOneShot(clip);
    }

    public void RefreshVolumes()
    {
        ApplyAll();
    }
}