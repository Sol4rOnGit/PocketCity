using UnityEngine;

public enum BuildingType { Residential, Industrial, Commercial}

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
}