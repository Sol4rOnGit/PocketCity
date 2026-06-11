using UnityEditor.Build;
using UnityEngine;

public class House : Building
{
    [Header("Building Vars")]
    [SerializeField] private int _maxResidents = 5;
    public int maxResidents => _maxResidents;
    public int residents;
    public float happiness; //0-1
}