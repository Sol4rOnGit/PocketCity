using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlayerManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    InputActionMap playerActionMap;
    InputAction placeAction;
    InputAction destroyAction;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GridManager gridManager;

    private Vector2Int _currentGridPosHovering;
    private Plane _gridPlane;
    

    void Start()
    {
        //Input
        playerActionMap = inputActions.FindActionMap("Player");
        placeAction = playerActionMap.FindAction("Place");
        destroyAction = playerActionMap.FindAction("Destroy");
        placeAction.Enable();
        destroyAction.Enable();

        //Camera
        if (playerCamera == null) { playerCamera = Camera.main; }

        //Mathematical plane
        _gridPlane = new Plane(Vector3.up, Vector3.zero); //Plane to collide with
    }

    void Update()
    {
        HandleMouseRaycast();
        HandlePlayerInput();
    }

    private void HandleMouseRaycast()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray raycast = playerCamera.ScreenPointToRay(mousePos);

        if (_gridPlane.Raycast(raycast, out float distance)) {
            Vector3 worldpoint = raycast.GetPoint(distance);
            _currentGridPosHovering = WorldToGridPosition(worldpoint);
        }
    }

    private void HandlePlayerInput()
    {
        if (placeAction.WasPressedThisFrame())
        {
            if (!gridManager.GetMapGrid().ContainsKey(_currentGridPosHovering))
            {
                gridManager.createGridElement(_currentGridPosHovering);
            }
        }

        if (destroyAction.WasPressedThisFrame())
        {
            if (gridManager.GetMapGrid().ContainsKey(_currentGridPosHovering))
            {
                gridManager.eraseGridElement(_currentGridPosHovering);
            }
        }

    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridManager.getGridScale());
        int y = Mathf.RoundToInt(worldPos.z / gridManager.getGridScale());

        return new Vector2Int(x, y);
    }

    private void OnDisable()
    {
        placeAction.Disable();
        destroyAction.Disable();
    }
}
