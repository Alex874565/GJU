using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform lantern;
    [SerializeField] private Rigidbody rb;

    [Header("Settings")]
    [SerializeField] private float sensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float bodyFollowSpeed = 5f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (InputManager.Instance == null) return;

        Vector2 look = InputManager.Instance.Look;

        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Camera rotates instantly
        yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FixedUpdate()
    {
        // Body slowly follows camera yaw
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);

        Quaternion newRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            bodyFollowSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newRotation);
    }

    private void LateUpdate()
    {
        // Body follows rigidbody (yaw only)
        bodyVisual.rotation = Quaternion.Slerp(
            bodyVisual.rotation,
            rb.rotation,
            bodyFollowSpeed * Time.deltaTime
        );

        Quaternion targetRotation = Quaternion.Euler(pitch, 0f, 0f);

        lantern.localRotation = Quaternion.Slerp(
            lantern.localRotation,
            targetRotation,
            bodyFollowSpeed * Time.deltaTime
        );
    }
}