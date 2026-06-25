using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public enum CommercialType { Office, Entertainment, Shops}

public class Commercial : Employer
{
    private void Start()
    {
        CalculatePerEmployeeTax();
    }

    private void CalculatePerEmployeeTax()
    {
        float min = 30f;
        float max = 80f;
        float average = 45f;
        float density = 1f; //higher makes it tighter, less makes it looser

        float rand = Random.value;
        float taxPerEmployee;

        if (rand < 0.5f)
        {
            float ratio = Mathf.Pow(rand * 2f, density);
            taxPerEmployee = Mathf.Lerp(min, average, ratio);
        } else
        {
            float ratio = Mathf.Pow((rand - 0.5f) * 2f, 1f / density);
            taxPerEmployee = Mathf.Lerp(average, max, ratio);
        }

        taxRevenue = maxEmployees * taxPerEmployee;
    }

    public override void GenerateWealth()
    {
        float revenue = (employees * taxRevenue) / maxEmployees;

        revenue *= energySupplyHealthiness;

        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }

        //Debug.Log($"I just made {revenue} with {employees} emploees with max Employees {maxEmployees} and {taxRevenue} as my max revenue");
    }

    protected override void CheckForEmployees()
    {
        if (employees > maxEmployees/5) //NEED 20% FULL!!
        {
            lowEmployeeDays = 0;
            return;
        }

        lowEmployeeDays++;
        if (lowEmployeeDays > 7) //a week
        {
            GameManager.instance.UserNotification?.Invoke("Commericial block shut down due to lack of employees.", false);
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

        CheckShutdown(energySupplyHealthiness < 0.5f, 3);
    }
}
