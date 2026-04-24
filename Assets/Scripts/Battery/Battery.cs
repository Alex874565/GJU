using UnityEngine;

public class Battery : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform visual;

    [Header("Lift")]
    [SerializeField] private float liftHeight = 0.25f;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Float")]
    [SerializeField] private float floatAmplitude = 0.05f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private Vector3 rotationAxis = new Vector3(0.6f, 1f, 0.3f);
    [SerializeField] private float wobbleAmount = 8f;
    [SerializeField] private float wobbleSpeed = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] pickupSounds;

    private Vector3 visualBaseLocalPos;
    private bool isHighlighted;
    private float floatTimer;

    private void Awake()
    {
        if (visual == null)
            visual = transform;

        visualBaseLocalPos = visual.localPosition;
    }

    private void Update()
    {
        Vector3 targetLocalPos = visualBaseLocalPos;

        if (isHighlighted)
        {
            floatTimer += Time.deltaTime * floatFrequency;

            float floatOffset = Mathf.Sin(floatTimer) * floatAmplitude;
            targetLocalPos += Vector3.up * (liftHeight + floatOffset);

            visual.localRotation = Quaternion.Slerp(
                visual.localRotation,
                Quaternion.Euler(
                    Mathf.Sin(Time.time * 0.7f) * 10f,
                    visual.localEulerAngles.y,
                    Mathf.Cos(Time.time * 0.6f) * 10f
                ),
                Time.deltaTime * 2f
            );
        }

        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            targetLocalPos,
            moveSpeed * Time.deltaTime
        );
    }

    public void ChangeHighlight(bool highlighted)
    {
        if(highlighted)
        {
            InteractPrompt.Instance?.Show("Use");
        }
        else
        {
            InteractPrompt.Instance?.Hide();
        }
        isHighlighted = highlighted;
    }

    public void Interact(PlayerInteract player)
    {
        player.AddBattery(1);

        if (pickupSounds != null && pickupSounds.Length > 0)
        {
            AudioManager.PlaySFX(pickupSounds, transform.position);
        }

        Destroy(gameObject);
    }
}