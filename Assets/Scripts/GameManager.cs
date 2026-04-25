using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Linq;
using TMPro;

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
    
    [Header("Lightning")]
    [SerializeField] private LightningManager lightningManager;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private DialogueData beforeFlashlightDialogue;
    [SerializeField] private DialogueData afterFlashlightDialogue;

    [Header("Click Prompt")]
    [SerializeField] private GameObject clickPromptRoot;
    [SerializeField] private TextMeshProUGUI clickPromptText;

    [Header("Timing")]
    [SerializeField] private float thunderDelay = 0f;
    [SerializeField] private float afterThunderDelay = 2f;
    [SerializeField] private float afterCutsceneDelay = 0.8f;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Lantern lantern;

    private bool mouseClicked = false;

    private void Awake()
    {
        resettables = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<IResettable>()
            .ToArray();
        if (clickPromptRoot != null)
            clickPromptRoot.SetActive(false);

    }

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

        // STOP all input
        playerMovement.inputLocked = true;
        playerManager.inputLocked = true;

        // 1.First lightning
        if (thunderDelay > 0f)
            yield return new WaitForSeconds(thunderDelay);
        if (lightningManager != null)
            yield return StartCoroutine(lightningManager.StrikeOnce());
        yield return new WaitForSeconds(afterThunderDelay);

        // 2.Cutscene
        if (runNo == 1)
            ActivateDefaultEnvironment();
        else
            ActivateRandomEnvironment();

        foreach (var r in resettables)
            r.ResetState();

        yield return introCutscene.PlayRoutine();
        yield return new WaitForSeconds(afterCutsceneDelay);

        // 3.Intro dialogue
        if (introDialogue != null)
        {
            dialogueManager.PlayDialogue(introDialogue);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => !dialogueManager.isPlaying);
        }

        // 4.Flashlight dialogue
        if (beforeFlashlightDialogue != null)
        {
            dialogueManager.PlayDialogue(beforeFlashlightDialogue);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => !dialogueManager.isPlaying);
        }

        // 5.Prompt click to turn on flashlight
        yield return StartCoroutine(WaitForFlashlightClick());

        // 6.Flashlight is on
        playerManager.ToggleLantern(true);

        // 7.After flashlight dialogue
        if (afterFlashlightDialogue != null)
        {
            dialogueManager.PlayDialogue(afterFlashlightDialogue);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => !dialogueManager.isPlaying);
        }

        // 8.Game Start
        playerMovement.inputLocked = false;
        playerManager.inputLocked = false;
        lantern.SetIntroComplete();
    }

    IEnumerator WaitForFlashlightClick()
    {
        if (clickPromptRoot != null)
            clickPromptRoot.SetActive(true);

        if (clickPromptText != null)
            yield return StartCoroutine(TypewriterPrompt("click to turn on flashlight"));

        yield return new WaitUntil(() =>
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame);

        if (clickPromptRoot != null)
            clickPromptRoot.SetActive(false);
    }

    public void OnMouseClick(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            mouseClicked = true;
    }

    IEnumerator TypewriterPrompt(string text)
    {
        if (clickPromptText == null) yield break;
        clickPromptText.text = "";
        foreach (char c in text)
        {
            clickPromptText.text += c;
            yield return new WaitForSeconds(0.04f);
        }
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