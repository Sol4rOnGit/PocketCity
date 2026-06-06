using UnityEngine;

public class House : Building
{
    public int residents;
    public int maxResidents;
    public float happiness; //0-1

    public void SetupHouse(Vector2Int pos, int maxResidents)
    {
        SetPos(pos);
        this.maxResidents = maxResidents;
        this.happiness = 1.0f;
    }
}