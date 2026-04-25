using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Camera camera;
    [SerializeField] private Transform yawPivot;
    [SerializeField] private Transform pitchPivot;
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform lantern;
    [SerializeField] private Rigidbody rb;

    [Header("Settings")] 
    [SerializeField] private float lanternAimDistance = 10f;
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

        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (InputManager.Instance == null) return;

        Vector2 look = InputManager.Instance.Look;

        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void FixedUpdate()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);

        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            bodyFollowSpeed * Time.fixedDeltaTime
        ));
    }

    private void LateUpdate()
    {
        yawPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        bodyVisual.rotation = rb.rotation;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = ray.origin + ray.direction * lanternAimDistance;

        Vector3 dir = targetPoint - lantern.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            float t = 1f - Mathf.Exp(-bodyFollowSpeed * Time.deltaTime);

            lantern.rotation = Quaternion.Slerp(lantern.rotation, targetRot, t);
        }
    }
}