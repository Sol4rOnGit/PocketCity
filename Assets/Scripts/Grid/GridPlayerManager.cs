using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerMode
{
    RoadBuilding,

    ZoneResidential,
    ZoneCommercial,
    ZoneIndustrial,
    ZoneNoBuild,

    BuildingPlacement
}

public class GridPlayerManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    InputActionMap playerActionMap;
    InputAction placeAction;
    InputAction destroyAction;
    InputAction changePlayerModeAction;
    private PlayerMode currentPlayerMode = PlayerMode.RoadBuilding;


    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GridManager gridManager;

    [Header("Cursor")]
    [SerializeField] private GameObject cursor;
    private GameObject cursorInstance;

    private Vector2Int currentGridPosHovering;
    private Plane gridPlane;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI PlayerModeUIText;

    void Start()
    {
        //Input
        playerActionMap = inputActions.FindActionMap("Player");
        placeAction = playerActionMap.FindAction("Place");
        destroyAction = playerActionMap.FindAction("Destroy");
        changePlayerModeAction = playerActionMap.FindAction("ChangePlayerMode");
        placeAction.Enable();
        destroyAction.Enable();
        changePlayerModeAction.Enable();

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
        if (changePlayerModeAction.WasPressedThisFrame())
        {
            IncrementPlayerMode();
        }

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
        switch (currentPlayerMode)
        {
            case PlayerMode.RoadBuilding:
                gridManager.createRoadOnGrid(currentGridPosHovering); break;

            case PlayerMode.ZoneResidential:
                gridManager.zoneTileOnGrid(currentGridPosHovering, ZoneType.Residential); break;
            case PlayerMode.ZoneCommercial:
                gridManager.zoneTileOnGrid(currentGridPosHovering, ZoneType.Commercial); break;
            case PlayerMode.ZoneIndustrial:
                gridManager.zoneTileOnGrid(currentGridPosHovering, ZoneType.Industrial); break;
            case PlayerMode.ZoneNoBuild:
                gridManager.zoneTileOnGrid(currentGridPosHovering, ZoneType.NoBuild); break;

            case PlayerMode.BuildingPlacement:
                HandleSpecialBuildingPlacement();
                break;
            default:
                Debug.LogError("Invalid Player State!");
                break;
        }
    } 
    
    private void AttemptToEraseElement()
    {
        switch (currentPlayerMode)
        {
            case PlayerMode.RoadBuilding:
                gridManager.eraseRoadElement(currentGridPosHovering); break;

            case PlayerMode.ZoneResidential:
            case PlayerMode.ZoneCommercial:
            case PlayerMode.ZoneIndustrial:
            case PlayerMode.ZoneNoBuild:
                gridManager.removeZoneFromGrid(currentGridPosHovering); break;

            case PlayerMode.BuildingPlacement:
                gridManager.eraseRoadElement(currentGridPosHovering); break;
            default:
                Debug.LogError("Invalid Player State!");
                break;
        }
    }

    public void IncrementPlayerMode()
    {
        currentPlayerMode = (PlayerMode)(((int)currentPlayerMode + 1) % System.Enum.GetValues(typeof(PlayerMode)).Length);

        switch (currentPlayerMode) {
            case PlayerMode.RoadBuilding:
                PlayerModeUIText.text = "Road Building"; break;

            case PlayerMode.ZoneResidential:
                PlayerModeUIText.text = "Residential Zoning"; break;
            case PlayerMode.ZoneCommercial:
                PlayerModeUIText.text = "Commercial Zoning"; break;
            case PlayerMode.ZoneIndustrial:
                PlayerModeUIText.text = "Industrial Zoning"; break;
            case PlayerMode.ZoneNoBuild:
                PlayerModeUIText.text = "No Build Zoning"; break;

            case PlayerMode.BuildingPlacement:
                PlayerModeUIText.text = "W.I.P. Demolish Buildings"; break;
            default:
                Debug.LogError("Invalid Player State!");
                break;
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

    private void HandleSpecialBuildingPlacement()
    {

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
        changePlayerModeAction.Disable();
    }
}
