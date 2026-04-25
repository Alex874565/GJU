using System.Collections;
using UnityEngine;

public class TwitchingSpriteMonster : MonoBehaviour
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

    [Header("Twitch Timing")]
    [SerializeField] private float minInterval = 0.03f;
    [SerializeField] private float maxInterval = 0.12f;

    [Header("Burst")]
    [SerializeField] private int minBurst = 2;
    [SerializeField] private int maxBurst = 6;
    [SerializeField] private float minBurstDelay = 0.3f;
    [SerializeField] private float maxBurstDelay = 1.5f;

    [Header("Jitter")]
    [SerializeField] private float positionJitter = 0.02f;
    [SerializeField] private float rotationJitter = 3f;

    private Sprite[] activeFrames;
    private int currentIndex;
    private Coroutine routine;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
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

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
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

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
        }
    }

    private void NextFrame()
    {
        if (activeFrames == null || activeFrames.Length == 0)
            return;

        int next = Random.Range(0, activeFrames.Length);

        if (next == currentIndex)
            next = (next + 1) % activeFrames.Length;

        SetFrame(next);

        transform.localPosition = baseLocalPosition + Random.insideUnitSphere * positionJitter;

        transform.localRotation = baseLocalRotation * Quaternion.Euler(
            0f,
            0f,
            Random.Range(-rotationJitter, rotationJitter)
        );
    }

    private void SetFrame(int index)
    {
        currentIndex = index;

        if (spriteRenderer != null && activeFrames[index] != null)
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