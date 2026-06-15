using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    [Header("Dependencies")]
    [HideInInspector] private FinanceManager financeManager;
    public GridManager gridManager;

    [Header("City Statistics")]
    public int currentPopulation;
    public int currentUnemployed;
    public int currentVacanies;
    public int disastersSurvived;

    [Header("Health Statistics")]
    private int infectedPopulation; 
    public bool isLockdownActive = false; //Let user use later

    [Header("Disasters")]
    [SerializeField] private float gradePeriod = 60 * 2f;
    [SerializeField] private float minInterval = 60 * 1.5f;
    [SerializeField] private float maxInterval = 60 * 2.5f;

    private float secondsToBurnBuilding = 10f;

    [Header("Actions")]
    public Action OnDayEnd;
    public Action<string, bool> UserNotification;

    public readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Start()
    {
        financeManager = FinanceManager.instance;
        StartCoroutine(completeCommercialDay());
        StartCoroutine(randomEventTimer());
    }

    public void LosePopulation(int populationLeaving)
    {
        currentPopulation -= populationLeaving;

        if (currentPopulation >= populationLeaving)
        {
            //Remove unemplyed
            currentUnemployed -= populationLeaving;
        }
        else
        {
            int employedLeaving = populationLeaving - currentUnemployed;
            currentUnemployed = 0;

            int leftoverUnfulfilledJobs = RemoveWorkersFromBuildings(employedLeaving);
            if (leftoverUnfulfilledJobs > 0)
            {
                Debug.LogError($"Simulation Error: {leftoverUnfulfilledJobs} workers left the city but can't be found in any buildings!!!");
            } 
        }
    }

    public void LoseJobs(int jobsLost, int employeesLost)
    {
        currentUnemployed += employeesLost;
        int unfilledVacancies = jobsLost - employeesLost;
        currentVacanies -= unfilledVacancies;

        if (currentVacanies < 0) currentVacanies = 0;
        if (currentUnemployed < 0) currentUnemployed = 0;
    }

    public int RemoveWorkersFromBuildings(int amount)
    {
        List<Vector2Int> positions = gridManager.BuildingPositions;
        if (positions.Count == 0) return amount;

        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();
        List<Building> employers = new List<Building>();

        //Add buildings to the list
        foreach (Vector2Int position in positions)
        {
            if (mapGrid.TryGetValue(position, out var tile) && tile.buildingScript != null)
            {
                if (tile.buildingScript is Commercial || tile.buildingScript is Industrial)
                {
                    employers.Add(tile.buildingScript);
                }
            } 
        }
        //if no buildings, return amount
        if (employers.Count == 0) return amount;

        for (int i = 0; i < 20 && amount > 0; i++) //20 iterations (hard-coded)
        {
            Building randomEmployer = employers[UnityEngine.Random.Range(0, employers.Count)];

            //Remove an employee from the buildings (if possible)
            if(randomEmployer is Commercial commercialScript && commercialScript.employees > 0)
            {
                commercialScript.employees--;
                currentVacanies++;
                amount--;
            }

            else if (randomEmployer is Industrial industrialScript && industrialScript.employees > 0)
            {
                industrialScript.employees--;
                currentVacanies++;
                amount--;
            }
        }

        //Linear search if failed
        if (amount > 0)
        {
            foreach (Building employer in employers)
            {
                //Commercial
                if(employer is Commercial commercialScript)
                {
                    while (commercialScript.employees > 0 && amount > 0)
                    {
                        commercialScript.employees--;
                        currentVacanies++;
                        amount--;
                    }
                }

                //Industrial
                else if (employer is Industrial industrialScript)
                {
                    while (industrialScript.employees > 0 && amount > 0)
                    {
                        industrialScript.employees--;
                        currentVacanies++;
                        amount--;
                    }
                }

                if (amount == 0) break; //Enough done
            }
        }

        return amount;
    }

    private IEnumerator completeCommercialDay()
    {
        while (true)
        {
            PreDayEndFunctions();

            if (!isLockdownActive) { OnDayEnd?.Invoke(); }
            else { UserNotification?.Invoke("Lockdown Active! Day passed", false); }

            DayEndFunctions();

            yield return new WaitForSeconds(3f); //every 3 seconds
        }
    }

    private IEnumerator randomEventTimer()
    {
        yield return new WaitForSeconds(gradePeriod);

        while (true)
        {
            int randomInt = UnityEngine.Random.Range(0, 4);
            switch (randomInt)
            {
                case 0: break;
                case 1:
                    Earthquake(); break;
                case 2:
                    SetBuildingOnFire(); break;
                case 3:
                    TriggerVirusOutbreak(); break;
                default: Debug.Log("Game Manager - randomEventTimer() | Invalid random Integer number"); break;
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(minInterval, maxInterval));
        }
    }

    private void PreDayEndFunctions()
    {
        financeManager.prevMoney = financeManager.currentMoney;

        if (ChunkManager.instance != null) { ChunkManager.instance.DistributeUtilitiesAcrossCity(); }
    }

    private void DayEndFunctions()
    {
        financeManager.Purchase(gridManager.RoadPositions.Count * financeManager.roadMaintainanceCost);
        CheckForFires();
        CheckForInfections();
    }

    private void Earthquake()
    {
        Debug.Log("Earthquake!!");

        if(gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        UserNotification?.Invoke("Earthquake!", false);

        //Why is it destroyign all the buildings now!??!
        int numBuildingsToDestroy = UnityEngine.Random.Range(
            (int)Mathf.Max(1, (gridManager.BuildingPositions.Count/10)), 
            (int)(gridManager.BuildingPositions.Count/5));


        for (int i = 0; i < numBuildingsToDestroy + 1; i++)
        {
            if (gridManager.BuildingPositions.Count == 0) break;

            int randomInt = UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count);
            Vector2Int buildingPos = gridManager.BuildingPositions[randomInt];

            gridManager.forceRemoveElement(buildingPos);
        }

        disastersSurvived++;
    }

    //Fire

    private void SetBuildingOnFire()
    {
        if(gridManager.BuildingPositions.Count == 0) { return; }
        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

        if(mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
        {
            if (!tile.buildingScript.isOnFire)
            {
                tile.buildingScript.IgniteFire();

                //Timer
                StartCoroutine(BurnBuilding(randomPos, tile.buildingScript));

                //Call fire services
                if (ServiceManager.instance != null) ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
                else { Debug.LogError("Service Manager not found!"); }
            }
        }
    }

    private void CheckForFires()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

        if(mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
        {
            if (tile.buildingScript.isOnFire)
            {
                StartCoroutine(SpreadFire(randomPos, mapGrid));
            }
        }
    }

    private IEnumerator BurnBuilding(Vector2Int pos, Building buildingScript)
    {
        yield return new WaitForSeconds(secondsToBurnBuilding);

        if(buildingScript != null && buildingScript.isOnFire)
        {
            UserNotification?.Invoke($"{pos.x}, {pos.y} burned down!", true);
            gridManager.forceRemoveElement(pos);
        }
    }

    private IEnumerator SpreadFire(Vector2Int pos, Dictionary<Vector2Int, GridManager.GridTile> mapGrid)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 9f));

        if (!mapGrid.TryGetValue(pos, out GridManager.GridTile sourceTile) || sourceTile.buildingScript == null || !sourceTile.buildingScript.isOnFire)
        {
            yield break;
        }

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkPos = pos + dir;
            if (mapGrid.TryGetValue(checkPos, out GridManager.GridTile tile) && tile.buildingScript != null && !tile.buildingScript.isOnFire)
            {
                if (UnityEngine.Random.Range(0, 2) == 0) continue;

                tile.buildingScript.IgniteFire();
                StartCoroutine(BurnBuilding(checkPos, tile.buildingScript));
            }
        }
    }

    //HEALTH

    private void TriggerVirusOutbreak(bool newVirus = true)
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

        if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
        {
            if (tile.buildingScript is House houseScript && !tile.buildingScript.isInfected)
            {
                tile.buildingScript.Infect();

                infectedPopulation += houseScript.residents;
                if (newVirus) { UserNotification?.Invoke("A virus outbreak has occured virus!", true); }
                else { UserNotification?.Invoke("Another house has been infected!", true); }


                //Timer
                StartCoroutine(KillBuilding(randomPos, houseScript));

                //Call fire services
                if (ServiceManager.instance != null) ServiceManager.instance.DispatchAmbulance(tile.buildingScript);
                else { Debug.LogError("Service Manager not found!"); }
            }
        }
    }

    private IEnumerator KillBuilding(Vector2Int randomPos, House houseScript)
    {
        yield return new WaitForSeconds(10f);

        TriggerVirusOutbreak(false);

        if (houseScript != null && houseScript.isInfected)
        {
            gridManager.forceRemoveElement(randomPos);
            UserNotification?.Invoke("A building has been destroyed as it has been overriden with viruses!", true);
        }
    }

    private void CheckForInfections()
    {
        //////// TO ADD!!
        //Check for infected buildings and then dispatch ambulance if it isn't already being attended to

        return;
    }


    //-- Tornado

    //-- Nuclear fallout

    //CRIME

    //Robberies

    //-- Terrorism

    //Gang wars

    //Police


    //POLITICS

    //Blackout ???

    //Video game choices issues -> e.g. raise taxes but you nuke half the city

    //Political unrest -> people start rioting (become unemployed, set stuff on fire)

    //Strikes & burnout 

    //-- Country declares war on you 


    //HEALTH!!

    //Viruses

    //Lockdowns

    //Hospitals


    //Super rare ones:

    //asteroid attack

    //Alien invasion
}
