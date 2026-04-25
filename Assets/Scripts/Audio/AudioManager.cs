using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup ambienceMixerGroup;
    
    [Header("Pause / Menu Music")]
    [SerializeField] private AudioSource pauseMusicSource;
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private float audioFadeDuration = 0.5f;
    [SerializeField] private float sceneMusicFadeDuration = 0.6f;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 12;
    [SerializeField] private Vector2 randomPitchRange = new Vector2(0.92f, 1.08f);

    private readonly List<AudioSource> sfxPool = new();
    private readonly List<AudioSource> pausedSfxSources = new();

    private readonly List<AudioSource> managedSources = new();
    private readonly Dictionary<AudioSource, float> managedSourceVolumes = new();

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
        if (pauseMusicSource != null)
        {
            pauseMusicSource.outputAudioMixerGroup = ambienceMixerGroup;
            pauseMusicSource.loop = true;

            if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            {
                pauseMusicSource.volume = 1f;
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
            source.volume = 1f;

            source.outputAudioMixerGroup = sfxMixerGroup;

            sfxPool.Add(source);
        }
    }

    private AudioSource GetFreeSFXSource()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null && !source.isPlaying)
                return source;
        }

        return sfxPool[0];
    }

    public void RegisterManagedLoop(AudioSource source, float baseVolume = 1f)
    {
        if (source == null) return;

        if (!managedSources.Contains(source))
            managedSources.Add(source);

        managedSourceVolumes[source] = baseVolume;
        source.volume = baseVolume;
        source.playOnAwake = false;
    }

    public void RefreshVolumes()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = isPausedAudio ? 0f : 1f;
        }

        foreach (AudioSource source in managedSources)
        {
            if (source == null) continue;

            float baseVolume = managedSourceVolumes.TryGetValue(source, out float v) ? v : 1f;
            source.volume = isPausedAudio ? 0f : baseVolume;
        }
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
            foreach (AudioSource source in managedSources)
            {
                if (source != null)
                    source.UnPause();
            }

            ResumeAllSFX();
            audioFadeRoutine = StartCoroutine(FadePauseAudio(false));
        }
    }

    private IEnumerator FadePauseAudio(bool paused)
    {
        float sfxStart = GetCurrentSfxVolume();
        float sfxTarget = paused ? 0f : 1f;

        float musicStart = pauseMusicSource != null ? pauseMusicSource.volume : 0f;
        float musicTarget = paused ? 1f : 0f;

        CacheManagedVolumes();

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
            SetManagedVolumes(paused ? 1f - t : t);

            if (pauseMusicSource != null)
                pauseMusicSource.volume = Mathf.Lerp(musicStart, musicTarget, t);

            yield return null;
        }

        SetSfxPoolVolume(sfxTarget);
        SetManagedVolumes(paused ? 0f : 1f);

        if (pauseMusicSource != null)
        {
            pauseMusicSource.volume = musicTarget;

            if (!paused)
                pauseMusicSource.Pause();
        }

        if (paused)
        {
            foreach (AudioSource source in managedSources)
            {
                if (source != null && source.isPlaying)
                    source.Pause();
            }
        }
    }

    public IEnumerator FadeMenuMusic(bool fadeIn)
    {
        if (pauseMusicSource == null)
            yield break;

        float target = fadeIn ? 1f : 0f;

        if (fadeIn && !pauseMusicSource.isPlaying)
            pauseMusicSource.Play();

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
            source.volume = 1f;
        }

        foreach (AudioSource source in managedSources)
        {
            if (source == null) continue;

            source.UnPause();

            float baseVolume = managedSourceVolumes.TryGetValue(source, out float v) ? v : 1f;
            source.volume = baseVolume;
        }

        pausedSfxSources.Clear();

        if (pauseMusicSource != null)
        {
            pauseMusicSource.loop = true;
            pauseMusicSource.volume = 1f;

            if (!pauseMusicSource.isPlaying)
                pauseMusicSource.Play();
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

    private float GetCurrentSfxVolume()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                return source.volume;
        }

        return 1f;
    }

    private void SetSfxPoolVolume(float volume)
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = volume;
        }
    }

    private void CacheManagedVolumes()
    {
        foreach (AudioSource source in managedSources)
        {
            if (source == null) continue;

            if (!managedSourceVolumes.ContainsKey(source))
                managedSourceVolumes[source] = source.volume;
        }
    }

    private void SetManagedVolumes(float multiplier)
    {
        foreach (AudioSource source in managedSources)
        {
            if (source == null) continue;

            float baseVolume = managedSourceVolumes.TryGetValue(source, out float v) ? v : 1f;
            source.volume = baseVolume * multiplier;
        }
    }

    public static void PlaySFX(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.localPosition = Vector3.zero;
        source.spatialBlend = 0f;
        source.panStereo = 0f;
        source.pitch = Random.Range(Instance.randomPitchRange.x, Instance.randomPitchRange.y);
        source.volume = 1f;

        source.PlayOneShot(clip);
    }

    public static void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 1f;
        source.panStereo = 0f;
        source.pitch = Random.Range(Instance.randomPitchRange.x, Instance.randomPitchRange.y);
        source.volume = 1f;

        source.PlayOneShot(clip);
    }

    public static void PlaySFX(AudioClip clip, Vector3 position, float volumeMultiplier)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 1f;
        source.panStereo = 0f;

        source.pitch = Random.Range(
            Instance.randomPitchRange.x,
            Instance.randomPitchRange.y
        ) * Mathf.Lerp(1.1f, 0.85f, volumeMultiplier);

        source.volume = volumeMultiplier;
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
        source.pitch = Random.Range(Instance.randomPitchRange.x, Instance.randomPitchRange.y);
        source.volume = volumeMultiplier;

        source.PlayOneShot(clip);
    }

    public static void PlaySFXWithPitch(AudioClip clip, Vector3 position, float volumeMultiplier, float pitch)
    {
        if (Instance == null || clip == null) return;
        if (Instance.isPausedAudio) return;

        AudioSource source = Instance.GetFreeSFXSource();

        source.transform.position = position;
        source.spatialBlend = 0f;
        source.panStereo = 0f;
        source.pitch = pitch;
        source.volume = volumeMultiplier;

        source.PlayOneShot(clip);
    }

    public static void PlaySFX(AudioClip[] clips, Vector3 position, float volumeMultiplier = 1f)
    {
        if (Instance == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlaySFX(clip, position, volumeMultiplier);
    }
}