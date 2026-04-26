using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuInit : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float fadeOutTime = 1f;
    [SerializeField] private float skipFadeTime = 0.35f;

    [Header("Disable After")]
    [SerializeField] private GameObject objectToDisable;
    
    [Header("Enable After Intro")]
    [SerializeField] private GameObject[] enableAfterIntro;

    private Coroutine routine;
    private bool skipping;
    private bool finished;
    
    public static float ignoreInputUntil;
    public static bool skipIntroOnce;

    private void Start()
    {
        foreach (GameObject obj in enableAfterIntro)
            if (obj != null) obj.SetActive(false);
        
        if (skipIntroOnce)
        {
            skipIntroOnce = false;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        routine = StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (finished || skipping)
            return;

        bool clicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool pressedKey =
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;

        if (clicked || pressedKey)
            Skip();
    }

    private void Skip()
    {
        skipping = true;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SkipRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return FadeTo(1f, fadeInTime);
        yield return new WaitForSeconds(waitTime);
        yield return FadeTo(0f, fadeOutTime);

        Finish();
    }

    private IEnumerator SkipRoutine()
    {
        yield return null; // prevents same-frame fade fighting/flicker
        yield return FadeTo(0f, skipFadeTime);

        Finish();
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void Finish()
    {
        MainMenuInit.ignoreInputUntil = Time.realtimeSinceStartup + 0.35f;
        
        foreach (GameObject obj in enableAfterIntro)
            if (obj != null) obj.SetActive(true);
        
        finished = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (objectToDisable != null)
            objectToDisable.SetActive(false);
    }
}