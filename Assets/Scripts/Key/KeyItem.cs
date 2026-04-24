using UnityEngine;
using UnityEngine.InputSystem;

public class KeyItem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pickupRange = 2f;

    [Header("Player Reference")]
    [SerializeField] private Transform player;

    private bool _playerInRange = false;
    private bool _pickedUp = false;

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
        if (_pickedUp || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= pickupRange;

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
            PickUp();
        }
    }

    private void PickUp()
    {
        _pickedUp = true;
        _playerInRange = false;
        InteractPrompt.Instance?.Hide();
        PlayerInventory.Instance?.PickUpKey();
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}