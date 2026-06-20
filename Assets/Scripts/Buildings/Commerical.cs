using UnityEngine;

//Don't forget to remove this in runtime once building demolition is introduced!!
public enum CommercialType { Office, Entertainment, Shops}

public class Commercial : Building
{
    [Header("Prefab conditions")]
    [SerializeField] private int maxEmployees = 50;
    public int GetMaxEmployees() { return maxEmployees; }
    [SerializeField] private float taxRevenue = 1500f;

    public int employees = 0;
    public float energySupplyHealthiness = 1; //0-1

    private int badDays = 0;
    private int lowEmployeeDays = 0;

    private void Start()
    {
        calculatePerEmployeeTax();
    }

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
            GameManager.instance.OnDayEnd += CheckForEmployees;
            GameManager.instance.OnDayEnd += GenerateWealth;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated += OnUtilities;
    }

    public void OnDisable()
    {
        if(GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
            GameManager.instance.OnDayEnd -= CheckForEmployees;
            GameManager.instance.OnDayEnd -= GenerateWealth;
        }

        if (ChunkManager.instance != null) ChunkManager.instance.BuildingUtilitiesUpdated -= OnUtilities;
    }

    private void calculatePerEmployeeTax()
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

    public void GenerateWealth()
    {
        float revenue = (employees * taxRevenue) / maxEmployees;

        revenue *= energySupplyHealthiness;

        if (revenue > 0) { FinanceManager.instance.Gain(revenue); }

        Debug.Log($"I just made {revenue} with {employees} emploees with max Employees {maxEmployees} and {taxRevenue} as my max revenue");
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

    private void CheckForEmployees()
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

    public void OnUtilities()
    {
        if (ChunkManager.instance == null || this.isDestroying) return;

        ChunkManager.ChunkData chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
        if (chunk == null) return;

        energySupplyHealthiness = chunk.powerConsumed > 0 ?
            Mathf.Clamp01((float)(chunk.powerGenerated + chunk.powerImported) / chunk.powerConsumed)
            : 1;

        if (energySupplyHealthiness < 0.5f)
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
