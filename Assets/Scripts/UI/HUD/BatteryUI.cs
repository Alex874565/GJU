using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BatteryUI : MonoBehaviour
{
    [Header("Flashlight")]
    public Lantern lantern;

    [Header("Segments — index 0 = left, 4 = right")]
    public Image[] segments = new Image[5];

    [Header("Colors")]
    public Color colorSeg1 = new Color(0.71f, 0.25f, 0.25f, 0.9f);  // red
    public Color colorSeg2 = new Color(0.78f, 0.47f, 0.20f, 0.9f);  // orange
    public Color colorSeg3 = new Color(0.78f, 0.66f, 0.20f, 0.9f);  // yellow
    public Color colorSeg4 = new Color(0.45f, 0.70f, 0.30f, 0.9f);  // light green
    public Color colorSeg5 = new Color(0.29f, 0.60f, 0.25f, 0.9f);  // dark green

    [Header("Label & Warning")]
    public TextMeshProUGUI batteryText;
    public TextMeshProUGUI warningLabel;

    [Header("Flicker")]
    public float pulseSpeedNormal = 2f;
    public float pulseSpeedCritical = 5f;

    private Color[] segmentColors;

    void Start()
    {
        segmentColors = new Color[]
        {
            colorSeg1,
            colorSeg2,
            colorSeg3,
            colorSeg4,
            colorSeg5
        };
    }

    void Update()
    {
        if (lantern == null) return;

        float battery = lantern.GetTotalBattery01();
        int activeCount = Mathf.CeilToInt(battery * segments.Length);
        activeCount = Mathf.Clamp(activeCount, 0, segments.Length);

        UpdateSegments(activeCount);
        UpdateText(battery);
        UpdateWarning(activeCount);
    }

    void UpdateSegments(int activeCount)
    {
        Color activeColor = activeCount > 0 ? segmentColors[activeCount - 1] : Color.clear;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;

            bool isActive = i < activeCount;
            segments[i].enabled = isActive;

            if (!isActive) continue;

            bool isCurrentSegment = (i == activeCount - 1);

            Color c = activeColor;

            if (isCurrentSegment)
            {
                float speed = activeCount == 1 ? pulseSpeedCritical : pulseSpeedNormal;
                float pulse = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
                c.a = Mathf.Lerp(0.25f, 0.9f, pulse);
            }
            else
            {
                c.a = 0.9f;
            }

            segments[i].color = c;
        }
    }

    void UpdateText(float battery)
    {
        if (batteryText == null) return;
        batteryText.text = Mathf.RoundToInt(battery * 100f) + "%";
    }

    void UpdateWarning(int activeCount)
    {
        if (warningLabel == null) return;

        if (activeCount == 0) warningLabel.text = "DEAD";
        else if (activeCount == 1) warningLabel.text = "CRITICAL";
        else if (activeCount == 2) warningLabel.text = "LOW";
        else warningLabel.text = "";
    }
}