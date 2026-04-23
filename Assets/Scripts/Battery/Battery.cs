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

            visual.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
        }

        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            targetLocalPos,
            moveSpeed * Time.deltaTime
        );
    }

    public void ChangeHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
    }

    public void Interact(PlayerInteract player)
    {
        player.AddBattery(1);
        Destroy(gameObject);
    }
}