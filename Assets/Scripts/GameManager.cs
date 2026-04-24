using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int runNo;

    [Header("Player")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Transform playerTransform;

    [Header("Cutscenes")]
    [SerializeField] private CutscenePlayer introCutscene;

    private void Start()
    {
        ResetGame();
    }

    [ContextMenu("Next Run / Reset Game")]
    public void ResetGame()
    {
        StartCoroutine(ResetGameRoutine());
    }

    private IEnumerator ResetGameRoutine()
    {
        runNo++;

        playerTransform.position = introCutscene.transform.position;
        playerTransform.rotation = introCutscene.transform.rotation;

        playerManager.ResetAllStates();

        yield return introCutscene.PlayRoutine();
        playerManager.ToggleLantern(true);
    }
}