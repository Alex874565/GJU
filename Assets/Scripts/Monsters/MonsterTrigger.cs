using UnityEngine;
using System.Collections;

public class MonsterTrigger : MonoBehaviour, IResettable
{
    [SerializeField] private GameObject monsterObject;

    [Range(0f, 1f)]
    [SerializeField] private float activationChance = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip preSound;
    [SerializeField] private float delayAfterSound = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueData dialogueBeforeMonster;
    [SerializeField] private bool waitForDialogue = true;

    private bool usedThisRun;

    private void OnTriggerEnter(Collider other)
    {
        if (usedThisRun) return;
        if (!other.CompareTag("Player")) return;

        usedThisRun = true;

        if (Random.value > activationChance)
            return;

        StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        if (dialogueManager != null && dialogueBeforeMonster != null)
        {
            dialogueManager.PlayDialogue(dialogueBeforeMonster);

            if (waitForDialogue)
            {
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => !dialogueManager.isPlaying);
            }
        }

        if (audioSource != null && preSound != null)
        {
            audioSource.PlayOneShot(preSound);
            yield return new WaitForSeconds(preSound.length + delayAfterSound);
        }

        monsterObject?.SetActive(true);
    }
    
    public void SetUsed()
    {
        usedThisRun = true;
    }

    public void ForceDeactivateAndMarkUsed()
    {
        usedThisRun = true;

        if (monsterObject != null)
            monsterObject.SetActive(false);
    }
    
    public void ResetState()
    {
        usedThisRun = false;
        monsterObject?.SetActive(false);
    }
}