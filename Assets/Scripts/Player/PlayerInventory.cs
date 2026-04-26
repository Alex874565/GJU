using UnityEngine;
using System;

public class PlayerInventory : MonoBehaviour, IResettable
{
    public static PlayerInventory Instance { get; private set; }

    public bool HasKey { get; private set; } = false;

    public static event Action OnKeyPickedUp;
    public static event Action OnKeyUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PickUpKey()
    {
        if (HasKey) return;
        HasKey = true;
        OnKeyPickedUp?.Invoke();
        Debug.Log("[PlayerInventory] Key picked up.");
    }

    public void ResetState()
    {
        HasKey = false;
        Debug.Log("[PlayerInventory] Reset key state.");

        // Optional: notify UI to update
        OnKeyUsed?.Invoke(); // forces UI to hide key icon if you use it that way
    }
    
    public bool UseKey()
    {
        if (!HasKey) return false;
        HasKey = false;
        OnKeyUsed?.Invoke();
        Debug.Log("[PlayerInventory] Key used on door.");
        return true;
    }
}