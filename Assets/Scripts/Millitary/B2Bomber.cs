using System.Collections;
using UnityEngine;

public class B2Bomber : MovingAttacker
{
    [Header("Dependencies")]
    private GridManager gridManager;
    private HealthSystem healthSystem;

    [SerializeField] private GameObject nukePrefab;

    [Header("Settings")]
    private float moveSpeed = 10f;
    public override float MoveSpeed => moveSpeed;

    private Vector3 targetPos;

    void Start()
    {
        gridManager = GridManager.instance;
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null) healthSystem.onDeath += OnDeath; else Debug.LogError("Health System not found on B2Bomber.");

        //Initialise
        transform.position = new Vector3(200, 40, 200);
        targetPos = SelectATarget();

        Vector3 flatTarget = new(targetPos.x, transform.position.y, targetPos.z);
        transform.LookAt(flatTarget);

        StartCoroutine(FlyOverTarget());
    }

    private void OnDisable()
    {
        if (healthSystem != null) healthSystem.onDeath -= OnDeath;
    }

    private Vector3 SelectATarget()
    {
        if (gridManager.BuildingPositions.Count == 0)
            return Vector3.zero;

        Vector2Int randomBuildingGridPos = gridManager.BuildingPositions[Random.Range(0, gridManager.BuildingPositions.Count)];
        float scale = gridManager.getGridScale();
        Vector3 randomBuildingPos = new Vector3(randomBuildingGridPos.x * scale, 0f, randomBuildingGridPos.y * scale);

        return randomBuildingPos;
    }

    private IEnumerator FlyOverTarget()
    {
        bool bombDropped = false;
        float prevDist = float.MaxValue;

        while (!bombDropped)
        {
            Vector3 dir = targetPos - new Vector3(transform.position.x, 0f, transform.position.z);
            float currentDist = dir.magnitude;

            if (currentDist < 1.5f || currentDist > prevDist)
            {
                DropBomb();
                bombDropped = true;
            }

            transform.Translate(moveSpeed * Time.deltaTime * Vector3.forward, Space.Self);

            yield return null;
        }
    }

    private void DropBomb()
    {
        GameObject nuke = Instantiate(nukePrefab, transform.position, Quaternion.identity, MilitaryManager.instance.transform);
        nuke.GetComponent<Nuke>().SetParent(gameObject);
        StartCoroutine(Exfil());
    }

    private IEnumerator Exfil()
    {
        moveSpeed *= 2;

        while (true)
        {
            //Just go up and away
            //Pitch up (set eul x rot to like 30)
            transform.Translate(moveSpeed * Time.deltaTime * Vector3.forward, Space.Self);

            if (transform.position.y > 500)
                Destroy(gameObject);

            yield return null;
        }

    }

    private void OnDeath()
    {
        //Explode
        Destroy(gameObject);
    }
}
