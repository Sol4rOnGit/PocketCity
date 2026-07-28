using UnityEngine;

public class BladeSpin : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject topBlades;
    [SerializeField] private GameObject tailRotor;

    [SerializeField] private int rotSpeedTop = 500;
    [SerializeField] private int rotSpeedTail = 800;

    private void Update()
    {
        topBlades.transform.Rotate(0f, 0f, rotSpeedTop * Time.deltaTime, Space.Self); //z 
        tailRotor.transform.Rotate(rotSpeedTail * Time.deltaTime, 0f, 0f, Space.Self); //x
    }
}
