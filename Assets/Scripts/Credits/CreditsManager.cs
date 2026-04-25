using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsManager : MonoBehaviour
{
    public static bool showWarningNext = true;

    [Header("Warning")]
    public CanvasGroup warningCanvasGroup;
    public float warningDisplayDuration = 5f;
    public float warningFadeDuration = 1.5f;

    [Header("Credits")]
    public RectTransform creditsContainer;
    public CanvasGroup creditsCanvasGroup;
    public float scrollSpeed = 60f;
    public float fadeInDuration = 1.5f;
    public float endDelay = 3f;

    private bool showWarning;

    void Start()
    {
        showWarning = showWarningNext;
        showWarningNext = true;

        if (showWarning)
            StartCoroutine(PlaySequence());
        else
            StartCoroutine(PlayCreditsOnly());
    }

    IEnumerator PlaySequence()
    {
        warningCanvasGroup.alpha = 0f;
        creditsCanvasGroup.alpha = 0f;

        yield return StartCoroutine(Fade(warningCanvasGroup, 0f, 1f, warningFadeDuration));
        yield return new WaitForSeconds(warningDisplayDuration);
        yield return StartCoroutine(Fade(warningCanvasGroup, 1f, 0f, warningFadeDuration));

        yield return StartCoroutine(PlayCreditsScroll());
    }

    IEnumerator PlayCreditsOnly()
    {
        warningCanvasGroup.alpha = 0f;
        creditsCanvasGroup.alpha = 0f;

        yield return StartCoroutine(PlayCreditsScroll());
    }

    IEnumerator PlayCreditsScroll()
    {
        yield return StartCoroutine(Fade(creditsCanvasGroup, 0f, 1f, fadeInDuration));

        float totalHeight = creditsContainer.rect.height + Screen.height;
        float elapsed = 0f;
        float duration = totalHeight / scrollSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            creditsContainer.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(endDelay);
        yield return StartCoroutine(Fade(creditsCanvasGroup, 1f, 0f, fadeInDuration));
        SceneManager.LoadScene("Main Menu");
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}