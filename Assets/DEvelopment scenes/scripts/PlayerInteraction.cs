using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactionText;

    private DoorController currentDoor;

    private void Start()
    {
        if (interactionText != null)
            interactionText.text = "";
    }

    private void Update()
    {
        if (currentDoor == null)
        {
            if (interactionText != null)
                interactionText.text = "";

            return;
        }

        interactionText.text = currentDoor.IsOpen()
            ? "Press E to Close"
            : "Press E to Open";

       // if (InputManager2.Instance != null && InputManager.Instance.InteractPressed)
       // {
           // currentDoor.ToggleDoor();
       // }
    }

    private void OnTriggerEnter(Collider other)
    {
        DoorController door = other.GetComponentInParent<DoorController>();

        if (door != null)
            currentDoor = door;
    }

    private void OnTriggerExit(Collider other)
    {
        DoorController door = other.GetComponentInParent<DoorController>();

        if (door != null && door == currentDoor)
        {
            currentDoor = null;

            if (interactionText != null)
                interactionText.text = "";
        }
    }
}