using UnityEngine;

public class KeyInventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject keyIconRoot;

    private void Awake()
    {
        keyIconRoot.SetActive(false);

        PlayerInventory.OnKeyPickedUp += ShowKeyIcon;
        PlayerInventory.OnKeyUsed += HideKeyIcon;
    }

    private void OnDestroy()
    {
        PlayerInventory.OnKeyPickedUp -= ShowKeyIcon;
        PlayerInventory.OnKeyUsed -= HideKeyIcon;
    }

    private void ShowKeyIcon()
    {
        keyIconRoot.SetActive(true);
    }

    private void HideKeyIcon()
    {
        keyIconRoot.SetActive(false);
    }
}