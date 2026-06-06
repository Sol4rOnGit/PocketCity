using System.Collections.Generic;
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


        if (gridManager.RoadPositions.Count == 0) return; //No infrastructure then return.

        Vector2Int targetSpawnPos = Vector2Int.zero;
        bool foundValidSpot = false;

        int rand = Random.Range(1, 101);
        if (rand <= 40 && gridManager.BuildingPositions.Count > 0)
        {
            //Opt 2 which was to cluster expand
            
            Vector2Int randomBuilding = gridManager.BuildingPositions[Random.Range(0, gridManager.BuildingPositions.Count)];
            targetSpawnPos = GetRandomEmptyNeighbour(randomBuilding, currentMapGrid);

            if (targetSpawnPos != Vector2Int.left * 99999) { foundValidSpot = true; currentBuildingType = currentMapGrid[randomBuilding].buildingType; }
        } 
        
        if (!foundValidSpot) //If option 2 failed, skipped or have no buildings then roadside
        {
            targetSpawnPos = TrySpawnAlongRoad(gridManager.RoadPositions, currentMapGrid);
            if (targetSpawnPos != Vector2Int.left * 99999) { foundValidSpot = true; }
        }

        if (foundValidSpot) { ExecuteBuildingPlacement(targetSpawnPos, currentBuildingType); }

    }

    private Vector2Int TrySpawnAlongRoad(List<Vector2Int> roadPositions, Dictionary<Vector2Int, GridManager.GridTile> currentGrid)
    {
        int maxAttempts = Mathf.Min(10, roadPositions.Count);
        int currentAttempts = 0;

        while (currentAttempts < maxAttempts)
        {
            Vector2Int randomRoad = roadPositions[Random.Range(0, roadPositions.Count)];
            Vector2Int spot = GetRandomEmptyNeighbour(randomRoad, currentGrid);

            if (spot != Vector2Int.left * 99999) { return spot; }
        }

        return Vector2Int.left * 99999;

    }

    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private Vector2Int GetRandomEmptyNeighbour(Vector2Int center, Dictionary<Vector2Int, GridManager.GridTile> currentGrid)
    {
        int dirlen = directions.Length;
        int startIndex = Random.Range(0, dirlen);
        
        for (int i = 0; i < 4; i++)
        {
            int currentIndex = (startIndex + i) % dirlen;
            Vector2Int checkpos = center + directions[currentIndex];

            if (!currentGrid.ContainsKey(checkpos))
            {
                return checkpos;
            }
        }

        return Vector2Int.left * 99999; //error if surrounded from all sides, this will not trigger found valid spot
    }
    
    private void ExecuteBuildingPlacement(Vector2Int spawnPos, BuildingType? buildingType)
    {
        if (!buildingType.HasValue)
        {
            int randomCategory = Random.Range(0, 3);
            buildingType = (BuildingType)randomCategory;
        }

        List<GameObject> targetList = buildingType switch { BuildingType.Residential => HousePrefabs, BuildingType.Industrial => IndustryPrefabs, BuildingType.Commercial => CommercialPrefabs, _ => HousePrefabs };

        if (targetList == null || targetList.Count == 0) return;

        GameObject prefab = targetList[Random.Range(0, targetList.Count)];

        float scale = gridManager.getGridScale();
        Vector3 worldPos = new Vector3(spawnPos.x * scale, 0f, spawnPos.y * scale);

        GameObject BuildingInstance = Instantiate(prefab, worldPos, Quaternion.identity);
        BuildingInstance.name = $"{prefab.name} ({spawnPos.x}, {spawnPos.y}";

        gridManager.createBuildingOnGrid(spawnPos, BuildingInstance, (BuildingType)buildingType, BuildingInstance.name);
    }
}
