using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject firetruckPrefab;
    [SerializeField] private GameObject policecarPrefab;
    [SerializeField] private GameObject ambulancePrefab;

    private GridPathfinder gridPathfinder;
    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;

        gridPathfinder = new GridPathfinder();
    }

    public void Start()
    {
        gridManager = GridManager.instance;
    }

    //Dispatch functions
    public void DispatchFiretruck(Building burningBuilding)
    {
        if (EventManager.instance.isFlooded) return;
        if (burningBuilding == null) return;
        if (gridPathfinder == null) { Debug.LogError("ERROR! NO GRID PATHFINDER!"); return; }

        List<Vector2Int> route = null;
        Building bestStation = FindClosestReachableService<FireStation>(burningBuilding.gridPos, fs => fs.HasTrucks(), out route);

        if (bestStation == null)
        {
            GameManager.instance.UserNotification?.Invoke("Burning burning but there is no fire stations!", false);
            return; 
        }

        if (route == null || route.Count == 0)
        {
            GameManager.instance.UserNotification?.Invoke("Burning burning but no path to a fire stations!", false);
            return;
        }

        if (bestStation is FireStation fireStation)
        {

            if (FinanceManager.instance.Purchase(FinanceManager.instance.serviceChargeFire) == false)
            {
                GameManager.instance.UserNotification?.Invoke("Not enough money to dispatch firetruck for a fire!", false);
                return;
            }

            fireStation.DispatchTruck();
        } else
        {
            Debug.LogError("ServiceMananger: Fire station not a fire station.");
        }

        burningBuilding.isFiretruckOnRoute = true;

        Vector2Int spawnGridPos = route[0];
        float scale = gridManager.getGridScale();
        Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * scale, 0f, spawnGridPos.y * scale);

        GameObject truckObj = Instantiate(firetruckPrefab, spawnWorldPos, Quaternion.identity);

        Firetruck firetruck = truckObj.GetComponent<Firetruck>();
        firetruck.Init(route, burningBuilding, scale, bestStation.gridPos);
    }

    public bool DispatchAmbulance(Building infectedBuilding)
    {
        if (EventManager.instance.isFlooded) return false;
        if (infectedBuilding == null) return false;
        if (gridPathfinder == null) { Debug.LogError("ERROR! NO GRID PATHFINDER!"); return false; }

        List<Vector2Int> route = null;
        Building bestHospital = FindClosestReachableService<Hospital>(infectedBuilding.gridPos, h => h.HasAmbulances(), out route);

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
                return false;
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

    public bool DispatchPolice(Building building)
    {
        if (EventManager.instance.isFlooded) return false;
        if (building == null) return false;
        if (gridPathfinder == null) { Debug.LogError("ERROR! NO GRID PATHFINDER!"); return false; }

        List<Vector2Int> route = null;
        Building bestStation = FindClosestReachableService<PoliceStation>(building.gridPos, ps => ps.HasPolice(), out route);

        if (bestStation == null && route == null)
        {
            GameManager.instance.UserNotification?.Invoke("Crime scene but there are no way for police to access!", false);
            return false;
        }

        if (route == null || route.Count == 0)
        {
            GameManager.instance.UserNotification?.Invoke("Crime scene but no path from police station!", true);
            return false;
        }

        if (bestStation is PoliceStation policeStation)
        {
            if (FinanceManager.instance.Purchase(FinanceManager.instance.serviceChargePoliceTrip) == false)
            {
                GameManager.instance.UserNotification?.Invoke("Not enough money to dispatch police to crime scene!", true);
                return false;
            }

            policeStation.DispatchPolice();
        }
        else
        {
            Debug.LogError("ServiceMananger: PoliceStation not a PoliceStation.");
        }

        Vector2Int spawnGridPos = route[0];
        float scale = gridManager.getGridScale();
        Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * scale, 0f, spawnGridPos.y * scale);

        GameObject policeObj = Instantiate(policecarPrefab, spawnWorldPos, Quaternion.identity);

        Police police = policeObj.GetComponent<Police>();
        police.Init(route, building, scale, bestStation.gridPos);

        return true;
    }

    //Generic functions
    private T FindClosestReachableService<T>(Vector2Int targetPos, Func<T, bool> hasAvailableVehicles, out List<Vector2Int> bestRoute) where T : Building
    {
        T closestBuilding = null;
        bestRoute = null;
        int shortestRouteLength = int.MaxValue;
        bool foundBuildingOfThisType = false;

        foreach (var kvp in gridManager.GetMapGrid())
        {
            GridManager.GridTile tile = kvp.Value;

            if (tile.buildingScript != null && tile.buildingScript is T serviceBuilding)
            {
                foundBuildingOfThisType = true;

                if (!hasAvailableVehicles(serviceBuilding)) continue;

                List<Vector2Int> testRoute = CalculateRoadPath(tile.buildingScript.gridPos, targetPos);

                if (testRoute == null || testRoute.Count == 0) continue;

                if (testRoute.Count < shortestRouteLength)
                {
                    shortestRouteLength = testRoute.Count;
                    bestRoute = testRoute;
                    closestBuilding = serviceBuilding;
                }
            }
        }

        if (!foundBuildingOfThisType)
        {
            bestRoute = null;
            return null;
        }

        return closestBuilding;
    }

    //Helper functions
    public List<Vector2Int> CalculateRoadPath(Vector2Int start, Vector2Int end)
    {
        return gridPathfinder.FindPath(gridManager, start, end);
    }
}
