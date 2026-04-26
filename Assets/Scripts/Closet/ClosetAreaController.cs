using UnityEngine;

public class ClosetAreaController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private CapsuleCollider playerCollider;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Rigidbody playerRb;

    [Header("Collider Size")]
    [SerializeField] private float closetRadius = 0.18f;
    private float normalRadius;

    [Header("Smoothing")]
    [SerializeField] private float transitionSpeed = 8f;

    [Header("Camera Wall Safety")]
    [SerializeField] private float closetCameraBackOffset = 0.15f;
    [SerializeField] private float normalCameraZ;

    [Header("Closet Door")]
    [SerializeField] private DoorController closetDoor;

    private bool inCloset;
    private bool wasHidden;

    private void Start()
    {
        if (playerCollider != null)
            normalRadius = playerCollider.radius;
    }

    private void Update()
    {
        if (playerCollider == null || cameraPivot == null) return;

        float targetRadius = inCloset ? closetRadius : normalRadius;

        playerCollider.radius = Mathf.Lerp(
            playerCollider.radius,
            targetRadius,
            Time.deltaTime * transitionSpeed
        );

        UpdateHiddenState();
    }

    private void UpdateHiddenState()
    {
        bool hidden = inCloset && closetDoor != null && !closetDoor.IsOpen();

        if (hidden == wasHidden)
            return;

        if (playerManager != null)
            playerManager.RegisterHiddenSource(hidden);

        wasHidden = hidden;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        inCloset = true;
        UpdateHiddenState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        inCloset = false;

        if (wasHidden && playerManager != null)
        {
            playerManager.RegisterHiddenSource(false);
            wasHidden = false;
        }

        if (playerRb != null)
        {
            playerRb.angularVelocity = Vector3.zero;
            playerRb.linearVelocity = new Vector3(0f, playerRb.linearVelocity.y, 0f);
            playerRb.rotation = Quaternion.Euler(0f, playerRb.rotation.eulerAngles.y, 0f);
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }
}