using System.Collections;
using UnityEngine;
using TMPro;

public class ButtonFlicker : MonoBehaviour
{
    public TextMeshProUGUI buttonText;

    public IEnumerator DoFlicker()
    {
        Color original = buttonText.color;

        SetAlpha(0.05f);
        yield return new WaitForSeconds(0.04f);
        SetAlpha(1f);
        yield return new WaitForSeconds(0.05f);
        SetAlpha(0.1f);
        yield return new WaitForSeconds(0.04f);
        SetAlpha(1f);

        buttonText.color = original;
    }

    void SetAlpha(float alpha)
    {
        Color c = buttonText.color;
        c.a = alpha;
        buttonText.color = c;
    }
}