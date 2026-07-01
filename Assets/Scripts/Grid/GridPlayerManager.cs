using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlayerManager : MonoBehaviour
{
    public static GridPlayerManager instance { get; private set; }

    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    InputActionMap playerActionMap;
    InputAction placeAction;
    InputAction destroyAction;

    InputAction Btn1;
    InputAction Btn2;
    InputAction Btn3;

    InputAction CycleToolCategory;
    InputAction CycleToolType;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GridManager gridManager;

    [Header("Cursor")]
    [SerializeField] private GameObject cursor;
    private GameObject cursorInstance;
    public Action<Vector2Int> newCursorPosition;

    private Vector2Int currentGridPosHovering;
    private Plane gridPlane;

    [Header("Special Buildings")]
    public GameObject waterTowerPrefab;
    public GameObject fireStationPrefab;
    public GameObject policeStationPrefab;
    public GameObject coalPowerStationPrefab;
    public GameObject nuclearPowerStationPrefab;
    public GameObject hospitalPrefab;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI PlayerModeUIText;

    [Header("Special Fx")]
    public Action<Vector2Int> buildingSpecialFx;

    [Header("Tools")]
    private IBuildTool activeTool;
    private IBuildTool roadTool = new RoadTool();
    private IBuildTool zoneTool = new ZoningTool();
    private IBuildTool buildingTool = new BuildingTool();

    public Action<IBuildTool> OnToolChanged;

    void Start()
    {
        //Input
        playerActionMap = inputActions.FindActionMap("Player");
        placeAction = playerActionMap.FindAction("Place");
        destroyAction = playerActionMap.FindAction("Destroy");

        Btn1 = playerActionMap.FindAction("Btn1");
        Btn2 = playerActionMap.FindAction("Btn2");
        Btn3 = playerActionMap.FindAction("Btn3");

        CycleToolCategory = playerActionMap.FindAction("CycleToolCategory");
        CycleToolType = playerActionMap.FindAction("CycleToolType");

        placeAction.Enable(); destroyAction.Enable();

        Btn1.Enable(); Btn2.Enable(); Btn3.Enable();

        CycleToolCategory.Enable(); CycleToolType.Enable();

        //Camera
        if (playerCamera == null) { playerCamera = Camera.main; }

        //Mathematical plane
        gridPlane = new Plane(Vector3.up, Vector3.zero); //Plane to collide with

        //Cursor instantiation
        cursorInstance = Instantiate(cursor, this.transform);

        //Select a tool
        SelectTool(roadTool);
    }
    private void OnDisable()
    {
        placeAction.Disable(); destroyAction.Disable();

        Btn1.Disable(); Btn2.Disable(); Btn3.Disable();

        CycleToolCategory.Disable(); CycleToolType.Disable();
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
        if(Btn1.WasPressedThisFrame()) SelectTool(roadTool);
        if(Btn2.WasPressedThisFrame()) SelectTool(zoneTool);
        if(Btn3.WasPressedThisFrame()) SelectTool(buildingTool);

        if (activeTool == null) return;

        if (CycleToolCategory.WasPressedThisFrame())
        {
            activeTool.CycleCategory();
            OnToolChanged?.Invoke(activeTool);
        }

        if (CycleToolType.WasPressedThisFrame())
        {
            activeTool.CycleType();
            OnToolChanged?.Invoke(activeTool);
        }

        if (placeAction.WasPressedThisFrame())
        {
            activeTool.OnPlaced(currentGridPosHovering, gridManager);
        }

        if (destroyAction.WasPressedThisFrame())
        {
            activeTool.OnErased(currentGridPosHovering, gridManager);
        }
    }


    Vector2Int oldPos = new Vector2Int(int.MinValue, int.MinValue);
    private void DrawTemporary(Vector2Int gridPos)
    {
        if (gridPos == oldPos) { return; } //Early return to save on performance

        if (cursorInstance == null) { Debug.Log("Error! Cursor not found."); return; }
        cursorInstance.transform.position = new Vector3(currentGridPosHovering.x * gridManager.getGridScale(), 0f, currentGridPosHovering.y * gridManager.getGridScale());

        newCursorPosition?.Invoke(gridPos);

        //Dragging to draw roads continuously
        if (placeAction.IsPressed()) { activeTool.OnPlaced(currentGridPosHovering, gridManager); }
        else if (destroyAction.IsPressed()) { activeTool.OnErased(currentGridPosHovering, gridManager); }

        oldPos = gridPos;
    }

    //Helper functions
    private void SelectTool(IBuildTool tool)
    {
        activeTool = tool;
        activeTool?.OnSelected();
        OnToolChanged?.Invoke(activeTool);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridManager.getGridScale());
        int y = Mathf.RoundToInt(worldPos.z / gridManager.getGridScale());

        return new Vector2Int(x, y);
    }
}
