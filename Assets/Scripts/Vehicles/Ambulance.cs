using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ambulance : MonoBehaviour
{
    private List<Vector2Int> travelRoute;
    private Building targetBuilding;
    private Vector2Int homeStationPos;
    private float gridScale;
    private ServiceManager serviceManager;

    public int currentPathIndex = 0;
    public bool isHealing = false;
    public bool isReturningHome = false;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotSpeed = 10f;

    [Header("Job Settings")]
    [SerializeField] private float timeToHeal = 3.0f; //Min 0.6s!!

    public void Init(List<Vector2Int> route, Building target, float scale, Vector2Int homeStation)
    {
        travelRoute = route;
        targetBuilding = target;
        gridScale = scale;
        homeStationPos = homeStation;
    }

    public void Start()
    {
        serviceManager = ServiceManager.instance;
    }

    private void Update()
    {
        if (travelRoute == null || travelRoute.Count == 0 || (isHealing && !isReturningHome)) { return; }

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        Vector2Int targetGridPos = travelRoute[currentPathIndex];
        Vector3 targetWorldPos = new Vector3(targetGridPos.x * gridScale, transform.position.y, targetGridPos.y * gridScale);

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        Vector3 dir = (targetWorldPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.05f)
        {
            currentPathIndex++;

            if (currentPathIndex >= travelRoute.Count)
            {
                if (!isReturningHome)
                {
                    StartCoroutine(HealBuilding());
                }
                else
                {
                    ReturnAmbulanceToHospitalInventory();
                    Destroy(gameObject);
                }

            }
        }
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

    private void ReturnToStation()
    {
        Vector2Int currentGridPos = travelRoute[travelRoute.Count - 1];

        List<Vector2Int> returnRoute = serviceManager.CalculateRoadPath(currentGridPos, homeStationPos);

        if (returnRoute != null && returnRoute.Count > 0)
        {
            travelRoute = returnRoute;
            currentPathIndex = 0;
            isReturningHome = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ReturnAmbulanceToHospitalInventory()
    {
        if (GridManager.instance == null) { throw new System.Exception("Error. No Grid Manager"); }

        var grid = GridManager.instance.GetMapGrid();
        if (grid.TryGetValue(homeStationPos, out var tile) && tile.buildingScript is Hospital hospital)
        {
            hospital.AmbulanceReturned();
        }
    }
}
