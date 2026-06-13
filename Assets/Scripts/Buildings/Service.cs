using UnityEngine;

public class Service : Building
{
    [SerializeField] private int _maxEmployees = 3;
    public int maxEmployees => _maxEmployees;
    public int employees = 0;

    public int monthlyCost; //To be implemented -> currently does nothing

    public void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += TryToHire;
        }
    }

    public void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd -= TryToHire;
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
}
