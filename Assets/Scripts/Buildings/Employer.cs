using UnityEngine;

public abstract class Employer : Building
{
    [Header("Employment")]
    [SerializeField] protected int maxEmployees = 50;
    public int GetMaxEmployees() => maxEmployees;
    public int employees = 0;

    [Header("Revenue")]
    [SerializeField] protected float taxRevenue = 1500f;
    protected float energySupplyHealthiness = 1; //0-1

    protected int badDays = 0;
    protected int lowEmployeeDays = 0;

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
            GameManager.instance.OnDayEnd += CheckForEmployees;
            GameManager.instance.OnDayEnd += GenerateWealth;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated += OnUtilities;
    }

    public void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
            GameManager.instance.OnDayEnd -= CheckForEmployees;
            GameManager.instance.OnDayEnd -= GenerateWealth;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated -= OnUtilities;
    }

    protected void TryToHire()
    {
        if (employees < maxEmployees && GameManager.instance.currentUnemployed > 0)
        {
            employees += 1;
            GameManager.instance.AdjustUnemployed(-1);
            GameManager.instance.AdjustVacanices(-1);
        }
    }

    public bool LoseEmployee()
    {
        if (employees < 1) { return false; }
        employees -= 1;
        GameManager.instance.currentVacanies += 1;
        return true;
    }

    protected void CheckShutdown(bool condition, int limit = 3)
    {
        if (condition)
        {
            badDays++;
            if (badDays >= limit) GameManager.instance.gridManager.forceRemoveElement(gridPos); 
        } else
        {
            badDays = 0;
        }
    }

    public abstract void GenerateWealth();
    protected abstract void CheckForEmployees();
    public abstract void OnUtilities();
}
