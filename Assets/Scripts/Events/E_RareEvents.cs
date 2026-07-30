using UnityEngine;

public class E_RareEvents : MonoBehaviour
{
    [Header("Millitary")]
    [SerializeField] private GameObject attackHelicopterPrefab;
    [SerializeField] private GameObject B2BomberPrefab;

    private Transform spawnTransform = MilitaryManager.instance.transform;

    public void SummonAttackHelicopter()
    {
        Instantiate(attackHelicopterPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity, spawnTransform);
    }

    public void SummonB2Bomber()
    {
        Instantiate(B2BomberPrefab, new Vector3(200f, 0f, 200f), Quaternion.identity, spawnTransform);
    }

    public void MilitaryInvasion()
    {
        Instantiate(attackHelicopterPrefab, new Vector3(100f, 10f, 100f), Quaternion.identity, spawnTransform);
        Instantiate(attackHelicopterPrefab, new Vector3(-100f, 10f, -100f), Quaternion.identity, spawnTransform);
        Instantiate(attackHelicopterPrefab, new Vector3(-100f, 10f, 100f), Quaternion.identity, spawnTransform);

        float chanceOfCloseHelicopter = 0.3f;
        if (Random.value > chanceOfCloseHelicopter)
        {
            Instantiate(attackHelicopterPrefab, new Vector3(100f, 10f, -100f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(10f, 10f, -10f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(-10f, 10f, 10f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity, spawnTransform);
        }

        if (Random.value > 0.6) SummonB2Bomber(); //40% chance
    }
}
