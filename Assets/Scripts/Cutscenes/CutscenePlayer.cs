using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Playables;
using System.Collections;

public class CutscenePlayer : MonoBehaviour
{
    public CinemachineCamera gameplayCam;
    public CinemachineCamera cutsceneCam;
    public PlayableDirector timeline;
    public MonoBehaviour playerController;

    public void Play()
    {
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        playerController.enabled = false;

        gameplayCam.Priority = 0;
        cutsceneCam.Priority = 20;

        timeline.Play();

        yield return new WaitUntil(() => timeline.state != PlayState.Playing);

        cutsceneCam.Priority = 0;
        gameplayCam.Priority = 20;

        playerController.enabled = true;
    }
}