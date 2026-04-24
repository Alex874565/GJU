using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractPrompt : MonoBehaviour
{
    public static InteractPrompt Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Hide();
    }

    public void Show(string label = "interact")
    {
        if (promptText != null) promptText.text = label;
        promptRoot.SetActive(true);
    }

    public void Hide()
    {
        promptRoot.SetActive(false);
    }
}