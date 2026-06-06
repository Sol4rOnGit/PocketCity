using UnityEngine;

public class Industrial : Building
{
    public int employees;
    public int maxEmployees;
    public int profits;
    public float companyWealth;
    public float companyProfits;
    public float taxRevenue;
    public float energySupplyHealthiness; //0-1
    public float waterSupplyHealthiness; //0-1

    public void SetupIndustrial(Vector2Int pos, int maxEmployees)
    {
        SetPos(pos);
        this.maxEmployees = maxEmployees;
    }
}