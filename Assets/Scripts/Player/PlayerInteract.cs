using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Lantern lantern;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerManager playerManager;

    [Header("Interaction Scan")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float sphereRadius = 0.15f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Monster Detection")]
    [SerializeField] private float monsterDetectDistance = 16f;
    [SerializeField] private LayerMask monsterMask;
    [SerializeField] private LayerMask visibilityMask;

    private IInteractable currentInteractable;
    private RaycastHit currentHit;

    private bool isLookingAtMonster;
    private Transform currentMonsterRoot;
    private Collider currentMonsterCollider;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnClickPressed += Interact;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnClickPressed -= Interact;
    }

    private void Update()
    {
        UpdateHighlight();
        UpdateMonsterDetection();

        if (playerManager != null)
            playerManager.SetSeeingMonster(isLookingAtMonster);
    }

    public void AddBattery(int value)
    {
        lantern.AddBattery(value);
    }

    public bool IsLookingAtMonster()
    {
        return isLookingAtMonster;
    }

    public Transform GetCurrentMonster()
    {
        return currentMonsterRoot;
    }

    private void UpdateHighlight()
    {
        IInteractable newInteractable = null;
        RaycastHit bestHit = default;
        float bestDistance = float.MaxValue;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            sphereRadius,
            interactDistance,
            interactMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable == null)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                newInteractable = interactable;
            }
        }

        if (newInteractable == currentInteractable)
            return;

        if (currentInteractable != null)
            currentInteractable.ChangeHighlight(false);

        currentInteractable = newInteractable;
        currentHit = bestHit;

        if (currentInteractable != null)
            currentInteractable.ChangeHighlight(true);
    }

    private void UpdateMonsterDetection()
    {
        isLookingAtMonster = false;
        currentMonsterRoot = null;
        currentMonsterCollider = null;

        if (playerCamera == null)
            return;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);

        Collider[] candidates = Physics.OverlapSphere(
            playerCamera.transform.position,
            monsterDetectDistance,
            monsterMask,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;

        foreach (Collider col in candidates)
        {
            if (col == null)
                continue;

            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds))
                continue;

            Vector3 pointToCheck = col.ClosestPoint(playerCamera.transform.position);
            Vector3 dir = pointToCheck - playerCamera.transform.position;
            float dist = dir.magnitude;

            if (dist <= 0.001f)
                continue;

            dir /= dist;

            if (Physics.Raycast(
                playerCamera.transform.position,
                dir,
                out RaycastHit hit,
                dist + 0.05f,
                visibilityMask,
                QueryTriggerInteraction.Ignore))
            {
                Transform hitRoot = hit.collider.transform.root;
                Transform candidateRoot = col.transform.root;

                if (hitRoot == candidateRoot)
                {
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        isLookingAtMonster = true;
                        currentMonsterRoot = candidateRoot;
                        currentMonsterCollider = col;
                    }
                }
            }
        }
    }

    private void Interact()
    {
        if (currentInteractable == null)
            lantern.ToggleOnOff();
        else
            currentInteractable.Interact(this);
    }

    private void OnDrawGizmos()
    {
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 start = ray.origin;
        Vector3 end = ray.origin + ray.direction * interactDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(start, sphereRadius);
        Gizmos.DrawWireSphere(end, sphereRadius);

        if (currentInteractable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentHit.point, sphereRadius * 0.5f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerCamera.transform.position, monsterDetectDistance);

        if (isLookingAtMonster && currentMonsterCollider != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(playerCamera.transform.position, currentMonsterCollider.bounds.center);
            Gizmos.DrawWireSphere(currentMonsterCollider.bounds.center, 0.25f);
        }
    }
}