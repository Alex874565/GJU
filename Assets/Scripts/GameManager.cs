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
    [SerializeField] private DialogueData repeatRunDialogue;

    [Header("Click Prompt")]
    [SerializeField] private GameObject clickPromptRoot;
    [SerializeField] private TextMeshProUGUI clickPromptText;

    [Header("Timing")]
    [SerializeField] private float thunderDelay = 0f;
    [SerializeField] private float afterThunderDelay = 2f;
    [SerializeField] private float afterCutsceneDelay = 0.8f;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Lantern lantern;

    private Coroutine resetRoutine;
    private bool isResetting;
    
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
        if (isResetting) return;

        resetRoutine = StartCoroutine(ResetGameRoutine());
    }

    private IEnumerator ResetGameRoutine()
    {
        isResetting = true;

        if (lightningManager != null)
            lightningManager.StopLoop();

        runNo++;

        playerTransform.position = introCutscene.transform.position;
        playerTransform.rotation = introCutscene.transform.rotation;
        playerManager.ResetAllStates();

        lantern.InputLocked = true;
        playerMovement.inputLocked = true;
        playerManager.inputLocked = true;

        if (runNo == 1)
        {
            ActivateDefaultEnvironment();

            foreach (var r in resettables)
                r.ResetState();

            yield return introCutscene.PlayRoutine();
            yield return new WaitForSeconds(afterCutsceneDelay);
            if (lightningManager != null)
                lightningManager.StartLoop();

            if (introDialogue != null)
            {
                dialogueManager.PlayDialogue(introDialogue);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => !dialogueManager.isPlaying);
            }

            if (beforeFlashlightDialogue != null)
            {
                dialogueManager.PlayDialogue(beforeFlashlightDialogue);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => !dialogueManager.isPlaying);
            }

            lantern.InputLocked = false;
            yield return StartCoroutine(WaitForFlashlightClick());

            if (afterFlashlightDialogue != null)
            {
                dialogueManager.PlayDialogue(afterFlashlightDialogue);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => !dialogueManager.isPlaying);
            }
        }
        else
        {
            ActivateRandomEnvironment();

            foreach (var r in resettables)
                r.ResetState();

            yield return introCutscene.PlayRoutine();
            
            yield return new WaitForSeconds(afterCutsceneDelay);
            if (lightningManager != null)
                lightningManager.StartLoop();

            if (repeatRunDialogue != null)
            {
                dialogueManager.PlayDialogue(repeatRunDialogue);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => !dialogueManager.isPlaying);
            }
        }

        playerMovement.inputLocked = false;
        playerManager.inputLocked = false;
        lantern.InputLocked = false;
        
        isResetting = false;
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