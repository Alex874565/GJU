using UnityEngine;

public class MonsterIdentity : MonoBehaviour
{
    [SerializeField] private MonsterType monsterType;
    [SerializeField] private DialogueData firstSeenThisRunDialogue;

    [Header("Spawn Sound")]
    [SerializeField] private AudioClip[] spawnSounds;
    [SerializeField] private float spawnVolume = 1f;

    public MonsterType Type => monsterType;
    public DialogueData FirstSeenThisRunDialogue => firstSeenThisRunDialogue;

    public void PlaySpawnSound()
    {
        if (spawnSounds == null || spawnSounds.Length == 0)
            return;

        AudioClip clip = spawnSounds[Random.Range(0, spawnSounds.Length)];
        AudioManager.PlaySFX(clip, transform.position, spawnVolume);
    }
}