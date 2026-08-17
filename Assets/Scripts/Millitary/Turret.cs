using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject rotatingBase;
    [SerializeField] private GameObject turretHead;
    [SerializeField] private GameObject muzzlePoint;
    [SerializeField] private Collider defenseRegionCollider;

    [SerializeField] private GameObject bulletTrailPrefab;
    [SerializeField] private LayerMask turretStrikeableLayerMask;

    [SerializeField] private AudioSource shotSoundSource;

    [Header("Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float fireRate = 15f;
    //[SerializeField] private float predAimingLeadTime = 0.3f;

    List<BaseEnemy> targetsInRange = new List<BaseEnemy>();
    private float nextTimeToFire = 0f;

    private Vector2Int gridPos = Vector2Int.zero;

    private void OnTriggerEnter(Collider other)
    {
        BaseEnemy target = other.GetComponentInParent<BaseEnemy>();
        if (target != null && !targetsInRange.Contains(target))
        {
            targetsInRange.Add(target);
            SortTargets();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BaseEnemy target = other.GetComponentInParent<BaseEnemy>();
        if (target != null && targetsInRange.Contains(target))
        {
            targetsInRange.Remove(target);
            SortTargets();
        }
    }

    private void Start()
    {
        float gridScale = GridManager.instance.getGridScale();
        gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x / gridScale), Mathf.RoundToInt(transform.position.z / gridScale));
        
        MilitaryManager.instance.turretPositions.Add(gridPos);
    }

    private void OnDisable()
    {
        MilitaryManager.instance.turretPositions.Remove(gridPos);
    }

    private void Update()
    {
        targetsInRange.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        ShootAtTarget();
    }

    private void ShootAtTarget()
    {
        if (targetsInRange.Count == 0)
        {
            StopShooting();
            return;
        }

        int turretIndex = MilitaryManager.instance.turretPositions.IndexOf(gridPos);
        bool UseAltTarget = (turretIndex + 1) % 3 == 0;

        int targetIndex = UseAltTarget ? targetsInRange.Count - 1 : 0;
        targetIndex = Mathf.Clamp(targetIndex, 0, targetsInRange.Count - 1);

        BaseEnemy currentTarget = targetsInRange[targetIndex];

        Vector3 predictedPos = GetPredictedPosition(currentTarget);
        AimAtTarget(predictedPos);
        
        if(Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + (1f / fireRate);
            StartShooting(currentTarget);
        }
    }

    private void AimAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        //Rotating base
        float yawDelta = Vector3.SignedAngle(rotatingBase.transform.up, direction, rotatingBase.transform.forward);
        float yawStep = Mathf.Clamp(yawDelta, -turnSpeed * Time.deltaTime, turnSpeed * Time.deltaTime);

        Vector3 baseEuler = rotatingBase.transform.localEulerAngles;
        baseEuler.z += yawStep;
        rotatingBase.transform.localEulerAngles = baseEuler;

        //Pitch of turret head
        float pitchDelta = Vector3.SignedAngle(turretHead.transform.up, direction, turretHead.transform.right);
        float currentPitch = turretHead.transform.localEulerAngles.x;

        if (currentPitch > 180f) currentPitch -= 360f;

        float targetPitch = Mathf.Clamp(currentPitch + pitchDelta, 0f, 45f);
        float pitchStep = Mathf.MoveTowards(currentPitch, targetPitch, turnSpeed * Time.deltaTime);
        turretHead.transform.localEulerAngles = new Vector3(pitchStep, 0f, 0f);


        //Rotation logic, max -45 degrees (means pointing up), min -90 degrees (pointing straight forward) for "turret head"
        //Z axis rotation on RotatingBase
        //should shoot out, and if immediate raycast hits the thing then deal damage to the BaseEnemy of the object
        //draw raycast as visualisation of bullet streak
    }

    private Vector3 GetPredictedPosition(BaseEnemy target)
    {
        float speed = target.TryGetComponent<MovingAttacker>(out var attacker) ? attacker.MoveSpeed : 0f;
        Vector3 velocity = target.transform.forward * speed;
        return target.transform.position + velocity * Time.deltaTime;
    }

    private void StartShooting(BaseEnemy currentEnemy)
    {
        Vector3 origin = muzzlePoint.transform.position;
        Vector3 direction = muzzlePoint.transform.up;

        //Debug.DrawRay(origin, direction * 50f, Color.red, 2f);
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 50f, turretStrikeableLayerMask))
        {
            BaseEnemy hitEnemy = hit.collider.GetComponentInParent<BaseEnemy>();
            if (hitEnemy != null)
            {
                hitEnemy.healthSystem.Damage(damage);
            }
            SpawnTrail(origin, hit.point);
        } else
        {
            SpawnTrail(origin, origin + direction * 50f);
        }

        //Audio
        shotSoundSource.Play();
    }

    private void SpawnTrail(Vector3 startVec3Pos, Vector3 endVec3Pos)
    {
        GameObject trail = Instantiate(bulletTrailPrefab, startVec3Pos, Quaternion.identity, muzzlePoint.transform);

        LineRenderer lineRenderer = trail.GetComponent<LineRenderer>();
        lineRenderer.SetPosition(0, startVec3Pos);
        lineRenderer.SetPosition(1, endVec3Pos);
        trail.AddComponent<BulletTrailFade>();
    }

    private void StopShooting()
    {
        //Bring pitch back down to 0 over time (coroutine?)
    }

    private void SortTargets()
    {
        targetsInRange.Sort((a, b) => b.DefensePriority.CompareTo(a.DefensePriority));
    }
}
