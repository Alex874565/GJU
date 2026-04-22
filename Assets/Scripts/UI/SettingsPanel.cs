using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("References")]
    public GameObject settingsPanel;
    public NeonFlicker titleFlicker;

    [Header("Main Menu")]
    public GameObject mainMenuContent;
    public GameObject monster;

    [Header("Animation")]
    public float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private bool isOpen = false;

    void Start()
    {
        canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = settingsPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (isOpen) return;
        StartCoroutine(DoFlickerThenOpen());
    }

    public void CloseSettings()
    {
        if (!isOpen) return;
        StartCoroutine(FadePanel(false));
    }

    IEnumerator DoFlickerThenOpen()
    {
        if (titleFlicker != null)
            yield return StartCoroutine(titleFlicker.DoFlickerAndReturn());
        yield return StartCoroutine(FadePanel(true));
    }

    IEnumerator FadePanel(bool open)
    {
        isOpen = open;
        settingsPanel.SetActive(true);

        if (mainMenuContent != null)
        {
            mainMenuContent.SetActive(!open);

            if (monster != null)
            {
                monster.SetActive(true);
                Image monsterImg = monster.GetComponent<Image>();
                if (monsterImg != null)
                {
                    Color c = monsterImg.color;
                    c.a = 0f;
                    monsterImg.color = c;
                }
            }

            if (!open)
            {
                NeonFlicker[] flickers = mainMenuContent.GetComponentsInChildren<NeonFlicker>(true);
                foreach (NeonFlicker f in flickers) { f.StopAllCoroutines(); f.Start(); }

                ScanlineEffect[] scanlines = mainMenuContent.GetComponentsInChildren<ScanlineEffect>(true);
                foreach (ScanlineEffect s in scanlines) { s.StopAllCoroutines(); s.Start(); }

                if (monster != null)
                {
                    BackgroundMonsterFlicker bmf = monster.GetComponent<BackgroundMonsterFlicker>();
                    if (bmf != null) { bmf.StopAllCoroutines(); bmf.Start(); }
                }
            }
        }

        float from = open ? 0f : 1f;
        float to = open ? 1f : 0f;
        float elapsed = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;

        if (!open)
            settingsPanel.SetActive(false);
    }
}