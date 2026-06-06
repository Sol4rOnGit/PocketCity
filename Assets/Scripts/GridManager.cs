using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    //Vars
    private const float GridScale = 2.0f;

    [Header("Prefabs")]
    [SerializeField] private GameObject StraightRoad;
    [SerializeField] private GameObject EndRoad;
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
        createGridElement(new Vector2Int(0, 0), 0, ThreeWayIntersection);

        //Left
        createGridElement(new Vector2Int(-1, 0), 0, StraightRoad);
        createGridElement(new Vector2Int(-2, 0), -180, EndRoad);

        //Up
        createGridElement(new Vector2Int(0, 1), -90, StraightRoad);
        createGridElement(new Vector2Int(0, 2), -90, EndRoad);

        //Right
        createGridElement(new Vector2Int(1, 0), 0, EndRoad);
    }

    private void createGridElement(Vector2Int pos, int rotationDegrees, GameObject prefab)
    {

        if (mapGrid.ContainsKey(pos))
        {
            return; //Already exists, should notify the user here later
        }

        //Figure out which prefab to spawn here logic to add here

        Vector3 WorldPos = new Vector3(pos.x * GridScale, 0f, pos.y * GridScale);

        Quaternion WorldRotation = Quaternion.Euler(0f, rotationDegrees, 0f);

        GameObject Tile = Instantiate(prefab, WorldPos, WorldRotation);
        Tile.name = $"{prefab.name} ({pos.x}, {pos.y})";

        GridTile newTile = new GridTile { tileName = prefab.name, instance = Tile, rotationDegrees = rotationDegrees };

        mapGrid.Add(pos, newTile);
    }
}
