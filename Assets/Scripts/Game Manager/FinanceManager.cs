using System;
using UnityEngine;

public struct FinancialReport
{
    public long totalIncome;
    public long playerCosts;
    public long maintenanceCosts;
    public long netRevenue; //Profit/Loss
}

public class FinanceManager : MonoBehaviour
{
    public static FinanceManager instance { get; private set; }

    private void Awake()
    {
        if(instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    [Header("Settings")]
    [SerializeField] private long initialMoney = 100_000; //reasonable values?: 300k for normal, 100k for hard. 1 million for city builder mode.
    public long currentMoney { get; private set; }
    public long prevMoney;

    [Header("Tracker")]
    public FinancialReport lastFinancialReport = new FinancialReport();
    private long dayIncomeTracker;
    private long dayMaintenanceTracker;
    private long dayPlayerCostTracker;

    [Header("Economics")]
    public float taxMultiplier = 1f;
    public float inflationRate = 0.01f; //1%

    [SerializeField] private long maxDebtLimit = -50_000;
    
    [Header("Costs")]
    public float costRoad = 245f;
    public float costRoadDemolition = 445f;
    public float costZoning = 1033f;

    public float roadMaintainanceCost = 20f;

    public float serviceChargeFire = 900f;
    public float serviceChargeHospital = 400f;

    [Header("Base Costs")]
    private float baseCostRoad;
    private float baseCostRoadDemolition;
    private float baseCostZoning;

    private float baseRoadMaintainanceCost;

    private float baseServiceChargeFire;
    private float baseServiceChargeHospital;

    [Header("Actions")]
    public Action<long> OnMoneyChanged;

    void Start()
    {
        currentMoney = initialMoney;
        prevMoney = currentMoney;
        OnMoneyChanged?.Invoke(currentMoney);

        //Set base
        baseCostRoad = costRoad;
        baseCostRoadDemolition = costRoadDemolition;
        baseCostZoning = costZoning;

        baseRoadMaintainanceCost = roadMaintainanceCost;

        baseServiceChargeFire = serviceChargeFire;
        baseServiceChargeHospital = serviceChargeHospital;
    }

    public bool Purchase(float amount)
    {
        if (amount <= 0 || float.IsNaN(amount)) return false;

        long cost = (long)amount;

        if (currentMoney < cost)
        {
            return false;
        }

        currentMoney -= cost;
        dayPlayerCostTracker += cost;
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public bool Purchase(float amount, float multiplier)
    {
        return Purchase(amount * multiplier);
    }
    public void ForcePurchase(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;

        long cost = (long)amount;
        currentMoney -= cost;
        dayMaintenanceTracker += cost;

        OnMoneyChanged?.Invoke(currentMoney);

        if (currentMoney <= maxDebtLimit)
        {
            GameManager.instance.GameOver();
        }
    }
    public void MaintainancePurchase(int numRoads)
    {
        ForcePurchase(roadMaintainanceCost * numRoads);
    }

    public void Steal(long amount)
    {
        Debug.Log($"currentMoney before steal: {currentMoney}");
        currentMoney -= amount;
        dayPlayerCostTracker -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"currentMoney after steal: {currentMoney}");
    }

    public void Gain(float amount)
    {
        if (amount <= 0 || float.IsNaN(amount)) return;

        long gained = (long)amount * (long)taxMultiplier;
        currentMoney += gained;
        dayIncomeTracker += gained;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void DoDailyReport()
    {
        lastFinancialReport.totalIncome = dayIncomeTracker;
        lastFinancialReport.playerCosts = dayPlayerCostTracker;
        lastFinancialReport.maintenanceCosts = dayMaintenanceTracker;
        lastFinancialReport.netRevenue = dayIncomeTracker - (dayMaintenanceTracker + dayPlayerCostTracker);

        dayIncomeTracker = 0;
        dayMaintenanceTracker = 0;
        dayPlayerCostTracker = 0;
    }

    public void Inflate(int population, int daysPassed)
    {
        float currentMultiplier = GetInflationForDaysPassed(daysPassed);

        //Update the costs
        costRoad = (baseCostRoad * currentMultiplier);
        costRoadDemolition = (baseCostRoadDemolition * currentMultiplier);
        costZoning = (baseCostZoning * currentMultiplier);

        roadMaintainanceCost = (baseRoadMaintainanceCost * currentMultiplier);

        serviceChargeFire = (baseServiceChargeFire * currentMultiplier);
        serviceChargeHospital = (baseServiceChargeHospital * currentMultiplier);
    }

    private float GetInflationForDaysPassed(int daysPassed)
    {
        if (daysPassed > 130)
        {
            return 3f + (0.2f * (daysPassed - 100)); //mad increase now
        }

        return 1f + (0.02f * (daysPassed));
    }
}
