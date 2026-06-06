using UnityEngine;
using UnityEngine.UIElements;

public enum BuildingType { Residential, Industrial, Commercial}

public class Building : MonoBehaviour
{
    [Header("Shared properties")]
    public string buildingName;
    public int constructionCost;
    public int destructionCost;
    public BuildingType type;
    public Vector2Int gridPosition;

    public void SetPos(Vector2Int Pos)
    {
        gridPosition = Pos;
    }
    public void Alert()
    {
        Debug.Log("Message the game that I require attention.");
    }
}