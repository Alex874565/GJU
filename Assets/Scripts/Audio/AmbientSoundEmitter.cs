using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientSoundEmitter : MonoBehaviour
{
    [Header("Audio Clip")]
    [SerializeField] private AudioClip soundClip;

    [Header("Playback")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool randomizeStartTime = false;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Pitch")]
    [SerializeField] private bool randomizePitchOnStart = false;
    [SerializeField] private float pitch = 1f;
    [SerializeField] private Vector2 randomPitchRange = new Vector2(0.95f, 1.05f);

    [Header("3D Sound")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 1f;

    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;

    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Advanced")]
    [SerializeField] private bool useCustomCurve = false;
    [SerializeField]
    private AnimationCurve customRolloffCurve =
        AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplySettings();
    }

    private void Start()
    {
        if (playOnAwake)
            Play();
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);

        if (randomPitchRange.x > randomPitchRange.y)
        {
            float temp = randomPitchRange.x;
            randomPitchRange.x = randomPitchRange.y;
            randomPitchRange.y = temp;
        }

        if (Application.isPlaying && audioSource != null)
            ApplySettings();
    }

    private void ApplySettings()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.clip = soundClip;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;

        audioSource.volume = volume;
        audioSource.pitch = randomizePitchOnStart
            ? Random.Range(randomPitchRange.x, randomPitchRange.y)
            : pitch;

        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        audioSource.rolloffMode = useCustomCurve
            ? AudioRolloffMode.Custom
            : rolloffMode;

        if (useCustomCurve)
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloffCurve);
    }

    public void Play()
    {
        if (soundClip == null)
        {
            Debug.LogWarning($"SoundEmitter on {gameObject.name} has no AudioClip assigned.");
            return;
        }

        ApplySettings();

        if (randomizeStartTime && soundClip.length > 0f)
        {
            audioSource.time = Random.Range(0f, soundClip.length);
        }

        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void Pause()
    {
        audioSource.Pause();
    }

    public void Resume()
    {
        audioSource.UnPause();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
            audioSource.volume = volume;
    }

    public void SetPitch(float newPitch)
    {
        pitch = newPitch;

        if (audioSource != null)
            audioSource.pitch = pitch;
    }
}