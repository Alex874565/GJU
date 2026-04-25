using System.Collections;
using UnityEngine;
using TMPro;

public class ButtonFlicker : MonoBehaviour
{
    public TextMeshProUGUI buttonText;

    [Header("Audio")]
    [SerializeField] private AudioClip[] crackleClips;
    [SerializeField] private float crackleVolume = 0.4f;
    
    public IEnumerator DoFlicker()
    {
        Color original = buttonText.color;

        int flickCount = Random.Range(3, 7);

        for (int i = 0; i < flickCount; i++)
        {
            float alpha = Random.Range(0.05f, 0.25f);
            Flick(alpha);

            yield return new WaitForSecondsRealtime(Random.Range(0.02f, 0.06f));

            Flick(1f);

            yield return new WaitForSecondsRealtime(Random.Range(0.02f, 0.08f));
        }

        // final settle
        SetAlpha(1f);
        buttonText.color = original;
    }
    
    void Flick(float alpha)
    {
        SetAlpha(alpha);

        float intensity = 1f - alpha;
        PlayCrackle(intensity);
    }

    void PlayCrackle(float intensity)
    {
        if (crackleClips == null || crackleClips.Length == 0)
            return;

        AudioClip clip = crackleClips[Random.Range(0, crackleClips.Length)];

        float volume = Mathf.Lerp(0.1f, 0.5f, intensity);

        // 🔥 UI = slightly higher & sharper
        float pitch = Random.Range(1.05f, 1.3f);

        AudioManager.PlaySFXWithPitch(clip, transform.position, volume, pitch);
    }

    void SetAlpha(float alpha)
    {
        Color c = buttonText.color;
        c.a = alpha;
        buttonText.color = c;
    }
}