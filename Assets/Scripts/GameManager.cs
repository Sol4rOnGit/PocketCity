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

    //[Header("Dependencies")]
    [HideInInspector] private FinanceManager financeManager;

    [Header("Actions")]
    public Action OnDayEnd;

    private void Start()
    {
        financeManager = FinanceManager.instance;
        StartCoroutine(completeCommercialDay());
    }

    private IEnumerator completeCommercialDay()
    {
        while (true)
        {
            financeManager.prevMoney = financeManager.currentMoney;
            OnDayEnd?.Invoke();
            yield return new WaitForSeconds(3f); //every 3 seconds
        }
    }
}
