using UnityEngine;

public enum BuildingType { Residential, Industrial, Commercial, Special}

public class Building : MonoBehaviour
{
    [Header("Shared properties")]
    public string buildingName;
    public BuildingType type;
    public Vector2Int gridPos;

    [Header("Utilities Requirements")]
    public int powerGenerated = 0;
    public int powerConsumed = 0;
    public int waterGenerated = 0;
    public int waterConsumed = 0;

    [Header("Fire")]
    public bool isOnFire = false;
    [SerializeField] private GameObject fireParticles;
    private GameObject activeFireEffect;

    [Header("Infected")]
    public bool isInfected = false;
    public bool isAmbulanceOnRoute = false;
    [SerializeField] private GameObject infectionParticles;
    private GameObject activeInfectionEffect;

    [HideInInspector] public bool isDestroying;

    public void IgniteFire()
    {
        if (isOnFire) { return; }
        isOnFire = true;

        if( fireParticles != null )
        {
            activeFireEffect = Instantiate(fireParticles, transform.position + (Vector3.up * 0.5f), Quaternion.Euler(-90, 0, 0), transform);
        }
    }

    public void ExtinguishFire()
    {
        if (!isOnFire) { return; }
        isOnFire = false;

        if (activeFireEffect != null ) { Destroy(activeFireEffect); }
    }

    public void Infect()
    {
        if ( isInfected ) { return; }
        isInfected = true;

        if (infectionParticles != null )
        {
            activeInfectionEffect = Instantiate(infectionParticles, transform.position + (Vector3.up * 0.5f), Quaternion.identity, transform);
        }
    }

    public void Heal()
    {
        if (!isInfected) { return; }
        isInfected = false;

        if (activeInfectionEffect != null ) { Destroy(activeInfectionEffect); }
    }
}