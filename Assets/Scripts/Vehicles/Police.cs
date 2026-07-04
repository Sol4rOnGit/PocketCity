using System.Collections;
using UnityEngine;

public class Police : ServiceVehicle
{
    [Header("Police Car Specific")]
    public bool isSolvingCrime = false;
    [SerializeField] private float timeToSolveCrimeSeconds = 2f;

    public override bool IsPerformingJob => isSolvingCrime;

    protected override void StartJob()
    {
        StartCoroutine(SolveCrime());
    }

    private IEnumerator SolveCrime()
    {
        isSolvingCrime = true;

        if (targetBuilding != null)
        {
            Vector3 buildingWorldPos = new Vector3(targetBuilding.gridPos.x * gridScale, transform.position.y, targetBuilding.gridPos.y * gridScale);
            Vector3 lookDir = (buildingWorldPos - transform.position).normalized;

            if (lookDir != Vector3.zero) { transform.rotation = Quaternion.LookRotation(lookDir); }
        }

        yield return new WaitForSeconds(timeToSolveCrimeSeconds);

        if (targetBuilding != null)
        {
            targetBuilding.isCrimeScene = false;
        }

        ReturnToStation(); 
    }

    protected override void ReturnToInventory()
    {
        if (GridManager.instance == null) { Debug.LogError("No grid manager found!"); }

        var grid = GridManager.instance.GetMapGrid();
        if (grid.TryGetValue(homeStationPos, out var gridTile) && gridTile.buildingScript is PoliceStation policeStation)
        {
            policeStation.PoliceReturned();
        }

    }
}
