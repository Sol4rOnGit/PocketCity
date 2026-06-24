using UnityEngine;

public class FireStation : Service
{
    [Header("Fire Station")]
    [SerializeField] private int maxFireTrucks = 2;
    private int currentFireTrucks = 2;

    public bool HasTrucks()
    {
        return (currentFireTrucks > 0);
    }

    public bool DispatchTruck()
    {
        if (currentFireTrucks > 0)
        {
            currentFireTrucks--;
            return true;
        } else
        {
            return false;
        }
    }

    public void TruckReturned()
    {
        if (currentFireTrucks == maxFireTrucks)
        {
            Debug.LogError("Somehow more firetrucks returned than dispatched.");
            return;
        }

        currentFireTrucks += 1;
    }
}
