using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    //public Boolean Enabled = true;
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;

    FinanceManager financeManager;

    private void Awake()
    {
        financeManager = GameObject.FindAnyObjectByType<FinanceManager>();

        if (financeManager == null) { Debug.LogError("UIManager couldn't find the Finance Manager!?!?"); }
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
        Debug.Log("Working!");

        if (currentMoneyUIText == null) { Debug.LogError("Missing UI reference."); return; }

        string formattedMoney;

        if (currentMoney >= 1_000_000_000_000)
        {
            float trillions = (float)currentMoney / 1_000_000_000_000;
            formattedMoney = $"{trillions:0.00} trillion";
        }
        else if (currentMoney >= 1_000_000_000)
        {
            float billions = (float)currentMoney / 1_000_000_000;
            formattedMoney = $"{billions:0.00} billion";
        } else if (currentMoney >= 1_000_000)
        {
            float millions = (float)currentMoney / 1_000_000;
            formattedMoney = $"{millions:0.00} million";
        }
        else {
            formattedMoney = $"{currentMoney:N0}";
        }

        currentMoneyUIText.text = formattedMoney;
    }
}
