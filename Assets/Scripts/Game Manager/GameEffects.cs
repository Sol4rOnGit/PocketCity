using UnityEngine;

public class GameEffects : MonoBehaviour
{
    public static GameEffects instance;

    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    [Header("Dependencies")]
    [SerializeField] private FinanceManager financeManager;
    [SerializeField] private ChunkManager chunkManager;
    private CityGenerator cityGenerator;

    void Start()
    {
        financeManager = FinanceManager.instance;
        chunkManager = ChunkManager.instance;
        cityGenerator = CityGenerator.instance;
    }

    //Positive effects
    public void IncreaseTaxes()
    {
        Debug.Log("Increase tax");
        financeManager.taxMultiplier += 0.01f;
    }

    public void IncreaseHappiness()
    {
        Debug.Log("Increase Happiness");
        chunkManager.IncreaseBaselineHappiness(10f);
    }

    public void IncreaseCityGrowthSpeed()
    {
        Debug.Log("Increase City Growth Speed");
        cityGenerator.IncreaseSpawningRate();
    }

    //Negative effects

    public void Take20PercentNetWorth()
    {
        Debug.Log("take 20%!");
        float netWorth = financeManager.currentMoney;

        if (netWorth < 0) return;

        long amountToTake = (long)(netWorth * 0.2);
        Debug.Log($"Stealing {amountToTake} from {netWorth}");

        financeManager.Steal(amountToTake);
    }

    public void SuddenPowerSurge()
    {
        Debug.Log("Power surge!!");
        int power = UnityEngine.Random.Range(3000, 8000);
        float seconds = UnityEngine.Random.Range(3f, 30f);

        chunkManager.StartCoroutine(chunkManager.IncreasePowerDemandTemporarily(power, seconds));
    }

}
