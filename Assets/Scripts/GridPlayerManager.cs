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

    [Header("Cursor")]
    [SerializeField] private GameObject cursor;
    private GameObject cursorInstance;

    private Vector2Int currentGridPosHovering;
    private Plane gridPlane;


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
        gridPlane = new Plane(Vector3.up, Vector3.zero); //Plane to collide with

        //Cursor instantiation
        cursorInstance = Instantiate(cursor, this.transform);
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

        if (gridPlane.Raycast(raycast, out float distance)) {
            Vector3 worldpoint = raycast.GetPoint(distance);
            currentGridPosHovering = WorldToGridPosition(worldpoint);

            DrawTemporary(currentGridPosHovering);
        }
    }

    private void HandlePlayerInput()
    {
        if (placeAction.WasPressedThisFrame())
        {
            AttemptToCreateElement();
        }

        if (destroyAction.WasPressedThisFrame())
        {
            AttemptToEraseElement();
        }
    }

    private void AttemptToCreateElement()
    {
        if (!gridManager.GetMapGrid().ContainsKey(currentGridPosHovering))
        {
            gridManager.createGridElement(currentGridPosHovering);
        }
    }

    private void AttemptToEraseElement()
    {
        if (gridManager.GetMapGrid().ContainsKey(currentGridPosHovering))
        {
            gridManager.eraseGridElement(currentGridPosHovering);
        }
    }

    Vector2Int oldPos = new Vector2Int(int.MinValue, int.MinValue);
    private void DrawTemporary(Vector2Int gridPos)
    {
        if (gridPos == oldPos) { return; } //Early return to save on performance

        if (cursorInstance == null) { Debug.Log("Error! Cursor not found."); return; }
        cursorInstance.transform.position = new Vector3(currentGridPosHovering.x * gridManager.getGridScale(), 0f, currentGridPosHovering.y * gridManager.getGridScale());

        //Dragging to draw roads continuously
        if (placeAction.IsPressed()) { AttemptToCreateElement(); }
        else if (destroyAction.IsPressed()) { AttemptToEraseElement(); }

        oldPos = gridPos;
    }

    //Helper functions

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
