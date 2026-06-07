using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    //Vars
    private const float GridScale = 2.0f;

    public float getGridScale()
    {
        return GridScale;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject SquareRoad;
    [SerializeField] private GameObject EndRoad;
    [SerializeField] private GameObject StraightRoad;
    [SerializeField] private GameObject Turn;
    [SerializeField] private GameObject ThreeWayIntersection;
    [SerializeField] private GameObject FourWayIntersection;

    //Grid
    public class GridTile
    {
        public string tileName;
        public GameObject instance;
        public int rotationDegrees;
        public BuildingType buildingType;
        public bool isRoad;
    }

    private Dictionary<Vector2Int, GridTile> mapGrid = new Dictionary<Vector2Int, GridTile> ();
    public List<Vector2Int> RoadPositions { get; private set; } = new List<Vector2Int>();
    public List<Vector2Int> BuildingPositions = new List<Vector2Int>();

    public Dictionary<Vector2Int, GridTile> GetMapGrid()
    {
        return mapGrid;
    }

    private void Awake()
    {
        PlaceInitialGrid();
    }

    //Creation/Deletion Public Methods
    public void createRoadOnGrid(Vector2Int pos)
    {

        if (mapGrid.ContainsKey(pos))
        {
            Debug.Log($"The grid point at {pos.x}, {pos.y} already has an element!");
            return; //Already exists, should notify the user here later
        }

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
        if (mapGrid.ContainsKey(pos)) {return; }

        GridTile buildingTile = new GridTile { tileName = buildingName, instance = buildingInstance, rotationDegrees = rotationDegrees, buildingType = buildingType, isRoad = false };

        mapGrid.Add(pos, buildingTile);
        BuildingPositions.Add(pos);
    }

    public void eraseGridElement(Vector2Int pos)
    {
        if (!mapGrid.ContainsKey(pos))
        {
            Debug.Log("Nothing to destroy.");
            return;
        }

        GridTile tileData = mapGrid[pos];

        if (!tileData.isRoad) { return; } //can only remove other stuff

        //Destroy instance
        if (tileData.instance != null) { Destroy(tileData.instance); }

        //Remove from memory
        mapGrid.Remove(pos);
        RoadPositions.Remove(pos);

        //Update the other stuff around it
        updateGridElement(pos + Vector2Int.up);
        updateGridElement(pos + Vector2Int.down);
        updateGridElement(pos + Vector2Int.left);
        updateGridElement(pos + Vector2Int.right);
    }

    private void PlaceInitialGrid()
    {
        //Place a 3 way intersection, to 3 straight roads with 3 end roads -> start

        //Middle
        createRoadOnGrid(new Vector2Int(0, 0));

        //Left
        createRoadOnGrid(new Vector2Int(-1, 0));
        createRoadOnGrid(new Vector2Int(-2, 0));

        //Up
        createRoadOnGrid(new Vector2Int(0, 1));
        createRoadOnGrid(new Vector2Int(0, 2));

        //Right
        createRoadOnGrid(new Vector2Int(1, 0));
        createRoadOnGrid(new Vector2Int(2, 0));
        createRoadOnGrid(new Vector2Int(2, 1));
    }

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
}