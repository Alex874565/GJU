using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SliderGlowEffect : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referinte")]
    public Image fillImage;
    public Image handleImage;

    [Header("Culori Glow")]
    public Color colorLow = new Color(0.3f, 0.25f, 0.15f, 1f);
    public Color colorHigh = new Color(0.95f, 0.82f, 0.5f, 1f);

    [Header("Glow Intensitate")]
    public float maxGlowAlpha = 0.6f;
    public float glowPulseSpeed = 2f;

    private Slider slider;
    private Image glowImage;
    private float currentGlowAlpha;
    private bool isDragging = false;

    void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnValueChanged);
        glowImage = CreateGlowOverlay();
        UpdateVisuals(slider.value);
    }

    Image CreateGlowOverlay()
    {
        GameObject glowObj = new GameObject("GlowOverlay");
        glowObj.transform.SetParent(fillImage.transform.parent, false);

        Image glow = glowObj.AddComponent<Image>();
        glow.sprite = fillImage.sprite;
        glow.type = fillImage.type;
        glow.raycastTarget = false;

        RectTransform glowRT = glow.GetComponent<RectTransform>();
        RectTransform fillRT = fillImage.GetComponent<RectTransform>();
        glowRT.anchorMin = fillRT.anchorMin;
        glowRT.anchorMax = fillRT.anchorMax;
        glowRT.offsetMin = fillRT.offsetMin;
        glowRT.offsetMax = fillRT.offsetMax;
        glowRT.pivot = fillRT.pivot;

        glowObj.transform.SetSiblingIndex(fillImage.transform.GetSiblingIndex());
        glow.color = new Color(1f, 1f, 1f, 0f);
        return glow;
    }

    void Update()
    {
        if (!isDragging) return;

        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) / 2f;
        float targetAlpha = Mathf.Lerp(maxGlowAlpha * 0.5f, maxGlowAlpha, pulse) * slider.value;
        currentGlowAlpha = Mathf.Lerp(currentGlowAlpha, targetAlpha, Time.deltaTime * 8f);

        if (glowImage != null)
        {
            Color gc = glowImage.color;
            gc.a = currentGlowAlpha;
            glowImage.color = gc;
        }
    }

    public void OnDrag(PointerEventData eventData) => isDragging = true;

    public void OnPointerDown(PointerEventData eventData) => isDragging = true;

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        StartCoroutine(FadeGlow());
    }

    void OnValueChanged(float value) => UpdateVisuals(value);

    void UpdateVisuals(float value)
    {
        if (fillImage != null)
            fillImage.color = Color.Lerp(colorLow, colorHigh, value);

        if (handleImage != null)
        {
            Color hc = Color.Lerp(colorLow, colorHigh, value);
            hc.a = 0.9f;
            handleImage.color = hc;
        }

        if (glowImage != null && !isDragging)
        {
            Color gc = Color.Lerp(colorLow, colorHigh, value);
            gc.a = value * maxGlowAlpha * 0.4f;
            glowImage.color = gc;
            currentGlowAlpha = gc.a;
        }
    }

    IEnumerator FadeGlow()
    {
        float start = currentGlowAlpha;
        float target = slider.value * maxGlowAlpha * 0.4f;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            currentGlowAlpha = Mathf.Lerp(start, target, t);

            if (glowImage != null)
            {
                Color gc = glowImage.color;
                gc.a = currentGlowAlpha;
                glowImage.color = gc;
            }

            yield return null;
        }
    }
}