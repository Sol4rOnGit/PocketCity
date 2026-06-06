using System;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    //Vars
    private const float GridScale = 2.0f;

    [Header("Prefabs")]
    [SerializeField] private GameObject SquareRoad;
    [SerializeField] private GameObject EndRoad;
    [SerializeField] private GameObject StraightRoad;
    [SerializeField] private GameObject Turn;
    [SerializeField] private GameObject ThreeWayIntersection;
    [SerializeField] private GameObject FourWayIntersection;

    //Grid?
    public class GridTile
    {
        public string tileName;
        public GameObject instance;
        public int rotationDegrees;
    }

    private Dictionary<Vector2Int, GridTile> mapGrid = new Dictionary<Vector2Int, GridTile> ();

    private void Start()
    {
        TestGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void TestGrid()
    {
        //Place a 3 way intersection, to 3 straight roads with 3 end roads
        createGridElement(new Vector2Int(0, 0));

        //Left
        createGridElement(new Vector2Int(-1, 0));
        createGridElement(new Vector2Int(-2, 0));

        //Up
        createGridElement(new Vector2Int(0, 1));
        createGridElement(new Vector2Int(0, 2));

        //Right
        createGridElement(new Vector2Int(1, 0));
        createGridElement(new Vector2Int(2, 0));
        createGridElement(new Vector2Int(2, 1));

        //Test
        createGridElement(new Vector2Int(0, 0)); //Shouldn't happen

        createGridElement(new Vector2Int(5, 3));
        createGridElement(new Vector2Int(5, 4));
        createGridElement(new Vector2Int(5, 5));
        createGridElement(new Vector2Int(4, 3));
        createGridElement(new Vector2Int(4, 2));
        createGridElement(new Vector2Int(6, 3));
        createGridElement(new Vector2Int(6, 4));
        createGridElement(new Vector2Int(6, 5));
        createGridElement(new Vector2Int(6, 6));
        createGridElement(new Vector2Int(6, 7));
        createGridElement(new Vector2Int(5, 2));
        createGridElement(new Vector2Int(5, 1)); 
        createGridElement(new Vector2Int(7, 8));
        createGridElement(new Vector2Int(8, 9));
        createGridElement(new Vector2Int(7, 9));
        createGridElement(new Vector2Int(9, 10));
    }

    private (int rotationDegrees, GameObject prefab) DecideOnPrefab(Vector2Int pos)
    {
        //Check above, left, right, bottom.
        bool hasUp = mapGrid.ContainsKey(pos + Vector2Int.up);
        bool hasDown = mapGrid.ContainsKey(pos + Vector2Int.down);
        bool hasLeft = mapGrid.ContainsKey(pos + Vector2Int.left);
        bool hasRight = mapGrid.ContainsKey(pos + Vector2Int.right);

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

    private void createGridElement(Vector2Int pos)
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

        GridTile newTile = new GridTile { tileName = prefab.name, instance = Tile, rotationDegrees = rotationDegrees };

        mapGrid.Add(pos, newTile);

        //Update the four grids around it
        updateGridElement(pos + Vector2Int.up);
        updateGridElement(pos + Vector2Int.down);
        updateGridElement(pos + Vector2Int.left);
        updateGridElement(pos + Vector2Int.right);
    }

    private void updateGridElement(Vector2Int pos)
    {
        if (!mapGrid.ContainsKey(pos))
        {
            return;
            //Doesn't exist so early return
        }

        GridTile tileData = mapGrid[pos];

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
        tileData.instance = Tile;
        tileData.rotationDegrees = newRotationDegrees;
    }
}