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
    [SerializeField] private long currentMoney;

    public long GetCurrentCapital() { return currentMoney; }

    [Header("Costs")]
    public int costRoad = 145;
    public int costRoadDemolition = 245;
    public int costZoning = 535;
    public int costDemolitionBase = 15000;

    [Header("Actions")]
    public Action<long> OnMoneyChanged;
    public Action OnDayEnd; //To ask everything

    void Start()
    {
        currentMoney = initialMoney;
        OnMoneyChanged?.Invoke(currentMoney);
        StartCoroutine(completeCommercialDay());
    }

    public IEnumerator completeCommercialDay()
    {
        while (true){
            OnDayEnd?.Invoke();
            yield return new WaitForSeconds(3f); //every 3 seconds
        }
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
