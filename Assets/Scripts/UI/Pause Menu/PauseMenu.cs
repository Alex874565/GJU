using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("References")]
    public GameObject pausePanel;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public float fadeDuration = 0.3f;
    
    [Header("Player Control")]
    [SerializeField] PlayerLook playerLook;
    [SerializeField] private PlayerMovement playerMove;
    [SerializeField] private PlayerInteract playerInteract;

    private bool isPaused = false;
    
    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        pausePanel.SetActive(false);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
            UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        AudioManager.Instance.SetPausedAudio(true);

        playerLook.enabled = false;
        playerMove.enabled = false;
        playerInteract.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(FadePanel(true));
    }

    public void ResumeGame()
    {
        StartCoroutine(FadeAndResume());
    }

    public void QuitToMainMenu()
    {
        StartCoroutine(QuitSequence());
    }

    IEnumerator FadeAndResume()
    {
        EventSystem.current?.SetSelectedGameObject(null);

        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        AudioManager.Instance.SetPausedAudio(false);
        
        playerLook.enabled = true;
        playerMove.enabled = true;
        playerInteract.enabled = true;
        
        yield return StartCoroutine(FadePanel(false));

        isPaused = false;
        Time.timeScale = 1f;
        
    }

    IEnumerator QuitSequence()
    {
        yield return StartCoroutine(FadePanel(false));

        isPaused = false;
        Time.timeScale = 1f;

        AudioManager.Instance.ResumeAudioForMainMenu();

        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    IEnumerator FadePanel(bool open)
    {
        pausePanel.SetActive(true);
        float from = open ? 0f : 1f;
        float to = open ? 1f : 0f;
        float elapsed = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }
        canvasGroup.alpha = to;
        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;
        if (!open)
            pausePanel.SetActive(false);
    }
}