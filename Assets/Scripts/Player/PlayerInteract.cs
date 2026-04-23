using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Lantern lantern;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Scan")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float sphereRadius = 0.15f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Monster Detection (Full Screen)")]
    [SerializeField] private float monsterDetectDistance = 12f;
    [SerializeField] private LayerMask monsterMask;
    [SerializeField] private LayerMask visibilityMask;

    private IInteractable currentInteractable;
    private RaycastHit currentHit;

    private bool isLookingAtMonster;
    private Collider currentMonster;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        InputManager.Instance.OnClickPressed += Interact;
    }

    private void OnDestroy()
    {
        InputManager.Instance.OnClickPressed -= Interact;
    }

    private void Update()
    {
        UpdateHighlight();
        UpdateMonsterDetection();
    }

    public void AddBattery(int value)
    {
        lantern.AddBattery(value);
    }

    // --------------------------
    // INTERACTION (unchanged)
    // --------------------------
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

    // --------------------------
    // MONSTER DETECTION (FULL SCREEN)
    // --------------------------
    private void UpdateMonsterDetection()
    {
        isLookingAtMonster = false;
        currentMonster = null;

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
            // 1. Must be on screen
            if (!GeometryUtility.TestPlanesAABB(planes, col.bounds))
                continue;

            // 2. Must not be behind walls
            Vector3 dir = (col.bounds.center - playerCamera.transform.position);
            float dist = dir.magnitude;
            dir.Normalize();

            if (Physics.Raycast(
                playerCamera.transform.position,
                dir,
                out RaycastHit hit,
                dist,
                visibilityMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == col)
                {
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        currentMonster = col;
                        isLookingAtMonster = true;
                        Debug.Log("Looking at monster: " + col.name);
                    }
                }
            }
        }
    }

    public bool IsLookingAtMonster()
    {
        return isLookingAtMonster;
    }

    public Collider GetCurrentMonster()
    {
        return currentMonster;
    }

    // --------------------------
    // DEBUG
    // --------------------------
    private void OnDrawGizmos()
    {
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Interaction gizmo
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

        // Monster debug
        if (isLookingAtMonster && currentMonster != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerCamera.transform.position, currentMonster.bounds.center);
            Gizmos.DrawWireSphere(currentMonster.bounds.center, 0.3f);
        }
    }

    // --------------------------
    // INPUT
    // --------------------------
    private void Interact()
    {
        if (currentInteractable == null)
        {
            lantern.ToggleOnOff();
        }
        else
        {
            currentInteractable.Interact(this);
        }
    }
}