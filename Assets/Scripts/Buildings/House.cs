using UnityEngine;

public class House : Building
{
    [Header("Building Vars")]
    public int maxResidents { get; private set; }
    public int residents;
    public float happiness; //0-1
}