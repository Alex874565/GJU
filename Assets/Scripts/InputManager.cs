/* using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [SerializeField] private PlayerInput playerInput;
    
    private InputAction movementAction;
    private InputAction lookAction;
    private InputAction clickAction;
    private InputAction interactAction;

    public Vector2 Movement { get; private set; }
    public Vector2 Look { get; private set; }

    public event Action OnClickPressed;

    public event Action OnInteractPressed;

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
        clickAction = playerInput.actions["Attack"];
        clickAction.performed += ctx => OnClickPressed?.Invoke();
        interactAction = playerInput.actions["Interact"];
        interactAction.performed += ctx => OnInteractPressed?.Invoke();
    }

    private void Update()
    {
        Movement = movementAction.ReadValue<Vector2>();
        Look = lookAction.ReadValue<Vector2>();
    }
} */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    private InputAction movementAction;
    private InputAction lookAction;
    private InputAction clickAction;
    private InputAction interactAction;

    public Vector2 Movement { get; private set; }
    public Vector2 Look { get; private set; }

    public event Action OnClickPressed;
    public event Action OnInteractPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
            Debug.LogError("[InputManager] NU există PlayerInput pe obiect!");
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.actions.Enable();
    }

    private void Start()
    {
        movementAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        clickAction = playerInput.actions["Attack"];
        interactAction = playerInput.actions["Interact"];

        interactAction.performed += OnInteractPerformed;

        Debug.Log("[InputManager] Interact găsit: " + interactAction.name);
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
    }

    private void Update()
    {
        Movement = movementAction.ReadValue<Vector2>();
        Look = lookAction.ReadValue<Vector2>();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            OnClickPressed?.Invoke();

        if (interactAction != null && interactAction.WasPressedThisFrame())
            OnInteractPressed?.Invoke();
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[InputManager] Attack pressed, time: " + Time.time);
        OnClickPressed?.Invoke();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[InputManager] Interact pressed");
        OnInteractPressed?.Invoke();
    }
}