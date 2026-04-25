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

    private void OnEnable()
    {
        active = true;

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

        if (lifeRoutine != null)
            StopCoroutine(lifeRoutine);

        gameObject.SetActive(false);
    }
}