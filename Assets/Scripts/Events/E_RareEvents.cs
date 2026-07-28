using UnityEngine;

public class E_RareEvents : MonoBehaviour
{
    [Header("Millitary")]
    [SerializeField] private GameObject attackHelicopterPrefab;

    public void SummonAttackHelicopter()
    {
        Instantiate(attackHelicopterPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity, transform);
    }
}
