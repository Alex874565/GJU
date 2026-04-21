using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform yawPivot; // 👈 ADD THIS

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothing = 10f;

    private Vector3 currentVelocity;
    private Vector3 targetVelocity;

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

        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);
    }
}