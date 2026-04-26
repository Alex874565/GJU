using UnityEngine;

public class PlayerSafeZoneDetector : MonoBehaviour
{
    [Header("Safe Zone")]
    [SerializeField] private LayerMask safeZoneLayer;

    private int safeZoneCount = 0;

    public bool IsInSafeZone => safeZoneCount > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (IsInLayerMask(other.gameObject.layer, safeZoneLayer))
        {
            safeZoneCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInLayerMask(other.gameObject.layer, safeZoneLayer))
        {
            safeZoneCount--;
            safeZoneCount = Mathf.Max(0, safeZoneCount);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}