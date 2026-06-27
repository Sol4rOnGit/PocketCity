using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GridPlayerManager gridPlayerManager;

    [Header("Basic UI")]
    //public Boolean Enabled = true;
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;
    [SerializeField] private TMPro.TextMeshProUGUI addedMoneyUIText;

    [SerializeField] private UnityEngine.UI.Slider dayProgressBar;
    [SerializeField] private TMPro.TextMeshProUGUI daysPassedUIText;

    [SerializeField] private TMPro.TextMeshProUGUI userNotificationUIText;
    private Coroutine notifRoutine;

    [SerializeField] private TMPro.TextMeshProUGUI cursorPosUIText;

    [Header("Stats UI")]
    [SerializeField] private GameObject CityStatsPanel;
    private bool statsPanelActive;
    [SerializeField] private TMPro.TextMeshProUGUI PopulationUIText;

    [SerializeField] private TMPro.TextMeshProUGUI UnemployedUIText;
    [SerializeField] private TMPro.TextMeshProUGUI VacanciesUIText;
    [SerializeField] private TMPro.TextMeshProUGUI DisastersSurvivedUIText;

    [SerializeField] private TMPro.TextMeshProUGUI lastDayIncome;
    [SerializeField] private TMPro.TextMeshProUGUI lastDayMaintenance;
    [SerializeField] private TMPro.TextMeshProUGUI lastDayPlayer;

    [SerializeField] private TMPro.TextMeshProUGUI PowerDeltaUIText;
    [SerializeField] private TMPro.TextMeshProUGUI WaterDeltaUIText;

    [Header("QuestionUI")]
    [SerializeField] private GameObject QuestionContainer;
    [SerializeField] private TMPro.TextMeshProUGUI QuestionField;
    private EventManager.PoliticalQuestion activeQuestion;
    private Coroutine questionTimer;

    [Header("ZoningVars")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string zoningLayerName = "ZoningVisual";
    public InputActionAsset inputActions;
    InputAction toggleZoningUI;
    InputAction toggleStatsPanelUI;
    InputAction accept;
    InputAction deny;

    [Header("SpecialFx")]
    [SerializeField] private GameObject SpecialFxContainer;
    [SerializeField] private TMPro.TextMeshProUGUI specialFxTitle;
    [SerializeField] private TMPro.TextMeshProUGUI option1;
    InputAction one;
    private Building targetBuilding;

    FinanceManager financeManager;
    EventManager eventManager;

    private void Awake()
    {
        financeManager = FinanceManager.instance;
        eventManager = EventManager.instance;

        if (financeManager == null) { Debug.LogError("Finance Manager not found!"); }
        if (eventManager == null) { Debug.LogError("Event Manager not found!"); }
    }

    private void Start()
    {
        //Mapping
        InputActionMap UIMap = inputActions.FindActionMap("UI");
        UIMap.Enable();
        toggleZoningUI = UIMap.FindAction("ToggleZoningUI");
        toggleStatsPanelUI = UIMap.FindAction("ToggleStatsPanel");
        accept = UIMap.FindAction("Accept");
        deny = UIMap.FindAction("Deny");
        one = UIMap.FindAction("One");

        statsPanelActive = CityStatsPanel.activeSelf;

        updateCurrentMoneyUI(financeManager.currentMoney);
    }

    private void Update()
    {
        HandleUserInput();
        HandleStatsUpdate();
    }

    //Sub/Unsub
    private void OnEnable()
    {
        //Subscriptions
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += UpdateDaysPassed;
            GameManager.instance.UserNotification += NotifyUser;
            GameManager.instance.OnDayProgress += UpdateDayProgressBar;

        } else { Debug.LogError("No game manager!"); }

        if (gridPlayerManager != null)
        {
            gridPlayerManager.newCursorPosition += UpdateCursorPosition;
            gridPlayerManager.buildingSpecialFx += HandleBuildingSpecialFx;
        }
        else { Debug.LogError("No grid player manager!"); }

        if (financeManager != null) financeManager.OnMoneyChanged += updateCurrentMoneyUI; else Debug.LogError("No finance manager!");

        if (eventManager != null) eventManager.onQueueChanged += CheckForPendingQuestions; else Debug.LogError("No event manager!");


    }

    private void OnDisable()
    {
        //Unsub
        if (GameManager.instance != null)
        {
            GameManager.instance.OnDayEnd += UpdateDaysPassed;
            GameManager.instance.UserNotification -= NotifyUser;
            GameManager.instance.OnDayProgress -= UpdateDayProgressBar;

        }
        else { Debug.LogError("No game manager!"); }

        if (gridPlayerManager != null)
        {
            gridPlayerManager.newCursorPosition -= UpdateCursorPosition;
            gridPlayerManager.buildingSpecialFx -= HandleBuildingSpecialFx;
        }
        else { Debug.LogError("No grid player manager!"); }

        if (financeManager != null) financeManager.OnMoneyChanged -= updateCurrentMoneyUI;

        if (eventManager != null) eventManager.onQueueChanged -= CheckForPendingQuestions; else Debug.LogError("No event manager!");
    }

    private void HandleUserInput()
    {
        if (accept == null) Debug.Log("sumting wong.");

        if (toggleZoningUI.WasPressedThisFrame()) { ToggleZoningLayer(); }
        if (toggleStatsPanelUI.WasPressedThisFrame()) { ToggleStatsPanel(); }

        if (QuestionContainer.activeSelf)
        {
            if (accept.WasPressedThisFrame()) {
                Debug.Log("Accept Input Pressed");  
                RespondToQuestion(true); 
            }
            if (deny.WasPressedThisFrame()) {
                Debug.Log("Deny Input Pressed");
                RespondToQuestion(false); 
            }
        }

        if (SpecialFxContainer.activeSelf)
        {
            if (one.WasPressedThisFrame())
            {
                DoBuildingSpecialFx();
            }
        }
    }

    //Stats
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
    private void HandleStatsUpdate()
    {
        if (statsPanelActive)
        {
            PopulationUIText.text = $"Population: {GameManager.instance.currentPopulation}";
            UnemployedUIText.text = $"Unemployed: {GameManager.instance.currentUnemployed}";
            VacanciesUIText.text = $"Vacancies: {GameManager.instance.currentVacanies}";
            DisastersSurvivedUIText.text = $"Disasters Survived: {GameManager.instance.disastersSurvived}";

            FinancialReport report = financeManager.lastFinancialReport;

            if (lastDayIncome != null) lastDayIncome.text = $"Revenue: {ReturnTextFromMoney(report.totalIncome)}";
            if (lastDayIncome != null) lastDayMaintenance.text = $"Maintenance Costs: {ReturnTextFromMoney(report.maintenanceCosts)}";
            if (lastDayIncome != null) lastDayPlayer.text = $"Player Costs: {ReturnTextFromMoney(report.playerCosts)}";

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

    //Day
    private void UpdateDayProgressBar(float progressRatio)
    {
        if (dayProgressBar == null) { Debug.LogWarning("Day Progress Bar Not Assigned to UI mananger."); return; }

        dayProgressBar.value = progressRatio;
    }

    private void UpdateDaysPassed()
    {
        if (daysPassedUIText == null) { Debug.LogWarning("Day Passed UI text Not Assigned to UI mananger."); return; }

        if (GameManager.instance == null) { Debug.LogError("No Game Manager! UI Manager."); }
        int daysPassed = GameManager.instance.daysPassed; 

        daysPassedUIText.text = $"Day {daysPassed.ToString()}";
    }

    //Simple functions
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

        if (layerIndex == -1) { Debug.LogError($"Zoning Layer with the layer name {zoningLayerName} doesn't exist!"); }

        playerCamera.cullingMask ^= 1 << layerIndex;
    }

    private void NotifyUser(string Text, bool emergency)
    {
        userNotificationUIText.enabled = true;
        userNotificationUIText.text = Text;

        Color targetColor = emergency ? Color.red : Color.white;
        targetColor.a = 1f;

        userNotificationUIText.color = targetColor;

        if (notifRoutine != null) { StopCoroutine(notifRoutine); }
        notifRoutine = StartCoroutine(HideNotificationAfterSeconds());
    }
    
    private void UpdateCursorPosition(Vector2Int pos)
    {
        cursorPosUIText.text = $"({pos.x}, {pos.y})";
    }

    //Political Questions

    private void UpdateQuestion(string question)
    {
        QuestionContainer.SetActive(true);
        QuestionField.text = question;

        if (questionTimer != null) StopCoroutine(questionTimer);
        questionTimer = StartCoroutine(HideQuestionAfterSeconds());
    }

    private void CheckForPendingQuestions()
    {
        //If there is a question showing or there is one waiting, return
        if (activeQuestion != null || eventManager.PendingQuestions.Count == 0) return; 

        activeQuestion = eventManager.PendingQuestions[0];
        UpdateQuestion(activeQuestion.Question);
    }

    private void RespondToQuestion(bool choice)
    {
        if (activeQuestion == null) {
            QuestionContainer.SetActive(false);
            return; 
        }

        if (questionTimer != null)
        {
            StopCoroutine(questionTimer);
            questionTimer = null;
        }

        if (!activeQuestion.TaskCompletionSource.Task.IsCompleted) activeQuestion.TaskCompletionSource.SetResult(choice);        

        QuestionContainer.SetActive(false);

        activeQuestion = null;

        CheckForPendingQuestions();
    }

    //Special fx

    private void HandleBuildingSpecialFx(Vector2Int gridPos)
    {
        var mapGrid = GridManager.instance.GetMapGrid();

        if (mapGrid.TryGetValue(gridPos, out var tile) && tile.buildingScript != null)
        {
            SpecialFxContainer.SetActive(true);
            targetBuilding = tile.buildingScript;

            //Labelling
            if (tile.buildingScript is Service serviceScript) { specialFxTitle.text = $"{targetBuilding.buildingName} functions"; }
            else { specialFxTitle.text = $"{targetBuilding.type} functions"; }
              
            option1.text = $"[1] Earthquake Retrofit : £{targetBuilding.RetroFitCost}";
        }
    }

    private void DoBuildingSpecialFx()
    {
        if (targetBuilding == null) return;

        targetBuilding.RetroFit();
        SpecialFxContainer.SetActive(false);
    }

    private void CloseSpecialFx()
    {
        SpecialFxContainer.SetActive(false);
        targetBuilding = null;
    }

    //Helper functions
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
    private IEnumerator HideQuestionAfterSeconds(int seconds = 3)
    {
        yield return new WaitForSeconds(seconds);

        RespondToQuestion(false);
    }
}
