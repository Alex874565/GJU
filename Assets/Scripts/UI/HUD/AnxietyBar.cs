using UnityEngine;
using UnityEngine.UI;

public class AnxietyBar : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;
    public Image heartIcon;
    public PlayerManager playerManager;

    [Header("Colors")]
    public Color lowAnxietyColor = new Color(0.71f, 0.31f, 0.31f, 1f);
    public Color highAnxietyColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Header("Pulse")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.08f;

    private float displayedAnxiety = 0f;
    private float smoothSpeed = 5f;
    private RectTransform fillRect;
    private float fillMaxWidth;
    private float startAnchoredX;
    private Vector2 heartBaseSize;

    void Start()
    {
        fillRect = fillImage.GetComponent<RectTransform>();
        fillMaxWidth = fillRect.sizeDelta.x;
        startAnchoredX = fillRect.anchoredPosition.x - fillMaxWidth / 2f;
        heartBaseSize = heartIcon.rectTransform.sizeDelta;
        UpdateFill(0f);
    }

    void Update()
    {
        if (playerManager == null) return;

        float target = playerManager.Anxiety01;
        displayedAnxiety = Mathf.Lerp(displayedAnxiety, target, Time.deltaTime * smoothSpeed);

        UpdateFill(displayedAnxiety);
        UpdateColor(displayedAnxiety);
        UpdatePulse(displayedAnxiety);
    }

    void UpdateFill(float t)
    {
        float targetWidth = fillMaxWidth * t;
        fillRect.sizeDelta = new Vector2(targetWidth, fillRect.sizeDelta.y);
        fillRect.anchoredPosition = new Vector2(startAnchoredX + targetWidth / 2f, fillRect.anchoredPosition.y);
    }

    void UpdateColor(float t)
    {
        Color c = Color.Lerp(lowAnxietyColor, highAnxietyColor, t);
        fillImage.color = c;
        if (heartIcon != null)
            heartIcon.color = c;
    }

    void UpdatePulse(float t)
    {
        if (heartIcon == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * (1f + t * 2f)) * pulseIntensity * t;
        heartIcon.rectTransform.sizeDelta = heartBaseSize * pulse;
    }
}