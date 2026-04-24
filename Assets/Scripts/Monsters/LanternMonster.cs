using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class LanternMonster : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;

    [Header("Spawn")]
    [SerializeField] private float minAppearDelay = 3f;
    [SerializeField] private float maxAppearDelay = 8f;
    [SerializeField] private float minSpawnDistance = 2f;
    [SerializeField] private float maxSpawnDistance = 6f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private int spawnAttempts = 100;

    [Header("Attack")]
    [SerializeField] private float waitBeforeAttack = 2f;
    [SerializeField] private float invisibleTimeBeforeAttack = 0.35f;
    [SerializeField] private float deactivateAfterAttack = 0.1f;

    [Header("Flicker")]
    [SerializeField] private float flickerDuration = 2f;
    [SerializeField] private float startHiddenTime = 0.45f;
    [SerializeField] private float endHiddenTime = 0.03f;
    [SerializeField] private Vector2 flashTimeRange = new Vector2(0.025f, 0.07f);
    [SerializeField] private float doubleFlashChance = 0.45f;
    [SerializeField] private float doubleFlashGap = 0.04f;
    [SerializeField] private float jitterAmount = 0.12f;

    [Header("Jumpscare Movement")]
    [SerializeField] private float lungeDuration = 0.25f;
    [SerializeField] private float stopDistanceFromCamera = 0.8f;

    private bool appeared;
    private bool attacked;
    private bool attacking;
    private bool wasLanternOn;

    private float appearDelay;
    private float appearTimer;
    private float attackTimer;

    private void Start()
    {
        appearDelay = Random.Range(minAppearDelay, maxAppearDelay);

        bool lanternOn = playerManager != null && !playerManager.IsLanternOff;
        bool lightsOff = playerManager != null && playerManager.AreLightsOff;

        wasLanternOn = lanternOn;

        appeared = lanternOn && lightsOff;

        if (monsterVisual != null)
            monsterVisual.SetActive(appeared);
    }

    private void Update()
    {
        if (attacked || playerManager == null) return;

        bool lanternOn = !playerManager.IsLanternOff;
        bool lightsOff = playerManager.AreLightsOff;
        bool condition = lanternOn && lightsOff;

        bool lanternJustTurnedOn = lanternOn && !wasLanternOn;
        wasLanternOn = lanternOn;

        if (lanternJustTurnedOn && condition && appeared && !attacking)
        {
            TrySpawnAtCameraCorner(false);
        }

        if (!appeared)
        {
            if (!lanternOn) return;

            appearTimer += Time.deltaTime;

            if (appearTimer >= appearDelay)
                TrySpawnAtCameraCorner(true);

            return;
        }

        if (!attacking && monsterVisual != null)
            monsterVisual.SetActive(condition);

        if (!condition) return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= waitBeforeAttack)
            StartCoroutine(FlickerThenAttackRoutine());
    }

    private void TrySpawnAtCameraCorner(bool resetAttackTimer)
    {
        for (int i = 0; i < spawnAttempts; i++)
        {
            Vector3 viewportPoint = GetRandomCornerViewportPoint();
            Ray ray = playerCamera.ViewportPointToRay(viewportPoint);

            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 wantedPos = ray.origin + ray.direction * distance;

            if (!NavMesh.SamplePosition(wantedPos, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                continue;

            Vector3 spawnPos = hit.position;
            Vector3 viewport = playerCamera.WorldToViewportPoint(spawnPos);

            bool inView =
                viewport.z > 0f &&
                viewport.x > 0.02f && viewport.x < 0.98f &&
                viewport.y > 0.02f && viewport.y < 0.98f;

            bool nearCorner =
                (viewport.x < 0.35f || viewport.x > 0.65f) &&
                (viewport.y < 0.35f || viewport.y > 0.65f);

            if (!inView || !nearCorner)
                continue;

            transform.position = spawnPos;

            Vector3 lookDir = playerCamera.transform.position - transform.position;
            lookDir.y = 0f;

            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            appeared = true;

            if (resetAttackTimer)
                attackTimer = 0f;

            monsterVisual?.SetActive(true);
            return;
        }

        Debug.LogWarning("LanternMonster failed to find corner spawn.");
    }

    private Vector3 GetRandomCornerViewportPoint()
    {
        float x = Random.value < 0.5f
            ? Random.Range(0.08f, 0.25f)
            : Random.Range(0.75f, 0.92f);

        float y = Random.value < 0.5f
            ? Random.Range(0.08f, 0.25f)
            : Random.Range(0.75f, 0.92f);

        return new Vector3(x, y, 0f);
    }

    private IEnumerator FlickerThenAttackRoutine()
    {
        if (attacking) yield break;
        attacking = true;

        Vector3 originalPos = transform.position;
        float timer = 0f;

        monsterVisual.SetActive(false);

        while (timer < flickerDuration)
        {
            float t = timer / flickerDuration;
            float hiddenTime = Mathf.Lerp(startHiddenTime, endHiddenTime, t);

            yield return new WaitForSeconds(hiddenTime);
            timer += hiddenTime;

            yield return FlashOnce(originalPos, t);

            if (Random.value < doubleFlashChance)
            {
                yield return new WaitForSeconds(doubleFlashGap);
                timer += doubleFlashGap;

                yield return FlashOnce(originalPos, t);
            }

            timer += flashTimeRange.y;
        }

        transform.position = originalPos;
        monsterVisual.SetActive(false);

        yield return new WaitForSeconds(invisibleTimeBeforeAttack);

        yield return StartCoroutine(AttackRoutine());
    }

    private IEnumerator FlashOnce(Vector3 originalPos, float intensity)
    {
        Vector3 jitter = new Vector3(
            Random.Range(-jitterAmount, jitterAmount) * intensity,
            Random.Range(-jitterAmount * 0.4f, jitterAmount * 0.4f) * intensity,
            Random.Range(-jitterAmount, jitterAmount) * intensity
        );

        transform.position = originalPos + jitter;
        monsterVisual.SetActive(true);

        yield return new WaitForSeconds(Random.Range(flashTimeRange.x, flashTimeRange.y));

        monsterVisual.SetActive(false);
        transform.position = originalPos;
    }

    private IEnumerator AttackRoutine()
    {
        attacked = true;

        monsterVisual?.SetActive(true);

        if (audioSource != null && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);

        playerManager?.AddAnxiety(100f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = playerCamera.transform.position - playerCamera.transform.forward * stopDistanceFromCamera;

        float timer = 0f;

        while (timer < lungeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / lungeDuration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.LookAt(playerCamera.transform);

            yield return null;
        }

        yield return new WaitForSeconds(deactivateAfterAttack);

        monsterVisual?.SetActive(false);
        gameObject.SetActive(false);
    }
}