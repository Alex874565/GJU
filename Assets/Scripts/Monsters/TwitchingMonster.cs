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
    [SerializeField] private Transform visualRoot;

    [Header("Frame Sets")]
    [SerializeField] private FrameSet[] frameSets;
    [SerializeField] private int selectedSetIndex = 0;
    [SerializeField] private bool randomizeSetOnEnable = false;

    [Header("Twitch Timing")]
    [SerializeField] private float minInterval = 0.03f;
    [SerializeField] private float maxInterval = 0.12f;

    [Header("Burst")]
    [SerializeField] private int minBurst = 2;
    [SerializeField] private int maxBurst = 6;
    [SerializeField] private float minBurstDelay = 0.3f;
    [SerializeField] private float maxBurstDelay = 1.5f;

    [Header("Visual Jitter Only")]
    [SerializeField] private float positionJitter = 0.02f;
    [SerializeField] private float rotationJitter = 3f;

    private Sprite[] activeFrames;
    private int currentIndex;
    private Coroutine routine;

    private Vector3 baseVisualLocalPosition;
    private Quaternion baseVisualLocalRotation;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualRoot == null && spriteRenderer != null)
            visualRoot = spriteRenderer.transform;

        if (visualRoot != null)
        {
            baseVisualLocalPosition = visualRoot.localPosition;
            baseVisualLocalRotation = visualRoot.localRotation;
        }
    }

    private void OnEnable()
    {
        ChooseFrameSet();

        if (activeFrames == null || activeFrames.Length == 0 || spriteRenderer == null)
            return;

        SetFrame(0);
        routine = StartCoroutine(TwitchLoop());
    }

    private void OnDisable()
    {
        if (routine != null)
            StopCoroutine(routine);

        ResetVisual();
    }

    private void ChooseFrameSet()
    {
        if (frameSets == null || frameSets.Length == 0)
            return;

        int index = randomizeSetOnEnable
            ? Random.Range(0, frameSets.Length)
            : Mathf.Clamp(selectedSetIndex, 0, frameSets.Length - 1);

        activeFrames = frameSets[index].frames;
    }

    private IEnumerator TwitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBurstDelay, maxBurstDelay));

            int burstCount = Random.Range(minBurst, maxBurst + 1);

            for (int i = 0; i < burstCount; i++)
            {
                NextFrame();
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }

            ResetVisual();
        }
    }

    private void NextFrame()
    {
        int next = Random.Range(0, activeFrames.Length);

        if (next == currentIndex)
            next = (next + 1) % activeFrames.Length;

        SetFrame(next);

        if (visualRoot == null) return;

        Vector3 jitter = Random.insideUnitSphere * positionJitter;
        jitter.z = 0f;

        visualRoot.localPosition = baseVisualLocalPosition + jitter;
        visualRoot.localRotation = baseVisualLocalRotation * Quaternion.Euler(
            0f,
            0f,
            Random.Range(-rotationJitter, rotationJitter)
        );
    }

    private void ResetVisual()
    {
        if (visualRoot == null) return;

        visualRoot.localPosition = baseVisualLocalPosition;
        visualRoot.localRotation = baseVisualLocalRotation;
    }

    private void SetFrame(int index)
    {
        currentIndex = index;

        if (activeFrames[index] != null)
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