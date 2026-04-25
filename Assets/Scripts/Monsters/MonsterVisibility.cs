using UnityEngine;

public class MonsterVisibility : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Renderer monsterRenderer;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float maxViewDistance = 25f;
    [SerializeField] private SimpleDisappearMonster disappearMonster;

    public bool IsVisible { get; private set; }
    private bool wasVisible;
    
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

        Vector3 monsterPoint = monsterRenderer.bounds.center;
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(monsterPoint);

        if (viewportPoint.z <= 0f)
            return false;

        if (viewportPoint.x < 0f || viewportPoint.x > 1f ||
            viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        float distance = Vector3.Distance(playerCamera.transform.position, monsterPoint);

        if (distance > maxViewDistance)
            return false;

        Vector3 dir = monsterPoint - playerCamera.transform.position;

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