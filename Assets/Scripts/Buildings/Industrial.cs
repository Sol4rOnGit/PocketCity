using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public class Industrial : Building
{
    [Header("Prefab conditions")]
    [SerializeField] private int maxEmployees = 50;
    [SerializeField] private float taxRevenue = 1500f;

    public int employees = 25;
    public float energySupplyHealthiness = 1; //0-1
    public float waterSupplyHealthiness = 1; //0-1

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += GenerateWealth;
        }
    }

    public void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= GenerateWealth;
        }
    }

    public void GenerateWealth()
    {
        float revenue = employees * taxRevenue;
        revenue *= (energySupplyHealthiness + waterSupplyHealthiness)/2;
        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }
    }
}