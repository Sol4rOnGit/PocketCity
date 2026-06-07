using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("Houses")]
    [SerializeField] private List<GameObject> HousePrefabs;

    [Header("Industrial")]
    [SerializeField] private List<GameObject> IndustryPrefabs;

    [Header("Commercial")]
    [SerializeField] private List<GameObject> CommercialPrefabs;

    [Header("Grid & Settings")]
    [SerializeField] private GridManager gridManager;

    [Header("Spawning Settings")]
    [SerializeField] private float startWaitTimeSeconds = 30.0f;
    [SerializeField] private float finalWaitTimeSeconds = 5.0f;
    [SerializeField] private float realTimeUntilFinalTimeSeconds = 900.0f; //15 minutes

    private float spawnTimer = 0.0f;
    private float timeSinceStartSeconds = 0.0f;

    private void Update()
    {
        timeSinceStartSeconds += Time.deltaTime;

        float growthIntensity = Mathf.Clamp01(timeSinceStartSeconds / realTimeUntilFinalTimeSeconds);

        float currentSpawnInterval = Mathf.Lerp(startWaitTimeSeconds, finalWaitTimeSeconds, growthIntensity);

        spawnTimer -= Time.deltaTime;

        if (spawnTimer < 0.0f)
        {
            GenerateBuilding();
            spawnTimer = currentSpawnInterval;
        }
    }

    private void GenerateBuilding()
    {
        //Grid manager dictionary
        Dictionary<Vector2Int, GridManager.GridTile> currentMapGrid = gridManager.GetMapGrid();
        BuildingType? currentBuildingType = null;
        Vector2Int? directionToRoad = null;


        if (gridManager.RoadPositions.Count == 0) return; //No infrastructure then return.

        Vector2Int targetSpawnPos = Vector2Int.zero;
        bool foundValidSpot = false;

        int rand = Random.Range(1, 101);
        if (rand <= 40 && gridManager.BuildingPositions.Count > 0)
        {
            //Opt 2 which was to cluster expand
            
            Vector2Int randomBuilding = gridManager.BuildingPositions[Random.Range(0, gridManager.BuildingPositions.Count)];
            (targetSpawnPos, _) = GetRandomEmptyNeighbour(randomBuilding, currentMapGrid);

            

            if (targetSpawnPos != Vector2Int.left * 99999) { 
                foundValidSpot = true;
                currentBuildingType = currentMapGrid[randomBuilding].buildingType; 
            }
        } 
        
        if (!foundValidSpot) //If option 2 failed, skipped or have no buildings then roadside
        {
            (targetSpawnPos, directionToRoad) = TrySpawnAlongRoad(gridManager.RoadPositions, currentMapGrid);
            directionToRoad = -directionToRoad; //invert
            if (targetSpawnPos != Vector2Int.left * 99999) { foundValidSpot = true; }
        }

        if (foundValidSpot) { ExecuteBuildingPlacement(targetSpawnPos, currentBuildingType, directionToRoad, currentMapGrid); }

    }

    private (Vector2Int spot, Vector2Int dir) TrySpawnAlongRoad(List<Vector2Int> roadPositions, Dictionary<Vector2Int, GridManager.GridTile> currentGrid)
    {
        int maxAttempts = Mathf.Min(10, roadPositions.Count);
        int currentAttempts = 0;

        while (currentAttempts < maxAttempts)
        {
            Vector2Int randomRoad = roadPositions[Random.Range(0, roadPositions.Count)];
            (Vector2Int spot, Vector2Int dirToRoad) = GetRandomEmptyNeighbour(randomRoad, currentGrid);

            if (spot != Vector2Int.left * 99999) { return (spot, dirToRoad); }
        }

        return (Vector2Int.left * 99999, Vector2Int.left * 99999);

    }

    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private (Vector2Int spot, Vector2Int direction) GetRandomEmptyNeighbour(Vector2Int center, Dictionary<Vector2Int, GridManager.GridTile> currentGrid)
    {
        int dirlen = directions.Length;
        int startIndex = Random.Range(0, dirlen);
        
        for (int i = 0; i < 4; i++)
        {
            int currentIndex = (startIndex + i) % dirlen;
            Vector2Int checkpos = center + directions[currentIndex];

            if (!currentGrid.ContainsKey(checkpos))
            {
                return (checkpos, directions[currentIndex]);
            }
        }

        return (Vector2Int.left * 99999, Vector2Int.left * 99999); //error if surrounded from all sides, this will not trigger found valid spot
    }

    private void ExecuteBuildingPlacement(Vector2Int spawnPos, BuildingType? buildingType, Vector2Int? directionToRoad, Dictionary<Vector2Int, GridManager.GridTile> currentMapGrid)
    {
        if (!buildingType.HasValue)
        {
            int randomCategory = Random.Range(0, 3);
            buildingType = (BuildingType)randomCategory;
        }

        List<GameObject> targetList = buildingType switch { BuildingType.Residential => HousePrefabs, BuildingType.Industrial => IndustryPrefabs, BuildingType.Commercial => CommercialPrefabs, _ => HousePrefabs };

        if (targetList == null || targetList.Count == 0) return;

        GameObject prefab = targetList[Random.Range(0, targetList.Count)];

        Vector2Int finalDir = Vector2Int.down; //default

        if (!directionToRoad.HasValue)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int checkPos = spawnPos + dir;
                if (currentMapGrid.TryGetValue(checkPos, out GridManager.GridTile neighbour) && neighbour.isRoad)
                {
                    directionToRoad = dir;
                    break;
                }
            }
        }

        //Direction to angle
        int rotationDegrees = 0; //if it's up or 
        if (directionToRoad.HasValue) {
            //if (directionToRoad == Vector2Int.down) rotationDegrees = 0;
            if (directionToRoad == Vector2Int.up) rotationDegrees = 180;
            if (directionToRoad == Vector2Int.left) rotationDegrees = 90;
            if (directionToRoad == Vector2Int.right) rotationDegrees = -90;
        }
        

        float scale = gridManager.getGridScale();
        Vector3 worldPos = new Vector3(spawnPos.x * scale, 0f, spawnPos.y * scale);
        Quaternion worldRot = Quaternion.Euler(0f, rotationDegrees, 0f);

        GameObject BuildingInstance = Instantiate(prefab, worldPos, worldRot);
        BuildingInstance.name = $"{prefab.name} ({spawnPos.x}, {spawnPos.y}";

        gridManager.createBuildingOnGrid(spawnPos, BuildingInstance, rotationDegrees, (BuildingType)buildingType, BuildingInstance.name); //NEED ROTATION HERE!!!
    }
}
