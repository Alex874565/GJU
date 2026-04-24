using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    [SerializeField] private int runNo;
    [Header("Player")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private Transform playerTransform;

    [Header("Cutscenes")]
    [SerializeField] private CutscenePlayer introCutscene;
    
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerManager = player.GetComponent<PlayerManager>();
    }
    
    public void ResetGame()
    {
        playerTransform.position = introCutscene.transform.position;
        playerManager.ResetAnxiety();
    }
}