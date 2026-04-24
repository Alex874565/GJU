using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRange = 2f;

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Animation")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";

    [Header("Player Reference")]
    [SerializeField] private Transform player;

    private bool _playerInRange = false;
    private bool _isOpen = false;

    private void Start()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (_isOpen || player == null) return;

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

        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryOpen();
        }
    }

    private void TryOpen()
    {
        if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey)
        {
            PlayerInventory.Instance.UseKey();
            OpenDoor();
        }
        else
        {
            DialogueManager.Instance?.PlayDialogue(lockedDialogue);
        }
    }

    private void OpenDoor()
    {
        _isOpen = true;
        _playerInRange = false;
        InteractPrompt.Instance?.Hide();

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTriggerName);
        }
        else
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log("[LockedDoor] Door opened.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}