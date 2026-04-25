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
        {
            InputManager.Instance.OnInteractPressed += Interact;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteractPressed -= Interact;
        }
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

    private void Interact()
    {
        if(currentInteractable != null)
            currentInteractable.Interact(this);
    }
}