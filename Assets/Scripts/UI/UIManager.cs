using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateCurrentMoneyUI(long currentMoney)
    {
        string formattedMoney;

        if (currentMoney >= 1_000_000_000)
        {
            float trillions = (float)currentMoney / 1_000_000_000;
            formattedMoney = $"{trillions:0.00} trillion";
        }
        else if (currentMoney >= 1_000_000_000)
        {
            float billions = (float)currentMoney / 1_000_000_0000;
            formattedMoney = $"{billions:0.00} billion";
        }
        else {
            formattedMoney = $"{currentMoney:N0}";
        }
    }
}
