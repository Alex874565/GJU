
using UnityEngine;

public class NewMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform yawPivot; // 👈 ADD THIS

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothing = 10f;

    [Header("Step Climb")]
    [SerializeField] private float stepHeight = 0.3f;
    [SerializeField] private float stepUpSpeed = 3f;
    [SerializeField] private float stepCheckDistance = 0.5f;
    [SerializeField] private float stepCooldown = 0.2f;
    [SerializeField] private LayerMask stepLayerMask = ~0;

    private Vector3 currentVelocity;
    private Vector3 targetVelocity;

    private float lastStepTime = -Mathf.Infinity;

    private void FixedUpdate()
    {
        if (InputManager.Instance == null) return;

        Vector2 input = InputManager.Instance.Movement;

        // 👉 Use camera direction
        Vector3 forward = yawPivot.forward;
        Vector3 right = yawPivot.right;

        // flatten (no vertical movement)
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection =
            right * input.x +
            forward * input.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        targetVelocity = moveDirection * moveSpeed;

        float smoothFactor = 1f - Mathf.Exp(-smoothing * Time.fixedDeltaTime);
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, smoothFactor);

        // Attempt step climb when moving into a low obstacle
        TryStepClimb(moveDirection);

        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
    }

    /// <summary>
    /// Detects small obstacles in front of the player and gives a short upward velocity
    /// so a Rigidbody-based character can step up onto low steps (like a CharacterController).
    /// </summary>
    /// <param name="moveDirection">World-space movement direction (may be zero).</param>
    private void TryStepClimb(Vector3 moveDirection)
    {
        // Nothing to do if not moving horizontally
        if (moveDirection.sqrMagnitude < 0.0001f) return;

        // Respect cooldown to avoid repeatedly re-triggering the step
        if (Time.time < lastStepTime + stepCooldown) return;

        Vector3 dirHorizontal = moveDirection;
        dirHorizontal.y = 0f;
        dirHorizontal.Normalize();

        // Origins for the low and high raycasts relative to the player's position
        Vector3 originLow = transform.position + Vector3.up * 0.1f;
        Vector3 originHigh = transform.position + Vector3.up * (stepHeight + 0.1f);

        // If low ray hits (obstacle at foot level) but high ray doesn't (space above step),
        // we can step up.
        bool hitLow = Physics.Raycast(originLow, dirHorizontal, stepCheckDistance, stepLayerMask);
        bool hitHigh = Physics.Raycast(originHigh, dirHorizontal, stepCheckDistance, stepLayerMask);

        if (hitLow && !hitHigh)
        {
            // Apply an upward velocity to climb the step.
            // We only change the vertical component; horizontal movement remains controlled by currentVelocity.
            Vector3 lv = rb.linearVelocity;
            lv.y = stepUpSpeed;
            rb.linearVelocity = lv;

            lastStepTime = Time.time;
        }
    }
}