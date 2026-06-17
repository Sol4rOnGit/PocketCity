using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridPlayerManager gridPlayerManager;

    [Header("Basic UI")]
    //public Boolean Enabled = true;
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;
    [SerializeField] private TMPro.TextMeshProUGUI addedMoneyUIText;

    [SerializeField] private Slider dayProgressBar;

    [SerializeField] private TMPro.TextMeshProUGUI userNotificationUIText;
    private Coroutine notifRoutine;

    [SerializeField] private TMPro.TextMeshProUGUI cursorPosUIText;

    [Header("Stats UI")]
    [SerializeField] private GameObject CityStatsPanel;
    private bool statsPanelActive;
    [SerializeField] private TMPro.TextMeshProUGUI PopulationUIText;
    [SerializeField] private TMPro.TextMeshProUGUI UnemployedUIText;
    [SerializeField] private TMPro.TextMeshProUGUI VacanciesUIText;
    [SerializeField] private TMPro.TextMeshProUGUI MaintenanceUIText;
    [SerializeField] private TMPro.TextMeshProUGUI DisastersSurvivedUIText;

    [SerializeField] private TMPro.TextMeshProUGUI PowerDeltaUIText;
    [SerializeField] private TMPro.TextMeshProUGUI WaterDeltaUIText;

    [Header("ZoningVars")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string zoningLayerName = "ZoningVisual";
    public InputActionAsset inputActions;
    InputAction toggleZoningUI;
    InputAction toggleStatsPanelUI;

    FinanceManager financeManager;

    private void Awake()
    {
        financeManager = GameObject.FindAnyObjectByType<FinanceManager>();

        if (financeManager == null) { Debug.LogError("UIManager couldn't find the Finance Manager."); }
    }

    private void Start()
    {
        InputActionMap UIMap = inputActions.FindActionMap("UI");
        toggleZoningUI = UIMap.FindAction("ToggleZoningUI");
        toggleStatsPanelUI = UIMap.FindAction("ToggleStatsPanel");

        statsPanelActive = CityStatsPanel.activeSelf;

        updateCurrentMoneyUI(financeManager.currentMoney);
    }

    private void Update()
    {
        HandleUserInput();
        HandleStatsUpdate();
    }

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UserNotification += NotifyUser;
            GameManager.instance.OnDayProgress += UpdateDayProgressBar;

        } else { Debug.LogError("No game manager!"); }
        if (financeManager != null) financeManager.OnMoneyChanged += updateCurrentMoneyUI; else Debug.LogError("No finance manager!");

        if (gridPlayerManager != null) gridPlayerManager.newCursorPosition += UpdateCursorPosition; else Debug.LogError("No grid player manager!");
    }

    private void OnDisable()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.UserNotification -= NotifyUser;
            GameManager.instance.OnDayProgress -= UpdateDayProgressBar;

        }
        else { Debug.LogError("No game manager!"); }
        if (financeManager != null) financeManager.OnMoneyChanged -= updateCurrentMoneyUI;
        if (gridPlayerManager != null) gridPlayerManager.newCursorPosition -= UpdateCursorPosition;
    }
    
    private void HandleUserInput()
    {
        if (toggleZoningUI.WasPressedThisFrame()) { ToggleZoningLayer(); }
        if (toggleStatsPanelUI.WasPressedThisFrame()) { ToggleStatsPanel(); }
    }

    public void updateCurrentMoneyUI(long currentMoney)
    {
        if (currentMoneyUIText == null) { Debug.LogError("Missing UI reference."); return; }

        long delta = currentMoney - financeManager.prevMoney;

        string formattedMoney = ReturnTextFromMoney(currentMoney);
        string deltaMoney = (delta > 0) ? $"+{ReturnTextFromMoney(delta)}" : $"{ReturnTextFromMoney(delta)}";

        currentMoneyUIText.text = formattedMoney;
        addedMoneyUIText.text = deltaMoney;
    }

    private void ToggleZoningLayer()
    {
        if (playerCamera == null) { Debug.LogError("Camera not found."); return; }

        int layerIndex = LayerMask.NameToLayer(zoningLayerName);

        if(layerIndex == -1) { Debug.LogError($"Zoning Layer with the layer name {zoningLayerName} doesn't exist!"); }

        playerCamera.cullingMask ^= 1 << layerIndex;
    }

    private void HandleStatsUpdate()
    {
        if (statsPanelActive)
        {
            PopulationUIText.text = $"Population: {GameManager.instance.currentPopulation}";
            UnemployedUIText.text = $"Unemployed: {GameManager.instance.currentUnemployed}";
            VacanciesUIText.text = $"Vacancies: {GameManager.instance.currentVacanies}";
            MaintenanceUIText.text = $"Maintenance: Road:{financeManager.roadMaintainanceCost * GameManager.instance.gridManager.RoadPositions.Count}";
            DisastersSurvivedUIText.text = $"Disasters Survived: {GameManager.instance.disastersSurvived.ToString()}";

            if (ChunkManager.instance != null)
            {
                ChunkManager chunkManager = ChunkManager.instance;
                int powerDelta = chunkManager.GlobalPowerCapacity - chunkManager.GlobalPowerDemand;
                int waterDelta = chunkManager.GlobalWaterCapacity - chunkManager.GlobalWaterDemand;

                PowerDeltaUIText.text = $"Power Delta: {powerDelta}MW";
                WaterDeltaUIText.text = $"Water Delta: {waterDelta}L";

                PowerDeltaUIText.color = (powerDelta >= 0) ? Color.white : Color.red;
                WaterDeltaUIText.color = (waterDelta >= 0) ? Color.white : Color.red;
            }
        }
    }

    private void UpdateDayProgressBar(float progressRatio)
    {
        if (dayProgressBar == null) { Debug.LogWarning("Day Progress Bar Not Assigned to UI mananger."); return; }

        dayProgressBar.value = progressRatio;
    }

    private void NotifyUser(string Text, bool emergency)
    {
        userNotificationUIText.enabled = true;
        userNotificationUIText.text = Text;

        Color targetColor = emergency ? Color.white : Color.red;
        targetColor.a = 1f;

        userNotificationUIText.color = targetColor;

        if (notifRoutine != null) { StopCoroutine(notifRoutine); }
        notifRoutine = StartCoroutine(HideNotificationAfterSeconds());
    }

    private IEnumerator HideNotificationAfterSeconds(int seconds = 3)
    {
        yield return new WaitForSeconds(seconds);

        //Fade logic
        float fadeDurationSeconds = 0.5f;
        float currentTimeSeconds = 0f;
        Color originalColour = userNotificationUIText.color;

        while (currentTimeSeconds < fadeDurationSeconds)
        {
            currentTimeSeconds += Time.deltaTime;

            Color newColour = originalColour;
            newColour.a = Mathf.Lerp(originalColour.a, 0f, (currentTimeSeconds / fadeDurationSeconds));
            userNotificationUIText.color = newColour;

            yield return null;
        }

        userNotificationUIText.enabled = false;
    }

    private void UpdateCursorPosition(Vector2Int pos)
    {
        cursorPosUIText.text = $"({pos.x}, {pos.y})";
    }

    private void ToggleStatsPanel()
    {
        if (statsPanelActive)
        {
            CityStatsPanel.SetActive(false);
            statsPanelActive = false;
        } 
        else
        {
            CityStatsPanel.SetActive(true);
            statsPanelActive = true;
        }
    }

    private string ReturnTextFromMoney(long amount)
    {
        if (amount >= 1_000_000_000_000)
        {
            float trillions = (float)amount / 1_000_000_000_000;
            return $"£{trillions:0.00} trillion";
        }
        else if (amount >= 1_000_000_000)
        {
            float billions = (float)amount / 1_000_000_000;
            return $"£{billions:0.00} billion";
        }
        else if (amount >= 1_000_000)
        {
            float millions = (float)amount / 1_000_000;
            return $"£{millions:0.00} million";
        }
        else
        {
            return $"£{amount:N0}";
        }
    }
}
