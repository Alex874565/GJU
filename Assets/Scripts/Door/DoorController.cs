using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door State")]
    [SerializeField] private bool startOpen = false;
    [SerializeField] private bool isLocked = false;

    [Header("Door Rotation")]
    [SerializeField] private float openAngle = -85f;
    [SerializeField] private float openSpeed = 5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Audio")]
    [SerializeField] private AudioClip[] openSounds;
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private AudioClip[] lockedSounds;
    [SerializeField] private AudioClip[] unlockSounds;

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        isOpen = startOpen;

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

    public void ChangeHighlight(bool highlighted)
    {
        if (highlighted)
            InteractPrompt.Instance?.Show("Interact");
        else
            InteractPrompt.Instance?.Hide();
    }

    public void Interact(PlayerInteract player)
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (isLocked)
        {
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey)
            {
                PlayerInventory.Instance.UseKey();
                isLocked = false;

                if (unlockSounds != null && unlockSounds.Length > 0)
                    AudioManager.PlaySFX(unlockSounds, transform.position);

                ToggleDoor();
            }
            else
            {
                if (lockedSounds != null && lockedSounds.Length > 0)
                    AudioManager.PlaySFX(lockedSounds, transform.position);

                DialogueManager.Instance?.PlayDialogue(lockedDialogue);
            }

            return;
        }

        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            if (openSounds != null && openSounds.Length > 0)
                AudioManager.PlaySFX(openSounds, transform.position);
        }
        else
        {
            if (closeSounds != null && closeSounds.Length > 0)
                AudioManager.PlaySFX(closeSounds, transform.position);
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}