using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerInteract playerInteract;

    [Header("Fear")]
    [SerializeField] private float fearRiseSpeed = 2.5f;
    [SerializeField] private float fearFallSpeed = 1.2f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 0.7f;
    [SerializeField] private float bobAmplitude = 0.03f;
    [SerializeField] private float sideBobAmplitude = 0.015f;
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float positionLerpSpeed = 8f;

    [Header("Idle Breathing")]
    [SerializeField] private float idleBreathFrequency = 0.45f;
    [SerializeField] private float idleBreathFearFrequency = 1.8f;
    [SerializeField] private float idleBreathAmplitude = 0.004f;
    [SerializeField] private float idleBreathFearAmplitude = 0.012f;

    [Header("Turn Tilt")]
    [SerializeField] private float turnTiltAmount = 0.6f;
    [SerializeField] private float maxTilt = 7f;
    [SerializeField] private float tiltLerpSpeed = 12f;

    [Header("Walk Roll")]
    [SerializeField] private float walkRollAmount = 1f;

    [Header("Pitch Sway")]
    [SerializeField] private float swayPitchAmount = 0.05f;
    [SerializeField] private float maxSwayPitch = 0.6f;
    [SerializeField] private float pitchLerpSpeed = 10f;

    [Header("Mouse Position Sway")]
    [SerializeField] private float swayPositionAmount = 0.002f;
    [SerializeField] private float maxSwayPosition = 0.01f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private float bobTimer;

    private float currentTilt;
    private float currentPitchSway;
    private float fear;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        UpdateFear();
        ApplyPositionEffects();
        ApplyRotationEffects();
    }

    private void UpdateFear()
    {
        bool lookingAtMonster = playerInteract != null && playerInteract.IsLookingAtMonster();
        float targetFear = lookingAtMonster ? 1f : 0f;
        float speed = lookingAtMonster ? fearRiseSpeed : fearFallSpeed;
        fear = Mathf.MoveTowards(fear, targetFear, speed * Time.deltaTime);
    }

    private void ApplyPositionEffects()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        float speed = horizontalVelocity.magnitude;
        bool isMoving = speed > movementThreshold;

        Vector3 bobOffset;

        if (isMoving)
        {
            float bobSpeedMultiplier = 0.5f + Mathf.Clamp(speed, 0f, 1.5f) * 0.5f;
            bobTimer += Time.deltaTime * bobFrequency * bobSpeedMultiplier;

            float bobY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            float bobX = Mathf.Cos(bobTimer * Mathf.PI * 2f) * sideBobAmplitude;

            bobOffset = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            float breathFrequency = Mathf.Lerp(idleBreathFrequency, idleBreathFearFrequency, fear);
            float breathAmplitude = Mathf.Lerp(idleBreathAmplitude, idleBreathFearAmplitude, fear);

            bobTimer += Time.deltaTime * breathFrequency;

            float breathWave = Mathf.Sin(bobTimer * Mathf.PI * 2f);
            float breathY = breathWave * breathAmplitude;

            bobOffset = new Vector3(0f, breathY, 0f);
        }

        Vector3 targetPosition = initialLocalPosition + bobOffset + GetMousePositionSway();

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            positionLerpSpeed * Time.deltaTime
        );
    }

    private void ApplyRotationEffects()
    {
        if (InputManager.Instance == null) return;

        Vector2 look = InputManager.Instance.Look;

        float targetPitchSway = Mathf.Clamp(
            -look.y * swayPitchAmount,
            -maxSwayPitch,
            maxSwayPitch
        );

        float turnTilt = -look.x * turnTiltAmount;

        float walkRoll = 0f;
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude > movementThreshold)
        {
            walkRoll = Mathf.Sin(bobTimer * Mathf.PI) * walkRollAmount;
        }

        float targetTilt = Mathf.Clamp(turnTilt + walkRoll, -maxTilt, maxTilt);

        currentPitchSway = Mathf.Lerp(
            currentPitchSway,
            targetPitchSway,
            pitchLerpSpeed * Time.deltaTime
        );

        currentTilt = Mathf.Lerp(
            currentTilt,
            targetTilt,
            tiltLerpSpeed * Time.deltaTime
        );

        transform.localRotation = initialLocalRotation * Quaternion.Euler(currentPitchSway, 0f, currentTilt);
    }

    private Vector3 GetMousePositionSway()
    {
        if (InputManager.Instance == null) return Vector3.zero;

        Vector2 look = InputManager.Instance.Look;

        float offsetX = Mathf.Clamp(-look.x * swayPositionAmount, -maxSwayPosition, maxSwayPosition);
        float offsetY = Mathf.Clamp(-look.y * swayPositionAmount, -maxSwayPosition, maxSwayPosition);

        return new Vector3(offsetX, offsetY, 0f);
    }
}