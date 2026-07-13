using UnityEngine;

public class Service : Employer
{
    public float dailyCost; //To be implemented -> currently does nothing
    private float baseDailyCost;

    public void Start()
    {
        if (dailyCost == 0) { dailyCost = 100; }
        baseDailyCost = dailyCost;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += MaintenanceCosts;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= MaintenanceCosts;
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

    public override void GenerateWealth()
    {
        return;
    }

    protected override void CheckForEmployees()
    {
        return;
    }

    public override void OnUtilities()
    {
        return;
    }
}
