using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int runNo;

    [Header("Player")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject playerVisual;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private PlayerLook playerLook;

    [Header("Environment Variation")]
    [SerializeField] private GameObject[] environments;

    [Header("Timing")]
    [SerializeField] private float delayBeforeResetAfterDeath = 1.5f;
    
    [Header("Cutscenes")]
    [SerializeField] private CutscenePlayer introCutscene;

    private List<IResettable> resettables = new List<IResettable>();
    
    [Header("Lightning")]
    [SerializeField] private LightningManager lightningManager;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private DialogueData beforeFlashlightDialogue;
    [SerializeField] private DialogueData afterFlashlightDialogue;
    [SerializeField] private DialogueData[] repeatRunDialogues;

    [Header("Click Prompt")]
    [SerializeField] private GameObject clickPromptRoot;
    [SerializeField] private TextMeshProUGUI clickPromptText;

    [Header("Timing")]
    [SerializeField] private float thunderDelay = 0f;
    [SerializeField] private float afterThunderDelay = 2f;
    [SerializeField] private float afterCutsceneDelay = 0.8f;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Lantern lantern;
    
    [Header("Runtime Systems")]
    [SerializeField] private RandomAreaSoundPlayer soundSpawner;
    [SerializeField] private GameObject[] monstersToDeactivate;

    private Coroutine resetRoutine;
    private bool isResetting;
    
    private bool mouseClicked = false;

    private GameObject currentEnvironment;
    
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;
    
    private void Awake()
    {
        resettables = FindObjectsOfType<MonoBehaviour>().OfType<IResettable>().ToList();

        if (clickPromptRoot != null)
            clickPromptRoot.SetActive(false);

        // 🔥 STORE INITIAL WORLD TRANSFORM
        initialPlayerPosition = playerTransform.position;
        initialPlayerRotation = playerTransform.rotation;
    }

    private void Start()
    {
        ResetGame();
    }
    
    private void ResetPlayerTransform()
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;

            playerRb.position = initialPlayerPosition;
            playerRb.rotation = initialPlayerRotation;
        }

        playerTransform.SetPositionAndRotation(
            initialPlayerPosition,
            initialPlayerRotation
        );

        Physics.SyncTransforms();
    }
    
    [ContextMenu("Reset Game")]
    public void ResetGame()
    {
        if (isResetting) return;

        resetRoutine = StartCoroutine(ResetGameRoutine());
    }

    public void ResetGameAfterDeath()
    {
        if (isResetting) return;

        resetRoutine = StartCoroutine(ResetGameAfterDeathRoutine());
    }

    private IEnumerator ResetGameAfterDeathRoutine()
    {
        yield return new WaitForSeconds(delayBeforeResetAfterDeath);
        yield return StartCoroutine(ResetGameRoutine());
    }

    public void DeactivateAllMonsters()
    {
        MonsterSpawnManager.Instance?.UnregisterSpawn();

        foreach (GameObject monster in monstersToDeactivate)
        {
            if (monster != null)
                monster.SetActive(false);
        }
    }
    
    public void OnGeneratorActivated()
    {
        DeactivateAllMonsters();

        if (soundSpawner != null)
            soundSpawner.gameObject.SetActive(false);

        if (lightningManager != null)
            lightningManager.StopLoop(); // 🔥 stop lightning immediately
    }
    
    private IEnumerator ResetGameRoutine()
    {
        isResetting = true;

        if (lightningManager != null)
            lightningManager.StopLoop();

        runNo++;

        MonsterSpawnManager.Instance?.ResetRunSeenTypes();
        
        DeactivateAllMonsters();

        if (soundSpawner != null)
            soundSpawner.gameObject.SetActive(true);
        
        playerManager.ResetAllStates();
        
        lantern.InputLocked = true;
        playerMovement.inputLocked = true;
        playerManager.inputLocked = true;

        if (playerLook != null)
            playerLook.enabled = false; // 🔥 THIS is the missing piece
        
        ResetPlayerTransform();
        
        if (runNo == 1)
        {
            ActivateRandomEnvironment();
            
            foreach (var r in resettables)
                if (r != null)
                {
                    r.ResetState();
                }
                else
                {
                    resettables.Remove(r);
                }

            if (playerVisual != null)
                playerVisual.SetActive(false);

            yield return introCutscene.PlayRoutine();

            if (playerVisual != null)
                playerVisual.SetActive(true);
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
                if (r != null)
                {
                    r.ResetState();
                }
                else
                {
                    resettables.Remove(r);
                }

            if (playerVisual != null)
                playerVisual.SetActive(false);

            yield return introCutscene.PlayRoutine();

            if (playerVisual != null)
                playerVisual.SetActive(true);
            
            yield return new WaitForSeconds(afterCutsceneDelay);
            if (lightningManager != null)
                lightningManager.StartLoop();

            if (repeatRunDialogues != null && repeatRunDialogues.Length > 0)
            {
                DialogueData chosenDialogue =
                    repeatRunDialogues[Random.Range(0, repeatRunDialogues.Length)];

                if (chosenDialogue != null)
                {
                    dialogueManager.PlayDialogue(chosenDialogue);
                }
            }
        }

        playerMovement.inputLocked = false;
        playerManager.inputLocked = false;
        lantern.InputLocked = false;

        if (playerLook != null)
            playerLook.enabled = true;
        
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
        if (environments == null || environments.Length == 0)
            return;

        GameObject chosen = environments[Random.Range(0, environments.Length)];
        ActivateEnvironment(chosen);
    }
}