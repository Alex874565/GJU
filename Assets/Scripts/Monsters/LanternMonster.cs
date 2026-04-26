using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class LanternMonster : MonoBehaviour, IResettable
{
    [Header("References")]
    private PlayerManager playerManager;
    private Camera playerCamera;

    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private MonsterVisibility monsterVisibility;

    [Header("Spawn / Camera Edge Warp")]
    [SerializeField] private float minSpawnDistance = 2f;
    [SerializeField] private float maxSpawnDistance = 6f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private int spawnAttempts = 100;

    [Header("Lantern Timers")]
    [SerializeField] private float lanternOnTimeToAttack = 2f;
    [SerializeField] private float lanternOffTimeToDespawn = 4f;

    [Header("Attack")]
    [SerializeField] private float invisibleTimeBeforeAttack = 0.35f;
    [SerializeField] private float monsterCenterHeight = 1.6f;
    [SerializeField] private float overshootMultiplier = 1.15f;
    [SerializeField] private float clipThroughTime = 0.05f;
    [SerializeField] private float lungeStartDistance = 3.5f;

    [Header("Flicker Before Attack")]
    [SerializeField] private float flickerDuration = 2f;
    [SerializeField] private float startHiddenTime = 0.45f;
    [SerializeField] private float endHiddenTime = 0.03f;
    [SerializeField] private Vector2 flashTimeRange = new Vector2(0.025f, 0.07f);
    [SerializeField] private float doubleFlashChance = 0.45f;
    [SerializeField] private float doubleFlashGap = 0.04f;
    [SerializeField] private float jitterAmount = 0.12f;
    
    [SerializeField] private float flickerDistance = 2.2f;
    [SerializeField] private float cameraSideJitter = 0.15f;
    [SerializeField] private float cameraUpJitter = 0.08f;


    [Header("Jumpscare Movement")]
    [SerializeField] private float lungeDuration = 0.25f;
    [SerializeField] private float stopDistanceFromCamera = 0.8f;

    [Header("Despawn")]
    [SerializeField] private AudioClip[] despawnSounds;
    [SerializeField] private float despawnVolume = 1f;
    [SerializeField] private float maxDespawnWait = 2f;
    
    [SerializeField] private Lantern lantern;

    [SerializeField] private float frontSpawnDistance = 3f;
    [SerializeField] private float frontSpawnSideRandomness = 0.8f;
    [SerializeField] private float frontSpawnVerticalOffset = 0f;

    [SerializeField] private MonsterLookAtPlayer lookAtPlayer;
    
    [SerializeField] private SpriteRenderer monsterSprite;
    
    [SerializeField] private float flickerHeightOffset = -0.6f;
    
    [SerializeField] private float jumpVolume = 0.7f;

    private bool attacking;
    private bool attacked;
    private bool isDespawning;
    private bool wasLanternOn;

    private float lanternOnTimer;
    private float lanternOffTimer;

    private void Awake()
    {
        if (lookAtPlayer == null)
            lookAtPlayer = GetComponentInChildren<MonsterLookAtPlayer>();
        
        if (monsterVisibility == null)
            monsterVisibility = GetComponent<MonsterVisibility>();

        playerManager = FindObjectOfType<PlayerManager>();

        if (playerManager != null)
            playerCamera = playerManager.GetComponentInChildren<Camera>();
    }

    private void OnEnable()
    {
        if (lantern == null)
            lantern = FindObjectOfType<Lantern>();

        if (lantern != null)
        {
            lantern.OnLanternTurnedOn -= HandleLanternTurnedOn;
            lantern.OnLanternTurnedOn += HandleLanternTurnedOn;
        }
        
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerManager>();

        if (playerCamera == null && playerManager != null)
            playerCamera = playerManager.GetComponentInChildren<Camera>();

        if (monsterVisibility == null)
            monsterVisibility = GetComponent<MonsterVisibility>();

        attacking = false;
        attacked = false;
        isDespawning = false;

        lanternOnTimer = 0f;
        lanternOffTimer = 0f;

        bool lanternOn = playerManager != null && !playerManager.IsLanternOff;
        bool lightsOff = playerManager != null && playerManager.AreLightsOff;

        wasLanternOn = lanternOn;

        monsterVisual.SetActive(true);
        SetMonsterVisible(lanternOn && lightsOff);
    }
    
    private void OnDisable()
    {
        if (lantern != null)
            lantern.OnLanternTurnedOn -= HandleLanternTurnedOn;
    }
    
    public void SpawnFromSound(Vector3 soundPosition)
    {
        bool warped = TrySpawnAtCameraCorner();

        if (!warped)
            WarpInFrontOfPlayer();

        monsterVisual?.SetActive(false); // stays hidden until lantern turns on
    }
    
    private void SetMonsterVisible(bool visible)
    {
        if (monsterSprite != null)
            monsterSprite.enabled = visible;
    }
    
    private void HandleLanternTurnedOn()
    {
        if (!isActiveAndEnabled) return;
        if (attacking || attacked || isDespawning) return;
        if (playerCamera == null) return;

        bool warped = TrySpawnAtCameraCorner();

        if (!warped)
            WarpInFrontOfPlayer();

        monsterVisual?.SetActive(true);
    }
    
    private void WarpInFrontOfPlayer()
    {
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = playerCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 target =
            playerCamera.transform.position +
            forward * frontSpawnDistance +
            right * Random.Range(-frontSpawnSideRandomness, frontSpawnSideRandomness);

        target.y += frontSpawnVerticalOffset;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            transform.position = hit.position;
        else
            transform.position = target;
    }

    private void Update()
    {
        if (playerManager == null || attacked || isDespawning)
            return;

        bool lanternOn = !playerManager.IsLanternOff;
        bool lightsOff = playerManager.AreLightsOff;

        if (!attacking)
            SetMonsterVisible(lanternOn && lightsOff);

        if (attacking)
            return;

        if (lanternOn && lightsOff)
        {
            lanternOnTimer += Time.deltaTime;

            if (lanternOnTimer >= lanternOnTimeToAttack)
                StartCoroutine(FlickerThenAttackRoutine());
        }
        else
        {
            lanternOffTimer += Time.deltaTime;

            if (lanternOffTimer >= lanternOffTimeToDespawn)
                StartCoroutine(DespawnRoutine());
        }
    }

    private IEnumerator DespawnRoutine()
    {
        if (isDespawning || attacking || attacked)
            yield break;

        isDespawning = true;
        monsterVisibility?.ClearVisibility();

        if (monsterVisual != null)
            monsterVisual.SetActive(false);

        float delay = Mathf.Clamp(PlayDespawnSound(), 0.05f, maxDespawnWait);

        yield return new WaitForSeconds(delay);
        
        DeactivateMonster();
    }

    private float PlayDespawnSound()
    {
        if (despawnSounds == null || despawnSounds.Length == 0)
            return 0f;

        AudioClip clip = despawnSounds[Random.Range(0, despawnSounds.Length)];

        AudioManager.PlaySFX(clip, transform.position, despawnVolume);

        return clip.length;
    }

    private bool TrySpawnAtCameraCorner()
    {
        if (playerCamera == null)
            return false;

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
            return true;
        }

        return false;
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
        if (attacking || attacked || isDespawning)
            yield break;

        attacking = true;
        SetMonsterVisible(true);

        float timer = 0f;

        while (timer < flickerDuration)
        {
            float t = timer / flickerDuration;
            float hiddenTime = Mathf.Lerp(startHiddenTime, endHiddenTime, t);

            yield return new WaitForSeconds(hiddenTime);
            timer += hiddenTime;

            yield return FlashOnce(t);

            if (Random.value < doubleFlashChance)
            {
                yield return new WaitForSeconds(doubleFlashGap);
                timer += doubleFlashGap;

                yield return FlashOnce(t);
            }
        } 
       
        SetMonsterVisible(false);

        yield return new WaitForSeconds(invisibleTimeBeforeAttack);

        yield return StartCoroutine(AttackRoutine());
    }
    
    private Vector3 GetCameraFrontFlickerPosition()
    {
        return GetCameraFrontPosition(flickerDistance);
    }
    
    private Vector3 GetCameraFrontPosition(float distance)
    {
        Vector3 camPos = playerCamera.transform.position;

        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = playerCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 pos =
            camPos +
            forward * distance +
            right * Random.Range(-cameraSideJitter, cameraSideJitter);

        pos.y = camPos.y + flickerHeightOffset;

        return pos;
    }

    private IEnumerator FlashOnce(float intensity)
    {
        if (isDespawning)
            yield break;

        Vector3 basePos = GetCameraFrontPosition(flickerDistance);

        Vector3 jitter = new Vector3(
            Random.Range(-jitterAmount, jitterAmount) * intensity,
            Random.Range(-jitterAmount * 0.4f, jitterAmount * 0.4f) * intensity,
            Random.Range(-jitterAmount, jitterAmount) * intensity
        );

        transform.position = basePos + jitter;

        if (lookAtPlayer != null)
            lookAtPlayer.ForceFacePlayer();

        monsterVisual?.SetActive(true);

        yield return new WaitForSeconds(Random.Range(flashTimeRange.x, flashTimeRange.y));

        monsterVisual?.SetActive(false);
    }

    private IEnumerator AttackRoutine()
    {
        if (isDespawning)
            yield break;

        attacked = true;
        
        SetMonsterVisible(true);
        monsterVisibility?.ClearVisibility();

        if (lookAtPlayer != null)
            lookAtPlayer.enabled = false;

        if (jumpSound != null)
            AudioManager.PlaySFX(jumpSound, transform.position);

        playerManager?.AddAnxiety(120f);

        Vector3 camPos = playerCamera.transform.position;
        Vector3 camForward = playerCamera.transform.forward.normalized;

        // Starts directly in front of the camera
        Vector3 startPos = camPos + camForward * lungeStartDistance;

        // Ends close to camera center
        Vector3 targetPos = camPos + camForward * stopDistanceFromCamera;

        // Passes through camera
        Vector3 overshootPos = camPos - camForward * 0.6f;

        transform.position = startPos;

        float timer = 0f;

        while (timer < lungeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / lungeDuration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        float clipTimer = 0f;

        while (clipTimer < clipThroughTime)
        {
            clipTimer += Time.deltaTime;
            float t = clipTimer / clipThroughTime;

            transform.position = Vector3.Lerp(targetPos, overshootPos, t);
            yield return null;
        }

        if (lookAtPlayer != null)
            lookAtPlayer.enabled = true;

        DeactivateMonster();
    }

    private void DeactivateMonster()
    {
        StopAllCoroutines();

        monsterVisibility?.ClearVisibility();

        if (monsterVisual != null)
            monsterVisual.SetActive(false);
        
        DialogueManager.Instance?.StopMonsterDialogue();
        
        MonsterSpawnManager.Instance?.UnregisterSpawn();

        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        attacking = false;
        attacked = false;
        isDespawning = false;

        lanternOnTimer = 0f;
        lanternOffTimer = 0f;

        DeactivateMonster();
    }
}