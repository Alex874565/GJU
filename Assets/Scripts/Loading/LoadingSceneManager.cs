using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingSceneManager : MonoBehaviour
{
    public static string SceneToLoad = "Main";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Timing")]
    [SerializeField] private float minimumLoadTime = 1.5f;

    private IEnumerator Start()
    {
        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneToLoad);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f || timer < minimumLoadTime)
        {
            timer += Time.deltaTime;

            if (loadingText != null)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
            }

            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    public static void LoadScene(string sceneName)
    {
        SceneToLoad = sceneName;
        SceneManager.LoadScene("Loading");
    }
}