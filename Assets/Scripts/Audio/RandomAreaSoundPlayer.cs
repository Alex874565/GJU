using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RandomAreaSoundPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerManager playerManager;

    [Header("Sound")]
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private float volume = 1f;

    [Header("Timing")]
    [SerializeField] private float minDelay = 4f;
    [SerializeField] private float maxDelay = 10f;

    [Header("Area")]
    [SerializeField] private float minRadius = 3f;
    [SerializeField] private float maxRadius = 10f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float behindBiasChance = 0.8f;

    [Header("Turn Reaction")]
    [SerializeField] private float reactionWindow = 4f;
    [SerializeField] private float lookAngleThreshold = 25f;
    [SerializeField] private float reactionChance = 0.45f;
    [SerializeField] private float anxietyGain = 15f;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData turnAroundDialogue;

    [Header("Monster Spawn")]
    [SerializeField] private GameObject[] monsterObjects;
    [SerializeField] private float monsterSpawnChance = 0.35f;
    [SerializeField] private float navMeshSampleRadius = 6f;
    
    [Header("Stalker Spawn")]
    [SerializeField] private float stalkerMaxSpawnDistance = 5f;
    
    [Header("Blocked Areas")]
    [SerializeField] private Collider[] blockedAreas;
    
    [Header("Spawn Level")]
    [SerializeField] private float maxSpawnVerticalDifference = 1.2f;
    [SerializeField] private int sameLevelAttempts = 12;

    private Vector3 lastSoundPosition;
    private bool waitingForTurnReaction;
    private float reactionTimer;
    private bool reactionUsed;
    
    [SerializeField] private float fakeNoiseDialogueChance = 0.5f;

    private Coroutine loopRoutine;

    private void OnEnable()
    {
        loopRoutine = StartCoroutine(SoundLoop());
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
            StopCoroutine(loopRoutine);
    }

    private void Update()
    {
        UpdateTurnReaction();
    }
    
    private bool PlayerIsInBlockedArea()
    {
        if (player == null || blockedAreas == null)
            return false;

        foreach (Collider area in blockedAreas)
        {
            if (area == null) continue;

            if (area.bounds.Contains(player.position))
                return true;
        }

        return false;
    }

    private IEnumerator SoundLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            if (PlayerIsInBlockedArea())
                continue;

            PlayRandomSound();
        }
    }

    private void PlayRandomSound()
    {
        if (player == null || clips == null || clips.Length == 0)
            return;

        Vector3 dir = GetBiasedDirection();
        float distance = Random.Range(minRadius, maxRadius);

        Vector3 position = player.position + dir * distance;
        position.y = player.position.y + heightOffset;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float playDuration = Random.Range(0.3f, 1.2f);
        playDuration = Mathf.Min(playDuration, clip.length);

        StartCoroutine(AudioManager.PlayClipPartial(
            clip,
            position,
            volume,
            playDuration,
            playDuration
        ));

        lastSoundPosition = position;

        StartCoroutine(SpawnMonsterAfterSound(playDuration));

        waitingForTurnReaction = true;
        reactionTimer = reactionWindow;
        reactionUsed = false;
    }
    
    private IEnumerator SpawnMonsterAfterSound(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (PlayerIsInBlockedArea())
            yield break;

        bool spawnedMonster = TrySpawnMonsterNearSound();

        if (spawnedMonster)
            waitingForTurnReaction = false;
    }

    private Vector3 GetBiasedDirection()
    {
        if (Random.value < behindBiasChance)
        {
            float angle = Random.Range(-100f, 100f);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * -player.forward;

            dir += player.right * Random.Range(-0.25f, 0.25f);
            dir.y = 0f;

            return dir.normalized;
        }

        Vector3 random = Random.insideUnitSphere;
        random.y = 0f;
        return random.normalized;
    }

    private void UpdateTurnReaction()
    {
        if (PlayerIsInBlockedArea())
        {
            waitingForTurnReaction = false;
            return;
        }
        if (!waitingForTurnReaction || reactionUsed || player == null)
            return;

        reactionTimer -= Time.deltaTime;

        if (reactionTimer <= 0f)
        {
            waitingForTurnReaction = false;
            return;
        }

        Vector3 toSound = lastSoundPosition - player.position;
        toSound.y = 0f;

        if (toSound.sqrMagnitude < 0.001f)
            return;

        float angle = Vector3.Angle(player.forward, toSound.normalized);

        if (angle > lookAngleThreshold)
            return;

        reactionUsed = true;
        waitingForTurnReaction = false;

        if (Random.value > reactionChance)
            return;

        TriggerReaction();
    }
    
    private void TriggerReaction()
    {
        if (Random.value > fakeNoiseDialogueChance)
            return;

        if (dialogueManager != null && turnAroundDialogue != null && !dialogueManager.isPlaying)
            dialogueManager.PlayDialogue(turnAroundDialogue);
    }

    private bool TrySpawnMonsterNearSound()
    {
            if (MonsterSpawnManager.Instance != null &&
                MonsterSpawnManager.Instance.HasActiveMonster())
                return false;
        
        if (monsterObjects == null || monsterObjects.Length == 0)
            return false;

        if (Random.value > monsterSpawnChance)
            return false;

        if (MonsterSpawnManager.Instance != null &&
            !MonsterSpawnManager.Instance.TryRegisterSpawn())
            return false;

        if (!TrySampleSameLevelNavMesh(lastSoundPosition, out NavMeshHit hit))
        {
            MonsterSpawnManager.Instance?.UnregisterSpawn();
            return false;
        }

        // 🔥 pick an inactive monster
        GameObject monster = GetAvailableMonster();

        if (monster == null)
        {
            Debug.Log("No available monster (all active)");
            MonsterSpawnManager.Instance?.UnregisterSpawn();
            return false;
        }

        MonsterIdentity identity = monster.GetComponent<MonsterIdentity>();

        Vector3 spawnPosition = hit.position;

        if (identity != null && identity.Type == MonsterType.Stalker)
        {
            Vector3 fromPlayer = spawnPosition - player.position;
            fromPlayer.y = 0f;

            if (fromPlayer.magnitude > stalkerMaxSpawnDistance)
            {
                Vector3 closerPosition = player.position + fromPlayer.normalized * stalkerMaxSpawnDistance;

                if (NavMesh.SamplePosition(closerPosition, out NavMeshHit closerHit, navMeshSampleRadius, NavMesh.AllAreas))
                    spawnPosition = closerHit.position;
            }
        }

        monster.transform.position = spawnPosition;

        Vector3 lookDir = player.position - monster.transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
            monster.transform.rotation = Quaternion.LookRotation(lookDir);

        monster.SetActive(true);

        if (identity != null)
        {
            identity.PlaySpawnSound();

            if (MonsterSpawnManager.Instance != null &&
                MonsterSpawnManager.Instance.IsFirstSeenThisRun(identity.Type))
            {
                if (dialogueManager != null &&
                    identity.FirstSeenThisRunDialogue != null &&
                    !dialogueManager.isPlaying)
                {
                    dialogueManager.PlayMonsterDialogue(identity.FirstSeenThisRunDialogue);
                }
            }
        }
        
        return true;
    }
    
    private bool TrySampleSameLevelNavMesh(Vector3 center, out NavMeshHit bestHit)
    {
        // First try exact sound position
        if (NavMesh.SamplePosition(center, out bestHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            if (Mathf.Abs(bestHit.position.y - player.position.y) <= maxSpawnVerticalDifference)
                return true;
        }

        // Then try nearby points on the same horizontal level
        for (int i = 0; i < sameLevelAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * navMeshSampleRadius;

            Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
            candidate.y = player.position.y;

            if (!NavMesh.SamplePosition(candidate, out bestHit, navMeshSampleRadius, NavMesh.AllAreas))
                continue;

            if (Mathf.Abs(bestHit.position.y - player.position.y) <= maxSpawnVerticalDifference)
                return true;
        }

        return false;
    }
    
    private GameObject GetAvailableMonster()
    {
        // shuffle for randomness
        for (int i = 0; i < monsterObjects.Length; i++)
        {
            int index = Random.Range(0, monsterObjects.Length);
            GameObject m = monsterObjects[index];

            if (m != null && !m.activeInHierarchy)
                return m;
        }

        return null;
    }
}