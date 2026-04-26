using System.Collections;
using UnityEngine;

public class DoubleDoorController : MonoBehaviour, IInteractable
{
    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private bool startOpen;

    [Header("Rotation")]
    [SerializeField] private float leftOpenAngle = -85f;
    [SerializeField] private float rightOpenAngle = 85f;
    [SerializeField] private float openDuration = 0.75f;
    [SerializeField] private float closeDuration = 0.55f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Lock")]
    [SerializeField] private bool isLocked;
    [SerializeField] private bool canBeUnlocked = true;
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Audio")]
    [SerializeField] private AudioClip[] openSounds;
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private AudioClip[] lockedSounds;
    [SerializeField] private AudioClip[] unlockSounds;

    private bool isOpen;
    private Quaternion leftClosedRot;
    private Quaternion rightClosedRot;
    private Coroutine moveRoutine;

    private void Start()
    {
        if (leftDoor != null)
            leftClosedRot = leftDoor.localRotation;

        if (rightDoor != null)
            rightClosedRot = rightDoor.localRotation;

        isOpen = startOpen;

        if (isOpen)
            SetDoorRotations(1f);
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
            if (!canBeUnlocked)
            {
                PlayLockedFeedback();
                return;
            }

            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey)
            {
                PlayerInventory.Instance.UseKey();
                isLocked = false;

                PlayRandom(unlockSounds);
                ToggleDoors();
            }
            else
            {
                PlayLockedFeedback();
            }

            return;
        }

        ToggleDoors();
    }

    private void ToggleDoors()
    {
        isOpen = !isOpen;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        PlayRandom(isOpen ? openSounds : closeSounds);

        moveRoutine = StartCoroutine(RotateDoors(
            isOpen ? 1f : 0f,
            isOpen ? openDuration : closeDuration,
            isOpen ? openCurve : closeCurve
        ));
    }

    private IEnumerator RotateDoors(float targetOpenAmount, float duration, AnimationCurve curve)
    {
        Quaternion leftStart = leftDoor.localRotation;
        Quaternion rightStart = rightDoor.localRotation;

        Quaternion leftTarget = Quaternion.Slerp(
            leftClosedRot,
            leftClosedRot * Quaternion.Euler(0f, leftOpenAngle, 0f),
            targetOpenAmount
        );

        Quaternion rightTarget = Quaternion.Slerp(
            rightClosedRot,
            rightClosedRot * Quaternion.Euler(0f, rightOpenAngle, 0f),
            targetOpenAmount
        );

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(timer / duration));

            if (leftDoor != null)
                leftDoor.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);

            if (rightDoor != null)
                rightDoor.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);

            yield return null;
        }

        if (leftDoor != null)
            leftDoor.localRotation = leftTarget;

        if (rightDoor != null)
            rightDoor.localRotation = rightTarget;
    }

    private void SetDoorRotations(float openAmount)
    {
        if (leftDoor != null)
            leftDoor.localRotation = leftClosedRot * Quaternion.Euler(0f, leftOpenAngle * openAmount, 0f);

        if (rightDoor != null)
            rightDoor.localRotation = rightClosedRot * Quaternion.Euler(0f, rightOpenAngle * openAmount, 0f);
    }

    private void PlayLockedFeedback()
    {
        PlayRandom(lockedSounds);
        DialogueManager.Instance?.PlayDialogue(lockedDialogue);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        AudioManager.PlaySFX(clip, transform.position);
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}