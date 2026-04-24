using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactionText;

    private DoorController currentDoor;
    private bool subscribed;

    private void Start()
    {
        Debug.Log("[PlayerInteraction] Start");

        if (interactionText != null)
            interactionText.text = "";
        else
            Debug.LogWarning("[PlayerInteraction] interactionText NU este setat în Inspector!");

        TrySubscribe();
    }

    private void OnEnable()
    {
        Debug.Log("[PlayerInteraction] OnEnable");
        TrySubscribe();
    }

    private void OnDisable()
    {
        Debug.Log("[PlayerInteraction] OnDisable");
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Debug.Log("[PlayerInteraction] OnDestroy");
        Unsubscribe();
    }

    private void Update()
    {
        if (currentDoor == null)
        {
            if (interactionText != null)
                interactionText.text = "";

            return;
        }

        if (interactionText != null)
        {
            interactionText.text = currentDoor.IsOpen()
                ? "Press E to Close"
                : "Press E to Open";
        }
    }

    private void HandleInteract()
    {
        Debug.Log("[PlayerInteraction] HandleInteract primit!");

        if (currentDoor == null)
        {
            Debug.LogWarning("[PlayerInteraction] Ai apăsat E, dar currentDoor este NULL.");
            return;
        }

        Debug.Log("[PlayerInteraction] ToggleDoor pe: " + currentDoor.name);
        currentDoor.ToggleDoor();
    }

    private void TrySubscribe()
    {
        if (subscribed)
        {
            Debug.Log("[PlayerInteraction] Deja subscribed.");
            return;
        }

        if (InputManager.Instance == null)
        {
            Debug.LogWarning("[PlayerInteraction] InputManager.Instance este NULL. Nu pot face subscribe.");
            return;
        }

        InputManager.Instance.OnInteractPressed += HandleInteract;
        subscribed = true;

        Debug.Log("[PlayerInteraction] Subscribed la InputManager.OnInteractPressed.");
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteractPressed -= HandleInteract;
            Debug.Log("[PlayerInteraction] Unsubscribed de la InputManager.OnInteractPressed.");
        }

        subscribed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PlayerInteraction] Trigger Enter cu: " + other.name);

        DoorController door = other.GetComponentInParent<DoorController>();

        if (door != null)
        {
            currentDoor = door;
            Debug.Log("[PlayerInteraction] currentDoor setat la: " + currentDoor.name);
        }
        else
        {
            Debug.LogWarning("[PlayerInteraction] Colliderul nu are DoorController în parent.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("[PlayerInteraction] Trigger Exit cu: " + other.name);

        DoorController door = other.GetComponentInParent<DoorController>();

        if (door != null && door == currentDoor)
        {
            Debug.Log("[PlayerInteraction] currentDoor resetat.");
            currentDoor = null;

            if (interactionText != null)
                interactionText.text = "";
        }
    }
}