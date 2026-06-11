using System;
using System.Collections;
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
    [SerializeField] private GridManager gridManager;

    [Header("City Statistics")]
    public int currentPopulation;
    public int currentUnemployed;
    public int currentVacanies;

    [Header("Disasters")]

    [Header("Actions")]
    public Action OnDayEnd;

    private void Start()
    {
        financeManager = FinanceManager.instance;
        StartCoroutine(completeCommercialDay());
        StartCoroutine(randomEventTimer());
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
            int randomInt = UnityEngine.Random.Range(0, 1);
            switch (randomInt)
            {
                case 0: break;
                case 1: Earthquake(); break;
                default: Debug.Log("Game Manager - randomEventTimer() | Invalid random Integer number"); break;
            }
            yield return new WaitForSeconds(UnityEngine.Random.Range((60 * 5f), (60 * 10f))); //is this reaseonable? 
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
        UnityEngine.Random.Range((int)Mathf.Max(1, (gridManager.GetMapGrid().Count/20)), (int)(gridManager.GetMapGrid().Count/5));

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
