using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

    private Coroutine routine;
    private bool skipped;
    
    void Start()
    {
        showWarning = showWarningNext;
        showWarningNext = true;
        
        routine = StartCoroutine(PlayCreditsOnly());
    }

    private void Update()
    {
        if (skipped) return;

        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool enter = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
        bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (clicked || enter || space)
            SkipToMainMenu();
    }

    private void SkipToMainMenu()
    {
        skipped = true;

        if (routine != null)
            StopCoroutine(routine);

        MainMenuInit.skipIntroOnce = true;
        MainMenuInit.ignoreInputUntil = Time.realtimeSinceStartup + 0.35f;

        SceneManager.LoadScene("Main Menu");
    }
    
    IEnumerator PlaySequence()
    {
        warningCanvasGroup.alpha = 0f;
        creditsCanvasGroup.alpha = 0f;
        creditsCanvasGroup.gameObject.SetActive(false);

        yield return StartCoroutine(Fade(warningCanvasGroup, 0f, 1f, warningFadeDuration));
        yield return new WaitForSeconds(warningDisplayDuration);

        AsyncOperation loadOp = SceneManager.LoadSceneAsync("Main");
        loadOp.allowSceneActivation = false;

        yield return StartCoroutine(Fade(warningCanvasGroup, 1f, 0f, warningFadeDuration));

        loadOp.allowSceneActivation = true;
    }

    IEnumerator PlayCreditsOnly()
    {
        warningCanvasGroup.alpha = 0f;
        creditsCanvasGroup.alpha = 0f;

        yield return StartCoroutine(PlayCreditsScroll());
    }

    IEnumerator PlayCreditsScroll()
    {
        creditsCanvasGroup.alpha = 1f;
        creditsContainer.anchoredPosition = new Vector2(0f, -Screen.height);

        float containerHeight = creditsContainer.rect.height;
        float totalDistance = Screen.height + containerHeight + Screen.height;
        float elapsed = 0f;
        float duration = totalDistance / scrollSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            creditsContainer.anchoredPosition = new Vector2(
                0f,
                -Screen.height + (totalDistance * (elapsed / duration))
            );
            yield return null;
        }

        MainMenuInit.skipIntroOnce = true;
        MainMenuInit.ignoreInputUntil = Time.realtimeSinceStartup + 0.35f;
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