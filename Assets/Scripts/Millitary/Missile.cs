using UnityEngine;

public class Missile : MovingAttacker
{
    [Header("Dependencies")]
    public GameObject explosionPrefab;
    [SerializeField] private AudioSource explosionAudioSource;

    [Header("Settings")]
    [SerializeField] private float missileSpeed = 5f;
    [HideInInspector] public override float MoveSpeed => missileSpeed;
    [SerializeField] private float missileTurningSpeed = 200f;

    private Vector2Int targetGridPos;
    private Vector3 targetPos;

    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.onDeath += OnIntercept;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null) { healthSystem.onDeath -= OnIntercept; }
    }

    private void OnIntercept()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity, MilitaryManager.instance.transform);
        DoExplosionAudio();
        Destroy(gameObject);
    }

    public void Initialise(Vector2Int targetGridPosVec2I)
    {
        targetGridPos = targetGridPosVec2I;
        float gridScale = GridManager.instance.getGridScale();
        targetPos = new Vector3(targetGridPosVec2I.x * gridScale, 0f, targetGridPosVec2I.y * gridScale);
    }

    private void Update()
    {
        Vector3 dir = (targetPos - transform.position);

        if (dir.magnitude > 3f)
        {
            dir += Vector3.up * 1.5f; //slight arc
        }

        if (dir.magnitude < 0.5f)
        {
            StrikeTarget();
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, missileTurningSpeed * Time.deltaTime);

        transform.Translate(Vector3.forward * missileSpeed * Time.deltaTime, Space.Self);
    }

    private void StrikeTarget()
    {
        if (EventManager.instance.isFlooded)
        {
            Destroy(gameObject);
            return;
        }

        if (GridManager.instance.GetMapGrid().TryGetValue(targetGridPos, out var tile) && tile.buildingScript != null)
        {
            GridManager.instance.forceRemoveElement(targetGridPos);
        }

        Instantiate(explosionPrefab, transform.position, Quaternion.identity, MilitaryManager.instance.transform);
        DoExplosionAudio();
        Destroy(gameObject);
    }

    private void DoExplosionAudio()
    {
        //Audio
        explosionAudioSource.transform.SetParent(MilitaryManager.instance.transform);
        explosionAudioSource.Play();
        Destroy(explosionAudioSource.gameObject, explosionAudioSource.clip.length);
    }
}
