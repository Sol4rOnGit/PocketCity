using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public class House : Building
{
    [Header("Building Vars")]
    [SerializeField] private int _maxResidents = 5;
    public int maxResidents => _maxResidents;
    public int residents;
    public float happiness; //0-100

    private int daysWithLowHappiness = 10000;

    public void OnEnable()
    {
        if (ChunkManager.instance != null) { ChunkManager.instance.BuildingUtilitiesUpdated += OnUtilities; }
    }

    public void OnDisable()
    {
        if (ChunkManager.instance != null) { ChunkManager.instance.BuildingUtilitiesUpdated -= OnUtilities; }
    }

    public void Start()
    {
        OnUtilities(); //Destroy if bad neighbourhood
    }

    public void OnUtilities()
    {
        if (ChunkManager.instance == null) return;

        ChunkManager.ChunkData chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
        if (chunk == null) return;

        this.happiness = chunk.averageHappiness;

        if (this.happiness < 20f)
        {
            daysWithLowHappiness++;

            if (daysWithLowHappiness >= 3)
            {
                GameManager.instance.gridManager.forceRemoveElement(gridPos);
            }
        }
        else
        {
            daysWithLowHappiness = 0;
        }
    }
}