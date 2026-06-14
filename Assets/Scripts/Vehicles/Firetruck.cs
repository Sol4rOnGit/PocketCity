using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Firetruck : MonoBehaviour
{
    private List<Vector2Int> travelRoute;
    private Building targetBuilding;
    private Vector2Int homeStationPos;
    private float gridScale;
    private ServiceManager serviceManager;

    public int currentPathIndex = 0;
    public bool isExtinguishing = false;
    public bool isReturningHome = false;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotSpeed = 10f;

    [Header("Job Settings")]
    [SerializeField] private float timeToPutOutFire = 3.0f; //Min 0.6s!!

    [Header("VFX")]
    [SerializeField] private GameObject waterSprayPrefab;

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
        if (travelRoute == null || travelRoute.Count == 0 || (isExtinguishing && !isReturningHome)) { return; }

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
                if (!isReturningHome) { 
                    StartCoroutine(ExtinguishFire());
                }
                else
                {
                    ReturnTruckToStationInventory();
                    Destroy(gameObject);
                }
                
            }
        }
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

    private void ReturnToStation()
    {
        Vector2Int currentGridPos = travelRoute[travelRoute.Count - 1];

        List<Vector2Int> returnRoute = serviceManager.CalculateRoadPath(currentGridPos, homeStationPos);

        if (returnRoute != null && returnRoute.Count > 0)
        {
            travelRoute = returnRoute;
            currentPathIndex = 0;
            isReturningHome = true;
        } else
        {
            Destroy(gameObject);
        }
    }

    private void ReturnTruckToStationInventory()
    {
        if (GridManager.instance == null) { throw new System.Exception("Error. No Grid Manager"); } 

        var grid = GridManager.instance.GetMapGrid();
        if (grid.TryGetValue(homeStationPos, out var tile) && tile.buildingScript is FireStation fireStation)
        {
            fireStation.TruckReturned();
        }
    }
}
