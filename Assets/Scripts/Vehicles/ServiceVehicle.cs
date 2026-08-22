using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ServiceVehicle : MonoBehaviour
{
    protected List<Vector2Int> travelRoute;
    protected Building targetBuilding;
    protected Vector2Int homeStationPos;
    protected float gridScale;
    protected ServiceManager serviceManager;

    [HideInInspector] public int currentPathIndex = 0;
    [HideInInspector] public bool isReturningHome = false;

    [Header("Movement Settings")]
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected float rotSpeed = 15f;
    public abstract bool IsPerformingJob { get; }
    protected bool returned = false;
    public virtual void Init(List<Vector2Int> route, Building target, float scale, Vector2Int homeStation)
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
        if (travelRoute == null || travelRoute.Count == 0 || (IsPerformingJob && !isReturningHome)) { return; }

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
                    StartJob();
                }
                else
                {
                    ReturnToInventory();
                    Destroy(gameObject);
                }

            }
        }
    }

    protected void ReturnToStation()
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
            ReturnToInventory();
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (!returned) ReturnToInventory();
    }
    protected abstract void StartJob();
    protected abstract void ReturnToInventory();
}
