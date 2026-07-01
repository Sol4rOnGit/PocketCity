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
    public EventManager eventManager;

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

    private int experience = 0;
    private int experienceLevel = 1;

    [Header("Actions")]
    public Action OnDayEnd;
    public Action<string, bool> UserNotification;
    public Action<float> OnDayProgress;
    public Action<int, int> OnXPChanged; //XPPoints, maxXPpoints,
    public Action<int> OnNewXPLevel; //XPLevel

    public readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Start()
    {
        financeManager = FinanceManager.instance;
        eventManager = EventManager.instance;
        StartCoroutine(completeCommercialDay());
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

            PreDayEndFunctions();

            //Day functions
            if (!eventManager.isLockdownActive) { OnDayEnd?.Invoke(); }
            else { UserNotification?.Invoke("Lockdown Active! Day passed", false); }

            GainExperience(10);

            DayEndFunctions();

            daysPassed++;

            //Update next day
            float ratio = daysUntilFinal > 0 ? (float)daysPassed / daysUntilFinal : 1.0f;
            ratio = Mathf.Clamp01(ratio);

            dayDuration = Mathf.Lerp(startDayDuration, finalDayDuration, ratio);
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

        eventManager.CheckForFires();
        
        financeManager.DoDailyReport();

        eventManager.CheckForInfections();
    }

    public void GameOver()
    {
        StopAllCoroutines();
        
        Time.timeScale = 0;

        UserNotification?.Invoke("Game Over!", true);
    }

    //Employment game mechanics
    public void AdjustUnemployed(int amount)
    {
        if (currentUnemployed + amount < 0) { Debug.LogError("Invalid current employment adjustment."); return; }

        currentUnemployed = Mathf.Max(0, currentUnemployed + amount);
    }

    public void AdjustVacanices(int amount)
    {
        if (currentVacanies + amount < 0) { Debug.LogError("Invalid current vacancies adjustment."); return; }

        currentVacanies = Mathf.Max(0, currentVacanies + amount);
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
        AdjustUnemployed(employeesLost);

        int unfilledVacancies = jobsLost - employeesLost;

        if (currentVacanies >= unfilledVacancies)
        {
            AdjustVacanices(-unfilledVacancies);
        }
        else
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
            if (randomEmployer is Commercial commercialScript && commercialScript.employees > 0)
            {
                commercialScript.employees--;
                AdjustVacanices(1);
                amount--;
            }

            else if (randomEmployer is Industrial industrialScript && industrialScript.employees > 0)
            {
                industrialScript.employees--;
                AdjustVacanices(1);
                amount--;
            }
        }

        //Linear search if failed
        if (amount > 0)
        {
            foreach (Building employer in employers)
            {
                //Commercial
                if (employer is Commercial commercialScript)
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
        GainExperience(1);

        if (buildingScript == null) return;

        if (buildingScript is House house)
        {
            currentPopulation += house.residents;
            currentUnemployed += house.residents;
        }
        else if (buildingScript is Commercial commercial)
        {
            int maxJobs = commercial.GetMaxEmployees();
            commercial.employees = Mathf.Min(maxJobs, currentUnemployed);
            currentUnemployed -= commercial.employees;

            currentVacanies += (maxJobs - commercial.employees);
        }
        else if (buildingScript is Industrial industrial)
        {
            int maxJobs = industrial.GetMaxEmployees();
            industrial.employees = Mathf.Min(maxJobs, currentUnemployed);
            currentUnemployed -= industrial.employees;

            currentVacanies += (maxJobs - industrial.employees);
        }
    }

    //Experience mechanics

    public void GainExperience(int amount)
    {
        experience += amount;

        UpdateLevel();

        OnXPChanged?.Invoke(GetXPInCurrentLevel(), GetXPRequiredForNextLevel());
    }

    private bool UpdateLevel()
    {
        int oldLevel = experienceLevel;

        while (experience >= GetXPRequiredForLevel(experienceLevel + 1))
        {
            experienceLevel++;
            OnNewXPLevel?.Invoke(experienceLevel);
        }

        return oldLevel != experienceLevel; //True if updated
    }

    public int GetXPRequiredForLevel(int level)
    {
        if (level <= 1) return 100;

        return (level * 90) + 10;
    }

    private int GetXPInCurrentLevel()
    {
        return experience - GetXPRequiredForLevel(experienceLevel);
    }

    private int GetXPRequiredForNextLevel()
    {
        if (experienceLevel < 0) return 0;

        return GetXPRequiredForLevel(experienceLevel + 1) - GetXPRequiredForLevel(experienceLevel);
    }
}
