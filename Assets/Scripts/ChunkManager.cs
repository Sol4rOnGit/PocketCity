using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Boolean showTrees;

    [Header("Requirements")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform playerTransform;

    [Header("Chunk settings")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int viewDistance = 5;

    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();
    Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    float scale;

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

                if(!generatedChunks.Contains(chunkCord))
                {
                    GenerateChunkEnvironemnt(chunkCord);
                    generatedChunks.Add(chunkCord);
                }
            }
        }
    }

    private void GenerateChunkEnvironemnt(Vector2Int chunkCord)
    {
        //Use the code currently in GridManager(), move it all here

        int startGridX = chunkCord.x * chunkSize;
        int endGridX = startGridX + chunkSize;
        int startGridY = chunkCord.y * chunkSize;
        int endGridY = startGridY + chunkSize;

        for (int x = startGridX; x <= endGridX; x++)
        {
            for (int y = startGridY; y <= endGridY; y++)
            {
                Vector2Int tilePos = new Vector2Int(x, y);

                if(gridManager.GetMapGrid().ContainsKey(tilePos)) continue;
                if(gridManager.TreeGrid.ContainsKey(tilePos)) continue;

                int roll = UnityEngine.Random.Range(0, 100);
                if (roll <= 40) { continue; }

                Vector3 worldpos = new Vector3(x * scale, 0f, y * scale);
                Quaternion randomRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 4) * 90, 0f);

                gridManager.SpawnTreeInChunk(tilePos, worldpos, randomRotation);
            }
        }
    }

    private Vector2Int GetChunkFromPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / scale);
        int gridY = Mathf.RoundToInt(worldPos.z / scale);

        int chunkX = Mathf.FloorToInt((float)gridX / chunkSize);
        int chunkY = Mathf.FloorToInt((float)gridY / chunkSize);

        return new Vector2Int(chunkX, chunkY);
    }
}
