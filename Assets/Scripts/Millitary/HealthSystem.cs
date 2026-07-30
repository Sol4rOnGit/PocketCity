using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private float MaxHealth = 500;
    private float Health;

    public Action onDeath;

    public void Awake()
    {
        SetInitialHealth();
    }

    private void SetInitialHealth()
    {
        if (GetComponent<Missile>() == null) { Health = MaxHealth; }
        else
        {
            if (GameManager.instance != null)
            {
                Health = Mathf.Max(100, GameManager.instance.daysPassed);
            }
        }
    }

    public float GetHealth() { return Health; }

    public float GetMaxHealth() { return MaxHealth; }
    public void Damage(float damage)
    {
        Health = Health - damage;

        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        onDeath?.Invoke();
    }
}
