using System.Collections;
using UnityEngine;

public class Firetruck : ServiceVehicle
{
    [Header("Firetruck specific")]
    public bool isExtinguishing = false;
    [SerializeField] private float timeToPutOutFire = 3.0f; //Min 0.6s!!

    [Header("VFX")]
    [SerializeField] private GameObject waterSprayPrefab;
    public override bool IsPerformingJob => isExtinguishing;

    protected override void StartJob()
    {
        StartCoroutine(ExtinguishFire());
    }

    private IEnumerator ExtinguishFire()
    {
        isExtinguishing = true;

        if (targetBuilding != null && targetBuilding.isOnFire)
        {
            GameObject waterSprayInstance = null;

            Vector3 buildingWorldPos = new Vector3(targetBuilding.gridPos.x * gridScale, transform.position.y, targetBuilding.gridPos.y * gridScale);
            Vector3 lookDir = (buildingWorldPos - transform.position).normalized;

            if (lookDir != Vector3.zero) { transform.rotation = Quaternion.LookRotation(lookDir); }

            Vector3 spawnOffset = (transform.forward * 0.5f) + (Vector3.up * 0.2f);
            if (waterSprayPrefab != null)
            {
                waterSprayInstance = Instantiate(waterSprayPrefab, transform.position + spawnOffset, transform.rotation, this.transform);
            } else { Debug.LogWarning("No water spray prefab."); }

            if (timeToPutOutFire > 0.5)
            {
                yield return new WaitForSeconds(timeToPutOutFire - 0.5f);
            }

            if (waterSprayInstance != null) {
                waterSprayInstance.GetComponentInChildren<ParticleSystem>().Stop();

                yield return new WaitForSeconds(0.5f);
                Destroy(waterSprayInstance); 
            }

            if (targetBuilding != null)
            {
                targetBuilding.ExtinguishFire();
            }
        }

        ReturnToStation();
    }

    protected override void ReturnToInventory()
    {
        if (GridManager.instance == null) { Debug.LogError("No grid manager found!"); return; }

        var grid = GridManager.instance.GetMapGrid();
        if (grid.TryGetValue(homeStationPos, out var gridTile) && gridTile.buildingScript is FireStation fireStation)
        {
            fireStation.TruckReturned();
        }
    }
}
