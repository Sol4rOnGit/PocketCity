using UnityEngine;

public class Hospital : Building
{
    [Header("Hospital Setup")]
    [SerializeField] private int maxAmbulances = 3;
    private int currentAmbulances = 3;

    public bool HasAmbulances()
    {
        return (currentAmbulances > 0);
    }

    public bool DispatchAmbulance()
    {
        if (currentAmbulances > 0)
        {
            currentAmbulances--;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AmbulanceReturned()
    {
        currentAmbulances += 1;
    }
}
