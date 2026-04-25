using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Audio")]
    [SerializeField] private AudioClip[] defaultSteps;
    [SerializeField] private AudioClip[] carpetSteps;

    [Header("Layers")]
    [SerializeField] private LayerMask carpetLayer;

    [Header("Step Settings")]
    [SerializeField] private float stepInterval = 0.5f;
    [SerializeField] private float minSpeed = 0.1f;
    [SerializeField] private float groundCheckDistance = 1.2f;

    private AudioSource audioSource;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (rb == null) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed > minSpeed && IsGrounded())
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    private void PlayFootstep()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
            return;

        AudioClip[] chosenClips;

        // 👇 verifică layerul
        if (((1 << hit.collider.gameObject.layer) & carpetLayer) != 0)
        {
            chosenClips = carpetSteps;
        }
        else
        {
            chosenClips = defaultSteps;
        }

        if (chosenClips.Length == 0) return;

        AudioClip clip = chosenClips[Random.Range(0, chosenClips.Length)];
        audioSource.PlayOneShot(clip);
    }
}