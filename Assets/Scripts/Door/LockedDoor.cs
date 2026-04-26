using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Audio")]
    [SerializeField] private AudioClip[] openSounds;
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private AudioClip[] lockedSounds;
    [SerializeField] private AudioClip[] unlockSounds;
    
    [Header("Lock Settings")]
    [SerializeField] private bool canBeUnlocked = true;

    private bool isHighlighted;
    private bool isOpen;
    private bool unlocked;

    public void ChangeHighlight(bool highlighted)
    {
        if (isOpen) return;

        isHighlighted = highlighted;

        if (highlighted)
            InteractPrompt.Instance?.Show("Interact");
        else
            InteractPrompt.Instance?.Hide();
    }

    public void Interact(PlayerInteract player)
    {
        if (isOpen)
        {
            CloseDoor();
            return;
        }

        TryOpen();
    }

    private void TryOpen()
    {
        // 🚫 Door cannot ever be unlocked
        if (!canBeUnlocked)
        {
            if (lockedSounds != null && lockedSounds.Length > 0)
                AudioManager.PlaySFX(lockedSounds, transform.position);

            DialogueManager.Instance?.PlayDialogue(lockedDialogue);
            return;
        }

        // 🔓 Normal unlock behavior
        if (unlocked || (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey))
        {
            if (!unlocked)
            {
                PlayerInventory.Instance.UseKey();
                unlocked = true;

                if (unlockSounds != null && unlockSounds.Length > 0)
                    AudioManager.PlaySFX(unlockSounds, transform.position);
            }

            OpenDoor();
        }
        else
        {
            if (lockedSounds != null && lockedSounds.Length > 0)
                AudioManager.PlaySFX(lockedSounds, transform.position);

            DialogueManager.Instance?.PlayDialogue(lockedDialogue);
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        InteractPrompt.Instance?.Hide();

        if (openSounds != null && openSounds.Length > 0)
            AudioManager.PlaySFX(openSounds, transform.position);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTriggerName);
    }

    private void CloseDoor()
    {
        isOpen = false;

        if (closeSounds != null && closeSounds.Length > 0)
            AudioManager.PlaySFX(closeSounds, transform.position);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(closeTriggerName);
    }
}