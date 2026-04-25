using UnityEngine;
using System.Collections;

public class SimpleDisappearMonster : MonoBehaviour, IResettable
{
    [Header("Settings")]
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private bool randomizeLifetime = false;
    [SerializeField] private Vector2 lifetimeRange = new Vector2(2f, 5f);

    private Coroutine lifeRoutine;
    private bool active;
    private bool hasBeenSeen;

    private void OnEnable()
    {
        active = true;
        hasBeenSeen = false;

        // ❌ DON'T start routine here anymore
    }

    // 👉 Call this from your vision system / raycast / trigger
    public void OnSeen()
    {
        if (!active || hasBeenSeen) return;

        hasBeenSeen = true;

        float duration = randomizeLifetime
            ? Random.Range(lifetimeRange.x, lifetimeRange.y)
            : lifeTime;

        lifeRoutine = StartCoroutine(LifeRoutine(duration));
    }

    private IEnumerator LifeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!active) yield break;

        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        active = false;
        hasBeenSeen = false;

        if (lifeRoutine != null)
            StopCoroutine(lifeRoutine);

        gameObject.SetActive(false);
    }
}