using UnityEngine;

public enum CommercialType { Office, Entertainment, Shops}

public class Commerical : Building
{
    public int employees;
    public int maxEmployees;
    public int profits;
    public float companyWealth;
    public float companyProfits;
    public float taxRevenue;
    public float energySupplyHealthiness; //0-1
    public CommercialType commercialType;

    public void SetupIndustrial(Vector2Int pos, int maxEmployees, string commercialType)
    {
        SetPos(pos);
        this.maxEmployees = maxEmployees;
        
        if(commercialType == "Office") { this.commercialType = CommercialType.Office; }
        if(commercialType == "Entertainment") { this.commercialType = CommercialType.Entertainment; }
        if(commercialType == "Shops") { this.commercialType = CommercialType.Shops; }

    }
}
