using System.Collections;
using UnityEngine;

public class Ambulance : ServiceVehicle
{
    [Header("Ambulance Specific")]
    public bool isHealing = false;
    [SerializeField] private float timeToHeal = 3.0f; //Min 0.6s!!

    public override bool IsPerformingJob => isHealing;

    protected override void StartJob()
    {
        StartCoroutine(HealBuilding());
    }
    private IEnumerator HealBuilding()
    {
        isHealing = true;

        House houseScript = null;

        if (targetBuilding is House house) { houseScript = house; }

        if (targetBuilding != null && houseScript.isInfected)
        {
            Vector3 buildingWorldPos = new Vector3(targetBuilding.gridPos.x * gridScale, transform.position.y, targetBuilding.gridPos.y * gridScale);
            Vector3 lookDir = (buildingWorldPos - transform.position).normalized;

            if (lookDir != Vector3.zero) { transform.rotation = Quaternion.LookRotation(lookDir); }

            yield return new WaitForSeconds(timeToHeal);

            if (targetBuilding != null)
            {
                houseScript.Heal();
            }
        } else
        {
            yield return new WaitForSeconds(0.1f);
        }

        ReturnToStation();
    }

    protected override void ReturnToInventory()
    {
        if (GridManager.instance == null) { throw new System.Exception("Error. No Grid Manager"); }

        var grid = GridManager.instance.GetMapGrid();
        if (grid.TryGetValue(homeStationPos, out var tile) && tile.buildingScript is Hospital hospital)
        {
            hospital.AmbulanceReturned();
        }
    }
}
