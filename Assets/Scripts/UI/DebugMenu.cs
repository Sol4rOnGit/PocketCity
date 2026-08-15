using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

public class DebugMenu : MonoBehaviour
{
    [Header("Enable/Disable")]
    [SerializeField] private GameObject DebugPanel;
    public InputActionAsset inputActions;
    private InputActionMap UIMap;
    private InputAction toggleDebugMenuAction;
    private bool isEnabled = false;

    [Header("Framerate")]
    [SerializeField] private TMPro.TextMeshProUGUI DisplayFPS;
    [SerializeField] private TMPro.TextMeshProUGUI DisplayFrametime;
    private float averageFPS;
    private float currentMinFPS;
    private float[] secondMins = new float[5];
    private int bufferIndex;

    private float secondTimer = 0f;
    private int framesInCurrentSec = 0;
    private float lowestInCurrentSec = float.MaxValue;

    [Header("System")]
    [SerializeField] private TMPro.TextMeshProUGUI DisplayRAM;
    [SerializeField] private TMPro.TextMeshProUGUI DisplayVRAM;

    [Header("Engine")]
    [SerializeField] private TMPro.TextMeshProUGUI DisplayGameObjCount;
    [SerializeField] private TMPro.TextMeshProUGUI DisplayActiveRbs;
    [SerializeField] private TMPro.TextMeshProUGUI DisplayTimeScale;

    private void Start()
    {
        UIMap = inputActions.FindActionMap("UI");
        toggleDebugMenuAction = UIMap.FindAction("DebugMenu");
    }

    void Update()
    {
        HandleInput();

        if (isEnabled)
        {
            UpdateFramerateStats();
            UpdateSystemStats();
            UpdateEngineStats();
        }
    }

    private void HandleInput()
    {
        if (toggleDebugMenuAction.WasPressedThisFrame())
        {
            isEnabled = !isEnabled;
            DebugPanel.SetActive(isEnabled);
        }
    }

    private void UpdateFramerateStats()
    {
        float uDt = Time.unscaledDeltaTime;
        if (uDt <= 0) return;
 
        float currentFPS = 1f/ uDt;
        secondTimer += uDt;
        framesInCurrentSec++;
        
        if (currentFPS < lowestInCurrentSec)
        {
            lowestInCurrentSec = currentFPS;
        }

        if (secondTimer >= 1)
        {
            averageFPS = framesInCurrentSec / secondTimer;
            secondMins[bufferIndex] = lowestInCurrentSec;
            bufferIndex = (bufferIndex + 1) % secondMins.Length;

            currentMinFPS = float.MaxValue;
            for (int i = 0; i < secondMins.Length; i++)
            {
                if (secondMins[i] < currentMinFPS && secondMins[i] > 0f)
                {
                    currentMinFPS = secondMins[i];
                }
            }

            if (DisplayFPS != null) DisplayFPS.text = $"FPS: {Mathf.RoundToInt(currentFPS)} | Avg {Mathf.RoundToInt(averageFPS)} | Min {Mathf.RoundToInt(currentMinFPS)}";
            if (DisplayFrametime != null) DisplayFrametime.text = $"Latency (ms): {Mathf.RoundToInt(uDt * 1000f)}";

            secondTimer = 0;
            framesInCurrentSec = 0;
            lowestInCurrentSec = float.MaxValue;
        } else
        {
            if (DisplayFPS != null) DisplayFPS.text = $"FPS: {Mathf.RoundToInt(currentFPS)} | Avg {Mathf.RoundToInt(averageFPS)} | Min {Mathf.RoundToInt(currentMinFPS)}";
        }
    }

    private void UpdateSystemStats()
    {
        DisplayRAM.text = $"RAM Usage (MB): {Profiler.GetTotalReservedMemoryLong() / 1048576}";

        long vramAllocated = Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576;
        if (vramAllocated > 0)
        {
            DisplayVRAM.text = $"VRAM Usage (MB): {Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576}/{SystemInfo.graphicsMemorySize}";
        } else
        {
            DisplayVRAM.text = $"VRAM (MB): {SystemInfo.graphicsMemorySize} [Can't get Usage]";
        }
        
    }

    private void UpdateEngineStats()
    {
        DisplayGameObjCount.text = $"GameObject Count: {FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length:N0}";
        DisplayActiveRbs.text = $"Active RBs: {FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length}";
        DisplayTimeScale.text = $"Time Scale: {Time.timeScale:0.00}";
    }
}
