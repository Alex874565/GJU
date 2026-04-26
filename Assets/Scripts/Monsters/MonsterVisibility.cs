using UnityEngine;

public class MonsterVisibility : MonoBehaviour
{
    private PlayerManager playerManager;
    private Camera playerCamera;
    [SerializeField] private Renderer monsterRenderer;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float maxViewDistance = 25f;
    [SerializeField] private SimpleDisappearMonster disappearMonster;
    private DialogueManager dialogueManager;
    
    public bool IsVisible { get; private set; }
    private bool wasVisible;

    private void Awake()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
        playerManager = FindObjectOfType<PlayerManager>();
        playerCamera = playerManager.gameObject.GetComponentInChildren<Camera>();
    }
    
    private void Update()
    {
        bool visible = IsVisibleToPlayer();
        IsVisible = visible;

        if (visible && !wasVisible)
        {
            playerManager.RegisterMonsterVisible(true);
            disappearMonster?.OnSeen();
        }
        else if (!visible && wasVisible)
        {
            playerManager.RegisterMonsterVisible(false);
        }

        wasVisible = visible;
    }
    
    public void ClearVisibility()
    {
        if (wasVisible && playerManager != null)
            playerManager.RegisterMonsterVisible(false);

        wasVisible = false;
        IsVisible = false;
    }
    
    private bool IsVisibleToPlayer()
    {
        if (playerCamera == null || monsterRenderer == null)
            return false;

        Bounds b = monsterRenderer.bounds;

        Vector3[] points =
        {
            b.center,
            new Vector3(b.min.x, b.center.y, b.center.z),
            new Vector3(b.max.x, b.center.y, b.center.z),
            new Vector3(b.center.x, b.min.y, b.center.z),
            new Vector3(b.center.x, b.max.y, b.center.z),
            new Vector3(b.min.x, b.max.y, b.center.z),
            new Vector3(b.max.x, b.max.y, b.center.z)
        };

        foreach (Vector3 point in points)
        {
            if (PointIsVisible(point))
                return true;
        }

        return false;
    }
    
    private bool PointIsVisible(Vector3 point)
    {
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(point);

        if (viewportPoint.z <= 0f)
            return false;

        if (viewportPoint.x < 0f || viewportPoint.x > 1f ||
            viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        float distance = Vector3.Distance(playerCamera.transform.position, point);

        if (distance > maxViewDistance)
            return false;

        Vector3 dir = point - playerCamera.transform.position;

        if (Physics.Raycast(
                playerCamera.transform.position,
                dir.normalized,
                out RaycastHit hit,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(transform) && hit.transform != transform)
                return false;
        }

        return true;
    }
    
    private void OnDisable()
    {
        if (wasVisible && playerManager != null)
        {
            playerManager.RegisterMonsterVisible(false);
            wasVisible = false;
        }
    }
}