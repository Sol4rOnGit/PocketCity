using System;
using UnityEngine;

public class FinanceManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float initialMoney = 100000;
    [SerializeField] private long currentMoney;

    [Header("Costs")]
    public int costRoad = 145;
    public int costRoadDemolition = 245;
    public int costZoning = 535;
    public int costDemolitionBase = 15000;

    public void Update()
    {
        if (UnityEngine.Random.Range(0, 100) < 1)
        {
            Debug.Log(currentMoney);
        }
    }

    public long GetCurrentCapital()
    {
        return currentMoney;
    }

    public bool Purchase(float amount)
    {
        long cost = (long)amount;

        if (currentMoney < cost)
        {
            return false;
        }

        currentMoney -= cost;
        return true;
    }

    public bool Purchase(float amount, float multiplier)
    {
        long cost = (long)amount * (long)multiplier;

        if (currentMoney < cost)
        {
            return false;
        }

        currentMoney -= cost;
        return true;
    }

    public void Gain(float amount)
    {
        currentMoney += (long)amount;
    }

    void Start()
    {
        currentMoney = (long)initialMoney;
    }
}
