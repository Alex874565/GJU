using UnityEngine;

public class Battery : MonoBehaviour, IInteractable
{
    [SerializeField] private float liftHeight = 0.25f;
    [SerializeField] private float speed = 5f;

    private Vector3 basePosition;
    private Vector3 targetPosition;

    private void Awake()
    {
        basePosition = transform.position;
        targetPosition = basePosition;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    public void ChangeHighlight(bool highlighted)
    {
        targetPosition = highlighted
            ? basePosition + Vector3.up * liftHeight
            : basePosition;
    }

    public void Interact(PlayerInteract player)
    {
        player.AddBattery(1);
        Destroy(gameObject);
    }
}