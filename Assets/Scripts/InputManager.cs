using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [SerializeField] private PlayerInput playerInput;
    
    private InputAction movementAction;
    private InputAction lookAction;

    public Vector2 Movement { get; private set; }
    public Vector2 Look { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        movementAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
    }

    private void Update()
    {
        Movement = movementAction.ReadValue<Vector2>();
        Look = lookAction.ReadValue<Vector2>();
    }
}