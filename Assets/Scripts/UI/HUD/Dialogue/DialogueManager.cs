using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private float voiceVolume = 1f;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    public bool isPlaying { get; private set; }

    private Coroutine dialogueRoutine;
    private bool monsterDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (dialogueText != null)
            dialogueText.text = "";
    }

    private void Start()
    {
        if (voiceSource != null && AudioManager.Instance != null)
            AudioManager.Instance.RegisterManagedLoop(voiceSource);
    }

    public void PlayDialogue(DialogueData data)
    {
        if (monsterDialogueActive) return;
        if (data == null || data.lines == null || data.lines.Length == 0) return;
        if (isPlaying) return;

        dialogueRoutine = StartCoroutine(PlaySequence(data.lines, false));
    }

    public void PlayDialogue(DialogueLine[] lines)
    {
        if (monsterDialogueActive) return;
        if (lines == null || lines.Length == 0) return;
        if (isPlaying) return;

        dialogueRoutine = StartCoroutine(PlaySequence(lines, false));
    }

    public void PlayDialogue(DialogueData data, bool overrideCurrent)
    {
        if (monsterDialogueActive && !overrideCurrent) return;
        if (data == null || data.lines == null || data.lines.Length == 0) return;

        if (isPlaying && !overrideCurrent) return;

        StopCurrentDialogue();
        dialogueRoutine = StartCoroutine(PlaySequence(data.lines, false));
    }

    public void PlayMonsterDialogue(DialogueData data)
    {
        PlayDialogue(data, true);
    }

    public void PlayMonsterDialoguePersistent(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0) return;

        StopCurrentDialogue();

        monsterDialogueActive = true;
        dialogueRoutine = StartCoroutine(PlayPersistentMonsterDialogue(data.lines));
    }

    public void StopMonsterDialogue()
    {
        if (!monsterDialogueActive) return;

        monsterDialogueActive = false;

        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = StartCoroutine(FadeOutAndClear());
    }

    public void StopCurrentDialogue()
    {
        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = null;

        if (voiceSource != null)
            voiceSource.Stop();

        if (dialogueText != null)
            dialogueText.text = "";

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        isPlaying = false;
        monsterDialogueActive = false;
    }

    private IEnumerator PlaySequence(DialogueLine[] lines, bool persistent)
    {
        isPlaying = true;

        foreach (DialogueLine line in lines)
        {
            yield return StartCoroutine(ShowLine(line));

            if (persistent)
            {
                while (monsterDialogueActive)
                    yield return null;

                break;
            }

            float waitTime = line.displayDuration;

            if (line.voiceover != null)
                waitTime = Mathf.Max(waitTime, line.voiceover.length);

            yield return new WaitForSeconds(waitTime);

            if (voiceSource != null)
                voiceSource.Stop();

            yield return StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, fadeOutDuration));

            if (dialogueText != null)
                dialogueText.text = "";

            yield return new WaitForSeconds(0.2f);
        }

        isPlaying = false;
        dialogueRoutine = null;
    }

    private IEnumerator PlayPersistentMonsterDialogue(DialogueLine[] lines)
    {
        isPlaying = true;

        DialogueLine line = lines[0];

        yield return StartCoroutine(ShowLine(line));

        while (monsterDialogueActive)
            yield return null;

        yield return StartCoroutine(FadeOutAndClear());
    }

    private IEnumerator ShowLine(DialogueLine line)
    {
        if (dialogueText != null)
            dialogueText.text = "";

        yield return StartCoroutine(FadeCanvas(canvasGroup.alpha, 1f, fadeInDuration));

        if (line.voiceover != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceover;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = SettingsController.GetSFXVolume() * voiceVolume;
            voiceSource.Play();
        }

        yield return StartCoroutine(Typewrite(line));
    }

    private IEnumerator FadeOutAndClear()
    {
        if (voiceSource != null)
            voiceSource.Stop();

        yield return StartCoroutine(FadeCanvas(canvasGroup.alpha, 0f, fadeOutDuration));

        if (dialogueText != null)
            dialogueText.text = "";

        isPlaying = false;
        dialogueRoutine = null;
        monsterDialogueActive = false;
    }

    private IEnumerator Typewrite(DialogueLine line)
    {
        if (dialogueText == null)
            yield break;

        dialogueText.text = "";

        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(line.typewriterSpeed);
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}