using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CouncilFX : MonoBehaviour
{
    [Header("Dependencies")]
    GridManager gridManager;
    FinanceManager financeManager;
    EventManager eventManager;

    [Header("Council FX Vars")]
    [SerializeField] private GameObject CouncilFXPanel;
    private bool councilFxPanelActive = false;

    // --different things--

    //Emergency Borrow
    private bool isEmergencyBorrowEnabled = false;
    [SerializeField] private Button emergencyBorrowButton;
    [SerializeField] private TMPro.TextMeshProUGUI emergencyBorrowButtonUGUI;

    //Lockdown
    private bool isLockdownEnabled = false;
    [SerializeField] private Button lockdownButton;
    [SerializeField] private TMPro.TextMeshProUGUI lockdownTextUGUI;

    //Maximise Housing
    private bool isMaxHousingEnabled = false;
    [SerializeField] private Button maxHousingButton;
    [SerializeField] private TMPro.TextMeshProUGUI maxHousingTextUGUI;

    //Overclock Grid
    private bool isOverclockActive = false;

    private bool isOverclockGridEnabled = false;
    [SerializeField] private Button overclockGridButton;
    [SerializeField] private TMPro.TextMeshProUGUI overclockGridTextUGUI;

    //Growth boost
    private bool isGrowthBoostEnabled = false;
    [SerializeField] private Button growthBoostButton;
    [SerializeField] private TMPro.TextMeshProUGUI growthBoostTextUGUI;

    //Maximise Business Revenue
    private float businessRevenueMultiplier = 1f;
    private int businessRevenueIncreaseCostThousands = 500;
    private bool maxLimitReached = false;

    private bool isMaxBusinessRevenueEnabled = false;
    [SerializeField] private Button maxBusinessRevenueButton;
    [SerializeField] private TMPro.TextMeshProUGUI maxBusinessRevenueTextUGUI;

    //Medical Miracle
    private bool isMedicalMiracleEnabled = false;
    [SerializeField] private Button medicalMiracleButton;
    [SerializeField] private TMPro.TextMeshProUGUI medicalMiracleTextUGUI;

    //Corporate Haven
    private bool isCorporateHavenEnabled = false;
    [SerializeField] private Button corporateHavenButton;
    [SerializeField] private TMPro.TextMeshProUGUI corporateHavenTextUGUI;

    //Panic Button
    private bool isPanicEnabled = false;

    private bool hasPanicked = false;
    [SerializeField] private Button panicButton;
    [SerializeField] private TMPro.TextMeshProUGUI panicTextUGUI;

    private void Start()
    {
        gridManager = GridManager.instance;
        financeManager = FinanceManager.instance;
        eventManager = EventManager.instance;

        if (GameManager.instance != null) GameManager.instance.OnNewXPLevel += HandleXPLevelChanged;

        CouncilFXPanel.SetActive(false);
        councilFxPanelActive = CouncilFXPanel.activeSelf;
    }

    private void OnDestroy()
    {
        if(GameManager.instance != null) GameManager.instance.OnNewXPLevel -= HandleXPLevelChanged;
    }

    public void TogglePanel(bool active)
    {
        gameObject.SetActive(active);
        if (active) UpdateButtonState();
    }

    public void PanelOnDayEnd()
    {
        if (gameObject.activeSelf) UpdateButtonState();
    }

    private void HandleXPLevelChanged(int level)
    {
        isEmergencyBorrowEnabled = level >= 20;
        isLockdownEnabled = level >= 25;
        isMaxHousingEnabled = level >= 30;
        isOverclockGridEnabled = level >= 35;
        isGrowthBoostEnabled = level >= 40;
        isMaxBusinessRevenueEnabled = level >= 45;
        isMedicalMiracleEnabled = level >= 65;
        isCorporateHavenEnabled = level >= 80;
        isPanicEnabled = level >= 100;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        //Emergency Borrow
        if (isEmergencyBorrowEnabled)
        {
            if (!financeManager.hasActiveDebt) //If no debt allow to take debt
            {
                emergencyBorrowButton.interactable = true;
                emergencyBorrowButtonUGUI.text = "Borrow 100k for 100 days @1%";
            }
            else
            {
                emergencyBorrowButton.interactable = financeManager.currentMoney >= financeManager.currentDebt;
                emergencyBorrowButtonUGUI.text = $"Pay back £{financeManager.currentDebt:N0} within {financeManager.debtPaymentDaysLeft} days";
            }
        }
        else
        {
            emergencyBorrowButton.interactable = false; emergencyBorrowButtonUGUI.text = "Emergency Borrow 100k at Interest [Level 20]";
        }

        //Lockdown
        if (isLockdownEnabled)
        {
            lockdownButton.interactable = true;

            if (eventManager.isLockdownActive)
            {
                lockdownTextUGUI.text = "Disable lockdown";
            }
            else
            {
                lockdownTextUGUI.text = "Initiate lockdown for £5k";
            }
        } 
        else
        { 
            lockdownButton.interactable = false; lockdownTextUGUI.text = "Initiate Lockdown [Level 25]"; 
        }

        //Max Housing
        if (isMaxHousingEnabled) { maxHousingButton.interactable = true; maxHousingTextUGUI.text = "Maximise Population for £150k"; }
        else { maxHousingButton.interactable = false; maxHousingTextUGUI.text = "Maximise Population [Level 30]"; }

        //Overclock Grid
        if (isOverclockGridEnabled) 
        { 
            overclockGridButton.interactable = !isOverclockActive; 
            overclockGridTextUGUI.text = isOverclockActive ? "Overclock Active!" : "10 second Grid Overclock for £300k"; 
        }
        else { overclockGridButton.interactable = false; overclockGridTextUGUI.text = "Overclock Grid [Level 35]"; }

        //Growth boost
        if (isGrowthBoostEnabled) 
        {
            bool isBoosted = CityGenerator.instance.isBoosted;
            growthBoostButton.interactable = !isBoosted; 
            growthBoostTextUGUI.text = isBoosted ? "Currently Boosted!" : "Temporarily Boost Growth for £50k"; 
        }
        else { growthBoostButton.interactable = false; growthBoostTextUGUI.text = "Growth Boost [Level 40]"; }

        //Max Business Revenue
        if (isMaxBusinessRevenueEnabled) 
        {
            maxBusinessRevenueButton.interactable = !maxLimitReached;
            maxBusinessRevenueTextUGUI.text = maxLimitReached ? "Maximal Employee Productivity reached." : $"Increase Employee Productivity for £{businessRevenueIncreaseCostThousands}k";
        }
        else { maxBusinessRevenueButton.interactable= false; maxBusinessRevenueTextUGUI.text = "Increase Business Revenue [Level 45]"; }

        //Medical Miracle
        if (isMedicalMiracleEnabled) { medicalMiracleButton.interactable = true; medicalMiracleTextUGUI.text = "Medical Miracle for £350k"; }
        else { medicalMiracleButton.interactable = false; medicalMiracleTextUGUI.text = "Medical Miracle [Level 65]"; }

        //Corporate Haven
        if (isCorporateHavenEnabled) { corporateHavenButton.interactable = true; corporateHavenTextUGUI.text = "Enable 14 day Corporate Haven for £500k"; }
        else { corporateHavenButton.interactable = false; corporateHavenTextUGUI.text = "Corporate Haven [Level 80]"; }

        //Panic
        if (hasPanicked) { panicButton.interactable = false; panicTextUGUI.text = "Used. Good Luck."; }
        else
        {
            panicButton.interactable = isPanicEnabled;
            panicTextUGUI.text = isPanicEnabled ? "... £2.5 million" : "??? [Level 100]";
        }
    }

    public void OnEmergencyBorrowClicked()
    {
        financeManager.ProcessEmergencyLoan(100_000, 100); //100k for 100 days
        UpdateButtonState();
    }

    public void OnLockdownButtonClicked()
    {
        if (!isLockdownEnabled) return;

        if (eventManager.isLockdownActive)
        {
            eventManager.isLockdownActive = false;
            lockdownTextUGUI.text = "Initiate Lockdown for £5k";
            return;
        }

        if (financeManager.Purchase(5000))
        {
            eventManager.isLockdownActive = true;
            lockdownTextUGUI.text = "Disable lockdown";
        }

        UpdateButtonState();
    }

    public void OnMaximiseHousingClicked(bool free = false)
    {
        if (!free && !financeManager.Purchase(150_000)) return;

        var mapGrid = gridManager.GetMapGrid();
        List<Vector2Int> buildingList = gridManager.BuildingPositions;

        foreach (Vector2Int pos in buildingList)
        {
            if (!mapGrid.TryGetValue(pos, out var gridTile)) continue;

            if (gridTile.buildingScript is House houseScript)
            {
                int incomingResidents = houseScript.maxResidents - houseScript.residents;

                if (incomingResidents > 0)
                {
                    houseScript.residents = houseScript.maxResidents;
                    GameManager.instance.currentPopulation += incomingResidents;
                }
            }
        }
    }

    public void OnOverclockGridClicked()
    {
        //For 60 seconds
        //Make every vehicle 5x faster TO DO
        //Free grid expansion done //Extremely OP -> down to 10 seconds
        //Infinite global energy + water done

        if (!financeManager.Purchase(75000)) return;
        GameManager.instance.StartCoroutine(OverClockCoroutine(10));
    }

    public void OnGrowthBoostButton()
    {
        if (CityGenerator.instance.isBoosted)
        {
            GameManager.instance.UserNotification?.Invoke("Already boosted!", false);
            return;
        }

        if (financeManager.Purchase(50000))
        {
            CityGenerator.instance.StartCoroutine(
                CityGenerator.instance.TemporarilyBoostSpawningRate(6, 10f) //factor of 6 for 10 seconds
            ); 
        }

        UpdateButtonState();
    }

    public void OnIncreaseBuildingRevenueClicked(bool free = false)
    {
        if (maxLimitReached) { UpdateButtonState(); return; }

        float temp = 1f;

        switch (businessRevenueMultiplier)
        {
            case 1:
                temp = 1.5f; 
                break;
            case 1.5f:
                temp = 2f; 
                break;
            case 2:
                temp = 3f; 
                break;
            case 3:
                temp = 4f; maxLimitReached = true; 
                break;
            default:
                maxLimitReached = true;
                return;
        }

        if (!free && !financeManager.Purchase(businessRevenueIncreaseCostThousands * 1000)) return;

        businessRevenueIncreaseCostThousands = (int)(businessRevenueIncreaseCostThousands * temp);
        businessRevenueMultiplier = temp;

        var mapGrid = gridManager.GetMapGrid();
        List<Vector2Int> buildingList = gridManager.BuildingPositions;

        foreach (Vector2Int pos in buildingList)
        {
            if (!mapGrid.TryGetValue(pos, out var gridTile)) continue;

            if (gridTile.buildingScript is Employer employer)
            {
                GameManager.instance.UpdateTaxRevenueMultiplier(temp);
                //employer.TryToMassHire();
            }
        }

        UpdateButtonState();
    }

    public void OnMedicalMiracleClicked(bool free = false)
    {
        if (!free && !financeManager.Purchase(350_000)) return;

        //Cure every current infection & grant immunity for 60 seconds

        StartCoroutine(GrantVirusImmunity(60));

        var mapGrid = gridManager.GetMapGrid();

        foreach (Vector2Int pos in gridManager.BuildingPositions)
        {
            if (!mapGrid.TryGetValue(pos, out var gridTile)) continue;

            if (gridTile.buildingScript is House houseScript)
            {
                houseScript.Heal();
            }
        }
    }

    public void OnCorporateHavenClicked(bool free = false)
    {
        //if (!free && !financeManager.Purchase(500_000)) return;

        //14 day
        //Double tax revenue
        //All negative disasters are resolved
        //Zoning costs 25%
        //Maintenance costs cut
    }

    public void OnPanicClicked()
    {
        if (hasPanicked) return;
        if (!financeManager.Purchase(2_500_000)) return;

        hasPanicked = true;

        OnMedicalMiracleClicked(true);
        OnCorporateHavenClicked(true);
        OnMaximiseHousingClicked(true);
        OnIncreaseBuildingRevenueClicked(true);

        //Solve everything...
        //Maintenance cost reset to day 0
        //OnMedicalCure called
        //CorporateHaven called
        //Houses & Buildings maximised (called both functions)

        //Can only do this once

        UpdateButtonState();
    }

    //Helper functions
    private IEnumerator OverClockCoroutine(int seconds)
    {
        isOverclockActive = true;
        UpdateButtonState();

        GameManager.instance.freeGridExpansion = true;
        ChunkManager.instance.IncreaseWaterAndPowerSupplyTemporarily(1_000_000, seconds);

        yield return new WaitForSeconds(seconds);

        GameManager.instance.freeGridExpansion = false;
        isOverclockActive = false;
        
        if (gameObject.activeSelf) UpdateButtonState();
    }

    private IEnumerator GrantVirusImmunity(int seconds)
    {
        GameManager.instance.isImmuneToViruses = true;

        yield return new WaitForSeconds(seconds);

        GameManager.instance.isImmuneToViruses = false;
    }
}
