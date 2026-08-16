using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Utilities, Nature managed in chunks so all the logic is here

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager instance { get; private set; }

    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform playerTransform;

    [Header("Global power/water variables")]
    public int GlobalPowerCapacity { get; private set; }
    public int GlobalPowerDemand { get; private set; }
    public int GlobalWaterCapacity { get; private set; }
    public int GlobalWaterDemand { get; private set; }

    [Header("Tree Settings")]
    [SerializeField] private Mesh[] treeMesh;
    [SerializeField] private Material treeMaterial;

    [SerializeField] private bool showTrees;
    private HashSet<Vector2Int> allTreePositions = new HashSet<Vector2Int>();

    public struct TreeInstance
    {
        public Vector2Int gridPos;
        public Matrix4x4 matrix;
    }

    [Header("Happiness settings")]
    [SerializeField] private float baselineHappiness = 0f;

    [Header("Chunk settings")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int viewDistance = 3;

    private Dictionary<Vector2Int, ChunkData> generatedChunks = new Dictionary<Vector2Int, ChunkData>();
    Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    float scale;

    public Action BuildingUtilitiesUpdated;

    public class ChunkData
    {
        public Vector2Int chunkCord;
        public List<TreeInstance> spawnedTrees = new List<TreeInstance>();

        public int powerGenerated;
        public int powerImported;
        public int powerConsumed;

        public int waterGenerated;
        public int waterImported;
        public int waterConsumed;

        public bool HasEnoughPower => powerGenerated + powerImported >= powerConsumed;
        public bool HasEnoughWater => waterGenerated + waterImported >= waterConsumed;

        public float averageHappiness = 50f;

        public ChunkData(Vector2Int cords)
        {
            chunkCord = cords;
        }
    }

    private void Start()
    {
        showTrees = PlayerPrefs.GetInt("TreeVisibility", 1) == 1;

        //Fallback
        if (playerTransform == null) { playerTransform = Camera.main.transform; }
        scale = gridManager.getGridScale();

        //Initial
        if (showTrees) { UpdateChunks(); }
    }

    private void Update()
    {
        if (!showTrees) { return; }
        DrawTreeMeshes();
        HandlePlayerChunkLoader();
    }

    private void HandlePlayerChunkLoader()
    {
        //if differrent to last chunk, update chunks around player.
        Vector2Int currentPlayerChunk = GetChunkFromPosition(playerTransform.position);

        if (currentPlayerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentPlayerChunk;
            UpdateChunks();
        }
    }

    private void UpdateChunks()
    {
        //check all chunks around player, generate
        for (int xOffset = -viewDistance; xOffset <= viewDistance; xOffset++)
        {
            for (int yOffset = -viewDistance; yOffset <= viewDistance; yOffset++)
            {
                Vector2Int chunkCord = new Vector2Int(lastPlayerChunk.x + xOffset, lastPlayerChunk.y + yOffset);

                if(!generatedChunks.ContainsKey(chunkCord))
                {
                    ChunkData newChunk = new ChunkData(chunkCord); 
                    generatedChunks.Add(chunkCord, newChunk);
                    GenerateChunkEnvironemnt(newChunk);
                }
            }
        }
    }

    private void GenerateChunkEnvironemnt(ChunkData chunk)
    {
        if (!showTrees) return;
        //Use the code currently in GridManager(), move it all here

        int startGridX = chunk.chunkCord.x * chunkSize;
        int endGridX = startGridX + chunkSize;
        int startGridY = chunk.chunkCord.y * chunkSize;
        int endGridY = startGridY + chunkSize;

        for (int x = startGridX; x < endGridX; x++)
        {
            for (int y = startGridY; y < endGridY; y++)
            {
                Vector2Int tilePos = new Vector2Int(x, y);

                if(gridManager.GetMapGrid().ContainsKey(tilePos)) continue;
                if(allTreePositions.Contains(tilePos)) continue;

                int roll = UnityEngine.Random.Range(0, 100);
                if (roll <= 40) { continue; }

                Vector3 worldpos = new Vector3(x * scale, 0f, y * scale);
                Quaternion randomRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90, 0f);

                TreeInstance newTree = new TreeInstance
                {
                    gridPos = tilePos,
                    matrix = Matrix4x4.TRS(worldpos, randomRotation, Vector3.one)
                };
                chunk.spawnedTrees.Add(newTree);
                allTreePositions.Add(tilePos);
            }
        }
    }

    private void calculateHappiness(ChunkData chunk)
    {
        float currentHappiness = 50f + baselineHappiness;

        if (!chunk.HasEnoughPower) { currentHappiness -= 30f; }
        if (!chunk.HasEnoughWater) { currentHappiness -= 30f; }

        chunk.averageHappiness = Mathf.Clamp(currentHappiness, 0f, 100f);

        //Debug.Log($"Chunk happiness: {chunk.averageHappiness} from {chunk.HasEnoughWater} & {chunk.HasEnoughPower}. with {baselineHappiness} baseline");
    }

    //Tree

    private void OnEnable()
    {
        GameManager.instance.OnTreeVisibilityChanged += HandleTreeVisChanged;
    }

    private void OnDisable()
    {
        GameManager.instance.OnTreeVisibilityChanged -= HandleTreeVisChanged;
    }

    private void HandleTreeVisChanged(bool visibility)
    {
        showTrees = visibility;

        foreach (var kvp in generatedChunks)
        {
            //SetChunksTreesActive(kvp.Value, showTrees);
        }

        if (showTrees)
        {
            UpdateChunks();
        }
    }

    private void DrawTreeMeshes()
    {
        if (treeMesh == null || treeMaterial == null) return;

        foreach (var kvp in generatedChunks)
        {
            ChunkData chunk = kvp.Value;
            if (chunk.spawnedTrees == null || chunk.spawnedTrees.Count == 0) continue;

            int batchSize = 1023;
            int totalTrees = chunk.spawnedTrees.Count;

            for(int i = 0; i < totalTrees; i+=batchSize)
            {
                int length = Mathf.Min(batchSize, totalTrees - i);
                Matrix4x4[] subMatrices = new Matrix4x4[length];

                for (int j = 0; j < length; j++)
                {
                    subMatrices[j] = chunk.spawnedTrees[i + j].matrix;
                }

                int meshIndex = Mathf.Abs(chunk.chunkCord.GetHashCode() + i) % treeMesh.Length;

                Graphics.DrawMeshInstanced(treeMesh[meshIndex], 0, treeMaterial, subMatrices, length);
            }
        }
    }

    public void ClearTreeAtPos(Vector2Int gridPos)
    {
        if (!allTreePositions.Contains(gridPos)) return;

        allTreePositions.Remove(gridPos);

        foreach (var kvp in generatedChunks) {
            ChunkData chunk = kvp.Value;
            int removedCount = chunk.spawnedTrees.RemoveAll(t => t.gridPos == gridPos);
            if (removedCount > 0) break;
        }
    }

    //Public functions

    public void DistributeUtilitiesAcrossCity()
    {
        int globalPowerSurplusPool = 0;
        int globalWaterSurplusPool = 0;

        int totalGlobalPower = 0;
        int totalGlobalWater = 0;

        int totalGlobalPowerDemand = 0;
        int totalGlobalWaterDemand = 0;

        List<ChunkData> powerDeficitChunks = new List<ChunkData>();
        List<ChunkData> waterDeficitChunks = new List<ChunkData>();

        foreach (var kvp in generatedChunks)
        {
            ChunkData chunk = kvp.Value;

            chunk.powerImported = 0;
            chunk.waterImported = 0;

            totalGlobalPower += chunk.powerGenerated;
            totalGlobalWater += chunk.waterGenerated;

            totalGlobalPowerDemand += chunk.powerConsumed;
            totalGlobalWaterDemand += chunk.waterConsumed;

            int localPowerBalance = chunk.powerGenerated - chunk.powerConsumed;
            if (localPowerBalance > 0) globalPowerSurplusPool += localPowerBalance;
            else if (localPowerBalance < 0) powerDeficitChunks.Add(chunk);

            int localWaterBalance = chunk.waterGenerated - chunk.waterConsumed;
            if (localWaterBalance > 0) globalWaterSurplusPool += localWaterBalance;
            else if (localWaterBalance < 0) waterDeficitChunks.Add(chunk);
        }

        GlobalPowerCapacity = totalGlobalPower;
        GlobalWaterCapacity = totalGlobalWater;

        GlobalPowerDemand = totalGlobalPowerDemand;
        GlobalWaterDemand = totalGlobalWaterDemand;

        powerDeficitChunks.Sort((a, b) => (b.powerConsumed - b.powerGenerated).CompareTo(a.powerConsumed - a.powerGenerated));
        waterDeficitChunks.Sort((a, b) => (b.waterConsumed - b.waterGenerated).CompareTo(a.waterConsumed - a.waterGenerated));

        foreach (ChunkData chunk in powerDeficitChunks)
        {
            //Power
            int powerDeficit = chunk.powerConsumed - chunk.powerGenerated;

            if (globalPowerSurplusPool >= powerDeficit)
            {
                chunk.powerImported = powerDeficit;
                globalPowerSurplusPool -= powerDeficit;
            }
            else
            {
                chunk.powerImported = 0;
            }
        }

        foreach (ChunkData chunk in waterDeficitChunks)
        {
            //Water
            int waterDefecit = chunk.waterConsumed - chunk.waterGenerated;

            if (globalWaterSurplusPool >= waterDefecit)

            {
                chunk.waterImported = waterDefecit;
                globalWaterSurplusPool -= waterDefecit;
            }
            else
            {
                chunk.waterImported = 0;
            }
        }

        foreach (var kvp in generatedChunks)
        {
            calculateHappiness(kvp.Value);
        }

        BuildingUtilitiesUpdated?.Invoke();

        //if (totalGlobalWater > 0) { return; } //Sell the water here later
        //if (totalGlobalPower > 0) { return; } //Sell the energy here later
    }

    public ChunkData GetChunkFromGridTile(Vector2Int gridPos)
    {
        int chunkX = gridPos.x >= 0 ? gridPos.x / chunkSize : (gridPos.x - chunkSize + 1) / chunkSize;
        int chunkY = gridPos.y >= 0 ? gridPos.y / chunkSize : (gridPos.y - chunkSize + 1) / chunkSize;
        Vector2Int chunkcord = new Vector2Int(chunkX, chunkY);

        if (generatedChunks.TryGetValue(chunkcord, out ChunkData chunkData))
        {
            return chunkData;
        }

        //Create new chunk if failed to find
        ChunkData newChunk = new ChunkData(chunkcord);
        generatedChunks.Add(chunkcord, newChunk);
        GenerateChunkEnvironemnt(newChunk);
        return newChunk;
    }

    public void AddBuildingToChunk(Vector2Int gridPos, int powerGen, int powerCons, int waterGen, int waterCons)
    {
        ChunkData chunk = GetChunkFromGridTile(gridPos);

        chunk.powerGenerated += powerGen;
        chunk.powerConsumed += powerCons;

        chunk.waterGenerated += waterGen;
        chunk.waterConsumed += waterCons;

        //DistributeUtilitiesAcrossCity(); //infinite loop fixed?
    }

    public void RemoveBuildingFromChunk(Vector2Int gridPos, int powerGen, int powerCons, int waterGen, int waterCons)
    {
        ChunkData chunk = GetChunkFromGridTile(gridPos);

        chunk.powerGenerated -= powerGen;
        chunk.powerConsumed -= powerCons;

        chunk.waterGenerated -= waterGen;
        chunk.waterConsumed -= waterCons;

        //DistributeUtilitiesAcrossCity(); //infinite loop fixed?
    }

    //Public event functions
    public void IncreaseBaselineHappiness(float increase)
    {
        baselineHappiness += increase;
    }
    public IEnumerator IncreasePowerDemandTemporarily(int power, float seconds)
    {
        if (generatedChunks.Count == 0) { yield break; }

        List<ChunkData> affectedChunks = new List<ChunkData>();
        List<Vector2Int> allChunkKeys = new List<Vector2Int>(generatedChunks.Keys);

        int chunksToAffect = allChunkKeys.Count / UnityEngine.Random.Range(4, 8); //1/4 to 1/8 of all chunks
        if (chunksToAffect == 0) { chunksToAffect = 1; } //atleast 1!

        int powerPerChunk = power / chunksToAffect;
        if (powerPerChunk <= 100) { powerPerChunk = 100; }

        for (int i = 0; i < chunksToAffect; i++)
        {
            if (allChunkKeys.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, allChunkKeys.Count);
            Vector2Int key = allChunkKeys[randomIndex];
            allChunkKeys.RemoveAt(randomIndex);

            ChunkData chunk = generatedChunks[key];

            chunk.powerConsumed += powerPerChunk;
            affectedChunks.Add(chunk);
        }

        DistributeUtilitiesAcrossCity();
        Debug.Log($"Surge active with {chunksToAffect} chunks loaded with an additional {powerPerChunk} MW each!");

        Debug.Log($"Demand is now {GlobalPowerDemand}");

        yield return new WaitForSeconds(seconds);

        foreach (ChunkData chunk in affectedChunks)
        {
            chunk.powerConsumed -= powerPerChunk;
        }

        DistributeUtilitiesAcrossCity();

        Debug.Log($"Demand is now {GlobalPowerDemand}, should be normalised due to end of surge");
    }

    public IEnumerator IncreaseWaterAndPowerSupplyTemporarily(int perChunkValue, float seconds)
    {
        if (generatedChunks.Count == 0) { yield break; }

        List<ChunkData> affectedChunks = new List<ChunkData>();
        List<Vector2Int> allChunkKeys = new List<Vector2Int>(generatedChunks.Keys);

        if (perChunkValue <= 100000) { perChunkValue = 100000; }

        int count = allChunkKeys.Count;
        for (int i = 0; i < allChunkKeys.Count; i++)
        {
            if (allChunkKeys.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, allChunkKeys.Count);
            Vector2Int key = allChunkKeys[randomIndex];
            allChunkKeys.RemoveAt(randomIndex);

            ChunkData chunk = generatedChunks[key];

            chunk.powerGenerated += perChunkValue;
            chunk.waterGenerated += perChunkValue;

            affectedChunks.Add(chunk);
        }

        DistributeUtilitiesAcrossCity();

        yield return new WaitForSeconds(seconds);

        foreach (ChunkData chunk in affectedChunks)
        {
            chunk.powerGenerated -= perChunkValue;
            chunk.waterGenerated -= perChunkValue;
        }

        DistributeUtilitiesAcrossCity();
    }

    //Helper functions

    private Vector2Int GetChunkFromPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / scale);
        int gridY = Mathf.RoundToInt(worldPos.z / scale);

        int chunkX = gridX >= 0 ? gridX / chunkSize : (gridX - chunkSize + 1) / chunkSize;
        int chunkY = gridY >= 0 ? gridY / chunkSize : (gridY - chunkSize + 1) / chunkSize;

        return new Vector2Int(chunkX, chunkY);
    }

    /*private void SetChunksTreesActive(ChunkData chunk, bool active)
    {
        foreach (GameObject tree in chunk.spawnedTrees)
        {
            if (tree != null) tree.SetActive(active);
        }
    }*/
}
