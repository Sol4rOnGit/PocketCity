using UnityEngine;

public class House : Building
{
    [Header("Building Vars")]
    [SerializeField] private int maxResidents;
    public int residents;
    public float happiness; //0-1
}