using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundMonsterFlicker : MonoBehaviour
{
    [Header("References")]
    public NeonFlicker titleFlicker;

    [Header("Positions")]
    public float leftPosX = -500f;
    public float rightPosX = 500f;
    public float minPosY = -100f;
    public float maxPosY = 100f;

    [Header("Timing")]
    public float minTimeBetweenAppearances = 8f;
    public float maxTimeBetweenAppearances = 16f;
    public float fadeInDuration = 0.8f;
    public float visibleDuration = 1.2f;
    public float fadeOutDuration = 1.5f;

    [Header("Intensity")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.35f;

    [Header("Movement")]
    public float swayAmount = 8f;
    public float swaySpeed = 0.6f;

    [Header("Creepy Movement")]
    public float creepyLurchDistance = 18f;
    public float creepyLurchSpeed = 3.5f;
    public float twitchIntensity = 5f;

    [Header("Glitch")]
    public float glitchChance = 0.3f;
    public float glitchDuration = 0.08f;
    public float glitchMaxOffset = 30f;
    public float glitchScaleWarp = 0.06f;

    private Image img;
    private RectTransform rt;
    private float fixedY;
    private bool isVisible = false;
    private Vector3 baseScale;
    private float creepyPhase;
    private float twitchTimer;
    private Vector2 twitchOffset;
    private float anchoredX;

    private void OnEnable()
    {
        img = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        fixedY = rt.anchoredPosition.y;
        baseScale = rt.localScale;
        creepyPhase = Random.Range(0f, Mathf.PI * 2f);
        SetAlpha(0f);
        StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isVisible = false;
        SetAlpha(0f);
    }

    void Update()
    {
        if (!isVisible) return;

        float swayY = Mathf.Sin(Time.time * swaySpeed + creepyPhase) * swayAmount
                    + Mathf.Sin(Time.time * swaySpeed * 2.7f + creepyPhase) * (swayAmount * 0.3f);

        float lurchX = Mathf.Sin(Time.time * creepyLurchSpeed * 0.4f + creepyPhase)
                     * Mathf.Abs(Mathf.Sin(Time.time * 0.7f)) * creepyLurchDistance;

        twitchTimer -= Time.deltaTime;
        if (twitchTimer <= 0f)
        {
            twitchOffset = Random.insideUnitCircle * twitchIntensity;
            twitchTimer = Random.Range(0.05f, 0.25f);
        }

        rt.anchoredPosition = new Vector2(anchoredX + lurchX, fixedY + swayY + twitchOffset.y);

        float breathe = 1f + Mathf.Sin(Time.time * 1.1f + creepyPhase) * 0.012f
                           + Mathf.Sin(Time.time * 3.3f) * 0.005f;
        rt.localScale = baseScale * breathe;
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            float wait = Random.Range(minTimeBetweenAppearances, maxTimeBetweenAppearances);
            yield return new WaitForSeconds(wait);

            anchoredX = Random.value > 0.5f ? leftPosX : rightPosX;
            fixedY = Random.Range(minPosY, maxPosY);
            rt.anchoredPosition = new Vector2(anchoredX, fixedY);
            creepyPhase = Random.Range(0f, Mathf.PI * 2f);

            if (titleFlicker != null)
                yield return StartCoroutine(SyncWithTitleFlicker());
            else
                yield return StartCoroutine(AppearAndDisappear());
        }
    }

    IEnumerator SyncWithTitleFlicker()
    {
        yield return StartCoroutine(titleFlicker.DoFlickerAndReturn());
        yield return StartCoroutine(AppearSequence());
    }

    IEnumerator AppearAndDisappear()
    {
        yield return StartCoroutine(AppearSequence());
    }

    IEnumerator AppearSequence()
    {
        yield return StartCoroutine(GlitchBurst(3, 0.06f));
        isVisible = true;
        StartCoroutine(RandomGlitchLoop());
        yield return StartCoroutine(Fade(0f, maxAlpha, fadeInDuration));
        yield return new WaitForSeconds(visibleDuration);
        yield return StartCoroutine(GlitchBurst(2, 0.07f));
        yield return StartCoroutine(Fade(maxAlpha, 0f, fadeOutDuration));
        isVisible = false;
        rt.localScale = baseScale;
    }

    IEnumerator RandomGlitchLoop()
    {
        while (isVisible)
        {
            float delay = Random.Range(0.2f, 1.0f);
            yield return new WaitForSeconds(delay);
            if (!isVisible) break;
            if (Random.value < glitchChance)
                yield return StartCoroutine(DoGlitch());
        }
    }

    IEnumerator DoGlitch()
    {
        int frames = Random.Range(2, 5);
        for (int i = 0; i < frames; i++)
        {
            rt.anchoredPosition = new Vector2(
                anchoredX + Random.Range(-glitchMaxOffset, glitchMaxOffset),
                fixedY + Random.Range(-glitchMaxOffset * 0.3f, glitchMaxOffset * 0.3f)
            );
            float sx = 1f + Random.Range(-glitchScaleWarp, glitchScaleWarp);
            float sy = 1f + Random.Range(-glitchScaleWarp, glitchScaleWarp);
            rt.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, 1f);
            yield return new WaitForSeconds(glitchDuration);
        }
        rt.localScale = baseScale;
    }

    IEnumerator GlitchBurst(int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            yield return StartCoroutine(DoGlitch());
            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}