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
        //Wait for everything to load
        if (GameManager.instance == null || FinanceManager.instance == null) return;

        //Ticking functionality
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
        Vector2Int directionToRoad = Vector2Int.zero;

        if (gridManager.RoadPositions.Count == 0) return; //No infrastructure then return.

        Vector2Int targetSpawnPos = Vector2Int.zero;
        bool foundValidSpot = false;
        
        if (!foundValidSpot) //If option 2 failed, skipped or have no buildings then roadside
        {
            (targetSpawnPos, directionToRoad, currentBuildingType) = TrySpawnAlongRoad(gridManager.RoadPositions, currentMapGrid);

            if (targetSpawnPos != Vector2Int.left * 99999)
            {
                directionToRoad = -directionToRoad; //Invert
                foundValidSpot = true;
            }
        }

        if (foundValidSpot) { ExecuteBuildingPlacement(targetSpawnPos, currentBuildingType, directionToRoad, currentMapGrid); }
    }

    private (Vector2Int spot, Vector2Int dir, BuildingType? buildingType) TrySpawnAlongRoad(List<Vector2Int> roadPositions, Dictionary<Vector2Int, GridManager.GridTile> currentGrid)
    {
        int maxAttempts = Mathf.Min(10, roadPositions.Count);
        int currentAttempts = 0;

        while (currentAttempts < maxAttempts)
        {
            Vector2Int randomRoad = roadPositions[Random.Range(0, roadPositions.Count)];
            (Vector2Int spot, Vector2Int dirToRoad) = GetRandomEmptyNeighbour(randomRoad, currentGrid, null);

            if (spot != Vector2Int.left * 99999)
            {
                if (currentGrid.TryGetValue(spot, out var tile))
                {
                    BuildingType? zoneBuildingType = ConvertZoneToBuildingType(tile.zoneType);
                    return (spot,  dirToRoad, zoneBuildingType);
                }
                else
                {
                    return (spot, dirToRoad, null);
                }
                    
            }

            currentAttempts++;
        }

        return (Vector2Int.left * 99999, Vector2Int.left * 99999, null);

    }

    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private (Vector2Int spot, Vector2Int direction) GetRandomEmptyNeighbour(Vector2Int center, Dictionary<Vector2Int, GridManager.GridTile> currentGrid, BuildingType? targetBuildingType)
    {
        int dirlen = directions.Length;
        int startIndex = Random.Range(0, dirlen);
        
        for (int i = 0; i < 4; i++)
        {
            int currentIndex = (startIndex + i) % dirlen;
            Vector2Int checkpos = center + directions[currentIndex];

            if (currentGrid.TryGetValue(checkpos, out var tile))
            {
                if (!tile.isRoad && tile.buildingType == null && tile.zoneType != ZoneType.None && tile.zoneType != ZoneType.NoBuild)
                {
                    if (targetBuildingType.HasValue)
                    {
                        BuildingType? tileType = ConvertZoneToBuildingType(tile.zoneType);
                        if (tileType == targetBuildingType.Value)
                        {
                            return (checkpos, directions[currentIndex]);
                        }
                    }
                    else
                    {
                        return (checkpos, directions[currentIndex]); //Not interested in matching building (roadside)
                    }
                    
                }
                
            } else
            {
                return (checkpos, directions[currentIndex]);
            }
        }

        return (Vector2Int.left * 99999, Vector2Int.left * 99999); //error if surrounded from all sides, this will not trigger found valid spot
    }

    private void ExecuteBuildingPlacement(Vector2Int spawnPos, BuildingType? buildingType, Vector2Int directionToRoad, Dictionary<Vector2Int, GridManager.GridTile> currentMapGrid)
    {
        if (currentMapGrid.TryGetValue(spawnPos, out var tile))
        {
            if (tile.buildingType != null || tile.isRoad) return;

            BuildingType? zoneConverted = ConvertZoneToBuildingType(tile.zoneType);
            if (zoneConverted.HasValue) { buildingType = zoneConverted.Value; }
        }

        //Force a building Type if it isn't already declared
        if (!buildingType.HasValue)
        {
            int randomCategory = Random.Range(0, 3);
            buildingType = (BuildingType)randomCategory;
        }

        //Grab Prefab
        List<GameObject> targetList = buildingType switch { BuildingType.Residential => HousePrefabs, BuildingType.Industrial => IndustryPrefabs, BuildingType.Commercial => CommercialPrefabs, _ => HousePrefabs };

        if (targetList == null || targetList.Count == 0) return;

        GameObject prefab = targetList[Random.Range(0, targetList.Count)];

        int rotationDegrees = GetRotationDegreesFromDirection(directionToRoad);

        //Spawning
        float scale = gridManager.getGridScale();
        Vector3 worldPos = new Vector3(spawnPos.x * scale, 0f, spawnPos.y * scale);
        Quaternion worldRot = Quaternion.Euler(0f, rotationDegrees, 0f);

        GameObject BuildingInstance = Instantiate(prefab, worldPos, worldRot, gridManager.transform);
        BuildingInstance.name = $"{prefab.name} ({spawnPos.x}, {spawnPos.y})";

        gridManager.createBuildingOnGrid(spawnPos, BuildingInstance, rotationDegrees, (BuildingType)buildingType, BuildingInstance.name);
    }

    //Helper functions

    private BuildingType? ConvertZoneToBuildingType(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Residential => BuildingType.Residential,
            ZoneType.Commercial => BuildingType.Commercial,
            ZoneType.Industrial => BuildingType.Industrial,
            _ => null
        };
    }

    //Direction to angle conversion for the building placement
    private int GetRotationDegreesFromDirection(Vector2Int direction)
    {
        int rotationDegrees = 0;

        //if (directionToRoad == Vector2Int.down) rotationDegrees = 0; 
        if (direction == Vector2Int.up) rotationDegrees = 180;
        if (direction == Vector2Int.left) rotationDegrees = 90;
        if (direction == Vector2Int.right) rotationDegrees = -90;

        return rotationDegrees;
    }
}
