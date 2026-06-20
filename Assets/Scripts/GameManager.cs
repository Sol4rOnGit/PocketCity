using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Initial settings")]
    public float startDayDuration = 3f;
    public float finalDayDuration = 1.5f;
    public float daysUntilFinal = 100f;

    private float dayDuration = 0f;

    [Header("City Statistics")]
    public int daysPassed = 0;
    public int currentPopulation = 0;
    public int currentUnemployed = 0;
    public int currentVacanies = 0;
    public int disastersSurvived = 0;

    [Header("Health Statistics")]
    private int infectedPopulation; 
    public bool isLockdownActive = false; //Let user use later

    [Header("Disasters")]
    [SerializeField] private float gradePeriod = 60f;
    [SerializeField] private float minInterval = 60 * 1.5f;
    [SerializeField] private float maxInterval = 60 * 2.5f;

    private float secondsToBurnBuilding = 10f;

    [Header("Actions")]
    public Action OnDayEnd;
    public Action<string, bool> UserNotification;
    public Action<float> OnDayProgress;

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
        if (currentPopulation < 0) { Debug.LogError("Something went wrong and population is now below zero."); currentPopulation = 0; }

        if (currentUnemployed >= populationLeaving)
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

        if (currentVacanies >= unfilledVacancies)
        {
            currentVacanies -= unfilledVacancies;
        } else
        {
            currentVacanies = 0;
        }

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

    public void OnBuildingSpawned(Building buildingScript)
    {
        if (buildingScript == null) return;

        if (buildingScript is House house)
        {
            currentPopulation += house.residents;
            currentUnemployed += house.residents;
        } else if (buildingScript is Commercial commercial)
        {
            int maxJobs = commercial.GetMaxEmployees();
            commercial.employees = Mathf.Min(maxJobs, currentUnemployed);
            currentUnemployed -= commercial.employees;

            currentVacanies += (maxJobs - commercial.employees);
        } else if (buildingScript is Industrial industrial)
        {
            int maxJobs = industrial.GetMaxEmployees();
            industrial.employees = Mathf.Min(maxJobs, currentUnemployed);
            currentUnemployed -= industrial.employees;

            currentVacanies += (maxJobs - industrial.employees);
        }
    }

    //Clocks
    private IEnumerator completeCommercialDay()
    {
        dayDuration = startDayDuration; //Lerp towards minimum later -> to add (make the game harder as the you go along in normal/hard mode)

        while (true)
        {
            float elapsedTime = 0f;

            while (elapsedTime < dayDuration)
            {
                elapsedTime += Time.deltaTime;
                float progressRatio = elapsedTime / dayDuration;
                OnDayProgress?.Invoke(progressRatio);

                yield return null;
            }

            //Day end functions, 3 staged
            PreDayEndFunctions();

            if (!isLockdownActive) { OnDayEnd?.Invoke(); }
            else { UserNotification?.Invoke("Lockdown Active! Day passed", false); }

            DayEndFunctions();

            daysPassed++;

            //Update next day
            float ratio = daysUntilFinal > 0 ? (float)daysPassed / daysUntilFinal : 1.0f;
            ratio = Mathf.Clamp01(ratio);

            dayDuration = Mathf.Lerp(startDayDuration, finalDayDuration, ratio);
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
        financeManager.MaintainancePurchase(gridManager.RoadPositions.Count);
        financeManager.Inflate(currentPopulation, daysPassed);

        if (UnityEngine.Random.Range(0, 7) == 6)
        {
            CheckForFires(); //12.5% chance of spreading everyday
        }
        
        financeManager.DoDailyReport();

        CheckForInfections();
    }

    public void GameOver()
    {
        StopAllCoroutines();

        UserNotification?.Invoke("Game Over!", true);

        Time.timeScale = 0;
    }

    //Natural disasters

    float minRatio = 0.01f;
    float maxRatio = 0.3f;

    private void Earthquake()
    {
        Debug.Log("Earthquake!!");

        if(gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        UserNotification?.Invoke("Earthquake!", false);

        //Update ratios
        float ratio = Mathf.Lerp(minRatio, maxRatio, daysPassed / daysUntilFinal);

        int numBuildingsToDestroy = (int)(ratio * gridManager.BuildingPositions.Count);

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

        var mapGrid = gridManager.GetMapGrid();

        foreach (Vector2Int buildingPos in gridManager.BuildingPositions) //Optimistaion -> hashmap for infected buildings and houses auto append? O(1)?
        {
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript is House houseScript)
            {
                if (!houseScript.isOnFire) continue;

                StartCoroutine(SpreadFire(buildingPos, mapGrid));
            }
        }

        return;
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

        List<GridManager.GridTile> validTargets = new List<GridManager.GridTile>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkPos = pos + dir;
            if (mapGrid.TryGetValue(checkPos, out GridManager.GridTile tile) && tile.buildingScript != null && !tile.buildingScript.isOnFire)
            {
                validTargets.Add(tile);
            }
        }

        if (validTargets.Count > 0 && UnityEngine.Random.Range(0, 2) != 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
            GridManager.GridTile tile = validTargets[randomIndex];

            tile.buildingScript.IgniteFire();

            StartCoroutine(BurnBuilding(tile.buildingScript.gridPos, tile.buildingScript));
            yield break; //only one building -> remove this if decide not to.
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
            if (tile.buildingScript is House houseScript && !houseScript.isInfected)
            {
                houseScript.Infect();

                infectedPopulation += houseScript.residents;
                if (newVirus) { UserNotification?.Invoke("A virus outbreak has occured!", true); }
                else { UserNotification?.Invoke("Another house has been infected!", true); }


                //Timer
                StartCoroutine(KillBuilding(randomPos, houseScript));

                //Call ambulances
                if (ServiceManager.instance != null)
                {
                    bool served = ServiceManager.instance.DispatchAmbulance(tile.buildingScript);
                    if (served) houseScript.isAmbulanceOnRoute = true;
                }
                else { Debug.LogError("Service Manager not found!"); }
            }
        }
    }

    private IEnumerator KillBuilding(Vector2Int randomPos, House houseScript)
    {
        yield return new WaitForSeconds(10f);

        if (houseScript == null || !houseScript.isInfected) yield break;

        for (int i = 0; i < houseScript.residents; i++)
        {
            TriggerVirusOutbreak(false);
        }

        gridManager.forceRemoveElement(randomPos);
        UserNotification?.Invoke("A building has been destroyed as it has been overriden with viruses!", true);
    }

    private void CheckForInfections()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        var serviceManager = ServiceManager.instance;
        if (serviceManager == null)
        {
            Debug.LogError("Service manager not found!");
            return;
        }

        var mapGrid = gridManager.GetMapGrid();

        foreach (Vector2Int buildingPos in gridManager.BuildingPositions) //Optimistaion -> hashmap for infected buildings and houses auto append? O(1)?
        {
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript is House houseScript){

                if (!houseScript.isInfected) continue;

                if (houseScript.isAmbulanceOnRoute) continue;
                houseScript.isAmbulanceOnRoute = true;

                serviceManager.DispatchAmbulance(houseScript);
            }
        }

        return;
    }

    //RUBBISH

    //Create a: destroyed house, commercial and industrail assets.
    //Create rubbish manager
    //Similar to fire/ambulance after but with a rubbish truck and landfill -> will have to buy!

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
