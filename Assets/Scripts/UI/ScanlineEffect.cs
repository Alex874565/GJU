using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScanlineEffect : MonoBehaviour
{
    [Header("References")]
    public RectTransform scanline1;
    public RectTransform scanline2;

    private float canvasHeight;

    void Start()
    {
        canvasHeight = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.height;

        if (scanline1 != null) StartCoroutine(RunScanline(scanline1, 0.9f, 0f, 0.55f));
        if (scanline2 != null) StartCoroutine(RunScanline(scanline2, 0.8f, 3.5f, 0.25f));
    }

    IEnumerator RunScanline(RectTransform rt, float speed, float initialDelay, float maxAlpha)
    {
        Image img = rt.GetComponent<Image>();
        img.color = new Color(0.78f, 0.65f, 0.42f, 0f);

        rt.anchoredPosition = new Vector2(0f, canvasHeight);

        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            rt.anchoredPosition = new Vector2(0f, canvasHeight);

            float e = 0f;
            while (e < 0.2f)
            {
                e += Time.deltaTime;
                img.color = new Color(0.78f, 0.65f, 0.42f, Mathf.Lerp(0f, maxAlpha, e / 0.2f));
                yield return null;
            }

            float totalDistance = canvasHeight * 2f;
            float traveled = 0f;

            while (traveled < totalDistance)
            {
                float step = speed * Time.deltaTime * 100f;
                rt.anchoredPosition += Vector2.down * step;
                traveled += step;

                float progress = traveled / totalDistance;
                if (progress > 0.7f)
                {
                    float fadeOut = 1f - ((progress - 0.7f) / 0.3f);
                    img.color = new Color(0.78f, 0.65f, 0.42f, maxAlpha * Mathf.Clamp01(fadeOut));
                }

                yield return null;
            }

            img.color = new Color(0.78f, 0.65f, 0.42f, 0f);
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }
}