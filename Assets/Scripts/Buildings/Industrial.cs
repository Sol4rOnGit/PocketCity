using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public class Industrial : Building
{
    [Header("Prefab conditions")]
    [SerializeField] private int maxEmployees = 50;
    public int GetMaxEmployees() { return maxEmployees; }
    [SerializeField] private float taxRevenue = 1500f;

    public int employees = 25;
    public float energySupplyHealthiness = 1; //0-1
    public float waterSupplyHealthiness = 1; //0-1

    private int badDays = 0;

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
        }
        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated += OnUtilities;
    }

    public void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
        }
        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated -= OnUtilities;
    }

    public void GenerateWealth()
    {
        float revenue = (employees * taxRevenue) / maxEmployees;
        revenue *= (energySupplyHealthiness + waterSupplyHealthiness)/2;
        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }
    }

    private void TryToHire()
    {
        if (employees < maxEmployees)
        {
            if (GameManager.instance.currentUnemployed > 0)
            {
                employees += 1;
                GameManager.instance.currentUnemployed -= 1;
                GameManager.instance.currentVacanies -= 1;
            }
        }
    }

    public bool LoseEmployee()
    {
        if (employees < 1) { return false; }
        employees -= 1;
        GameManager.instance.currentVacanies += 1;
        return true;
    }

    public void OnUtilities()
    {
        if (ChunkManager.instance == null || this.isDestroying) return;

        //Get chunk through position
        float scale = GameManager.instance.gridManager.getGridScale();

        ChunkManager.ChunkData chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
        if (chunk == null) return;

        energySupplyHealthiness = chunk.powerConsumed > 0 ?
            Mathf.Clamp01((float)(chunk.powerGenerated + chunk.powerImported) / chunk.powerConsumed)
            : 1;

        waterSupplyHealthiness = chunk.waterConsumed > 0 ?
            Mathf.Clamp01((float)(chunk.waterGenerated + chunk.waterImported) / chunk.waterConsumed) 
            : 1;

        GenerateWealth();

        if (energySupplyHealthiness < 0.7f || waterSupplyHealthiness < 0.7f)
        {
            badDays++;
            if (badDays >= 3)
            {
                GameManager.instance.gridManager.forceRemoveElement(gridPos);
            }
        }
        else
        {
            badDays = 0;
        }
    }
}