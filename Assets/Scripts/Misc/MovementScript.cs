using UnityEngine;
using UnityEngine.InputSystem;

public class MovementScript : MonoBehaviour
{
    [Header("Movement Vars")]
    private float moveSpeed;
    [SerializeField] private float zoomMultiplier = 50.0f;
    [SerializeField] private float sprintMultiplier = 3.0f;
    [SerializeField] private float minHeight = 6.0f;
    [SerializeField] private float maxHeight = 100.0f;
    [SerializeField] private Vector2 bounds = new(100, 100);

    [Header("Movement Actions")]
    public InputActionAsset InputActions;
    InputAction moveAction;
    InputAction zoomAction;
    InputAction sprintAction;

    private float currentMoveSpeed;
    private float currentZoomMultiplier;

    private bool canMove = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InputActionMap PlayerMap = InputActions.FindActionMap("Player");
        moveAction = PlayerMap.FindAction("Move");
        zoomAction = PlayerMap.FindAction("Zoom");
        sprintAction = PlayerMap.FindAction("Sprint");

        moveAction.Enable();
        zoomAction.Enable();
        sprintAction.Enable();

        moveSpeed = PlayerPrefs.GetFloat("MoveSpeed", 5f);

        currentMoveSpeed = moveSpeed;
        currentZoomMultiplier = zoomMultiplier;
    }

    // Update is called once per frame
    void Update()
    {
        if (canMove)
        {
            HandlePlanarMovement();
            HandleVerticalMovement();
        }

        HandleCursorState();
        HandleFasterMovement();
    }

    private InputDevice lastUsedDevice;

    private void HandleCursorState()
    {
        if (moveAction.activeControl == null) return;

        InputDevice currentDevice = moveAction.activeControl.device;

        if (currentDevice == lastUsedDevice) return;
        lastUsedDevice = currentDevice;

        if (currentDevice is Keyboard || currentDevice is Mouse)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (currentDevice is Gamepad)
        {
            Debug.Log("Switching to Controller");

            if (Mouse.current != null)
            {
                Vector2 screenCentre = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Mouse.current.WarpCursorPosition(screenCentre);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    private void HandlePlanarMovement()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y).normalized;

        transform.Translate(moveDir * currentMoveSpeed * Time.unscaledDeltaTime, Space.World);

        //Clamp to bounds on xz plane
        Vector3 clampedPos = transform.position;

        clampedPos.x = Mathf.Clamp(clampedPos.x, -bounds.x, bounds.x);
        clampedPos.z = Mathf.Clamp(clampedPos.z, -bounds.y, bounds.y);

        transform.position = clampedPos;
    }

    private void HandleVerticalMovement()
    {
        Vector2 inputVector = zoomAction.ReadValue<Vector2>();

        float scrollValue = inputVector.y;

        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            Vector3 moveDir = new Vector3(0f, 0f, scrollValue);

            transform.Translate(moveDir * currentZoomMultiplier * Time.unscaledDeltaTime, Space.Self);

            //Clamp
            Vector3 clampedPosition = transform.position;

            clampedPosition.y = Mathf.Clamp(clampedPosition.y, minHeight, maxHeight);

            transform.position = clampedPosition;
        }
    }

    private bool wasPressed = false;
    private bool isSprinting = false;
    private void HandleFasterMovement()
    {
        bool isPressed = sprintAction.IsPressed();

        if (isPressed == wasPressed) { return; }
        wasPressed = isPressed;

        if (GameManager.instance.toggleSprintEnabled)
        {
            if (isPressed) isSprinting = !isSprinting;
        } 
        else
        {
            isSprinting = isPressed;
        }

        if (isSprinting)
        {
            currentMoveSpeed = sprintMultiplier * moveSpeed;
            currentZoomMultiplier = sprintMultiplier * zoomMultiplier;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
            currentZoomMultiplier = zoomMultiplier;
        }
    }

    private void MovementSpeedChanged(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (!wasPressed) currentMoveSpeed = newSpeed;
    }

    private void OnEnable()
    {
        GameManager.instance.OnMoveSpeedChanged += MovementSpeedChanged;
        GameManager.instance.updateMovementPermissions += SetMovementPerms;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        zoomAction.Disable();
        sprintAction.Disable();

        if (GameManager.instance != null)
        {
            GameManager.instance.OnMoveSpeedChanged -= MovementSpeedChanged;
            GameManager.instance.updateMovementPermissions -= SetMovementPerms;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetMovementPerms(bool value)
    {
        canMove = value;
        Debug.Log(value);
        Debug.Log(canMove);
    }
}
