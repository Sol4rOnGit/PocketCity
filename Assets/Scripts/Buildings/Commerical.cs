using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public enum CommercialType { Office, Entertainment, Shops}

public class Commercial : Building
{
    [Header("Prefab conditions")]
    [SerializeField] private int maxEmployees = 50;
    [SerializeField] private CommercialType commercialType = CommercialType.Office;
    [SerializeField] private float taxRevenue = 1500f;

    public int employees = 25;
    public float energySupplyHealthiness = 1; //0-1

    private void OnEnable()
    {
        if (FinanceManager.instance != null)
        {
            FinanceManager.instance.OnDayEnd += GenerateWealth;
        }
    }

    public void OnDisable()
    {
        if(FinanceManager.instance != null)
        {
            FinanceManager.instance.OnDayEnd -= GenerateWealth;
        }
    }

    public void GenerateWealth()
    {
        float revenue = employees * taxRevenue;
        revenue *= energySupplyHealthiness;
        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }
    }
}
