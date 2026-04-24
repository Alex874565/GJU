using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    
    public void ResetGame()
    {
        playerManager.ResetAnxiety();
    }
}