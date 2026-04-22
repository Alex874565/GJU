using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Lantern lantern;
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Scan")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float sphereRadius = 0.35f;
    [SerializeField] private LayerMask interactMask = ~0;

    private IInteractable currentInteractable;
    private RaycastHit currentHit;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        UpdateHighlight();
    }

    public void AddBattery(int value)
    {
        lantern.AddBattery(value);
    }

    private void UpdateHighlight()
    {
        IInteractable newInteractable = null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // Try collider first
            newInteractable = hit.collider.GetComponent<IInteractable>();

            // If the collider itself doesn't have it, try parent
            if (newInteractable == null)
                newInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (newInteractable != null)
                currentHit = hit;
        }

        if (newInteractable == currentInteractable)
            return;

        if (currentInteractable != null)
            currentInteractable.ChangeHighlight(false);

        currentInteractable = newInteractable;

        if (currentInteractable != null)
            currentInteractable.ChangeHighlight(true);
    }
    
    private void OnDrawGizmos()
    {
        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 start = ray.origin;
        Vector3 end = ray.origin + ray.direction * interactDistance;

        // Draw line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);

        // Draw start sphere
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(start, sphereRadius);

        // Draw end sphere
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(end, sphereRadius);

        // Draw hit point if exists
        if (currentInteractable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentHit.point, sphereRadius * 0.5f);
        }
    }
}