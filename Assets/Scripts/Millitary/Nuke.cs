using UnityEngine;
using UnityEngine.Tilemaps;

public class Nuke : MovingAttacker
{
    GridManager gridManager;
    Rigidbody rb;
    HealthSystem healthSystem;

    [Header("Explosion prefabs")]
    [SerializeField] private GameObject bigExplosionObj;
    [SerializeField] private GameObject smallExplosionObj;

    private void Awake()
    {
       gridManager = GridManager.instance;
       rb = GetComponent<Rigidbody>();
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
            healthSystem.onDeath += OnIntercept;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.onDeath -= OnIntercept;
    }

    public override float MoveSpeed => rb.linearVelocity.magnitude;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground")) Explode();
    }

    private void OnIntercept()
    {
        SmallExplosion();
    }

    private void Explode()
    {
        if (EventManager.instance.isFlooded)
        {
            Destroy(gameObject);
            return;
        }

        int radius = 25;
        int innerRadius = 8;

        Instantiate(bigExplosionObj, transform.position, Quaternion.identity, MilitaryManager.instance.transform);

        Blast(radius, innerRadius);

        Destroy(gameObject);
    }

    private void SmallExplosion()
    {
        int radius = Mathf.RoundToInt(Mathf.Lerp(2, 5, 1/transform.position.y));
        int innerRadius = 6;

        Instantiate(smallExplosionObj, transform.position, Quaternion.identity, MilitaryManager.instance.transform);

        Blast(radius, innerRadius);

        Destroy(gameObject);
    }

    private void Blast(int radius, int innerRadius)
    {
        Vector2Int centre = new(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        var mapGrid = gridManager.GetMapGrid();
        gridManager.forceRemoveElement(centre);

        for (int i = centre.x - radius; i <= centre.x + radius; i++)
        {
            for (int j = centre.y - radius; j <= centre.y + radius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                float distance = Vector2Int.Distance(centre, pos);

                if (distance < innerRadius)
                {
                    gridManager.forceRemoveElement(pos);
                }
                else if (distance < radius)
                {
                    if (mapGrid.TryGetValue(pos, out var tile))
                    {
                        if (tile.buildingScript)
                        {
                            //Set building on fire
                            tile.buildingScript.IgniteFire();

                            StartCoroutine(EventManager.instance.BurnBuilding(pos, tile.buildingScript));

                            //Call fire services
                            if (ServiceManager.instance != null) ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
                            else { Debug.LogError("Service Manager not found!"); }
                        }
                    }
                }

            }
        }
    }
}
