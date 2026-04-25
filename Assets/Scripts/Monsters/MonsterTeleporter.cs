using UnityEngine;
using UnityEngine.AI;

public class MonsterTeleporter : MonoBehaviour, IResettable 
{
    [SerializeField] private enum MonsterType
    {
        Search,
        Stalker
    }
    
    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Type")]
    [SerializeField] private MonsterType monsterType;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerInteract playerInteract;

    private Transform player;

    [Header("Teleport Settings")]
    [SerializeField] private float minDistanceToTeleport = 6f;
    [SerializeField] private float teleportIntervalMin = 1.5f;
    [SerializeField] private float teleportIntervalMax = 3f;

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

    [Header("Stalker Area")]
    [SerializeField] private Collider activeArea;

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

    private float teleportTimer;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        
        player = p.transform;
        playerManager = player.gameObject.GetComponent<PlayerManager>();
        playerInteract = player.gameObject.GetComponent<PlayerInteract>();
        
        teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);

        if (agent != null)
            agent.updateRotation = false;

        ResetLookAwayDelay();
    }

    private void Update()
    {
        if (player == null || agent == null)
            return;

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
        }
        else
        {
            hideTimer = 0f;
            HandleTeleport();
        }
    }

    // =========================
    // STALKER TYPE
    // =========================

    private void UpdateStalkerBehavior()
    {
        if (activeArea != null && !activeArea.bounds.Contains(player.position))
        {
            EndEncounter();
            return;
        }

        bool thisMonsterIsVisible =
            playerInteract != null &&
            playerInteract.IsLookingAtMonster() &&
            playerInteract.GetCurrentMonster() == transform.root;
        
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
            
            if (!NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
                continue;

            float distToPlayer = Vector3.Distance(hit.position, player.position);

            if (distToPlayer < minDistanceFromPlayer)
                continue;

            if (IsBlocked(hit.position))
                continue;

            TeleportTo(hit.position);
            return;
        }
    }

    private bool IsBlocked(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        return Physics.Raycast(origin, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void TeleportTo(Vector3 position)
    {
        agent.Warp(position);

        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    // =========================
    // END ENCOUNTER
    // =========================

    private void EndEncounter()
    {
        if (playerManager != null)
            playerManager.SetEncounter(false);

        gameObject.SetActive(false);
    }
    
    public void ResetState()
    {
        hideTimer = 0f;
        lookAwayTimer = 0f;
        teleportTimer = Random.Range(teleportIntervalMin, teleportIntervalMax);
        ResetLookAwayDelay();

        if (agent != null)
            agent.Warp(startPosition);
        else
            transform.position = startPosition;

        transform.rotation = startRotation;

        if (playerManager != null)
            playerManager.SetEncounter(false);

        gameObject.SetActive(false);
    }
}