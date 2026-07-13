using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //Managers
    GameManager gameManager;
    GridPlayerManager gridPlayerManager;
    FinanceManager financeManager;
    EventManager eventManager;

    //Input actions
    public InputActionAsset inputActions;
    InputAction toggleZoningUI;
    InputAction toggleStatsPanelUI;
    InputAction toggleCouncilFXUI;
    InputAction accept;
    InputAction deny;

    [Header("Hotbar & Mode UI")]
    [SerializeField] private TMPro.TextMeshProUGUI playerModeShowText;
    [SerializeField] private Image roadImg;
    [SerializeField] private Image zoningImg;
    [SerializeField] private Image buildingImg;
    [SerializeField] private Color activeColour = Color.white;
    [SerializeField] private Color inactiveColour = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Basic UI")]
    //public Boolean Enabled = true;
    [SerializeField] private TMPro.TextMeshProUGUI currentMoneyUIText;
    [SerializeField] private TMPro.TextMeshProUGUI addedMoneyUIText;

    [SerializeField] private Slider dayProgressBar;
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

    [Header("SpecialFx")]
    [SerializeField] private GameObject SpecialFxContainer;
    [SerializeField] private TMPro.TextMeshProUGUI specialFxTitle;
    [SerializeField] private TMPro.TextMeshProUGUI option1;
    [SerializeField] private TMPro.TextMeshProUGUI buildingInfo1;
    [SerializeField] private TMPro.TextMeshProUGUI buildingInfo2;
    [SerializeField] private TMPro.TextMeshProUGUI buildingInfo3;
    [SerializeField] private TMPro.TextMeshProUGUI buildingInfo4;
    InputAction optionOneKey;
    private Building targetBuilding;

    [Header("Experience")]
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TMPro.TextMeshProUGUI experienceLevelDisplay;

    [Header("Council FX")]
    [SerializeField] private CouncilFX councilFXManager;
    private bool councilFXPanelActive = false;

    private void Awake()
    {
        gameManager = GameManager.instance;
        financeManager = FinanceManager.instance;
        eventManager = EventManager.instance;
        gridPlayerManager = GridPlayerManager.instance;

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
        toggleCouncilFXUI = UIMap.FindAction("ToggleCouncilFX");
        accept = UIMap.FindAction("Accept");
        deny = UIMap.FindAction("Deny");
        optionOneKey = UIMap.FindAction("OptionOneKey");

        toggleZoningUI.Enable(); toggleStatsPanelUI.Enable(); toggleCouncilFXUI.Enable();

        accept.Enable(); deny.Enable(); optionOneKey.Enable();

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
        if (gameManager != null)
        {
            gameManager.OnDayEndUI += UpdateDaysPassed;
            gameManager.OnDayEndUI += DayEndCouncilFXPanel;
            gameManager.UserNotification += NotifyUser;
            gameManager.OnDayProgress += UpdateDayProgressBar;
            gameManager.OnXPChanged += UpdateXP;
            gameManager.OnNewXPLevel += UpdateXPLevel;

        } else { Debug.LogError("No game manager!"); }

        if (gridPlayerManager != null)
        {
            gridPlayerManager.newCursorPosition += UpdateCursorPosition;
            gridPlayerManager.buildingSpecialFx += HandleBuildingSpecialFx;
            gridPlayerManager.OnToolChanged += HandleToolChanged;
        }
        else { Debug.LogError("No grid player manager!"); }

        if (financeManager != null) financeManager.OnMoneyChanged += updateCurrentMoneyUI; else Debug.LogError("No finance manager!");

        if (eventManager != null) eventManager.onQueueChanged += CheckForPendingQuestions; else Debug.LogError("No event manager!");


    }

    private void OnDisable()
    {
        //Unsub
        if (gameManager != null)
        {
            gameManager.OnDayEndUI -= UpdateDaysPassed;
            gameManager.OnDayEndUI -= DayEndCouncilFXPanel;
            gameManager.UserNotification -= NotifyUser;
            gameManager.OnDayProgress -= UpdateDayProgressBar;
            gameManager.OnXPChanged -= UpdateXP;
            gameManager.OnNewXPLevel -= UpdateXPLevel;

        }
        else { Debug.LogError("No game manager!"); }

        if (gridPlayerManager != null)
        {
            gridPlayerManager.newCursorPosition -= UpdateCursorPosition;
            gridPlayerManager.buildingSpecialFx -= HandleBuildingSpecialFx;
            gridPlayerManager.OnToolChanged -= HandleToolChanged;
        }
        else { Debug.LogError("No grid player manager!"); }

        if (financeManager != null) financeManager.OnMoneyChanged -= updateCurrentMoneyUI;

        if (eventManager != null) eventManager.onQueueChanged -= CheckForPendingQuestions; else Debug.LogError("No event manager!");

        //UI

        toggleZoningUI.Disable(); toggleStatsPanelUI.Disable(); toggleCouncilFXUI.Disable();

        accept.Disable(); deny.Disable(); optionOneKey.Disable();

    }

    private void HandleUserInput()
    {
        if (accept == null) Debug.Log("sumting wong.");

        if (toggleZoningUI.WasPressedThisFrame()) { ToggleZoningLayer(); CloseSpecialFx(); }
        if (toggleStatsPanelUI.WasPressedThisFrame()) { ToggleStatsPanel(); CloseSpecialFx(); }
        if (toggleCouncilFXUI.WasPressedThisFrame()) { ToggleCouncilFXPanel(); CloseSpecialFx(); }

        if (QuestionContainer.activeSelf)
        {
            if (accept.WasPressedThisFrame()) {
                Debug.Log("Accept Input Pressed");  
                RespondToQuestion(true);
                CloseSpecialFx();
            }
            if (deny.WasPressedThisFrame()) {
                Debug.Log("Deny Input Pressed");
                RespondToQuestion(false);
                CloseSpecialFx();
            }
        }

        if (SpecialFxContainer.activeSelf)
        {
            if (optionOneKey.WasPressedThisFrame())
            {
                DoBuildingSpecialFx();
                CloseSpecialFx();
            }
        }
    }

    //Toolbar
    private void HandleToolChanged(IBuildTool activeTool)
    {
        if (activeTool == null) return;

        string mainCat = activeTool.GetMainCategoryName();
        string subType = activeTool.GetSubTypeName();

        if (string.IsNullOrEmpty(subType))
        {
            playerModeShowText.text = mainCat;
        } else
        {
            playerModeShowText.text = $"{mainCat} : {subType}";
        }

        roadImg.color = (activeTool is RoadTool) ? activeColour : inactiveColour;
        zoningImg.color = (activeTool is ZoningTool) ? activeColour : inactiveColour;
        buildingImg.color = (activeTool is BuildingTool) ? activeColour : inactiveColour;
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

            //Info
            var chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
            buildingInfo1.text = $"Chunk Power: {chunk.powerGenerated + chunk.powerImported}MW In with {chunk.powerConsumed}MW Out. (Enough: {chunk.HasEnoughPower})";
            buildingInfo2.text = $"Chunk Water: {chunk.waterGenerated + chunk.waterImported}MW In with {chunk.waterConsumed}MW Out. (Enough: {chunk.HasEnoughWater})";

            if (tile.buildingScript is House houseScript)
            {
                buildingInfo3.text = $"Happiness: {Mathf.Round(houseScript.happiness)}, Residents: {houseScript.residents} of {houseScript.maxResidents} maximum.";
                buildingInfo4.text = $"Days with Low Happiness: {houseScript.daysWithLowHappiness}";
            } else if (tile.buildingScript is Employer employer)
            {
                buildingInfo3.text = $"Tax Revenue: {Mathf.Round(employer.GetTaxRevenue())}, Employers: {employer.employees} of {employer.GetMaxEmployees()} maximum.";
                buildingInfo4.text = $"Bad Emergy/Water Days: {employer.badDays}, Low Employee Days: {employer.lowEmployeeDays}";
            }

            option1.text = $"[F1] Earthquake Retrofit : £{targetBuilding.RetroFitCost}"; //[OptionOneKey] !!!! Update here if ever changed
        } else
        {
            SpecialFxContainer.SetActive(true);

            specialFxTitle.text = "Chunk Info";
            option1.text = "No actions to carry out.";
            buildingInfo4.text = "";

            //Info
            var chunk = ChunkManager.instance.GetChunkFromGridTile(gridPos);
            buildingInfo1.text = $"Chunk Power: {chunk.powerGenerated + chunk.powerImported}MW Supplied with {chunk.powerConsumed}MW Used. (Enough: {chunk.HasEnoughPower})";
            buildingInfo2.text = $"Chunk Water: {chunk.waterGenerated + chunk.waterImported}MW Supplied with {chunk.waterConsumed}MW Used. (Enough: {chunk.HasEnoughWater})";
            buildingInfo3.text = $"Chunk Happiness: {chunk.averageHappiness}";
        }
    }

    private void DoBuildingSpecialFx()
    {
        if (targetBuilding == null) return;

        targetBuilding.RetroFit();
        SpecialFxContainer.SetActive(false);
    }

    public void CloseSpecialFx()
    {
        SpecialFxContainer.SetActive(false);
        targetBuilding = null;
    }

    //XP

    private void UpdateXP(int experiencePoints, int experiencePointsLimit)
    {
        if (experiencePointsLimit > 0)
        {
            experienceSlider.value = (float)experiencePoints / experiencePointsLimit;
        } else
        {
            experienceSlider.value = 0;
        }
    }

    private void UpdateXPLevel(int experienceLevel)
    {
        experienceLevelDisplay.text = $"{experienceLevel}";
    }

    //Council
    private void ToggleCouncilFXPanel()
    {
        councilFXPanelActive = !councilFXPanelActive;
        councilFXManager.TogglePanel(councilFXPanelActive);

        if (gridPlayerManager != null)
        {
            gridPlayerManager.gridEditEnabled = !councilFXPanelActive;
        }
        
        CloseSpecialFx();
    }

    private void DayEndCouncilFXPanel() => councilFXManager.PanelOnDayEnd();

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
