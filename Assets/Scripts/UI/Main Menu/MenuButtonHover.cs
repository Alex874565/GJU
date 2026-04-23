using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    public TextMeshProUGUI buttonText;
    public Image dashImage;
    public Image borderTop;
    public Image borderBottom;

    [Header("Pause")]
    public PauseMenu pauseMenu;
    public bool isResume = false;
    public bool isQuitToMenu = false;

    [Header("Settings")]
    public SettingsPanel settingsPanel;
    public ButtonFlicker buttonFlicker;
    public bool isBack = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonPressClip;

    [Header("Colors")]
    public Color normalColor = new Color(0.78f, 0.65f, 0.42f, 0.55f);
    public Color hoverColor = new Color(0.90f, 0.78f, 0.55f, 1.0f);

    [Header("Animations")]
    public float hoverDuration = 0.3f;
    public float letterSpacingNormal = 28f;
    public float letterSpacingHover = 38f;

    [Header("Actions")]
    public string sceneToLoad = "";
    public bool isQuit = false;
    public bool showBorderBottom = false;

    private Coroutine hoverCoroutine;

    void Start()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        buttonText.color = normalColor;
        buttonText.characterSpacing = letterSpacingNormal;

        SetAlpha(dashImage, 0f);

        SetAlpha(borderTop, 0f);
        SetAlpha(borderBottom, 0f);

        if (borderBottom != null && !showBorderBottom)
            borderBottom.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(AnimateHover(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        hoverCoroutine = StartCoroutine(AnimateHover(false));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource != null && buttonPressClip != null)
            audioSource.PlayOneShot(buttonPressClip);

        if (isResume && pauseMenu != null)
        {
            StartCoroutine(FlickerThenAction(() => pauseMenu.ResumeGame()));
            return;
        }

        if (isQuitToMenu && pauseMenu != null)
        {
            StartCoroutine(FlickerThenAction(() => pauseMenu.QuitToMainMenu()));
            return;
        }

        if (isQuit)
        {
            StartCoroutine(FlickerThenAction(() => Quit()));
            return;
        }

        if (isBack && settingsPanel != null)
        {
            StartCoroutine(FlickerThenAction(() => settingsPanel.CloseSettings()));
            return;
        }

        if (settingsPanel != null && !isBack)
        {
            StartCoroutine(FlickerThenAction(() => settingsPanel.OpenSettings()));
            return;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(FlickerThenAction(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad)));
        }
    }

    IEnumerator FlickerThenAction(System.Action action)
    {
        float clipLength = buttonPressClip != null ? buttonPressClip.length : 0.2f;
        yield return new WaitForSeconds(clipLength);
        if (buttonFlicker != null)
            yield return StartCoroutine(buttonFlicker.DoFlicker());
        action?.Invoke();
    }

    IEnumerator AnimateHover(bool entering)
    {
        float elapsed = 0f;
        Color startColor = buttonText.color;
        Color targetColor = entering ? hoverColor : normalColor;
        float startSpacing = buttonText.characterSpacing;
        float targetSpacing = entering ? letterSpacingHover : letterSpacingNormal;

        float dashStart = dashImage != null ? dashImage.color.a : 0f;
        float dashTarget = entering ? 1f : 0f;

        float borderStart = borderTop != null ? borderTop.color.a : 0f;
        float borderTarget = entering ? 0.2f : 0f;

        while (elapsed < hoverDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / hoverDuration);

            buttonText.color = Color.Lerp(startColor, targetColor, t);
            buttonText.characterSpacing = Mathf.Lerp(startSpacing, targetSpacing, t);

            SetAlpha(dashImage, Mathf.Lerp(dashStart, dashTarget, t));
            SetAlpha(borderTop, Mathf.Lerp(borderStart, borderTarget, t));

            if (showBorderBottom)
                SetAlpha(borderBottom, Mathf.Lerp(borderStart, borderTarget, t));

            yield return null;
        }

        buttonText.color = targetColor;
        buttonText.characterSpacing = targetSpacing;
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    IEnumerator LoadSceneWithDelay()
    {
        float clipLength = buttonPressClip != null ? buttonPressClip.length : 0.2f;
        yield return new WaitForSeconds(clipLength);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
    }

    private void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}