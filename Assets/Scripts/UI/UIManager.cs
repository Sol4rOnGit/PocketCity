using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    //public Boolean Enabled = true;
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;
    [SerializeField] private TMPro.TextMeshProUGUI addedMoneyUIText;
    //private long lastMoney;

    FinanceManager financeManager;

    private void Awake()
    {
        financeManager = GameObject.FindAnyObjectByType<FinanceManager>();

        if (financeManager == null) { Debug.LogError("UIManager couldn't find the Finance Manager."); }
    }

    private void OnEnable()
    {
        financeManager.OnMoneyChanged += updateCurrentMoneyUI;
    }

    private void OnDisable()
    {
        financeManager.OnMoneyChanged -= updateCurrentMoneyUI;
    }

    private void Start()
    {
        updateCurrentMoneyUI(financeManager.GetCurrentCapital());
    }

    public void updateCurrentMoneyUI(long currentMoney)
    {
        if (currentMoneyUIText == null) { Debug.LogError("Missing UI reference."); return; }

        long delta = currentMoney - financeManager.prevMoney;

        string formattedMoney = ReturnTextFromMoney(currentMoney);
        string deltaMoney = (delta > 0) ? $"+{ReturnTextFromMoney(delta)}" : $"{ReturnTextFromMoney(delta)}";

        currentMoneyUIText.text = formattedMoney;
        addedMoneyUIText.text = deltaMoney;
    }

    private string ReturnTextFromMoney(long amount)
    {
        if (amount >= 1_000_000_000_000)
        {
            float trillions = (float)amount / 1_000_000_000_000;
            return $"£{trillions:0.00} trillion";
        }
        else if (amount >= 1_000_000_000)
        {
            float billions = (float)amount / 1_000_000_000;
            return $"£{billions:0.00} billion";
        }
        else if (amount >= 1_000_000)
        {
            float millions = (float)amount / 1_000_000;
            return $"£{millions:0.00} million";
        }
        else
        {
            return $"£{amount:N0}";
        }
    }
}
