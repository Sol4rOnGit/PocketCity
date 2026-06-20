using UnityEngine;

public class Service : Building
{
    [SerializeField] private int _maxEmployees = 3;
    public int maxEmployees => _maxEmployees;
    public int employees = 0;

    public float dailyCost; //To be implemented -> currently does nothing
    private float baseDailyCost;

    public void Start()
    {
        if (dailyCost == 0) { dailyCost = 100; }
        baseDailyCost = dailyCost;
    }

    public void OnEnable()
    {


        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
            GameManager.instance.OnDayEnd += MaintenanceCosts;
        }
    }

    public void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
            GameManager.instance.OnDayEnd -= MaintenanceCosts;
        }
    }

    public void TryToHire()
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

    public void MaintenanceCosts()
    {
        FinanceManager.instance.ForcePurchase(dailyCost);
        Inflate();
    }

    public void Inflate()
    {
        dailyCost += 0.04f * baseDailyCost;
    }
}
