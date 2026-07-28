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
    [SerializeField] private long initialMoney = 400_000; //reasonable values?: 400k for normal, 150k for hard. 1 million for city builder mode.
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
    public float costZoning = 550;

    public float roadMaintainanceCost = 20f;

    public float serviceChargeFire = 900f;
    public float serviceChargeHospital = 400f;
    public float serviceChargePoliceTrip = 200f;

    [Header("Base Costs")]
    private bool flagMaintenance = false;
    private int maintenanceResetDays = 0;

    private float baseCostRoad;
    private float baseCostZoning;

    private float baseRoadMaintainanceCost;

    private float baseServiceChargeFire;
    private float baseServiceChargeHospital;
    private float baseServiceChargePoliceTrip;

    [Header("Actions")]
    public Action<long> OnMoneyChanged;
    public Action OnInflationCompleted;

    [Header("Emergency Borrow Settings")]
    public long currentDebt { get; private set; } = 0;
    public bool hasActiveDebt { get; private set; } = false;
    public int debtPaymentDaysLeft { get; private set; } = 0;

    void Start()
    {
        currentMoney = initialMoney;
        prevMoney = currentMoney;
        OnMoneyChanged?.Invoke(currentMoney);

        //Set base
        baseCostRoad = costRoad;
        baseCostZoning = costZoning;

        baseRoadMaintainanceCost = roadMaintainanceCost;

        baseServiceChargeFire = serviceChargeFire;
        baseServiceChargeHospital = serviceChargeHospital;
        baseServiceChargePoliceTrip = serviceChargePoliceTrip;
    }

    public bool Purchase(float amount)
    {
        if (amount <= 0 || float.IsNaN(amount))
        {
            Debug.LogError($"Purchase rejected! Amount passed: {amount}. IsNan: {float.IsNaN(amount)}");
            return false;
        }

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
    public void RoadMaintainancePurchase(int numRoads)
    {
        ForcePurchase(roadMaintainanceCost * numRoads);
    }

    public void Steal(long amount)
    {
        currentMoney -= amount;
        dayPlayerCostTracker -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
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
        ProcessDailyDebtInflation();

        lastFinancialReport.totalIncome = dayIncomeTracker;
        lastFinancialReport.playerCosts = dayPlayerCostTracker;
        lastFinancialReport.maintenanceCosts = dayMaintenanceTracker;
        lastFinancialReport.netRevenue = dayIncomeTracker - (dayMaintenanceTracker + dayPlayerCostTracker);

        dayIncomeTracker = 0;
        dayMaintenanceTracker = 0;
        dayPlayerCostTracker = 0;
    }

    public void ResetMaintenance()
    {
        flagMaintenance = true;
    }

    public void Inflate(int population, int daysPassed)
    {
        float currentMultiplier = GetInflationForDaysPassed(daysPassed);

        //Update the costs
        costRoad = (baseCostRoad * currentMultiplier);
        costZoning = (baseCostZoning * currentMultiplier);

        roadMaintainanceCost = (baseRoadMaintainanceCost * currentMultiplier);

        serviceChargeFire = (baseServiceChargeFire * currentMultiplier);
        serviceChargeHospital = (baseServiceChargeHospital * currentMultiplier);
        serviceChargePoliceTrip = (baseServiceChargePoliceTrip * currentMultiplier);

        OnInflationCompleted?.Invoke();
    }

    private float GetInflationForDaysPassed(int daysPassed)
    {
        if (flagMaintenance) { maintenanceResetDays = daysPassed; flagMaintenance = false; }
        int adjustedDaysPassed = daysPassed - maintenanceResetDays;

        if (adjustedDaysPassed > 130)
        {
            return 3f + (0.03f * (adjustedDaysPassed - 100)); //increase more after day 130
        }

        return 1f + (0.02f * (adjustedDaysPassed)); //3f at 100 days hence 3f above
    }

    //Loan
    public void ProcessEmergencyLoan(int amount, int paybackTimeDays)
    {
        if (!hasActiveDebt)
        {
            //Take on a loan
            currentMoney += amount;
            currentDebt = amount;
            hasActiveDebt = true;
            debtPaymentDaysLeft = paybackTimeDays;

            OnMoneyChanged?.Invoke(currentMoney);
        } else
        {
            //Try pay back loan
            if (currentMoney >= currentDebt)
            {
                currentMoney -= currentDebt;
                dayPlayerCostTracker += currentDebt;

                currentDebt = 0;
                hasActiveDebt = false;

                OnMoneyChanged?.Invoke(currentMoney);
            } else { GameManager.instance.UserNotification?.Invoke("Not enough money to pay back loan!", true); }
        }
    }

    private void ProcessDailyDebtInflation()
    {
        if (hasActiveDebt && currentDebt > 0)
        {
            currentDebt = (long)(currentDebt * 1.01f);

            debtPaymentDaysLeft--;

            if (debtPaymentDaysLeft <= 0)
            {
                GameManager.instance.UserNotification?.Invoke("Loan term expired! Debt being collected...", true);

                ForcePurchase(currentDebt);

                currentDebt = 0;
                debtPaymentDaysLeft = 0;
                hasActiveDebt = false;
            }
        }
    }
}