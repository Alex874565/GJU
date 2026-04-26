using System.Collections;
using UnityEngine;

public class TwitchingMonster : MonoBehaviour
{
    [System.Serializable]
    public class FrameSet
    {
        public string name;
        public Sprite[] frames;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frame Sets")]
    [SerializeField] private FrameSet[] frameSets;
    [SerializeField] private int selectedSetIndex = 0;
    [SerializeField] private bool randomizeSetOnEnable = false;

    [Header("Timing")]
    [SerializeField] private bool continuous = true;
    [SerializeField] private float minInterval = 0.03f;
    [SerializeField] private float maxInterval = 0.12f;

    [Header("Burst")]
    [SerializeField] private int minBurst = 2;
    [SerializeField] private int maxBurst = 6;
    [SerializeField] private float minBurstDelay = 0.3f;
    [SerializeField] private float maxBurstDelay = 1.5f;

    private Sprite[] activeFrames;
    private int currentIndex;
    private Coroutine routine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void OnEnable()
    {
        ChooseFrameSet();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"{name}: Missing SpriteRenderer.");
            return;
        }

        if (activeFrames == null || activeFrames.Length == 0)
        {
            Debug.LogWarning($"{name}: No active frames assigned.");
            return;
        }

        SetFrame(0);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(TwitchLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void ChooseFrameSet()
    {
        if (frameSets == null || frameSets.Length == 0)
        {
            activeFrames = null;
            return;
        }

        int index = randomizeSetOnEnable
            ? Random.Range(0, frameSets.Length)
            : Mathf.Clamp(selectedSetIndex, 0, frameSets.Length - 1);

        activeFrames = frameSets[index].frames;
    }

    private IEnumerator TwitchLoop()
    {
        while (true)
        {
            if (continuous)
            {
                NextFrame();
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }
            else
            {
                yield return new WaitForSeconds(Random.Range(minBurstDelay, maxBurstDelay));

                int burstCount = Random.Range(minBurst, maxBurst + 1);

                for (int i = 0; i < burstCount; i++)
                {
                    NextFrame();
                    yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
                }
            }
        }
    }

    private void NextFrame()
    {
        if (activeFrames == null || activeFrames.Length == 0 || spriteRenderer == null)
            return;

        int next = Random.Range(0, activeFrames.Length);

        if (activeFrames.Length > 1 && next == currentIndex)
            next = (next + 1) % activeFrames.Length;

        SetFrame(next);
    }

    private void SetFrame(int index)
    {
        if (activeFrames == null || index < 0 || index >= activeFrames.Length)
            return;

        if (activeFrames[index] == null)
            return;

        currentIndex = index;
        spriteRenderer.sprite = activeFrames[index];
    }

    public void SetFrameSet(int index)
    {
        if (frameSets == null || frameSets.Length == 0)
            return;

        selectedSetIndex = Mathf.Clamp(index, 0, frameSets.Length - 1);
        ChooseFrameSet();
        SetFrame(0);
    }
}