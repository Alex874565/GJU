using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    private Transform player;
    
    [Header("Door State")]
    [SerializeField] private bool startOpen = false;
    [SerializeField] private bool isLocked = false;

    [Header("Door Rotation")]
    [SerializeField] private float openAngle = -85f;
    [SerializeField] private float openSpeed = 5f;
    [SerializeField] private float sideDeadzone = 0.15f;
    [SerializeField] private Transform doorCenterPoint;
    private float lastOpenSide = 1f;
    
    [Header("Open Direction")]
    [SerializeField] private Transform frontSidePoint;
    [SerializeField] private Transform backSidePoint;
    [SerializeField] private bool invertOpenDirection;

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Audio")]
    [SerializeField] private AudioClip[] openSounds;
    [SerializeField] private AudioClip[] closeSounds;
    [SerializeField] private AudioClip[] lockedSounds;
    [SerializeField] private AudioClip[] unlockSounds;
    
    [Header("Realistic Motion")]
    [SerializeField] private float openDuration = 0.75f;
    [SerializeField] private float closeDuration = 0.55f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine moveRoutine;

    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        isOpen = startOpen;

        if (isOpen)
            transform.localRotation = openRotation;
    }
    
    private Quaternion GetOpenRotationAwayFromPlayer()
    {
        if (player == null || frontSidePoint == null || backSidePoint == null)
            return closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        float frontDist = Vector3.Distance(player.position, frontSidePoint.position);
        float backDist = Vector3.Distance(player.position, backSidePoint.position);

        bool playerIsFront = frontDist < backDist;

        float finalAngle = playerIsFront
            ? -Mathf.Abs(openAngle)
            : Mathf.Abs(openAngle);

        if (invertOpenDirection)
            finalAngle *= -1f;

        return closedRotation * Quaternion.Euler(0f, finalAngle, 0f);
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

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        if (isOpen)
        {
            openRotation = GetOpenRotationAwayFromPlayer();

            if (openSounds != null && openSounds.Length > 0)
                AudioManager.PlaySFX(openSounds, transform.position);

            openRotation = GetOpenRotationAwayFromPlayer();
            moveRoutine = StartCoroutine(RotateDoor(openRotation, openDuration, openCurve));
        }
        else
        {
            if (closeSounds != null && closeSounds.Length > 0)
                AudioManager.PlaySFX(closeSounds, transform.position);

            moveRoutine = StartCoroutine(RotateDoor(closedRotation, closeDuration, closeCurve));
        }
    }
    
    private IEnumerator RotateDoor(Quaternion targetRotation, float duration, AnimationCurve curve)
    {
        Quaternion startRotation = transform.localRotation;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float t = curve.Evaluate(rawT);

            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        transform.localRotation = targetRotation;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}