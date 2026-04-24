using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

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

    public void PlayDialogue(DialogueData data)
    {
        if (isPlaying) return;
        StartCoroutine(PlaySequence(data));
    }

    public void PlayDialogue(DialogueLine[] lines)
    {
        if (isPlaying) return;
        StartCoroutine(PlaySequence(lines));
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
            yield return StartCoroutine(Typewrite(line));
            yield return new WaitForSeconds(line.displayDuration);
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