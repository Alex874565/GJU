using System.Collections;
using UnityEngine;
using TMPro;

public class NeonFlicker : MonoBehaviour
{
    [Header("Neon Color")]
    [SerializeField] private Color neonColor = new Color(0.78f, 0.65f, 0.42f, 1f);

    [Header("Flicker Timing")]
    [SerializeField] private float minTimeBetweenFlickers = 4f;
    [SerializeField] private float maxTimeBetweenFlickers = 10f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] crackleClips;
    [SerializeField] private float crackleVolume = 0.4f;
    [SerializeField] private Vector2 cracklePitch = new Vector2(0.8f, 1.1f);
    [SerializeField] private float crackleChance = 0.8f;

    private TextMeshProUGUI tmp;
    private Coroutine flickerLoopRoutine;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        if (tmp != null)
            tmp.color = neonColor;
    }

    private void OnEnable()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();

        if (tmp != null)
            tmp.color = neonColor;

        if (flickerLoopRoutine == null)
            flickerLoopRoutine = StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        if (flickerLoopRoutine != null)
        {
            StopCoroutine(flickerLoopRoutine);
            flickerLoopRoutine = null;
        }
    }

    private IEnumerator FlickerLoop()
    {
        while (true)
        {
            float wait = Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers);
            yield return new WaitForSeconds(wait);
            yield return DoFlicker();
        }
    }

    public IEnumerator DoFlickerAndReturn()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();

        if (tmp == null)
            yield break;

        yield return DoFlicker();

        SetAlpha(1f);
    }

    public IEnumerator DoFlicker()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();

        if (tmp == null)
            yield break;

        SetAlpha(0.05f);
        PlayCrackle(0.05f);
        yield return new WaitForSeconds(0.06f);

        SetAlpha(1f);
        yield return new WaitForSeconds(0.08f);

        SetAlpha(0.1f);
        PlayCrackle(0.1f);
        yield return new WaitForSeconds(0.05f);

        SetAlpha(1f);
        yield return new WaitForSeconds(0.07f);

        SetAlpha(0.03f);
        PlayCrackle(0.03f);
        yield return new WaitForSeconds(0.35f);

        SetAlpha(1f);
    }

    private void PlayCrackle(float intensity)
    {
        if (crackleClips == null || crackleClips.Length == 0)
            return;

        // 🔥 always crackle on deep dips
        bool isDeepFlicker = intensity < 0.08f;

        if (!isDeepFlicker && Random.value > crackleChance)
            return;

        AudioClip clip = crackleClips[Random.Range(0, crackleClips.Length)];

        float pitch = Random.Range(cracklePitch.x, cracklePitch.y);

        // deeper dip = louder
        float volume = Mathf.Lerp(0.2f, crackleVolume, 1f - intensity);

        AudioManager.PlayUISFX(clip, volume, pitch);
    }
    
    private void SetAlpha(float alpha)
    {
        if (tmp == null)
            return;

        Color c = neonColor;
        c.a = alpha;
        tmp.color = c;
    }

    public void RestartFlickerLoop()
    {
        if (!isActiveAndEnabled)
            return;

        if (flickerLoopRoutine != null)
            StopCoroutine(flickerLoopRoutine);

        flickerLoopRoutine = StartCoroutine(FlickerLoop());
    }
}