using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door State")]
    [SerializeField] private bool startOpen = false;

    [Header("Door Rotation")]
    [SerializeField] private float openAngle = -85f;
    [SerializeField] private float openSpeed = 5f;

    private bool isOpen;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        // Nu modifică poziția/rotația setată de tine în scenă.
        // Rotația din scenă devine poziția de CLOSED.
        closedRotation = transform.localRotation;

        // Fiecare ușă poate avea propriul unghi:
        // -85, +85, 120 etc.
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        isOpen = startOpen;

        // Dacă vrei ca ușa să înceapă deja deschisă
        if (isOpen)
            transform.localRotation = openRotation;
    }

    private void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}