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
    public bool isSpreadingFire = false;
    [SerializeField] private GameObject fireParticles;
    private GameObject activeFireEffect;

    [HideInInspector] public bool isDestroying;

    [Header("Earthquake")]
    public int RetroFitCost = 15_000;
    public bool isRetrofitted;

    public void IgniteFire()
    {
        if (this is FireStation) { return; }

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

    public int RetroFit()
    {
        if (isRetrofitted)
        {
            GameManager.instance.UserNotification?.Invoke("Building Already Retrofitted!", false);
            return -1;
        }

        if (FinanceManager.instance.Purchase(RetroFitCost))
        {
            Debug.Log($"Retrofitted {gridPos.x} {gridPos.y} succesfully!");
            isRetrofitted = true;
            return RetroFitCost;
        } else
        {
            GameManager.instance.UserNotification?.Invoke("Not enough money to retrofit!", false);
        }

        return -1;
    }
}