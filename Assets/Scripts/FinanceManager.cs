using System;
using System.Collections;
using UnityEngine;

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

    [Header("Economics")]
    public float inflationRate = 0.01f; //1%
    [SerializeField] private float maxInflation = 0.5f; //50%

    [SerializeField] private long maxDebtLimit = -50_000;
    
    [Header("Costs")]
    public float costRoad = 300f;
    public float costRoadDemolition = 400f;
    public float costZoning = 1530f;

    public float roadMaintainanceCost = 3f;

    public float serviceChargeFire = 1500f;
    public float serviceChargeHospital = 2000f;

    [Header("Base Costs")]
    private float baseCostRoad = 300f;
    private float baseCostRoadDemolition = 400f;
    private float baseCostZoning = 1530f;

    private float baseRoadMaintainanceCost = 3f;

    private float baseServiceChargeFire = 1500f;
    private float baseServiceChargeHospital = 2000f;

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

    public void Gain(float amount)
    {
        currentMoney += (long)amount;
        OnMoneyChanged?.Invoke(currentMoney);
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
        if (daysPassed < 10) return 1f;
        if (daysPassed < 20) return 1.01f;
        if (daysPassed < 30) return 1.03f;
        if (daysPassed < 40) return 1.05f;
        if (daysPassed < 50) return 1.1f;
        if (daysPassed < 70) return 1.2f;
        if (daysPassed < 90) return 1.3f;
        if (daysPassed < 130) return 1.4f;
        if (daysPassed < 150) return 1.5f;
        if (daysPassed < 180) return 1.6f;
        if (daysPassed < 220) return 1.7f;
        if (daysPassed < 250) return 1.8f;
        if (daysPassed < 300) return 1.9f;

        return 2f;
    }
}
