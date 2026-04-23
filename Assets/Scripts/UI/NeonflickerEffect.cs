using System.Collections;
using UnityEngine;
using TMPro;

public class NeonFlicker : MonoBehaviour
{
    [Header("Neon Color")]
    public Color neonColor = new Color(0.78f, 0.65f, 0.42f, 1f);

    [Header("Flicker Timing")]
    public float minTimeBetweenFlickers = 4f;
    public float maxTimeBetweenFlickers = 10f;

    private TextMeshProUGUI tmp;

    public void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.color = neonColor;
        StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            float wait = Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers);
            yield return new WaitForSeconds(wait);
            yield return StartCoroutine(DoFlicker());
        }
    }

    public IEnumerator DoFlickerAndReturn()
    {
        yield return StartCoroutine(DoFlicker());
    }

    public IEnumerator DoFlicker()
    {
        SetAlpha(0.05f);
        yield return new WaitForSeconds(0.06f);

        SetAlpha(1f);
        yield return new WaitForSeconds(0.08f);

        SetAlpha(0.1f);
        yield return new WaitForSeconds(0.05f);

        SetAlpha(1f);
        yield return new WaitForSeconds(0.07f);

        SetAlpha(0.03f);
        yield return new WaitForSeconds(0.35f);

        SetAlpha(1f);
    }

    void SetAlpha(float alpha)
    {
        Color c = neonColor;
        c.a = alpha;
        tmp.color = c;
    }
}