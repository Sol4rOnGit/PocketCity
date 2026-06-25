using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public class Industrial : Employer
{
    [Header("Industrial")]
    [SerializeField] private float waterSupplyHealthiness = 1; //0-1

    private void Start()
    {
        taxRevenue = maxEmployees * Random.Range(30f, 50f);
    }

    public override void GenerateWealth()
    {
        float revenue = (employees * taxRevenue) / maxEmployees;
        revenue *= (energySupplyHealthiness + waterSupplyHealthiness)/2;
        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }
    }

    protected override void CheckForEmployees()
    {
        if (employees > maxEmployees/20) //5% required
        {
            lowEmployeeDays = 0;
            return;
        }
        
        lowEmployeeDays++;

        if (lowEmployeeDays > 7) //a week
        {
            GameManager.instance.gridManager.forceRemoveElement(gridPos);
        }
    }

    public override void OnUtilities()
    {
        if (ChunkManager.instance == null || this.isDestroying) return;

        ChunkManager.ChunkData chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
        if (chunk == null) return;

        energySupplyHealthiness = chunk.powerConsumed > 0 ?
            Mathf.Clamp01((float)(chunk.powerGenerated + chunk.powerImported) / chunk.powerConsumed)
            : 1;

        waterSupplyHealthiness = chunk.waterConsumed > 0 ?
            Mathf.Clamp01((float)(chunk.waterGenerated + chunk.waterImported) / chunk.waterConsumed) 
            : 1;

        CheckShutdown(energySupplyHealthiness < 0.7f || waterSupplyHealthiness < 0.7f, 3);
    }
}