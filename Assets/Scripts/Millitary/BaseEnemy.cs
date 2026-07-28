using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class BaseEnemy : MonoBehaviour
{
    [Header("Dependenices")]
    [SerializeField] public HealthSystem healthSystem;

    [Header("Variables")]
    [SerializeField, Range(1, 10)] private int defensePriority = 1; //help determine what to target for turret
    public int DefensePriority => defensePriority;

    protected virtual void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
    }
}
