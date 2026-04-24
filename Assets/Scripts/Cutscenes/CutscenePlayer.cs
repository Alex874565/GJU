using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Playables;
using System.Collections;

public class CutscenePlayer : MonoBehaviour
{
    public CinemachineCamera gameplayCam;
    public CinemachineCamera cutsceneCam;
    public PlayableDirector timeline;
    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    public IEnumerator PlayRoutine()
    {
        InputManager.Instance.enabled = false;

        gameplayCam.Priority = 0;
        cutsceneCam.Priority = 20;

        timeline.Play();

        yield return new WaitUntil(() => timeline.state != PlayState.Playing);

        cutsceneCam.Priority = 0;
        gameplayCam.Priority = 20;

        InputManager.Instance.enabled = true;
    }
}