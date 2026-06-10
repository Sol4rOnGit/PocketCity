using System;
using System.Collections.Generic;
using UnityEngine;

public enum ZoneType
{
    None,
    NoBuild,
    Residential,
    Commercial,
    Industrial
}
public class GridManager : MonoBehaviour
{
    //Vars
    private const float GridScale = 2.0f;

    public float getGridScale()
    {
        return GridScale;
    }

    [Header("Road Prefabs")]
    [SerializeField] private GameObject SquareRoad;
    [SerializeField] private GameObject EndRoad;
    [SerializeField] private GameObject StraightRoad;
    [SerializeField] private GameObject Turn;
    [SerializeField] private GameObject ThreeWayIntersection;
    [SerializeField] private GameObject FourWayIntersection;

    [Header("Zoning Prefabs")]
    [SerializeField] private GameObject noBuildZonePrefab;
    [SerializeField] private GameObject residentialZonePrefab;
    [SerializeField] private GameObject commercialZonePrefab;
    [SerializeField] private GameObject industrialZonePrefab;

    [Header("Nature Settings")]
    [SerializeField] private GameObject[] TreePrefabs;

    //Grid

    public class GridTile
    {
        public string tileName;
        public GameObject instance;
        public int rotationDegrees;
        public BuildingType? buildingType;
        public Building buildingScript;
        public ZoneType zoneType;
        public bool isRoad;
    }

    private Dictionary<Vector2Int, GridTile> mapGrid = new Dictionary<Vector2Int, GridTile>();
    public Dictionary<Vector2Int, GameObject> TreeGrid = new Dictionary<Vector2Int, GameObject>();
    public List<Vector2Int> RoadPositions { get; private set; } = new List<Vector2Int>();

    [HideInInspector] public List<Vector2Int> BuildingPositions = new List<Vector2Int>();
    [HideInInspector] public List<Vector2Int> ZonedPositions = new List<Vector2Int>();

    public Dictionary<Vector2Int, GridTile> GetMapGrid()
    {
        return mapGrid;
    }

    private void Awake()
    {
        PlaceInitialGrid();
    }

    private FinanceManager financeManager;

    private void Start()
    {
        financeManager = FinanceManager.instance;
    }
    //Creation/Deletion Methods

    //Creation
    public void createRoadOnGrid(Vector2Int pos, bool isFree = false)
    {
        bool charged = false;

        if (mapGrid.TryGetValue(pos, out GridTile tile))
        {
            if (!isFree)
            {
                bool success = financeManager.Purchase(financeManager.costRoad);
                if (!success) return;
                charged = true;
            }

            if (!tile.isRoad && tile.buildingType == null)
            {
                if (tile.instance != null) { Destroy(tile.instance); }
                mapGrid.Remove(pos);
            }
            else
            {
                Debug.Log($"The grid point at {pos.x}, {pos.y} already has an element!");
                return; //Already exists, should notify the user here later
            }
        }

        //Cost
        if (!isFree && !charged)
        {
            bool success = financeManager.Purchase(financeManager.costRoad);
            if (!success) return;
        }

        ClearTreeAtPos(pos);

        //Figure out which ones to do
        (int rotationDegrees, GameObject prefab) = DecideOnPrefab(pos);

        Vector3 WorldPos = new Vector3(pos.x * GridScale, 0f, pos.y * GridScale);

        Quaternion WorldRotation = Quaternion.Euler(0f, rotationDegrees, 0f);

        GameObject Tile = Instantiate(prefab, WorldPos, WorldRotation, this.transform);
        Tile.name = $"{prefab.name} ({pos.x}, {pos.y})";

        GridTile newTile = new GridTile { tileName = prefab.name, instance = Tile, rotationDegrees = rotationDegrees, isRoad = true };

        mapGrid.Add(pos, newTile);
        RoadPositions.Add(pos);

        //Update the four grids around it
        updateGridElement(pos + Vector2Int.up);
        updateGridElement(pos + Vector2Int.down);
        updateGridElement(pos + Vector2Int.left);
        updateGridElement(pos + Vector2Int.right);
    }

    public void createBuildingOnGrid(Vector2Int pos, GameObject buildingInstance, int rotationDegrees, BuildingType buildingType, string buildingName)
    {
        //Grab the building script from the instance
        Building buildingScript = InitialiseBuildingScript(buildingInstance);

        //Attempt to spawn the building given tile is occupied
        if (mapGrid.TryGetValue(pos, out GridTile tile))
        {
            if (tile.isRoad || tile.buildingType != null)
            {
                if (buildingInstance != null) { Destroy(buildingInstance); return; } //Mem. cleanup
            }

            if (tile.instance != null) { Destroy(tile.instance); }

            //Building spawn h
            ClearTreeAtPos(pos);

            tile.tileName = buildingName;
            tile.instance = buildingInstance;
            tile.rotationDegrees = rotationDegrees;
            tile.buildingType = buildingType;
            tile.buildingScript = buildingScript;

            if (!BuildingPositions.Contains(pos)) { BuildingPositions.Add(pos); }

            return;
        }

        //Spawning building if tile isn't already occupied
        ClearTreeAtPos(pos);

        GridTile buildingTile = new GridTile { tileName = buildingName, instance = buildingInstance, rotationDegrees = rotationDegrees, buildingType = buildingType, isRoad = false, buildingScript = buildingScript };

        mapGrid.Add(pos, buildingTile);
        BuildingPositions.Add(pos);
    }

    public void zoneTileOnGrid(Vector2Int pos, ZoneType zoneType)
    {
        bool success = false;

        if (mapGrid.TryGetValue(pos, out GridTile tile))
        {
            if (tile.isRoad || tile.buildingType != null)
            {
                Debug.Log("Cannot zone a tile with pre-existing infrastucture.");
                return; 
            }

            if (tile.zoneType != zoneType)
            {
                success = financeManager.Purchase(financeManager.costZoning);
                if (!success) return;

                if (tile.instance != null) { Destroy(tile.instance); }
                tile.zoneType = zoneType;
                tile.tileName = $"{zoneType} Zone Slot";
                tile.instance = InstantiateZonePrefab(pos, zoneType);
            }
            return;
        }

        success = financeManager.Purchase(financeManager.costZoning);
        if (!success) return;

        GameObject currentInstance = InstantiateZonePrefab(pos, zoneType);

        GridTile zoneTile = new GridTile
        {
            tileName = $"{zoneType} Zone Slot",
            instance = currentInstance,
            zoneType = zoneType,
            isRoad = false
        };

        mapGrid.Add(pos, zoneTile);
        ZonedPositions.Add(pos);
    }

    public void SpawnTreeInChunk(Vector2Int gridPos, Vector3 worldPos, Quaternion randomRotation)
    {
        if (TreePrefabs == null || TreePrefabs.Length == 0) { return; }

        GameObject treePrefab = TreePrefabs[UnityEngine.Random.Range(0, TreePrefabs.Length)];
        GameObject treeInstance = Instantiate(treePrefab, worldPos, randomRotation, this.transform);
        treeInstance.name = $"Tree ({gridPos.x}, {gridPos.y})";

        TreeGrid.Add(new Vector2Int(gridPos.x, gridPos.y), treeInstance);
    }

    //Deletions
    public void eraseRoadElement(Vector2Int pos)
    {
        if (!mapGrid.ContainsKey(pos))
        {
            Debug.Log("Nothing to destroy.");
            return;
        }

        GridTile tileData = mapGrid[pos];

        if (!tileData.isRoad) { return; } //can only remove roads

        //Take money
        bool success = financeManager.Purchase(financeManager.costRoadDemolition);
        if (!success) { return; }

        //Destroy instance
        if (tileData.instance != null) { Destroy(tileData.instance); }

        if (tileData.zoneType != ZoneType.None)
        {
            tileData.buildingType = null;
            tileData.tileName = $"{tileData.zoneType} Zone Slot";
            tileData.instance = InstantiateZonePrefab(pos, tileData.zoneType);
            BuildingPositions.Remove(pos);
            return;
        }

        //Remove from memory
        mapGrid.Remove(pos);
        RoadPositions.Remove(pos);

        //Update the other stuff around it
        updateGridElement(pos + Vector2Int.up);
        updateGridElement(pos + Vector2Int.down);
        updateGridElement(pos + Vector2Int.left);
        updateGridElement(pos + Vector2Int.right);
    }

    public void removeZoneFromGrid(Vector2Int pos)
    {
        if (mapGrid.TryGetValue(pos, out GridTile tileData)) {
            if(tileData.isRoad || tileData.buildingType != null)
            {
                Debug.Log("Cannot unzone a tile with pre-existing infrastucture.");
                return;
            }

            if (tileData.instance != null) { Destroy(tileData.instance); }

            tileData.zoneType = ZoneType.None;
            ZonedPositions.Remove(pos);

            if(!tileData.isRoad || tileData.buildingType == null)
            {
                mapGrid.Remove(pos);
            }
        }
    }

    private void ClearTreeAtPos(Vector2Int pos)
    {
        if (TreeGrid.TryGetValue(pos, out GameObject treeInstance))
        {
            if (treeInstance != null)
            {
                Destroy(treeInstance);
            }
            TreeGrid.Remove(pos);
        }
    }
    private void PlaceInitialGrid()
    {
        //Random initial road layout

        //Middle
        createRoadOnGrid(new Vector2Int(0, 0), true);

        //Left
        createRoadOnGrid(new Vector2Int(-1, 0), true);
        createRoadOnGrid(new Vector2Int(-2, 0), true);

        //Up
        createRoadOnGrid(new Vector2Int(0, 1), true);
        createRoadOnGrid(new Vector2Int(0, 2), true);

        //Right
        createRoadOnGrid(new Vector2Int(1, 0), true);
        createRoadOnGrid(new Vector2Int(2, 0), true);
        createRoadOnGrid(new Vector2Int(2, 1), true);
    }

    //Helper functions
    private (int rotationDegrees, GameObject prefab) DecideOnPrefab(Vector2Int pos)
    {
        //Check above, left, right, bottom.
        bool hasUp = mapGrid.TryGetValue(pos + Vector2Int.up, out GridTile upTile) && upTile.isRoad;
        bool hasDown = mapGrid.TryGetValue(pos + Vector2Int.down, out GridTile downTile) && downTile.isRoad;
        bool hasLeft = mapGrid.TryGetValue(pos + Vector2Int.left, out GridTile leftTile) && leftTile.isRoad;
        bool hasRight = mapGrid.TryGetValue(pos + Vector2Int.right, out GridTile rightTile) && rightTile.isRoad;

        int connCount = 0; 
        if (hasUp) { connCount += 1; } if (hasDown) { connCount += 1; } if (hasLeft) { connCount += 1; } if (hasRight) { connCount += 1; }

        //Handle prefabs
        switch (connCount)
        {
            case 4:
                //if 4 of them dw and place a 4 way intersection, return 0 rotation
                return (0, FourWayIntersection);
            case 3:
                //if 3 of them, create a 3 way intersection. rotation so that the missing one idk figure this out later
                if (!hasDown) return (0, ThreeWayIntersection);
                if (!hasUp) return (180, ThreeWayIntersection);
                if (!hasLeft) return (90, ThreeWayIntersection);
                if (!hasRight) return (-90, ThreeWayIntersection);
                break;
            case 2:
                //Straight
                if (hasLeft && hasRight) { return (0, StraightRoad); }
                if (hasUp && hasDown) { return (-90, StraightRoad); }

                //Turn Left
                if (hasUp && hasRight) { return (0, Turn); }
                if (hasDown && hasRight) { return (90, Turn); }
                if (hasDown && hasLeft) { return (180, Turn); }
                if (hasUp && hasLeft) { return (-90, Turn); }
                break;
            case 1:
                //End of the road
                if (hasUp) return (90, EndRoad);
                if (hasDown) return (-90, EndRoad);
                if (hasLeft) return (0, EndRoad);
                if (hasRight) return (180, EndRoad);
                break;
            default:
                //Alone idk what to do
                return (0, SquareRoad);
        }

        throw new Exception("Error!");
    }

    private void updateGridElement(Vector2Int pos)
    {
        if (!mapGrid.ContainsKey(pos))
        {
            return;
            //Doesn't exist so early return
        }

        GridTile tileData = mapGrid[pos];

        if (!tileData.isRoad) return;

        (int newRotationDegrees, GameObject newPrefab) = DecideOnPrefab(pos);

        //Update prefab & rotation
        if (tileData.instance != null)
        {
            Destroy(tileData.instance);
        }

        //Instantiate new 
        Vector3 WorldPos = new Vector3(pos.x * GridScale, 0f, pos.y * GridScale);
        Quaternion WorldRotation = Quaternion.Euler(0f, newRotationDegrees, 0f);

        GameObject Tile = Instantiate(newPrefab, WorldPos, WorldRotation, this.transform);
        Tile.name = $"{newPrefab.name} ({pos.x}, {pos.y})";

        //Update GridTile with the new data (which is referenced in mapgrid so that is updated as well)
        tileData.tileName = newPrefab.name;
        tileData.instance = Tile;
        tileData.rotationDegrees = newRotationDegrees;
    }

    private GameObject InstantiateZonePrefab(Vector2Int pos, ZoneType zoneType)
    {
        GameObject currentPrefab = null;

        switch (zoneType)
        {
            case (ZoneType.NoBuild):
                currentPrefab = noBuildZonePrefab; break;
            case (ZoneType.Residential):
                currentPrefab = residentialZonePrefab; break;
            case (ZoneType.Commercial):
                currentPrefab = commercialZonePrefab; break;
            case (ZoneType.Industrial):
                currentPrefab = industrialZonePrefab; break;
            default:
                Debug.LogError($"Error! Invalid ZoneType {zoneType}");
                return noBuildZonePrefab;
        }

        if (currentPrefab == null) return null;

        float currentScale = getGridScale();
        Vector3 worldPos = new Vector3(pos.x * currentScale, 0.01f, pos.y * currentScale);

        GameObject zone = Instantiate(currentPrefab, worldPos, Quaternion.identity, this.transform);
        zone.name = $"{zoneType} Overlay ({pos.x}, {pos.y})";
        return zone;
    }

    private Building InitialiseBuildingScript(GameObject buildingInstance)
    {
        if (buildingInstance == null) return null;

        Building buildingScript = buildingInstance.GetComponent<Building>();

        //Set residents for house
        if (buildingScript is House houseScript)
        {
            houseScript.residents = UnityEngine.Random.Range(0, houseScript.maxResidents + 1);
        }

        return buildingScript;
    }
}