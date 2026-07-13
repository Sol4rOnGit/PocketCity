using UnityEngine;

public abstract class Employer : Building
{
    [Header("Employment")]
    [SerializeField] protected int maxEmployees = 50;
    public int GetMaxEmployees() => maxEmployees;
    public int employees = 0;

    [Header("Revenue")]
    protected float baseRevenue;
    [SerializeField] protected float taxRevenue = 1500f;
    public float GetTaxRevenue() => taxRevenue;
    protected float energySupplyHealthiness = 1; //0-1

    public int badDays = 0;
    public int lowEmployeeDays = 0;

    private void Awake()
    {
        baseRevenue = taxRevenue;
    }

    protected virtual void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
            GameManager.instance.OnDayEnd += CheckForEmployees;
            GameManager.instance.OnDayEnd += GenerateWealth;

            GameManager.instance.OnTaxRevenueChanged += UpdateRevenue;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated += OnUtilities;
    }

    protected virtual void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
            GameManager.instance.OnDayEnd -= CheckForEmployees;
            GameManager.instance.OnDayEnd -= GenerateWealth;

            GameManager.instance.OnTaxRevenueChanged -= UpdateRevenue;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated -= OnUtilities;
    }

    public void TryToMassHire()
    {
        if (GameManager.instance.currentUnemployed <= 0) return;

        int spaceAvailable = maxEmployees - employees;
        int availableWorkers = GameManager.instance.currentUnemployed;

        int totalToHire = Mathf.Min(spaceAvailable, availableWorkers);

        if (totalToHire > 0)
        {
            employees += totalToHire;
            GameManager.instance.AdjustUnemployed(-totalToHire);
            GameManager.instance.AdjustVacanices(-totalToHire);
        }
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
        GameManager.instance.AdjustVacanices(1);
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

    protected void UpdateRevenue()
    {
        taxRevenue = baseRevenue * GameManager.instance.taxRevenueMultiplier;
    }

    public abstract void GenerateWealth();
    protected abstract void CheckForEmployees();
    public abstract void OnUtilities();
}
