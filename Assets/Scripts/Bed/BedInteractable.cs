using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BedInteractable : MonoBehaviour, IInteractable, IResettable
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform sleepPosition;
    [SerializeField] private CutscenePlayer bedCutscene;
    [SerializeField] private GameObject playerVisual;
    [SerializeField] private GameObject lanternVisual; // optional if separate model
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private Lantern lantern;

    [Header("Settings")]
    [SerializeField] private string promptText = "Sleep";
    
    [SerializeField] private AudioClip endSound;
    [SerializeField] private float endVolume = 1f;

    private bool used;
    private bool isHighlighted;

    public void ChangeHighlight(bool highlighted)
    {
        isHighlighted = highlighted;

        if (used || playerManager == null || playerManager.AreLightsOff)
        {
            InteractPrompt.Instance?.Hide();
            return;
        }

        if (highlighted)
            InteractPrompt.Instance?.Show(promptText);
        else
            InteractPrompt.Instance?.Hide();
    }

    public void Interact(PlayerInteract player)
    {
        if (used) return;
        if (playerManager != null && playerManager.AreLightsOff) return;

        used = true;
        InteractPrompt.Instance?.Hide();

        StartCoroutine(BedRoutine());
    }

    private IEnumerator BedRoutine()
    {
        if (playerMovement != null)
            playerMovement.inputLocked = true;

        if (playerManager != null)
            playerManager.inputLocked = true;

        if (lantern != null)
            lantern.InputLocked = true;

        if (playerLook != null)
            playerLook.enabled = false;

        playerVisual?.SetActive(false);
        lanternVisual?.SetActive(false);
        
        TeleportPlayerToBed();

        if (bedCutscene != null)
            yield return bedCutscene.PlayRoutine();

        // 🔊 Play sound
        float delay = 0f;
        if (endSound != null)
        {
            AudioManager.PlaySFX(endSound, transform.position, endVolume);
            delay = endSound.length;
        }

        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene("Credits");
    }

    private void TeleportPlayerToBed()
    {
        if (sleepPosition == null || playerTransform == null)
            return;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;

            playerRb.MovePosition(sleepPosition.position);
            playerRb.MoveRotation(sleepPosition.rotation);
        }
        else
        {
            playerTransform.SetPositionAndRotation(
                sleepPosition.position,
                sleepPosition.rotation
            );
        }

        Physics.SyncTransforms();
    }

    public void ResetState()
    {
        used = false;
        isHighlighted = false;
        InteractPrompt.Instance?.Hide();
    }
}