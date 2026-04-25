using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Playables;
using System.Collections;

public class CutscenePlayer : MonoBehaviour
{
    public CinemachineCamera gameplayCam;
    public CinemachineCamera cutsceneCam;
    public PlayableDirector timeline;

    [Header("Cutscene Audio")]
    [SerializeField] private AudioSource[] cutsceneAudioSources;

    private bool isPlaying;

    private void Start()
    {
        RegisterAudioSources();
    }

    private void RegisterAudioSources()
    {
        if (AudioManager.Instance == null) return;

        foreach (AudioSource source in cutsceneAudioSources)
        {
            if (source == null) continue;

            source.playOnAwake = false;
            source.volume = SettingsController.GetSFXVolume();

            AudioManager.Instance.RegisterManagedLoop(source, source.volume);
        }
    }

    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayRoutine()
    {
        if (isPlaying) yield break;
        isPlaying = true;

        gameplayCam.Priority = 0;
        cutsceneCam.Priority = 20;

        timeline.Stop();
        timeline.time = 0;
        timeline.Play();

        yield return new WaitUntil(() => timeline.state != PlayState.Playing);

        cutsceneCam.Priority = 0;
        gameplayCam.Priority = 20;

        isPlaying = false;
    }
}