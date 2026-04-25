using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform yawPivot; // 👈 ADD THIS

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothing = 10f;

    [System.Serializable]
    public class SurfaceFootsteps
    {
        public Surface surface;
        public AudioClip[] clips;
    }

    [Header("Footsteps")]
    [SerializeField] private SurfaceFootsteps[] surfaceFootsteps;
    [SerializeField] private AudioClip[] defaultFootsteps;
    [SerializeField] private float stepDistance = 1.7f;
    [SerializeField] private float minSpeedForSteps = 0.15f;
    [SerializeField] private float footstepVolume = 0.85f;
    [SerializeField] private float firstStepOffset = 0.6f;
    [SerializeField] private float footstepPanAmount = 0.18f;
    [SerializeField] private float finalStepVolumeMultiplier = 0.65f;

    private bool wasMoving;
    private bool leftStep;

    [Header("Head Bob")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float bobAmount = 0.04f;
    [SerializeField] private float bobReturnSpeed = 10f;

    private float targetBobOffset;
    private float stepCycle;
    private Vector3 cameraBaseLocalPos;
    private float bobOffset;
    
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;

    public Surface WalkingSurface;

    public bool inputLocked = false;

    
    private void Start()
    {
        if (cameraPivot != null)
            cameraBaseLocalPos = cameraPivot.localPosition;
    }
    
    private void FixedUpdate()
    {
        if (InputManager.Instance == null) return;
        if (inputLocked) return;

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
        
        HandleFootsteps();
    }
    
    private void Update()
    {
        UpdateHeadBob();
    }

    private void HandleFootsteps()
    {
        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        bool isMoving = speed >= minSpeedForSteps;

        if (isMoving && !wasMoving)
        {
            stepCycle = stepDistance * firstStepOffset;
        }

        if (!isMoving)
        {
            if (wasMoving && !leftStep)
            {
                PlayFootstep(true, finalStepVolumeMultiplier);
                targetBobOffset = -bobAmount * 0.5f;
            }

            stepCycle = 0f;
            leftStep = true;
            wasMoving = false;
            return;
        }

        stepCycle += speed * Time.fixedDeltaTime;

        if (stepCycle >= stepDistance)
        {
            stepCycle = 0f;

            leftStep = !leftStep;

            PlayFootstep(leftStep, 1);

            targetBobOffset = leftStep ? -bobAmount : bobAmount * 0.6f;
        }

        wasMoving = true;
    }

    private void UpdateHeadBob()
    {
        if (cameraPivot == null) return;

        bobOffset = Mathf.Lerp(
            bobOffset,
            targetBobOffset,
            1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime)
        );

        targetBobOffset = Mathf.Lerp(
            targetBobOffset,
            0f,
            1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime)
        );

        Vector3 pos = cameraBaseLocalPos;
        pos.y += bobOffset;

        cameraPivot.localPosition = pos;
    }

    private void PlayFootstep(bool isLeftStep, float volumeMultiplier)
    {
        AudioClip[] clips = GetFootstepClips();
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        float volume = footstepVolume * volumeMultiplier;
        float pan = isLeftStep ? -footstepPanAmount : footstepPanAmount;

        AudioManager.PlaySFX(clip, transform.position, volume, pan);
    }

    private AudioClip[] GetFootstepClips()
    {
        foreach (SurfaceFootsteps surfaceSet in surfaceFootsteps)
        {
            if (surfaceSet.surface == WalkingSurface)
                return surfaceSet.clips;
        }

        return defaultFootsteps;
    }
}