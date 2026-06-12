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

    [Header("City Statistics")]
    public int currentPopulation;
    public int currentUnemployed;
    public int currentVacanies;
    public int disastersSurvived;

    [Header("Disasters")]

    [Header("Actions")]
    public Action OnDayEnd;

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
            OnDayEnd?.Invoke();
            DayEndFunctions();
            yield return new WaitForSeconds(3f); //every 3 seconds
        }
    }

    private IEnumerator randomEventTimer()
    {
        while (true)
        {
            int randomInt = 1;//UnityEngine.Random.Range(1, 1);
            switch (randomInt)
            {
                case 0: break;
                case 1: Earthquake(); break;
                default: Debug.Log("Game Manager - randomEventTimer() | Invalid random Integer number"); break;
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range(10f, 30f));
            //yield return new WaitForSeconds(UnityEngine.Random.Range((60 * 5f), (60 * 10f))); //is this reaseonable? 
        }
    }

    private void PreDayEndFunctions()
    {
        financeManager.prevMoney = financeManager.currentMoney;
    }

    private void DayEndFunctions()
    {
        financeManager.Purchase(gridManager.RoadPositions.Count * financeManager.roadMaintainanceCost);
    }

    private void Earthquake()
    {
        Debug.Log("Earthquake!!");

        if(gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        int numBuildingsToDestroy = UnityEngine.Random.Range(
            (int)Mathf.Max(1, (gridManager.GetMapGrid().Count/20)), 
            (int)(gridManager.GetMapGrid().Count/5));


        for (int i = 0; i < numBuildingsToDestroy + 1; i++)
        {
            if (gridManager.BuildingPositions.Count == 0) break;

            int randomInt = UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count);
            Vector2Int buildingPos = gridManager.BuildingPositions[randomInt];

            gridManager.forceRemoveElement(buildingPos);
        }

        disastersSurvived++;
    }

    //Earthquake -> destroy random buildings & infrastructure. Set stuff on fire

    //Tornado

    //Nuclear fallout

    //Terrorism

    //Gang wars

    //Police

    //Fire services

    //Random fires

    //Blackout ???

    //Political issues -> e.g. raise taxes but you nuke half the city

    //Strikes & burnout

    //Water & Energy

    //Country declares war on you

    //Viruses

    //Healthcare

    //Super rare
    
    //asteroid attack

    //Alien invasion
}
