using System.Collections;
using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable, IResettable
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform sleepPosition;
    [SerializeField] private CutscenePlayer bedCutscene;

    [Header("Settings")]
    [SerializeField] private string promptText = "Sleep";

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

        TeleportPlayerToBed();

        if (bedCutscene != null)
            yield return bedCutscene.PlayRoutine();

        if (playerMovement != null)
            playerMovement.inputLocked = false;

        if (playerManager != null)
            playerManager.inputLocked = false;
    }

    private void TeleportPlayerToBed()
    {
        if (sleepPosition == null || playerTransform == null)
            return;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;

            playerRb.position = sleepPosition.position;
            playerRb.rotation = sleepPosition.rotation;
        }

        playerTransform.SetPositionAndRotation(
            sleepPosition.position,
            sleepPosition.rotation
        );

        Physics.SyncTransforms();
    }

    public void ResetState()
    {
        used = false;
        isHighlighted = false;
        InteractPrompt.Instance?.Hide();
    }
}