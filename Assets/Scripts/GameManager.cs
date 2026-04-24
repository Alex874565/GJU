using UnityEngine;
using System.Collections;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int runNo;

    [Header("Player")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Transform playerTransform;

    [Header("Environment Variation")]
    [SerializeField] private GameObject defaultEnvironment; // first / lights-on version
    [SerializeField] private GameObject[] randomEnvironments;

    private GameObject currentEnvironment;
    
    [Header("Cutscenes")]
    [SerializeField] private CutscenePlayer introCutscene;

    private IResettable[] resettables;

    private void Awake()
    {
        resettables = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IResettable>()
            .ToArray();
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

        if (runNo == 1)
            ActivateDefaultEnvironment();
        else
            ActivateRandomEnvironment();

        foreach (var r in resettables)
            r.ResetState();

        yield return introCutscene.PlayRoutine();

        playerManager.ToggleLantern(true);
    }
    
    private void ActivateEnvironment(GameObject environment)
    {
        if (currentEnvironment != null)
            currentEnvironment.SetActive(false);

        currentEnvironment = environment;

        if (currentEnvironment != null)
            currentEnvironment.SetActive(true);
    }

    private void ActivateRandomEnvironment()
    {
        if (randomEnvironments == null || randomEnvironments.Length == 0)
        {
            ActivateEnvironment(defaultEnvironment);
            return;
        }

        GameObject chosen = randomEnvironments[Random.Range(0, randomEnvironments.Length)];
        ActivateEnvironment(chosen);
    }

    public void ActivateDefaultEnvironment()
    {
        ActivateEnvironment(defaultEnvironment);
    }
}