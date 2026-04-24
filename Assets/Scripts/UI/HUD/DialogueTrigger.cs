using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData dialogueData;

    [Header("Settings")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected, playing dialogue");
            if (triggerOnce && hasTriggered) return;
            hasTriggered = true;
            DialogueManager.Instance.PlayDialogue(dialogueData);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}