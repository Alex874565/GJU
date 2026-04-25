using UnityEngine;
using System.Collections;

public class MonsterTrigger : MonoBehaviour, IResettable
{
    [SerializeField] private GameObject monsterObject;

    [Range(0f, 1f)]
    [SerializeField] private float activationChance = 1f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip preSound;
    [SerializeField] private float delayAfterSound = 0.5f;

    private bool usedThisRun;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(gameObject.name + " is used " + usedThisRun);
        if (usedThisRun) return;
        Debug.Log(other.CompareTag("Player"));
        if (!other.CompareTag("Player")) return;

        usedThisRun = true;

        var dice = Random.value;
        Debug.Log(dice);
        if (dice > activationChance)
            return;

        StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        if (audioSource != null && preSound != null)
        {
            audioSource.PlayOneShot(preSound);
            yield return new WaitForSeconds(preSound.length + delayAfterSound);
        }

        Debug.Log(monsterObject);
        monsterObject?.SetActive(true);
    }

    public void ResetState()
    {
        usedThisRun = false;
        monsterObject?.SetActive(false);
    }
}