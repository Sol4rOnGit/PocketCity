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
    [SerializeField] private long initialMoney = 100000;
    public long currentMoney { get; private set; }
    public long prevMoney;

    [Header("Costs")]
    public int costRoad = 145;
    public int costRoadDemolition = 245;
    public int costZoning = 535;
    public int costDemolitionBase = 15000;

    public int roadMaintainanceCost = 1;

    [Header("Actions")]
    public Action<long> OnMoneyChanged;

    void Start()
    {
        currentMoney = initialMoney;
        prevMoney = currentMoney;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public bool Purchase(float amount)
    {
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
        long cost = (long)(amount * multiplier);

        if (currentMoney < cost)
        {
            return false;
        }

        currentMoney -= cost;
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public void Gain(float amount)
    {
        currentMoney += (long)amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }
}
