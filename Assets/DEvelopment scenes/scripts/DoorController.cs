using UnityEngine;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    [Header("Door State")]
    [SerializeField] private bool startOpen = false;
    [SerializeField] private bool isLocked = false;

    [Header("Door Rotation")]
    [SerializeField] private float openAngle = -85f;
    [SerializeField] private float openSpeed = 5f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Player Reference")]
    [SerializeField] private Transform player;

    private bool _isOpen;
    private bool _playerInRange = false;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    private void Start()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        _isOpen = startOpen;
        if (_isOpen)
            transform.localRotation = _openRotation;
    }

    private void Update()
    {
        Quaternion targetRotation = _isOpen ? _openRotation : _closedRotation;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        if (inRange && !_playerInRange)
        {
            _playerInRange = true;
            InteractPrompt.Instance?.Show("interact");
        }
        else if (!inRange && _playerInRange)
        {
            _playerInRange = false;
            InteractPrompt.Instance?.Hide();
        }

        if (_playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Debug.Log($"TryInteract — isLocked: {isLocked}, HasKey: {PlayerInventory.Instance?.HasKey}");

        if (isLocked)
        {
            if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey)
            {
                PlayerInventory.Instance.UseKey();
                isLocked = false;
                ToggleDoor();
            }
            else
            {
                Debug.Log("Playing locked dialogue");
                DialogueManager.Instance?.PlayDialogue(lockedDialogue);
            }
        }
        else
        {
            ToggleDoor();
        }
    }

    public void ToggleDoor()
    {
        _isOpen = !_isOpen;
    }

    public bool IsOpen()
    {
        return _isOpen;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}