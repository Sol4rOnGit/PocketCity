using UnityEngine;

public class PoliceStation : Service
{
    [Header("Police Station")]
    [SerializeField] private int maxPolice = 10;
    private int currentPolice = 10;

    public bool HasPolice()
    {
        return (currentPolice > 0);
    }

    public bool DispatchPolice()
    {
        if (currentPolice > 0)
        {
            currentPolice--;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void PoliceReturned()
    {
        if (currentPolice == maxPolice)
        {
            Debug.LogError("Somehow more firetrucks returned than dispatched.");
            return;
        }

        currentPolice += 1;
    }
}
