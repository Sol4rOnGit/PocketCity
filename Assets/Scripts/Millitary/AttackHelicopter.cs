using System.Collections;
using UnityEngine;

public class AttackHelicopter : MovingAttacker
{
    GridManager gridManager;
    MilitaryManager millitaryManager;

    [Header("Dependencies")]
    [SerializeField] private Transform helicopterModelTransform;
    [SerializeField] private HealthSystem healthSystem;

    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private GameObject[] missileSpawnPoints;

    [Header("Settings")]
    [SerializeField] private float minIntervalBetweenStrikesSeconds;
    [SerializeField] private float maxIntervalBetweenStrikesSeconds;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    public override float MoveSpeed => moveSpeed;
    [SerializeField] private float rotationSpeed = 150f;
    [SerializeField] private float flightHeight = 8f;
    [SerializeField] private Vector2 xzBounds = new Vector2(10f, 10f);
    private Vector3 currentHelicopterMovementTargetPos;

    [Header("Bob settings")]
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobFrequency = 1.5f;
    private float bobSeed;

    [Header("Tilt settings")]
    [SerializeField] private float maxPitchAngle = 30f;
    [SerializeField] private float maxRollAngle = 30f;
    [SerializeField] private float tiltSpeed = 5f;
    private Vector3 prevPos;
    private float prevYaw;
    private float currentAngularVelocity;

    private bool isDead;

    private void Start()
    {
        gridManager = GridManager.instance;
        millitaryManager = MilitaryManager.instance;

        prevPos = transform.position;
        prevYaw = transform.eulerAngles.y;

        bobSeed = Random.Range(0f, 100f);

        StartCoroutine(StrikeRoutine());

        if (healthSystem != null) healthSystem.onDeath += OnDeath; else Debug.LogError("Health System not found on Helicopter.");
    }

    private void OnDisable()
    {
        if (healthSystem != null) healthSystem.onDeath -= OnDeath;
    }

    private void Update()
    {
        HandleMovement();
        HandleTilt();
    }

    private void HandleMovement()
    {
        if (isDead) return;

        if (currentHelicopterMovementTargetPos == Vector3.zero) { SetNewTargetBuilding(); }

        Vector3 currentPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 targetMoveFlat = new Vector3(currentHelicopterMovementTargetPos.x, 0f, currentHelicopterMovementTargetPos.z);

        Vector3 targetMovePos = new Vector3(currentHelicopterMovementTargetPos.x, flightHeight, currentHelicopterMovementTargetPos.z);

        Vector3 dir = (targetMoveFlat - currentPosFlat);

        if (dir.magnitude < 2.5f)
        {
            SetNewTargetBuilding();
            MoveForwardWithBobbing();
            return;
        }

        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(currentPosFlat, targetMoveFlat) < 0.5f)
        {
            SetNewTargetBuilding();
        }

        MoveForwardWithBobbing();
    }

    private void MoveForwardWithBobbing()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);

        float sineOffset = Mathf.Sin((Time.time * bobFrequency) + bobSeed) * bobAmplitude;
        float noiseOffset = (Mathf.PerlinNoise(Time.time * 0.5f, bobSeed) - 0.5f) * (bobAmplitude * 0.5f);

        float targetY = flightHeight + sineOffset + noiseOffset;

        Vector3 currentPos = transform.position;
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * 3f);
        transform.position = currentPos;
    }

    private void HandleTilt()
    {
        if (helicopterModelTransform == null) { Debug.LogError("Helicopter model transform not appended!"); return; }

        float dt = Time.deltaTime;
        if (dt <= 0) return;

        Vector3 heliVel = (transform.position - prevPos) / dt;
        prevPos = transform.position;

        float fwdSpeed = Vector3.Dot(heliVel, transform.forward);
        float spdRatio = Mathf.Clamp01(fwdSpeed / moveSpeed);

        float targetPitch = spdRatio * maxPitchAngle;

        float currentYaw = transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(prevYaw, currentYaw);
        prevYaw = currentYaw;

        currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, yawDelta / dt, dt * 10f);

        float turnRatio = Mathf.Clamp(currentAngularVelocity / rotationSpeed, -1f, 1f);

        float targetRoll = -turnRatio * maxRollAngle;

        Quaternion targetLocalRot = Quaternion.Euler(targetPitch, 0f, targetRoll);
        helicopterModelTransform.localRotation = Quaternion.Slerp(
            helicopterModelTransform.localRotation,
            targetLocalRot,
            tiltSpeed * dt
        );
    }

    private void SetNewTargetBuilding()
    {
        if (gridManager.BuildingPositions.Count == 0) return;

        Vector2Int randomBuildingGridPos = gridManager.BuildingPositions[Random.Range(0, gridManager.BuildingPositions.Count)];

        float gridScale = gridManager.getGridScale();
        Vector3 randomBuildingWorldPos = new Vector3(randomBuildingGridPos.x * gridScale, 0f, randomBuildingGridPos.y * gridScale);

        float offsetX = Random.Range(-xzBounds.x, xzBounds.x);
        float offsetZ = Random.Range(-xzBounds.y, xzBounds.y);

        currentHelicopterMovementTargetPos = randomBuildingWorldPos + new Vector3(offsetX, 0f, offsetZ);
    }

    private IEnumerator StrikeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minIntervalBetweenStrikesSeconds, maxIntervalBetweenStrikesSeconds));

            if (healthSystem.GetHealth() < 100f) //Final bossfight fr
            {
                TryStrikeTurret();
            }
            else
            {
                StrikeRandomBuilding();
            }
        }
    }

    private void TryStrikeTurret()
    {
        if (millitaryManager.turretPositions.Length == 0) { StrikeRandomBuilding(); return; }

        Vector2Int randomTurretPos = millitaryManager.turretPositions[Random.Range(0, millitaryManager.turretPositions.Length)];

        InstantiateMissileToStrike(randomTurretPos);
    }

    private void StrikeRandomBuilding()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomBuildingGridPos = gridManager.BuildingPositions[Random.Range(0, gridManager.BuildingPositions.Count)];

        InstantiateMissileToStrike(randomBuildingGridPos);
    }

    private void InstantiateMissileToStrike(Vector2Int pos)
    {
        GameObject missile = Instantiate(missilePrefab, missileSpawnPoints[Random.Range(0, 2)].transform.position, Quaternion.identity, MilitaryManager.instance.transform);
        Missile missileScript = missile.GetComponent<Missile>();
        if (missileScript) missileScript.Initialise(pos);
        else { Debug.LogError("Missile Script not Found on missile!"); Destroy(missile); return; }
    }

    private void OnDeath()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        isDead = true;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        while (true)
        {
            if (transform.position.y < 1)
            {
                GameObject explosion = Instantiate(missilePrefab.GetComponent<Missile>().explosionPrefab, transform.position, Quaternion.identity, MilitaryManager.instance.transform);
                Destroy(explosion, 10f);
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }
}
