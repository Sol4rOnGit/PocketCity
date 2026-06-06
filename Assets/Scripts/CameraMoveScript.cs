using System;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMoveScript : MonoBehaviour
{
    [Header("Movement Actions")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float zoomMultiplier = 50.0f;
    [SerializeField] private float sprintMultiplier = 3.0f;
    [SerializeField] private float minHeight = 3.0f;
    [SerializeField] private float maxHeight = 1000.0f;
    public InputActionAsset InputActions;
    InputAction moveAction;
    InputAction zoomAction;
    InputAction sprintAction;

    private float currentMoveSpeed;
    private float currentZoomMultiplier;

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

        currentMoveSpeed = moveSpeed;
        currentZoomMultiplier = zoomMultiplier;
    }

    // Update is called once per frame
    void Update()
    {
        HandlePlanarMovement();
        HandleVerticalMovement();
        HandleFasterMovement();
    }

    private void HandlePlanarMovement()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y).normalized;

        transform.Translate(moveDir * currentMoveSpeed * Time.deltaTime, Space.World);
    }

    private void HandleVerticalMovement()
    {
        Vector2 inputVector = zoomAction.ReadValue<Vector2>();

        float scrollValue = inputVector.y;

        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            Vector3 moveDir = new Vector3(0f, 0f, scrollValue);

            transform.Translate(moveDir * currentZoomMultiplier * Time.deltaTime);

            //Clamp
            Vector3 clampedPosition = transform.position;

            clampedPosition.y = Mathf.Clamp(clampedPosition.y, minHeight, maxHeight);

            transform.position = clampedPosition;
        }
    }

    private bool wasPressed = false;
    private void HandleFasterMovement()
    {
        bool isPressed = sprintAction.IsPressed();

        if (isPressed == wasPressed) { return; } //Ignore redundant

        if (isPressed)
        {
            currentMoveSpeed = sprintMultiplier * moveSpeed;
            currentZoomMultiplier = sprintMultiplier * zoomMultiplier;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
            currentZoomMultiplier = zoomMultiplier;
        }

        wasPressed = isPressed;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        zoomAction.Disable();
        sprintAction.Disable();
    }
}
