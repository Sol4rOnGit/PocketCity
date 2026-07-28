using UnityEngine;

public class MilitaryManager : MonoBehaviour
{
    public static MilitaryManager instance { get; private set; }
    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    public GameObject turretPrefab;

    public Vector2Int[] turretPositions;
}
