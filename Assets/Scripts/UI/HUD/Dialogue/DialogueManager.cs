using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private float voiceVolume = 1f;

    [Header("References")]
    public TextMeshProUGUI dialogueText;
    public CanvasGroup canvasGroup;

    [Header("Timing")]
    public float fadeInDuration = 0.4f;
    public float fadeOutDuration = 0.6f;

    public bool isPlaying = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        canvasGroup.alpha = 0f;
    }
    
    private void Start()
    {
        if (voiceSource != null && AudioManager.Instance != null)
            AudioManager.Instance.RegisterManagedLoop(voiceSource);
    }

    private Coroutine dialogueRoutine;

    public void PlayDialogue(DialogueData data, bool overrideCurrent = false)
    {
        if (data == null || data.lines == null || data.lines.Length == 0) return;

        if (isPlaying)
        {
            if (!overrideCurrent) return;
            StopCurrentDialogue();
        }

        dialogueRoutine = StartCoroutine(PlaySequence(data));
    }

    public void PlayMonsterDialogue(DialogueData data)
    {
        PlayDialogue(data, true);
    }

    private void StopCurrentDialogue()
    {
        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        if (voiceSource != null)
            voiceSource.Stop();

        dialogueText.text = "";
        canvasGroup.alpha = 0f;
        isPlaying = false;
    }

    public void PlayDialogue(DialogueLine[] lines)
    {
        if (isPlaying) return;
        dialogueRoutine = StartCoroutine(PlaySequence(lines));
    }

    IEnumerator PlaySequence(DialogueData data)
    {
        yield return StartCoroutine(PlaySequence(data.lines));
    }

    IEnumerator PlaySequence(DialogueLine[] lines)
    {
        isPlaying = true;

        foreach (DialogueLine line in lines)
        {
            dialogueText.text = "";

            yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));

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

            float waitTime = line.displayDuration;

            if (line.voiceover != null)
                waitTime = Mathf.Max(waitTime, line.voiceover.length);

            yield return new WaitForSeconds(waitTime);

            if (voiceSource != null)
                voiceSource.Stop();

            yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));

            dialogueText.text = "";

            yield return new WaitForSeconds(0.2f);
        }

        isPlaying = false;
    }

    IEnumerator Typewrite(DialogueLine line)
    {
        dialogueText.text = "";
        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(line.typewriterSpeed);
        }
    }

    IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}