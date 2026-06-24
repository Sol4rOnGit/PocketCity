using System.Collections.Generic;
using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager instance { get; private set; }

    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;

        gridPathfinder = new GridPathfinder();
    }

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject firetruckPrefab;
    [SerializeField] private GameObject policecarPrefab;
    [SerializeField] private GameObject ambulancePrefab;

    private GridPathfinder gridPathfinder;

    public void Start()
    {
        gridManager = GridManager.instance;
    }
    public void DispatchFiretruck(Building burningBuilding)
    {
        if (burningBuilding == null) return;
        if (gridPathfinder == null) { Debug.LogError("ERROR! NO GRID PATHFINDER!"); return; }

        List<Vector2Int> route = null;
        Building bestStation = FindClosestReachableFireStation(burningBuilding.gridPos, out route);

        if (bestStation == null && route == null)
        {
            GameManager.instance.UserNotification?.Invoke("Burning burning but there is no fire stations!", false);
            return; 
        }

        if (route == null || route.Count == 0)
        {
            GameManager.instance.UserNotification?.Invoke("Burning burning but no path to a fire stations!", true);
            return;
        }

        if (bestStation is FireStation fireStation)
        {

            if (FinanceManager.instance.Purchase(FinanceManager.instance.serviceChargeFire) == false)
            {
                GameManager.instance.UserNotification?.Invoke("Not enough money to dispatch firetruck for a fire!", true);
                return;
            }

            fireStation.DispatchTruck();
        } else
        {
            Debug.LogError("ServiceMananger: Fire station not a fire station.");
        }


        Vector2Int spawnGridPos = route[0];
        float scale = gridManager.getGridScale();
        Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * scale, 0f, spawnGridPos.y * scale);

        GameObject truckObj = Instantiate(firetruckPrefab, spawnWorldPos, Quaternion.identity);

        Firetruck firetruck = truckObj.GetComponent<Firetruck>();
        firetruck.Init(route, burningBuilding, scale, bestStation.gridPos);
    }

    //Ambulance

    public bool DispatchAmbulance(Building infectedBuilding)
    {
        if (infectedBuilding == null) return false;
        if (gridPathfinder == null) { Debug.LogError("ERROR! NO GRID PATHFINDER!"); return false; }

        List<Vector2Int> route = null;
        Building bestHospital = FindClosestReachableHospital(infectedBuilding.gridPos, out route);

        if (bestHospital == null && route == null)
        {
            GameManager.instance.UserNotification?.Invoke("Infection but there are no Hospitals!", false);
            return false;
        }

        if (route == null || route.Count == 0)
        {
            GameManager.instance.UserNotification?.Invoke("Infection but no path to there from a hospital!", true);
            return false;
        }

        if (bestHospital is Hospital hospital)
        {
            if (FinanceManager.instance.Purchase(FinanceManager.instance.serviceChargeHospital) == false)
            {
                GameManager.instance.UserNotification?.Invoke("Not enough money to dispatch ambulance to infection!", true);
            }

            hospital.DispatchAmbulance();
        }
        else
        {
            Debug.LogError("ServiceMananger: Hospital not a hospital.");
        }

        Vector2Int spawnGridPos = route[0];
        float scale = gridManager.getGridScale();
        Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * scale, 0f, spawnGridPos.y * scale);

        GameObject ambulanceObj = Instantiate(ambulancePrefab, spawnWorldPos, Quaternion.identity);

        Ambulance ambulance = ambulanceObj.GetComponent<Ambulance>();
        ambulance.Init(route, infectedBuilding, scale, bestHospital.gridPos);

        

        return true;
    }

    public void DispatchPolice()
    {

    }

    //Helper functions
    private Building FindClosestReachableFireStation(Vector2Int targetPos, out List<Vector2Int> bestRoute)
    {
        Building closestStation = null;
        bestRoute = null;
        int shortestRouteLength = int.MaxValue;
        bool foundAStation = false;

        foreach (var kvp in gridManager.GetMapGrid())
        {
            GridManager.GridTile tile = kvp.Value;

            if (tile.buildingScript != null && tile.buildingScript is FireStation fireStation)
            {
                foundAStation = true;

                if (!fireStation.HasTrucks()) continue;

                List<Vector2Int> testRoute = CalculateRoadPath(tile.buildingScript.gridPos, targetPos);

                if (testRoute == null || testRoute.Count == 0) continue;

                if (testRoute.Count < shortestRouteLength)
                {
                    shortestRouteLength = testRoute.Count;
                    bestRoute = testRoute;
                    closestStation = tile.buildingScript;
                }
            }
        }

        if (!foundAStation)
        {
            bestRoute = null;
            return null;
        }

        return closestStation;
    }

    private Building FindClosestReachableHospital(Vector2Int targetPos, out List<Vector2Int> bestRoute)
    {
        Building closestStation = null;
        bestRoute = null;
        int shortestRouteLength = int.MaxValue;
        bool foundAStation = false;

        foreach (var kvp in gridManager.GetMapGrid())
        {
            GridManager.GridTile tile = kvp.Value;

            if (tile.buildingScript != null && tile.buildingScript is Hospital hospital)
            {
                foundAStation = true;

                if (!hospital.HasAmbulances()) continue;

                List<Vector2Int> testRoute = CalculateRoadPath(tile.buildingScript.gridPos, targetPos);

                if (testRoute == null || testRoute.Count == 0) continue;

                if (testRoute.Count < shortestRouteLength)
                {
                    shortestRouteLength = testRoute.Count;
                    bestRoute = testRoute;
                    closestStation = tile.buildingScript;
                }
            }
        }

        if (!foundAStation)
        {
            bestRoute = null;
            return null;
        }

        return closestStation;
    }

    public List<Vector2Int> CalculateRoadPath(Vector2Int start, Vector2Int end)
    {
        return gridPathfinder.FindPath(gridManager, start, end);
    }
}
