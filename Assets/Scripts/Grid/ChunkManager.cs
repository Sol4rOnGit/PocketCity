using System;
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
    public int GlobalPowerCapacity { get; private set; }
    public int GlobalPowerDemand { get; private set; }
    public int GlobalWaterCapacity { get; private set; }
    public int GlobalWaterDemand { get; private set; }

    [Header("Toggle")]
    [SerializeField] private Boolean showTrees;

    [Header("Requirements")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform playerTransform;

    [Header("Chunk settings")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int viewDistance = 5;

    private Dictionary<Vector2Int, ChunkData> generatedChunks = new Dictionary<Vector2Int, ChunkData>();
    Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    float scale;

    public Action BuildingUtilitiesUpdated;

    public class ChunkData
    {
        public Vector2Int chunkCord;

        public int powerGenerated;
        public int powerImported;
        public int powerConsumed;

        public int waterGenerated;
        public int waterImported;
        public int waterConsumed;

        public bool HasEnoughPower => powerGenerated + powerImported >= powerConsumed;
        public bool HasEnoughWater => waterGenerated + waterImported >= waterConsumed;

        public float averageHappiness = 50f;
        public int treeCount = 0;

        public ChunkData(Vector2Int cords)
        {
            chunkCord = cords;
        }
    }

    private void Start()
    {
        //Fallback
        if (playerTransform == null) { playerTransform = Camera.main.transform; }
        scale = gridManager.getGridScale();

        //Initial
        if (showTrees) { UpdateChunks(); }
    }

    private void Update()
    {
        if (!showTrees) { return; }
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
                    GenerateChunkEnvironemnt(newChunk);
                    generatedChunks.Add(chunkCord, newChunk);
                }
            }
        }
    }

    private void GenerateChunkEnvironemnt(ChunkData chunk)
    {
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
                if(gridManager.TreeGrid.ContainsKey(tilePos)) continue;

                int roll = UnityEngine.Random.Range(0, 100);
                if (roll <= 40) { continue; }

                Vector3 worldpos = new Vector3(x * scale, 0f, y * scale);
                Quaternion randomRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90, 0f);

                gridManager.SpawnTreeInChunk(tilePos, worldpos, randomRotation);

                chunk.treeCount++; //NEED TO GO AND UPDATE GRIDMANAGER TO DECREMENT THIS NUMBER
            }
        }
    }

    public void DistributeUtilitiesAcrossCity()
    {
        int totalGlobalPower = 0;
        int totalGlobalWater = 0;

        int totalGlobalPowerDemand = 0;
        int totalGlobalWaterDemand = 0;

        foreach (var kvp in generatedChunks)
        {
            ChunkData chunk = kvp.Value;

            chunk.powerImported = 0;
            chunk.waterImported = 0;

            totalGlobalPower += chunk.powerGenerated;
            totalGlobalWater += chunk.waterGenerated;

            totalGlobalPowerDemand += chunk.powerConsumed;
            totalGlobalWaterDemand += chunk.waterConsumed;
        }

        GlobalPowerCapacity = totalGlobalPower;
        GlobalWaterCapacity = totalGlobalWater;

        GlobalPowerDemand = totalGlobalPowerDemand;
        GlobalWaterDemand = totalGlobalWaterDemand;

        foreach (var kvp in generatedChunks)
        {
            ChunkData chunk = kvp.Value;

            //Power
            int powerDeficit = chunk.powerConsumed - chunk.powerGenerated;
            if (powerDeficit > 0)
            {
                if (totalGlobalPower >= powerDeficit)
                {
                    chunk.powerImported = powerDeficit;
                    totalGlobalPower -= powerDeficit;
                }
                else
                {
                    chunk.powerImported = totalGlobalPower;
                    totalGlobalPower = 0;
                }
            }

            //Water
            int waterDefecit = chunk.waterConsumed - chunk.waterGenerated;
            if (waterDefecit > 0)
            {
                if (totalGlobalWater >= waterDefecit)
                {
                    chunk.waterImported += waterDefecit;
                    totalGlobalWater -= waterDefecit;
                } else
                {
                    chunk.waterImported = totalGlobalWater;
                    totalGlobalWater = 0;
                }
            }

            calculateHappiness(chunk);
        }

        BuildingUtilitiesUpdated?.Invoke();

        //if (totalGlobalWater > 0) { return; } //Sell the water here later
        //if (totalGlobalPower > 0) { return; } //Sell the energy here later
    }

    public ChunkData GetChunkFromGridTile(Vector2Int gridPos)
    {
        int chunkX = Mathf.FloorToInt((float)gridPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt((float)gridPos.y / chunkSize);
        Vector2Int chunkcord = new Vector2Int(chunkX, chunkY);

        if (generatedChunks.TryGetValue(chunkcord, out ChunkData chunkData))
        {
            return chunkData;
        }

        //Create new chunk if failed to find
        ChunkData newChunk = new ChunkData(chunkcord);
        generatedChunks.Add(chunkcord, newChunk);
        return newChunk;
    }

    public void calculateHappiness(ChunkData chunk)
    {
        float currentHappiness = 50f;

        if (!chunk.HasEnoughPower) { currentHappiness -= 30f; }
        if (!chunk.HasEnoughWater) { currentHappiness -= 30f; }

        //Nature bonus
        currentHappiness += (chunk.treeCount / 5f) * 2f;

        chunk.averageHappiness = Mathf.Clamp(currentHappiness, 0f, 100f);
    }

    private Vector2Int GetChunkFromPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / scale);
        int gridY = Mathf.RoundToInt(worldPos.z / scale);

        int chunkX = Mathf.FloorToInt((float)gridX / chunkSize);
        int chunkY = Mathf.FloorToInt((float)gridY / chunkSize);

        return new Vector2Int(chunkX, chunkY);
    }

    public void AddBuildingToChunk(Vector2Int gridPos, int powerGen, int powerCons, int waterGen, int waterCons)
    {
        ChunkData chunk = GetChunkFromGridTile(gridPos);

        chunk.powerGenerated += powerGen;
        chunk.powerConsumed += powerCons;

        chunk.waterGenerated += waterGen;
        chunk.waterConsumed += waterCons;

        DistributeUtilitiesAcrossCity();
    }

    public void RemoveBuildingFromChunk(Vector2Int gridPos, int powerGen, int powerCons, int waterGen, int waterCons)
    {
        ChunkData chunk = GetChunkFromGridTile(gridPos);

        chunk.powerGenerated -= powerGen;
        chunk.powerConsumed -= powerCons;

        chunk.waterGenerated -= waterGen;
        chunk.waterConsumed -= waterCons;

        DistributeUtilitiesAcrossCity();
    }
}
