using System.Collections.Generic;
using UnityEngine;

public class MilitaryManager : MonoBehaviour
{
    public static MilitaryManager instance { get; private set; }
    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public GameObject turretPrefab;

    public List<Vector2Int> turretPositions = new List<Vector2Int>();
}
