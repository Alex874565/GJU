using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterTeleporter : MonoBehaviour, IResettable 
{
    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Type")]
    [SerializeField] private MonsterType monsterType;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    private PlayerManager playerManager;
    [SerializeField] private MonsterVisibility monsterVisibility;

    private Transform player;

    [Header("Teleport Settings")]
    [SerializeField] private float minDistanceToTeleport = 6f;
    [SerializeField] private float teleportIntervalMin = 1.5f;
    [SerializeField] private float teleportIntervalMax = 3f;
    [SerializeField] private float maxVerticalDifference = 1.5f;

    [Header("Distance Control")]
    [SerializeField] private float minDistanceFromPlayer = 2.5f;
    [SerializeField] private float minTeleportStep = 2f;
    [SerializeField] private float maxTeleportStep = 6f;

    [Header("NavMesh")]
    [SerializeField] private float navSampleRadius = 5f;
    [SerializeField] private int maxTeleportAttempts = 8;

    [Header("Blocking")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Search Type")]
    [SerializeField] private float hideWaitTime = 3f;
    private float hideTimer;

    [Header("Stalker Area")] [SerializeField]
    private float range = 5f;

    [Header("Look Away Delay (Stalker)")]
    [SerializeField] private float minLookAwayDelay = 0.2f;
    [SerializeField] private float maxLookAwayDelay = 0.7f;

    private float lookAwayTimer;
    private float currentLookAwayDelay;

    [Header("Direction Randomness")]
    [SerializeField] private float sidewaysAngle = 20f;
    [SerializeField] private float behindSidewaysAngle = 12f;
    
    [Header("Behind Teleport")]
    [SerializeField] private bool allowTeleportBehindPlayer = true;
    [SerializeField] private float behindTeleportChance = 0.25f;
    [SerializeField] private float behindDistance = 4f;
    
    [Header("Behind Teleport Range")]
    [SerializeField] private float behindMinDistance = 2f;
    [SerializeField] private float behindMaxDistance = 6f;
    
    [Header("Close Anxiety")]
    [SerializeField] private float closeAnxietyRange = 2f;
    [SerializeField] private float closeAnxietyGainPerSecond = 12f;
    
    [Header("Despawn Audio")]
    [SerializeField] private AudioClip[] despawnSounds;
    [SerializeField] private float despawnVolume = 1f;
    
    [Header("Same Level")]
    [SerializeField] private int sameLevelSampleAttempts = 12;
    
    public bool IsDespawning => isDespawning;

    private float teleportTimer;

    private void Awake()
    {
        playerManager = FindObjectOfType<PlayerManager>();
        
        startPosition = transform.position;
        startRotation = transform.rotation;

        SnapStartToNavMesh();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        
        player = p.transform;
        playerManager = player.gameObject.GetComponent<PlayerManager>();
        monsterVisibility = gameObject.GetComponent<MonsterVisibility>();
        
        teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);

        if (agent != null)
            agent.updateRotation = false;

        ResetLookAwayDelay();
    }
    
    private void OnEnable()
    {
        isDespawning = false;
        hideTimer = 0f;
        lookAwayTimer = 0f;
        teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);
        ResetLookAwayDelay();
    }

    private void Update()
    {
        if (isDespawning) return;
        if (player == null || agent == null) return;

        UpdateCloseAnxiety();

        switch (monsterType)
        {
            case MonsterType.Search:
                UpdateSearchBehavior();
                break;

            case MonsterType.Stalker:
                UpdateStalkerBehavior();
                break;
        }
    }
    
    private void UpdateCloseAnxiety()
    {
        if (isDespawning) return;
        if (playerManager == null || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= closeAnxietyRange)
            playerManager.AddAnxiety(closeAnxietyGainPerSecond * Time.deltaTime);
    }
    // =========================
    // SEARCH TYPE
    // =========================

    private void UpdateSearchBehavior()
    {
        if (playerManager != null && playerManager.IsHidden)
        {
            hideTimer += Time.deltaTime;

            if (hideTimer >= hideWaitTime)
                EndEncounter();

            return;
        }

        hideTimer = 0f;
        HandleTeleport();
    }

    // =========================
    // STALKER TYPE
    // =========================

    private void UpdateStalkerBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > range)
        {
            Debug.Log("Player is out of range, ending encounter.");
            EndEncounter();
            return;
        }

        bool thisMonsterIsVisible =
            monsterVisibility != null &&
            monsterVisibility.IsVisible;

        if (thisMonsterIsVisible)
        {
            ResetLookAwayDelay();
            return;
        }

        lookAwayTimer += Time.deltaTime;

        if (lookAwayTimer < currentLookAwayDelay)
            return;

        HandleTeleport();
    }

    private void ResetLookAwayDelay()
    {
        lookAwayTimer = 0f;
        currentLookAwayDelay = Random.Range(minLookAwayDelay, maxLookAwayDelay);
    }

    // =========================
    // TELEPORT LOGIC
    // =========================

    private void HandleTeleport()
    {
        teleportTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > minDistanceToTeleport && teleportTimer <= 0f)
        {
            TryTeleportCloser();
            teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);
        }
    }

    private void TryTeleportCloser()
    {
        float currentDistance = Vector3.Distance(transform.position, player.position);
        float maxAllowedStep = currentDistance - minDistanceFromPlayer;

        if (maxAllowedStep <= 0f)
            return;

        float minStep = Mathf.Min(minTeleportStep, maxAllowedStep);
        float maxStep = Mathf.Min(maxTeleportStep, maxAllowedStep);

        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            float step = Random.Range(minStep, maxStep);

            Vector3 direction;

            Vector3 rawTarget;

            bool wantsBehindTeleport = allowTeleportBehindPlayer && Random.value < behindTeleportChance;

            if (wantsBehindTeleport)
            {
                Vector3 behindDir = -player.forward;
                float behindAngle = Random.Range(-behindSidewaysAngle, behindSidewaysAngle);
                behindDir = Quaternion.AngleAxis(behindAngle, Vector3.up) * behindDir;

                Vector3 behindTarget = player.position + behindDir * behindDistance;

                float distFromMonster = Vector3.Distance(transform.position, behindTarget);

                bool inRange = distFromMonster >= behindMinDistance && distFromMonster <= behindMaxDistance;

                if (inRange)
                {
                    rawTarget = behindTarget;
                }
                else
                {
                    // fallback to normal teleport
                    Vector3 toPlayer = (player.position - transform.position).normalized;

                    Vector3 randomOffset = Random.insideUnitSphere * 1.5f;
                    randomOffset.y = 0f;

                    Vector3 fallbackDir = (toPlayer + randomOffset).normalized;

                    rawTarget = transform.position + fallbackDir * step;
                }
            }
            else
            {
                Vector3 toPlayer = (player.position - transform.position).normalized;
                float angle = Random.Range(-sidewaysAngle, sidewaysAngle);
                Vector3 fallbackDir = Quaternion.AngleAxis(angle, Vector3.up) * toPlayer;

                rawTarget = transform.position + fallbackDir * step;
            }
            Debug.Log("Trying teleport target: " + rawTarget);
            if (!TrySampleSameLevelNavMesh(rawTarget, out NavMeshHit hit))
                continue;

            float distToPlayer = Vector3.Distance(hit.position, player.position);

            if (distToPlayer < minDistanceFromPlayer)
                continue;

            //if (IsBlocked(hit.position))
                //continue;

            TeleportTo(hit.position);
            return;
        }
    }
    
    private bool TrySampleSameLevelNavMesh(Vector3 center, out NavMeshHit bestHit)
    {
        if (NavMesh.SamplePosition(center, out bestHit, navSampleRadius, NavMesh.AllAreas))
        {
            if (Mathf.Abs(bestHit.position.y - player.position.y) <= maxVerticalDifference)
                return true;
        }

        for (int i = 0; i < sameLevelSampleAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * navSampleRadius;

            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
            candidate.y = player.position.y;

            if (!NavMesh.SamplePosition(candidate, out bestHit, navSampleRadius, NavMesh.AllAreas))
                continue;

            if (Mathf.Abs(bestHit.position.y - player.position.y) <= maxVerticalDifference)
                return true;
        }

        return false;
    }

    private bool IsBlocked(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        return Physics.Raycast(origin, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void SnapStartToNavMesh()
    {
        if (!NavMesh.SamplePosition(startPosition, out NavMeshHit hit, 2, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name} could not snap start position to NavMesh near {startPosition}");
            return;
        }

        startPosition = hit.position;

        if (agent != null)
        {
            agent.enabled = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            agent.enabled = true;
            agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }
    
    private void TeleportTo(Vector3 position)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("Warp failed: target not on NavMesh");
            return;
        }

        agent.enabled = false;
        transform.position = hit.position;
        agent.enabled = true;

        agent.Warp(hit.position);

        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    // =========================
    // END ENCOUNTER
    // =========================

    private bool isDespawning;
    private void EndEncounter()
    {
        if (isDespawning) return;

        isDespawning = true;
        StartCoroutine(DespawnRoutine());
    }
    
    [SerializeField] private float despawnDelay = 0.2f;

    private IEnumerator DespawnRoutine()
    {
        if (playerManager != null)
            playerManager.SetEncounter(false);

        float delay = PlayDespawnSound();

        yield return new WaitForSeconds(delay);
        
        DialogueManager.Instance?.StopMonsterDialogue();
        MonsterSpawnManager.Instance?.UnregisterSpawn();
        gameObject.SetActive(false);
    }
    
    private float PlayDespawnSound()
    {
        if (despawnSounds == null || despawnSounds.Length == 0)
            return 0f;

        AudioClip clip = despawnSounds[Random.Range(0, despawnSounds.Length)];

        AudioManager.PlaySFX(clip, transform.position, despawnVolume);

        return clip.length - .1f; // 👈 key part
    }
    
    public void ResetState()
    {
        hideTimer = 0f;
        lookAwayTimer = 0f;
        teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);
        ResetLookAwayDelay();

        SnapStartToNavMesh();

        transform.rotation = startRotation;

        if (playerManager != null)
            playerManager.SetEncounter(false);

        if (monsterVisibility != null)
            monsterVisibility.ClearVisibility();
        
        MonsterSpawnManager.Instance?.UnregisterSpawn();
        gameObject.SetActive(false);
    }
}